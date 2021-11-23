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

        private const int RECEIVE_TIMEOUT = 1200;

        public int port;
        private Socket serverSocket;
        private IPEndPoint localIpEndPoint;
        private IPEndPoint controllerEndPoint;
        private readonly IStatusListener statusListener;

        public bool enabled = true;
        public bool controllerPaused;
        public long lastControllerMsgTime;

        private volatile uint effectDuration = 0;

        private PCController pcController;

        public UDPServer(IStatusListener listener, int port)
        {
            this.port = port;
            statusListener = listener;
            pcController = new PCController(this);
        }

        public void Start()
        {
            Thread thread = new Thread(Launch);
            thread.Start();
        }

        public void Launch()
        {
            enabled = true;
            localIpEndPoint = new IPEndPoint(IPAddress.Any, port);
            serverSocket = new Socket(localIpEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(localIpEndPoint);

            Thread receiverThread = new Thread(StartReceivingMessages);
            receiverThread.Start();
        }

        public void OnFfbEffect(uint effectDuration)
        {
            this.effectDuration = effectDuration;
        }

        /* ................................. <Receiver thread> .............................*/
        private void StartReceivingMessages()
        {
            PostStatusUpdate();

            try
            {
                EndPoint endPoint = localIpEndPoint;

                Console.WriteLine("Receiving started");

                while (true)
                {
                    byte[] receivedBytes = new byte[128];
                    int bytesCount = serverSocket.ReceiveFrom(receivedBytes, ref endPoint);
                    string receivedMessage = Encoding.UTF8.GetString(receivedBytes, 0, bytesCount);
                    OnMessageReceived((IPEndPoint)endPoint, receivedMessage);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("INFO: Receiving exception handled");
                Console.WriteLine("INFO: " + e.ToString());
                Shutdown();

                if (e is SocketException)
                {
                    if (((SocketException)e).SocketErrorCode == SocketError.TimedOut)
                    {
                        Shutdown();
                        Launch();
                    }
                }
            }
        }

        private void OnMessageReceived(IPEndPoint endPoint, string message)
        {
            if (endPoint.Equals(controllerEndPoint))
            {
                lastControllerMsgTime = TimeUtil.GetCurrentUnixTime();
                OnMessageFromController(message);
            }
            else if (message.Contains("TruckRemoteHello"))
            {
                lastControllerMsgTime = TimeUtil.GetCurrentUnixTime();
                OnHelloFromController(message, endPoint);
                serverSocket.ReceiveTimeout = RECEIVE_TIMEOUT;
            }
        }

        private void OnHelloFromController(string initMessage, IPEndPoint remoteEndPoint)
        {
            if (controllerEndPoint != null) return;
            Console.WriteLine("Hello from controller received!");

            string[] dataParts = initMessage.Substring(initMessage.IndexOf("\n") + 1).Split(',');
            pcController.OnRemoteControlConnected();

            byte[] bytesToAnswer = Encoding.UTF8.GetBytes("Hi!");
            serverSocket.SendTo(bytesToAnswer, remoteEndPoint);

            controllerEndPoint = remoteEndPoint;

            PostStatusUpdate();

            Thread controllerSender = new Thread(StartSendToController);
            controllerSender.Start();
        }

        private void OnMessageFromController(string message)
        {
            if (message.Contains("paused"))
            {
                if (!controllerPaused)
                {
                    controllerPaused = true;
                    PostStatusUpdate();
                }
                return;
            }
            else if (controllerPaused)
            {
                controllerPaused = false;
                PostStatusUpdate();
            }
            else if (message.Contains("goodbye"))
            {
                controllerEndPoint = null;
                controllerPaused = false;
                PostStatusUpdate();
                return;
            }

            string[] msgParts = message.Split(',');

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

            pcController.UpdateAccelerometerValue(accelerometerValue);
            pcController.UpdateBreakGasState(breakPressed, gasPressed);
            pcController.UpdateTurnSignals(leftSignalClick, rightSignalClick, emergencySignalClick);
            pcController.UpdateParkingBrake(parkingBrakeEnabled);
            pcController.UpdateLights(lightsState);
            pcController.UpdateHorn(hornState);
            pcController.UpdateCruise(isCruise);
        }

        /* ................................. </Receiver thread> .............................*/


        /* ................................. <Sender thread> .............................*/

        private void StartSendToController()
        {
            try
            {
                while (controllerEndPoint != null)
                {
                    var telemetry = Ets2TelemetryDataReader.Instance.Read();

                    //Saving telemetry data to local state
                    pcController.UpdateTelemetryData(telemetry);

                    var truck = telemetry.Truck;
                    var trailer = telemetry.Trailer1;

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

                    //Wipers and beacon
                    var wipersOn = truck.WipersOn;
                    var beaconOn = truck.LightsBeaconOn;

                    //Additional info
                    var islowFuel = truck.FuelWarningOn;
                    var fuelLevel = Math.Floor((truck.Fuel / truck.FuelCapacity) * 100);

                    var truckDamage =  TruckMath.Max(
                        Math.Floor(telemetry.Truck.WearCabin * 100),
                        Math.Floor(telemetry.Truck.WearChassis * 100),
                        Math.Floor(telemetry.Truck.WearEngine * 100),
                        Math.Floor(telemetry.Truck.WearTransmission * 100),
                        Math.Floor(telemetry.Truck.WearWheels * 100));

                    var trailerAttached = trailer.Attached;
                    var trailerDamage = TruckMath.Max(
                        Math.Floor(trailer.WearWheels * 100),
                        Math.Floor(trailer.WearChassis * 100));

                    var cargoDamage = Math.Floor(trailer.CargoDamage * 100);


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

                    string msgToControl = $"{engineOn},{isParkingEnabled}," +
                        $"{leftBlinkerOn},{rightBlinkerOn},{lightsState}," +
                        $"{wipersOn},{beaconOn},{islowFuel},{fuelLevel}," +
                        $"{truckDamage},{trailerAttached},{trailerDamage},{cargoDamage}," +
                        $"{effectDuration}";

                    byte[] messageToControllerBytes = Encoding.UTF8.GetBytes(msgToControl);
                    effectDuration = 0;

                    //Sending data to client
                    serverSocket.SendTo(messageToControllerBytes, controllerEndPoint);

                    if (controllerPaused) Thread.Sleep(50);
                    else Thread.Sleep(20);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("INFO: Send exception handled");
                Console.WriteLine("INFO: " + e.ToString());
            }
        }

        /* ................................. </Sender thread> .............................*/


        public void Shutdown()
        {
            enabled = false;
            try
            {
                serverSocket.Close();
            }
            catch (Exception) { }
            finally
            {
                controllerEndPoint = null;
                PostStatusUpdate();
            }
        }

        private void PostStatusUpdate()
        {
            statusListener.OnStatusUpdate(enabled, controllerEndPoint != null, controllerPaused);
        }
    }
}