using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Globalization;

namespace TruckRemoteServer
{
    public class UDPServer : IFfbListener
    {
        public interface IStatusListener
        {
            void OnStatusUpdate(bool isEnabled,
                bool controllerConnected, bool panelConnected,
                bool controllerPaused, bool panelPaused);
        }

        private const int RECEIVE_TIMEOUT = 800;

        public int port;
        private Socket serverSocket;
        private IPEndPoint localIpEndPoint;
        private IPEndPoint controllerEndPoint, panelEndPoint;
        private IStatusListener statusListener;

        public bool enabled = true;
        public bool controllerPaused, panelPaused;
        public long lastControllerMsgTime, lastPanelMsgTime;

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
                CheckTimeDifferences();
                OnMessageFromController(message);
            }
            else if (endPoint.Equals(panelEndPoint))
            {
                lastPanelMsgTime = TimeUtil.GetCurrentUnixTime();
                CheckTimeDifferences();
                OnMessageFromPanel(message);
            }
            else if (message.Contains("TruckRemoteHello"))
            {
                lastControllerMsgTime = TimeUtil.GetCurrentUnixTime();
                OnHelloFromController(message, endPoint);
                serverSocket.ReceiveTimeout = RECEIVE_TIMEOUT;
            }
            else if (message.Contains("TruckPanelRemoteHello"))
            {
                lastPanelMsgTime = TimeUtil.GetCurrentUnixTime();
                OnHelloFromPanel(message, endPoint);
                serverSocket.ReceiveTimeout = RECEIVE_TIMEOUT;
            }
        }

        private void OnHelloFromController(string initMessage, IPEndPoint remoteEndPoint)
        {
            if (controllerEndPoint != null) return;
            Console.WriteLine("Hello from controller received!");

            string[] dataParts = initMessage.Substring(initMessage.IndexOf("\n") + 1).Split(',');
            pcController.SetControllerStartValues(
                bool.Parse(dataParts[0]),
                bool.Parse(dataParts[1]),
                bool.Parse(dataParts[2]),
                int.Parse(dataParts[3]));
            pcController.OnRemoteControlConnected();

            byte[] bytesToAnswer = Encoding.UTF8.GetBytes("Hi!");
            serverSocket.SendTo(bytesToAnswer, remoteEndPoint);

            controllerEndPoint = remoteEndPoint;

            PostStatusUpdate();

            Thread controllerSender = new Thread(StartSendToController);
            controllerSender.Start();
        }

        private void OnHelloFromPanel(string initMessage, IPEndPoint remoteEndPoint)
        {
            if (panelEndPoint != null) return;
            Console.WriteLine("Hello from panel received!");

            string[] dataParts = initMessage.Substring(initMessage.IndexOf("\n") + 1).Split(',');
            pcController.SetPanelStartValues(
                bool.Parse(dataParts[0]),
                int.Parse(dataParts[1]),
                bool.Parse(dataParts[2]),
                bool.Parse(dataParts[3]));

            byte[] bytesToAnswer = Encoding.UTF8.GetBytes("Hi!");
            serverSocket.SendTo(bytesToAnswer, remoteEndPoint);

            panelEndPoint = remoteEndPoint;

            PostStatusUpdate();
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
            bool breakClicked = bool.Parse(msgParts[1]);
            bool gasClicked = bool.Parse(msgParts[2]);
            bool leftSignalEnabled = bool.Parse(msgParts[3]);
            bool rightSignalEnabled = bool.Parse(msgParts[4]);
            bool parkingBrakeEnabled = bool.Parse(msgParts[5]);
            int lightsState = int.Parse(msgParts[6]);
            int hornState = int.Parse(msgParts[7]);
            bool isCruise = bool.Parse(msgParts[8]);

            pcController.UpdateAccelerometerValue(accelerometerValue);
            pcController.UpdateBreakGasState(breakClicked, gasClicked);
            pcController.UpdateTurnSignals(leftSignalEnabled, rightSignalEnabled);
            pcController.UpdateParkingBrake(parkingBrakeEnabled);
            pcController.UpdateLights(lightsState);
            pcController.UpdateHorn(hornState);
            pcController.UpdateCruise(isCruise);
        }

        private void OnMessageFromPanel(string message)
        {
            if (message.Contains("paused"))
            {
                if (!panelPaused)
                {
                    panelPaused = true;
                    PostStatusUpdate();
                }
                return;
            }
            else if (panelPaused)
            {
                panelPaused = false;
                PostStatusUpdate();
            }
            else if (message.Contains("goodbye"))
            {
                panelEndPoint = null;
                panelPaused = false;
                PostStatusUpdate();
                return;
            }

            string[] msgParts = message.Split(',');

            bool diffBlock = bool.Parse(msgParts[0]);
            int wipersState = int.Parse(msgParts[1]);
            bool liftingAxle = bool.Parse(msgParts[2]);
            bool flashingBeacon = bool.Parse(msgParts[3]);

            pcController.UpdateDiffBlock(diffBlock);
            pcController.UpdateWipers(wipersState);
            pcController.UpdateLiftingAxle(liftingAxle);
            pcController.UpdateFlashingBeacon(flashingBeacon);
        }

        private void CheckTimeDifferences()
        {
            //Connected only one device
            if ((controllerEndPoint != null) != (panelEndPoint != null))
            {
                return;
            }

            long currentTime = TimeUtil.GetCurrentUnixTime();

            if (controllerEndPoint != null && currentTime - lastControllerMsgTime > RECEIVE_TIMEOUT*2)
            {
                controllerEndPoint = null;
                controllerPaused = false;
                PostStatusUpdate();
            }
            else if (panelEndPoint != null && currentTime - lastPanelMsgTime > RECEIVE_TIMEOUT*2)
            {
                panelEndPoint = null;
                panelPaused = false;
                PostStatusUpdate();
            }
        }

        /* ................................. </Receiver thread> .............................*/


        /* ................................. <Sender thread> .............................*/

        private void StartSendToController()
        {
            try
            {
                while (controllerEndPoint != null)
                {
                    byte[] messageToControllerBytes = Encoding.UTF8.GetBytes("" + effectDuration);
                    effectDuration = 0;
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
                panelEndPoint = null;
                PostStatusUpdate();
            }
        }

        private void PostStatusUpdate()
        {
            statusListener.OnStatusUpdate(enabled,
                controllerEndPoint != null, panelEndPoint != null,
                controllerPaused, panelPaused);
        }
    }
}