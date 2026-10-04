using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using TruckRemoteServer.Input;
using TruckRemoteServer.Protocol;
using TruckRemoteServer.Telemetry;

namespace TruckRemoteServer
{
    public sealed class ServerStatus
    {
        public static readonly ServerStatus Stopped = new ServerStatus(false, false, false);

        public ServerStatus(bool running, bool controllerConnected, bool controllerPaused)
        {
            Running = running;
            ControllerConnected = controllerConnected;
            ControllerPaused = controllerPaused;
        }

        public bool Running { get; }
        public bool ControllerConnected { get; }
        public bool ControllerPaused { get; }
    }

    //Better timer resolution for the sending thread (the default one is ~15.6 ms on Windows)
    public interface ITimerResolution
    {
        IDisposable Acquire();
    }

    //UDP server for one controller: applies its messages to the game input and sends the truck state back
    public class ControllerServer
    {
        //Controller is considered disconnected after this period of silence
        public const int CONTROLLER_TIMEOUT = 1200;
        //Socket receive timeout (to check controller's silence periodically)
        private const int RECEIVE_TIMEOUT = 300;
        //Truck state is sent 50 times per second, 20 times to a paused controller
        private const int SEND_INTERVAL = 20;
        private const int PAUSED_SEND_INTERVAL = 50;
        //Makes Windows not to break UDP socket with WSAECONNRESET after ICMP "Port unreachable"
        private const int SIO_UDP_CONNRESET = -1744830452;

        private const string HELLO_MESSAGE = "TruckRemoteHello";
        private const string HELLO_ANSWER = "Hi!";
        private const string PAUSED_MESSAGE = "paused";
        private const string GOODBYE_MESSAGE = "goodbye";

        private readonly ControllerInputMapper input;
        private readonly ITelemetrySource telemetrySource;
        private readonly IVirtualJoystick joystick;
        private readonly ITimerResolution timerResolution;
        private readonly ILogger<ControllerServer> logger;
        private readonly object stateLock = new object();
        //Drops controller messages that came out of order
        private readonly SequenceGate sequenceGate = new SequenceGate();

        private volatile Socket serverSocket;
        private volatile IPEndPoint controllerEndPoint;
        private volatile bool controllerPaused;
        private long lastControllerMessageTime;
        //Incremented on every controller connect/disconnect, stops outdated sender threads
        private int controllerSession;
        private int effectDuration;

        public ControllerServer(ControllerInputMapper input, ITelemetrySource telemetrySource,
            IVirtualJoystick joystick, ITimerResolution timerResolution, ILogger<ControllerServer> logger)
        {
            this.input = input;
            this.telemetrySource = telemetrySource;
            this.joystick = joystick;
            this.timerResolution = timerResolution;
            this.logger = logger;
            joystick.ForceFeedback += OnForceFeedback;
        }

        //Called on network threads
        public event Action<ServerStatus> StatusChanged;

        public int Port { get; private set; }

        public ServerStatus Status
        {
            get
            {
                lock (stateLock)
                {
                    return new ServerStatus(serverSocket != null, controllerEndPoint != null, controllerPaused);
                }
            }
        }

        //Returns false if the server can't be started (e.g. the port is busy)
        public bool Start(int port)
        {
            Socket socket;
            lock (stateLock)
            {
                if (serverSocket != null) return true;

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    try
                    {
                        socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
                    }
                    catch (Exception)
                    {
                        //Not Windows: there is no such problem
                    }
                    socket.Bind(new IPEndPoint(IPAddress.Any, port));
                    socket.ReceiveTimeout = RECEIVE_TIMEOUT;
                }
                catch (Exception e)
                {
                    logger.LogError("Can't start the server on port {Port}: {Message}", port, e.Message);
                    socket.Close();
                    PostStatus();
                    return false;
                }

                Port = ((IPEndPoint)socket.LocalEndPoint).Port;
                serverSocket = socket;
            }

            new Thread(() => ReceiveMessages(socket)) { IsBackground = true, Name = "UDP receiver" }.Start();
            logger.LogInformation("Server started on port {Port}", Port);
            PostStatus();
            return true;
        }

        public void Stop()
        {
            lock (stateLock)
            {
                DisconnectController();
                Socket socket = serverSocket;
                serverSocket = null;
                try
                {
                    socket?.Close();
                }
                catch (Exception)
                {
                    //Already closed
                }
            }
            PostStatus();
        }

        private void OnForceFeedback(uint duration)
        {
            Interlocked.Exchange(ref effectDuration, (int)Math.Min(duration, int.MaxValue));
        }

        /* Receiver thread */

        private void ReceiveMessages(Socket socket)
        {
            byte[] buffer = new byte[256];

            while (serverSocket == socket)
            {
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                int length;
                try
                {
                    length = socket.ReceiveFrom(buffer, ref endPoint);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut
                    || e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    CheckControllerTimeout();
                    continue;
                }
                catch (Exception e)
                {
                    //Socket was closed by Stop() or failed
                    if (serverSocket == socket)
                    {
                        logger.LogWarning("Receiving failed: {Error}", e);
                        Stop();
                    }
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, length);
                try
                {
                    OnMessage(socket, (IPEndPoint)endPoint, message);
                }
                catch (Exception e)
                {
                    //Malformed message shouldn't stop the server
                    logger.LogInformation("Can't process message \"{Message}\": {Error}", message, e.Message);
                }
                CheckControllerTimeout();
            }
        }

        private void CheckControllerTimeout()
        {
            lock (stateLock)
            {
                if (controllerEndPoint != null && MonotonicClock.Millis - lastControllerMessageTime > CONTROLLER_TIMEOUT)
                {
                    logger.LogInformation("Controller timed out");
                    DisconnectController();
                }
            }
        }

        private void OnMessage(Socket socket, IPEndPoint endPoint, string message)
        {
            lock (stateLock)
            {
                if (serverSocket != socket) return;

                if (endPoint.Equals(controllerEndPoint))
                {
                    lastControllerMessageTime = MonotonicClock.Millis;
                    OnMessageFromController(socket, endPoint, message);
                }
                else if (message.StartsWith(HELLO_MESSAGE))
                {
                    //Only one controller is supported. Another device can connect only after
                    //the current one has gone silent; the same device (reconnect) takes over at once
                    if (controllerEndPoint != null)
                    {
                        bool sameDevice = endPoint.Address.Equals(controllerEndPoint.Address);
                        bool timedOut = MonotonicClock.Millis - lastControllerMessageTime > CONTROLLER_TIMEOUT;
                        if (!sameDevice && !timedOut) return;
                        DisconnectController();
                    }
                    ConnectController(socket, endPoint);
                }
            }
        }

        //Must be called under stateLock
        private void ConnectController(Socket socket, IPEndPoint endPoint)
        {
            logger.LogInformation("Controller connected from {EndPoint}", endPoint);
            input.OnControllerConnected();
            Answer(socket, endPoint, HELLO_ANSWER);

            Interlocked.Exchange(ref effectDuration, 0);
            sequenceGate.Reset();
            controllerPaused = false;
            lastControllerMessageTime = MonotonicClock.Millis;
            controllerEndPoint = endPoint;
            int session = Interlocked.Increment(ref controllerSession);
            PostStatus();

            new Thread(() => SendToController(socket, endPoint, session)) { IsBackground = true, Name = "UDP sender" }
                .Start();
        }

        //Must be called under stateLock
        private void OnMessageFromController(Socket socket, IPEndPoint endPoint, string message)
        {
            //The same controller resumes the session after a network problem (its messages didn't come for a while
            //or ours didn't reach it). Controls aren't released and toggles aren't synchronized:
            //clicks made meanwhile must still happen
            if (message.StartsWith(HELLO_MESSAGE))
            {
                logger.LogInformation("Controller resumes the session");
                sequenceGate.Reset();
                Answer(socket, endPoint, HELLO_ANSWER);
                return;
            }

            if (message.StartsWith(GOODBYE_MESSAGE))
            {
                logger.LogInformation("Goodbye from controller received");
                DisconnectController();
                return;
            }

            if (message.StartsWith(PAUSED_MESSAGE))
            {
                if (!controllerPaused)
                {
                    controllerPaused = true;
                    //Nothing should stay pressed while controller is paused
                    input.ReleaseControls();
                    Interlocked.Exchange(ref effectDuration, 0);
                    PostStatus();
                }
                return;
            }

            ControllerMessage state = ControllerMessage.Parse(message, ControllerInputMapper.ActionsCount);
            if (state == null)
            {
                logger.LogInformation("Unknown message from controller: {Message}", message);
                return;
            }
            if (!sequenceGate.Accept(state.Sequence)) return;

            if (controllerPaused)
            {
                controllerPaused = false;
                PostStatus();
            }
            input.Apply(state);
        }

        //Must be called under stateLock
        private void DisconnectController()
        {
            if (controllerEndPoint == null) return;

            controllerEndPoint = null;
            controllerPaused = false;
            Interlocked.Increment(ref controllerSession);
            //Controller may disappear with gas or brake pressed
            input.ReleaseControls();
            PostStatus();
        }

        private static void Answer(Socket socket, EndPoint endPoint, string text)
        {
            socket.SendTo(Encoding.UTF8.GetBytes(text), endPoint);
        }

        /* Sender thread */

        //Sends at a fixed rate: the interval doesn't grow by the time of reading telemetry and sending.
        //Errors (e.g. the network is down for a moment) don't stop sending: the session may be resumed
        private void SendToController(Socket socket, IPEndPoint endPoint, int session)
        {
            long sequence = 0;
            long nextSendTime = 0;
            Stopwatch clock = Stopwatch.StartNew();

            using (timerResolution.Acquire())
            {
                while (session == Volatile.Read(ref controllerSession))
                {
                    try
                    {
                        TruckTelemetry truck = telemetrySource.Read();
                        input.UpdateTelemetry(truck);

                        //Paused controller doesn't read anything, so there's no need to flood it
                        if (!controllerPaused)
                        {
                            socket.SendTo(Encoding.UTF8.GetBytes(MakeMessage(truck, ++sequence)), endPoint);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        //Server is stopped
                        return;
                    }
                    catch (Exception e)
                    {
                        logger.LogInformation("Send error: {Message}", e.Message);
                    }

                    nextSendTime += controllerPaused ? PAUSED_SEND_INTERVAL : SEND_INTERVAL;
                    long wait = nextSendTime - clock.ElapsedMilliseconds;
                    //After a long delay (e.g. the PC was busy) messages aren't sent in a burst to catch up
                    if (wait < -SEND_INTERVAL) nextSendTime = clock.ElapsedMilliseconds;
                    if (wait > 0) Thread.Sleep((int)wait);
                }
            }
        }

        private string MakeMessage(TruckTelemetry truck, long sequence)
        {
            int lightsMode = ServerMessage.LightsMode(truck.ParkingLights, truck.LowBeam, truck.HighBeam);
            int effect = Interlocked.Exchange(ref effectDuration, 0);
            return ServerMessage.Format(truck.EngineOn, truck.ParkingBrake, truck.LeftBlinker, truck.RightBlinker,
                lightsMode, effect, truck.TrailerAttached, truck.Wipers, truck.Beacon, joystick.HasPedalAxes, sequence);
        }

        private void PostStatus()
        {
            StatusChanged?.Invoke(Status);
        }
    }
}
