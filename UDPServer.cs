using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Globalization;
using TruckRemoteServer.Data;

namespace TruckRemoteServer
{
    public class UDPServer : IFfbListener
    {
        public interface IStatusListener
        {
            void OnStatusUpdate(bool isEnabled, bool controllerConnected, bool controllerPaused);
        }

        //Controller is considered disconnected after this period of silence
        private const int CONTROLLER_TIMEOUT = 1200;
        //Socket receive timeout (to check controller's silence periodically)
        private const int RECEIVE_TIMEOUT = 300;
        //Makes Windows not to break UDP socket with WSAECONNRESET after ICMP "Port unreachable"
        private const int SIO_UDP_CONNRESET = -1744830452;

        private const string HELLO_MESSAGE = "TruckRemoteHello";
        private const string PAUSED_MESSAGE = "paused";
        private const string GOODBYE_MESSAGE = "goodbye";
        //Controller message: steering, brake, gas, left signal, right signal, emergency,
        //parking brake, lights, horn, cruise (all are required),
        //then optional: gas level, brake level (0..1), action counters (see PCController)
        private const int CONTROLLER_MESSAGE_PARTS = 10;
        private const int PEDAL_LEVELS_INDEX = 10;
        private const int ACTIONS_INDEX = 12;

        public int port;
        private volatile Socket serverSocket;
        private volatile IPEndPoint controllerEndPoint;
        private readonly IStatusListener statusListener;
        private readonly object stateLock = new object();

        public volatile bool enabled;
        public volatile bool controllerPaused;
        private long lastControllerMsgTime;

        //Incremented on every controller connect/disconnect, stops outdated sender threads
        private int controllerSession;
        private int effectDuration;

        private readonly PCController pcController;

        public UDPServer(IStatusListener listener, int port)
        {
            this.port = port;
            statusListener = listener;
            pcController = new PCController(this);
        }

        //Returns false if server can't be started (e.g. port is busy)
        public bool Start()
        {
            Socket socket;
            lock (stateLock)
            {
                if (enabled) return true;

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    try
                    {
                        socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
                    }
                    catch (Exception) { }
                    socket.Bind(new IPEndPoint(IPAddress.Any, port));
                    socket.ReceiveTimeout = RECEIVE_TIMEOUT;
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Can't start server on port " + port + ": " + e.Message);
                    socket.Close();
                    PostStatusUpdate();
                    return false;
                }

                serverSocket = socket;
                enabled = true;
            }

            Thread receiverThread = new Thread(() => ReceiveMessages(socket))
            {
                IsBackground = true,
                Name = "UDP receiver"
            };
            receiverThread.Start();
            PostStatusUpdate();
            return true;
        }

        public void OnFfbEffect(uint effectDuration)
        {
            Interlocked.Exchange(ref this.effectDuration, (int)Math.Min(effectDuration, int.MaxValue));
        }

        /* ................................. <Receiver thread> .............................*/
        private void ReceiveMessages(Socket socket)
        {
            Console.WriteLine("Receiving started");
            byte[] receivedBytes = new byte[256];

            while (serverSocket == socket)
            {
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                int bytesCount;
                try
                {
                    bytesCount = socket.ReceiveFrom(receivedBytes, ref endPoint);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut
                    || e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    CheckControllerTimeout();
                    continue;
                }
                catch (Exception e)
                {
                    //Socket was closed by Shutdown() or failed
                    if (serverSocket == socket)
                    {
                        Console.WriteLine("INFO: Receiving exception: " + e);
                        Shutdown();
                    }
                    break;
                }

                string receivedMessage = Encoding.UTF8.GetString(receivedBytes, 0, bytesCount);
                try
                {
                    OnMessageReceived(socket, (IPEndPoint)endPoint, receivedMessage);
                }
                catch (Exception e)
                {
                    //Malformed message shouldn't stop the server
                    Console.WriteLine("INFO: Can't process message \"" + receivedMessage + "\": " + e.Message);
                }
                CheckControllerTimeout();
            }
            Console.WriteLine("Receiving stopped");
        }

        private void CheckControllerTimeout()
        {
            lock (stateLock)
            {
                if (controllerEndPoint != null
                    && TimeUtil.GetMonotonicMillis() - lastControllerMsgTime > CONTROLLER_TIMEOUT)
                {
                    Console.WriteLine("Controller timed out");
                    DisconnectController();
                }
            }
        }

        private void OnMessageReceived(Socket socket, IPEndPoint endPoint, string message)
        {
            lock (stateLock)
            {
                if (serverSocket != socket) return;

                if (endPoint.Equals(controllerEndPoint))
                {
                    lastControllerMsgTime = TimeUtil.GetMonotonicMillis();
                    OnMessageFromController(socket, endPoint, message);
                }
                else if (message.StartsWith(HELLO_MESSAGE))
                {
                    //Only one controller is supported; another one can connect
                    //only after the current one has gone silent
                    if (controllerEndPoint != null)
                    {
                        if (TimeUtil.GetMonotonicMillis() - lastControllerMsgTime <= CONTROLLER_TIMEOUT) return;
                        DisconnectController();
                    }
                    OnHelloFromController(socket, endPoint);
                }
            }
        }

        private void OnHelloFromController(Socket socket, IPEndPoint remoteEndPoint)
        {
            Console.WriteLine("Hello from controller received!");

            pcController.OnRemoteControlConnected();
            SendHelloAnswer(socket, remoteEndPoint);

            Interlocked.Exchange(ref effectDuration, 0);
            controllerPaused = false;
            lastControllerMsgTime = TimeUtil.GetMonotonicMillis();
            controllerEndPoint = remoteEndPoint;
            int session = Interlocked.Increment(ref controllerSession);

            PostStatusUpdate();

            Thread controllerSender = new Thread(() => SendToController(socket, remoteEndPoint, session))
            {
                IsBackground = true,
                Name = "UDP sender"
            };
            controllerSender.Start();
        }

        private void SendHelloAnswer(Socket socket, IPEndPoint remoteEndPoint)
        {
            byte[] bytesToAnswer = Encoding.UTF8.GetBytes("Hi!");
            socket.SendTo(bytesToAnswer, remoteEndPoint);
        }

        private void OnMessageFromController(Socket socket, IPEndPoint endPoint, string message)
        {
            //Controller reconnected from the same address and port
            if (message.StartsWith(HELLO_MESSAGE))
            {
                pcController.OnRemoteControlConnected();
                SendHelloAnswer(socket, endPoint);
                return;
            }

            if (message.StartsWith(GOODBYE_MESSAGE))
            {
                Console.WriteLine("Goodbye from controller received");
                DisconnectController();
                return;
            }

            if (message.StartsWith(PAUSED_MESSAGE))
            {
                if (!controllerPaused)
                {
                    controllerPaused = true;
                    //Nothing should stay pressed while controller is paused
                    pcController.ReleaseControls();
                    Interlocked.Exchange(ref effectDuration, 0);
                    PostStatusUpdate();
                }
                return;
            }

            string[] msgParts = message.Split(',');
            if (msgParts.Length < CONTROLLER_MESSAGE_PARTS)
            {
                Console.WriteLine("INFO: Unknown message from controller: " + message);
                return;
            }

            double accelerometerValue = double.Parse(msgParts[0], CultureInfo.InvariantCulture);
            bool breakPressed = bool.Parse(msgParts[1]);
            bool gasPressed = bool.Parse(msgParts[2]);

            bool leftSignalClick = bool.Parse(msgParts[3]);
            bool rightSignalClick = bool.Parse(msgParts[4]);
            bool emergencySignalClick = bool.Parse(msgParts[5]);

            bool parkingBrakeEnabled = bool.Parse(msgParts[6]);
            bool lightsState = bool.Parse(msgParts[7]);

            int hornState = int.Parse(msgParts[8]);
            bool isCruise = bool.Parse(msgParts[9]);

            bool hasPedalLevels = msgParts.Length >= PEDAL_LEVELS_INDEX + 2;
            double gasLevel = 0, brakeLevel = 0;
            if (hasPedalLevels)
            {
                gasLevel = double.Parse(msgParts[PEDAL_LEVELS_INDEX], CultureInfo.InvariantCulture);
                brakeLevel = double.Parse(msgParts[PEDAL_LEVELS_INDEX + 1], CultureInfo.InvariantCulture);
            }

            int actionsCount = Math.Max(0, Math.Min(msgParts.Length - ACTIONS_INDEX, PCController.ActionsCount));
            int[] actionCounters = new int[actionsCount];
            for (int i = 0; i < actionsCount; i++)
            {
                actionCounters[i] = int.Parse(msgParts[ACTIONS_INDEX + i]);
            }

            if (controllerPaused)
            {
                controllerPaused = false;
                PostStatusUpdate();
            }

            if (double.IsNaN(accelerometerValue) || double.IsInfinity(accelerometerValue))
            {
                accelerometerValue = 0;
            }

            pcController.UpdateAccelerometerValue(accelerometerValue);
            pcController.UpdateBreakGasState(breakPressed, gasPressed);
            pcController.UpdateHorn(hornState);
            if (hasPedalLevels) pcController.UpdatePedalLevels(gasLevel, brakeLevel);

            //Toggle values are only synchronized on the first message after (re)connect,
            //otherwise their difference with the previous session would cause false clicks
            if (!pcController.SyncTogglesIfNeeded(leftSignalClick, rightSignalClick, emergencySignalClick,
                parkingBrakeEnabled, lightsState, isCruise, actionCounters))
            {
                pcController.UpdateActions(actionCounters);
                pcController.UpdateTurnSignals(leftSignalClick, rightSignalClick, emergencySignalClick);
                pcController.UpdateParkingBrake(parkingBrakeEnabled);
                pcController.UpdateLights(lightsState);
                pcController.UpdateCruise(isCruise);
            }
        }

        //Must be called under stateLock
        private void DisconnectController()
        {
            if (controllerEndPoint == null) return;

            controllerEndPoint = null;
            controllerPaused = false;
            Interlocked.Increment(ref controllerSession);
            //Controller may disappear with gas or brake pressed
            pcController.ReleaseControls();
            PostStatusUpdate();
        }

        /* ................................. </Receiver thread> .............................*/


        /* ................................. <Sender thread> .............................*/

        private void SendToController(Socket socket, IPEndPoint endPoint, int session)
        {
            try
            {
                while (session == Volatile.Read(ref controllerSession))
                {
                    var telemetry = Ets2TelemetryDataReader.Instance.Read();

                    //Saving telemetry data to local state
                    pcController.UpdateTelemetryData(telemetry);

                    //Paused controller doesn't read anything, so there's no need to flood it
                    if (!controllerPaused)
                    {
                        byte[] messageToControllerBytes = Encoding.UTF8.GetBytes(MakeMessageToController(telemetry));
                        socket.SendTo(messageToControllerBytes, endPoint);
                    }

                    if (controllerPaused) Thread.Sleep(50);
                    else Thread.Sleep(20);
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                Console.WriteLine("INFO: Send exception handled");
                Console.WriteLine("INFO: " + e.ToString());
            }
        }

        private string MakeMessageToController(IEts2TelemetryData telemetry)
        {
            var truck = telemetry.Truck;

            //Engine and parking brake
            var engineOn = truck.EngineOn;
            var isParkingEnabled = truck.ParkBrakeOn;

            //Blinkers
            var leftBlinkerOn = truck.BlinkerLeftOn;
            var rightBlinkerOn = truck.BlinkerRightOn;

            //Lights
            var parkingLights = truck.LightsParkingOn;
            var lowBeamOn = truck.LightsBeamLowOn;
            var highBeamOn = truck.LightsBeamHighOn;

            var lightsState = 0;

            if (highBeamOn)
            {
                if (lowBeamOn)
                {
                    lightsState = 3;
                }
                else if (parkingLights)
                {
                    lightsState = 1;
                }
            }
            else if (lowBeamOn)
            {
                lightsState = 2;
            }
            else if (parkingLights)
            {
                lightsState = 1;
            }

            int effect = Interlocked.Exchange(ref effectDuration, 0);

            //Additional state (0/1): trailer attached, wipers, beacon, analog pedals available.
            //Old controllers read only the first 6 values
            var trailerAttached = telemetry.Trailer1 != null && telemetry.Trailer1.Attached;

            return $"{engineOn},{isParkingEnabled}," +
                $"{leftBlinkerOn},{rightBlinkerOn}," + $"{lightsState}," +
                $"{effect}," +
                $"{Bit(trailerAttached)},{Bit(truck.WipersOn)},{Bit(truck.LightsBeaconOn)}," +
                $"{Bit(InputEmulator.HasPedalAxes())}";
        }

        private static int Bit(bool value)
        {
            return value ? 1 : 0;
        }

        /* ................................. </Sender thread> .............................*/


        public void Shutdown()
        {
            lock (stateLock)
            {
                enabled = false;
                DisconnectController();

                Socket socket = serverSocket;
                serverSocket = null;
                try
                {
                    socket?.Close();
                }
                catch (Exception) { }
            }
            PostStatusUpdate();
        }

        private void PostStatusUpdate()
        {
            statusListener.OnStatusUpdate(enabled, controllerEndPoint != null, controllerPaused);
        }
    }
}
