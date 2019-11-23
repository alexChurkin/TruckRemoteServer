﻿using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Drawing;

namespace TruckRemoteControlServer
{
    public class UDPServer
    {
        public int port = 18250;

        public bool enabled = true;
        public static bool controllerPaused = false;

        private UdpClient udpClient;
        private PCController controller = new PCController();

        private Label labelStatus;
        private Button buttonStop;
        private Button buttonStart;

        private IPEndPoint controllerEndPoint, panelEndPoint;

        public UDPServer(Label labelStatus, Button buttonStop, Button buttonStart)
        {
            this.labelStatus = labelStatus;
            this.buttonStart = buttonStart;
            this.buttonStop = buttonStop;
        }

        public void Start()
        {
            Thread thread = new Thread(LaunchServer);
            thread.Start();
        }

        public void LaunchServer()
        {
            enabled = true;
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
                IPEndPoint localIpEndPoint = new IPEndPoint(ipAddress, port);

                udpClient = new UdpClient(localIpEndPoint);

                StartListeningForMessages();
            }
            catch (Exception)
            {
                ShowStatus("Disabled", Color.OrangeRed);
                SetButtonsIsListening(false);
                controllerEndPoint = null;
                panelEndPoint = null;
            }
        }

        private void StartListeningForMessages()
        {
            ShowStatus("Enabled", Color.ForestGreen);
            SetButtonsIsListening(true);

            IPEndPoint remoteEndPoint = null;

            while (true)
            {
                byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                string receivedMessage = Encoding.UTF8.GetString(receivedBytes);

                if (remoteEndPoint.Equals(controllerEndPoint))
                {
                    OnMessageFromController(receivedMessage);
                }
                else if (remoteEndPoint.Equals(panelEndPoint))
                {
                    OnMessageFromPanel(receivedMessage);
                }
                else if (receivedMessage.Contains("TruckRemoteHello"))
                {
                    OnHelloFromController(receivedMessage, remoteEndPoint);
                }
                else if (receivedMessage.Equals("TruckRemoteHelloFromPanel"))
                {
                    OnHelloFromPanel(receivedMessage, remoteEndPoint);
                }
            }
        }

        private void OnHelloFromController(string initMessage, IPEndPoint remoteEndPoint)
        {
            if (controllerEndPoint != null) return;
            Debug.WriteLine("Hello from controller received!");

            string[] dataParts = initMessage.Substring(initMessage.IndexOf("\n") + 1).Split(',');
            controller.setStartValues(
                bool.Parse(dataParts[0]),
                bool.Parse(dataParts[1]),
                bool.Parse(dataParts[2]),
                int.Parse(dataParts[3]));

            byte[] bytesToAnswer = Encoding.UTF8.GetBytes("Hi!");
            udpClient.Send(bytesToAnswer, bytesToAnswer.Length, remoteEndPoint);

            controllerEndPoint = remoteEndPoint;

            udpClient.Client.ReceiveTimeout = 5000;
            UpdateStatus();
        }

        private void OnHelloFromPanel(string initMessage, IPEndPoint remoteEndPoint)
        {
            if (panelEndPoint != null) return;
            Debug.WriteLine("Hello from panel received!");

            //TODO Parse init message

            byte[] bytesToAnswer = Encoding.UTF8.GetBytes("Hi!");
            udpClient.Send(bytesToAnswer, bytesToAnswer.Length, remoteEndPoint);

            udpClient.Client.ReceiveTimeout = 5000;
            UpdateStatus();
        }

        private void OnMessageFromController(string message)
        {
            if (message.Contains("paused"))
            {
                controllerPaused = true;
                UpdateStatus();
            }
            else if (controllerPaused)
            {
                controllerPaused = false;
                UpdateStatus();
            }

            string[] msgParts = message.Split(',');

            double accelerometerValue = double.Parse(msgParts[0], CultureInfo.InvariantCulture);
            bool breakClicked = bool.Parse(msgParts[1]);
            bool gasClicked = bool.Parse(msgParts[2]);
            bool leftSignalEnabled = bool.Parse(msgParts[3]);
            bool rightSignalEnabled = bool.Parse(msgParts[4]);
            bool parkingBrakeEnabled = bool.Parse(msgParts[5]);
            int lightsState = int.Parse(msgParts[6]);
            bool isHorn = bool.Parse(msgParts[7]);

            controller.updateAccelerometerValue(accelerometerValue);
            controller.updateBreakGasState(breakClicked, gasClicked);
            controller.updateTurnSignals(leftSignalEnabled, rightSignalEnabled);
            controller.updateParkingBrake(parkingBrakeEnabled);
            controller.updateLights(lightsState);
            controller.updateHorn(isHorn);
        }

        private void OnMessageFromPanel(string message)
        {
            string[] msgParts = message.Split(',');
            //TODO Proccess message
        }

        public void Stop()
        {
            enabled = false;
            try
            {
                udpClient.Close();
            }
            catch (Exception)
            {
            }
        }


        private void UpdateStatus()
        {
            if (enabled)
            {
                if (controllerEndPoint != null && panelEndPoint != null)
                {
                    if (controllerPaused)
                    {
                        ShowStatus("Controller paused & Panel connected", Color.ForestGreen);
                    }
                    else
                    {
                        ShowStatus("Controller & Panel connected", Color.ForestGreen);
                    }
                }
                else if (controllerEndPoint != null)
                {
                    if (controllerPaused)
                    {
                        ShowStatus("Controller paused", Color.ForestGreen);
                    }
                    else
                    {
                        ShowStatus("Controller connected", Color.ForestGreen);
                    }
                }
                else if (panelEndPoint != null)
                {
                    ShowStatus("Panel connected", Color.ForestGreen);
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
            labelStatus.BeginInvoke((MethodInvoker)delegate ()
            {
                this.labelStatus.Text = labelText;
                this.labelStatus.ForeColor = color;
            });
        }

        private void SetButtonsIsListening(bool isConnected)
        {
            buttonStop.BeginInvoke((MethodInvoker)delegate ()
            {
                this.buttonStop.Enabled = isConnected;
            });
            buttonStart.BeginInvoke((MethodInvoker)delegate ()
            {
                this.buttonStart.Enabled = !isConnected;
            });
        }
    }
}