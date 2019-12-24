using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace TruckRemoteServer
{
    public class UDPServer
    {
        MainForm Main_Form = Application.OpenForms.OfType<MainForm>().Single(); //Acessing Main Form
        public int port = 18250;

        public bool enabled = Properties.Settings.Default.StartServerOnStartup;//true;
        public bool controllerPaused, panelPaused;
        public long lastControllerMsgTime, lastPanelMsgTime;

        private UdpClient udpClient;
        private IPEndPoint controllerEndPoint, panelEndPoint;

        internal PCController pcController = new PCController();

        private Label labelStatus;
        private Button buttonStartStop;

        public UDPServer(Label labelStatus, Button buttonStartStop)
        {
            this.labelStatus = labelStatus;
            this.buttonStartStop = buttonStartStop;
            pcController.initializeKeyMapping();
        }

        public void Start()
        {
            enabled = true;
            Main_Form.StartStopButtonProperties();
            Thread thread = new Thread(LaunchServer);
            thread.Start();
        }

        public void LaunchServer()
        {
            try
            {
                IPEndPoint localIpEndPoint = new IPEndPoint(IPAddress.Any, port);
                udpClient = new UdpClient(localIpEndPoint);
                StartListeningForMessages();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                ShowStatus("Disabled", Color.OrangeRed);
                controllerEndPoint = null;
                panelEndPoint = null;
            }
        }

        private void StartListeningForMessages()
        {
            ShowStatus("Enabled", Color.ForestGreen);
            IPEndPoint remoteEndPoint = null;

            try
            {
                while (true)
                {
                    byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                    string receivedMessage = Encoding.UTF8.GetString(receivedBytes);

                    if (remoteEndPoint.Equals(controllerEndPoint))
                    {
                        lastControllerMsgTime = getUnixTime();
                        CheckTimeDifferences();
                        OnMessageFromController(receivedMessage);
                    }
                    else if (remoteEndPoint.Equals(panelEndPoint))
                    {
                        lastPanelMsgTime = getUnixTime();
                        CheckTimeDifferences();
                        OnMessageFromPanel(receivedMessage);
                        Console.WriteLine(receivedMessage);
                    }
                    else if (receivedMessage.Contains("TruckRemoteHello"))
                    {
                        lastControllerMsgTime = getUnixTime();
                        OnHelloFromController(receivedMessage, remoteEndPoint);
                        udpClient.Client.ReceiveTimeout = 3000;
                    }
                    else if (receivedMessage.Contains("TruckPanelRemoteHello"))
                    {
                        lastPanelMsgTime = getUnixTime();
                        OnHelloFromPanel(receivedMessage, remoteEndPoint);
                        udpClient.Client.ReceiveTimeout = 3000;
                    }

                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut)
                {
                    StopServer();
                    LaunchServer();
                }
            }
        }

        private void OnHelloFromController(string initMessage, IPEndPoint remoteEndPoint)
        {
            if (controllerEndPoint != null) return;
            Debug.WriteLine("Hello from controller received!");

            string[] dataParts = initMessage.Substring(initMessage.IndexOf("\n") + 1).Split(',');
            pcController.SetControllerStartValues(
                bool.Parse(dataParts[0]),
                bool.Parse(dataParts[1]),
                bool.Parse(dataParts[2]),
                int.Parse(dataParts[3]));
            pcController.InitializeJoyIfNeccessary();

            byte[] bytesToAnswer = Encoding.UTF8.GetBytes("Hi!");
            udpClient.Send(bytesToAnswer, bytesToAnswer.Length, remoteEndPoint);

            controllerEndPoint = remoteEndPoint;

            UpdateUiState();
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
            udpClient.Send(bytesToAnswer, bytesToAnswer.Length, remoteEndPoint);

            panelEndPoint = remoteEndPoint;

            UpdateUiState();
        }

        private void OnMessageFromController(string message)
        {
            if (message.Contains("paused"))
            {
                if (!controllerPaused)
                {
                    controllerPaused = true;
                    UpdateUiState();
                }
                return;
            }
            else if (controllerPaused)
            {
                controllerPaused = false;
                UpdateUiState();
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
                if(!panelPaused)
                {
                    panelPaused = true;
                    UpdateUiState();
                }
                return;
            }
            else if (panelPaused)
            {
                panelPaused = false;
                UpdateUiState();
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

        public void Stop()
        {
            enabled = false;
            Main_Form.StartStopButtonProperties();
            StopServer();
        }

        public void StopServer() //Prevent cross thread access on disconnect
        {
            try
            {
                udpClient.Close();
            }
            catch (Exception)
            {
            }
            finally
            {
                controllerEndPoint = null;
                panelEndPoint = null;
                ShowStatus("Disabled", Color.OrangeRed);
            }
        }

        private void CheckTimeDifferences()
        {
            long currentTime = getUnixTime();
            if (controllerEndPoint != null && currentTime - lastControllerMsgTime > 3000)
            {
                controllerEndPoint = null;
                controllerPaused = false;
                UpdateUiState();
            }
            else if(panelEndPoint != null && currentTime - lastPanelMsgTime > 3000)
            {
                panelEndPoint = null;
                panelPaused = false;
                UpdateUiState();
            }
        }

        public void UpdateUiState()
        {
            if (enabled)
            {
                //All devices connected
                if (controllerEndPoint != null && panelEndPoint != null)
                {
                    if (controllerPaused && panelPaused)
                    {
                        ShowStatus("All devices paused", Color.ForestGreen);
                    }
                    else if(controllerPaused)
                    {
                        ShowStatus("Controller: paused\nPanel: active", Color.ForestGreen);
                    }
                    else if(panelPaused)
                    {
                        ShowStatus("Controller: active\nPanel: paused", Color.ForestGreen);
                    }
                    else
                    {
                        ShowStatus("All devices active", Color.ForestGreen);
                    }
                }
                //Connected only controller
                else if (controllerEndPoint != null)
                {
                    if (controllerPaused)
                    {
                        ShowStatus("Controller paused", Color.ForestGreen);
                    }
                    else
                    {
                        ShowStatus("Controller active", Color.ForestGreen);
                    }
                }
                //Connected only panel
                else if (panelEndPoint != null)
                {
                    if(panelPaused)
                    {
                        ShowStatus("Panel paused", Color.ForestGreen);
                    }
                    else
                    {
                        ShowStatus("Panel active", Color.ForestGreen);
                    }
                }
                else
                {
                    ShowStatus("Enabled", Color.ForestGreen);
                }
            }
            else
            {
                ShowStatus("Disabled", Color.OrangeRed);
            }
        }

        private void ShowStatus(string labelText, Color color)
        {
            try
            {   
                labelStatus.BeginInvoke((MethodInvoker)delegate ()
                {
                    labelStatus.Text = labelText;
                    labelStatus.ForeColor = color;
                });
            } catch(Exception) { }
        }
        
        private long getUnixTime()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }
}