using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Globalization;

namespace TruckRemoteControlServer
{
    public class UDPServer
    {
        public int port = 18250;

        int clientsCount = 0;
        public static bool paused = false;
        private bool running = true;
        private string lastMessage = "0,false,false,false,false,false";

        private UdpClient udpClient;
        private PCController controller = new PCController();

        public void Start()
        {
            Thread thread = new Thread(LaunchServer);
            thread.Start();
        }

        public void LaunchServer()
        {
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
                IPEndPoint localIpEndPoint = new IPEndPoint(ipAddress, port);

                Debug.WriteLine("UDP Client created");
                udpClient = new UdpClient(localIpEndPoint);

                StartListeningForConnection();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception with UDPServer: {e.Message}");
            }
        }

        private void StartListeningForConnection()
        {
            IPEndPoint anyIpEndPoint = null;

            while (true)
            {
                byte[] receivedBytes = udpClient.Receive(ref anyIpEndPoint);
                string clientMessage = Encoding.UTF8.GetString(receivedBytes);

                if (clientMessage.Equals("TruckRemoteHello"))
                {
                    Debug.WriteLine("Hello received!");
                    byte[] bytesToAnswer = Encoding.UTF8.GetBytes("Hi!");
                    udpClient.Send(bytesToAnswer, bytesToAnswer.Length, anyIpEndPoint);
                    ListenRemoteClient(anyIpEndPoint);
                    Debug.WriteLine("Listening for remote client ended");
                }
            }
        }

        private void ListenRemoteClient(IPEndPoint specificClientEndPoint)
        {
            Debug.WriteLine("Started listening");
            IPEndPoint newEndPoint = null;
            udpClient.Connect(specificClientEndPoint);
            while(true)
            {
                byte[] receivedBytes = udpClient.Receive(ref newEndPoint);
                if (!newEndPoint.Address.Equals(specificClientEndPoint.Address)) continue;

                string clientMessage = Encoding.UTF8.GetString(receivedBytes);

                Debug.WriteLine(clientMessage);

                if (clientMessage.Contains("paused"))
                {
                    //
                    continue;
                } else if (clientMessage.Contains("disconnected"))
                {
                    Debug.WriteLine("disconnected!!!! ");
                }


                string[] msgParts = clientMessage.Split(',');

                double accelerometerValue = double.Parse(msgParts[0], CultureInfo.InvariantCulture);
                bool breakClicked = bool.Parse(msgParts[1]);
                bool gasClicked = bool.Parse(msgParts[2]);
                bool leftSignalEnabled = bool.Parse(msgParts[3]);
                bool rightSignalEnabled = bool.Parse(msgParts[4]);
                bool parkingBrakeEnabled = bool.Parse(msgParts[5]);

                controller.updateAccelerometerValue(accelerometerValue);
                controller.updateBreakGasState(breakClicked, gasClicked);
                controller.updateTurnSignals(leftSignalEnabled, rightSignalEnabled);
                controller.updateParkingBrake(parkingBrakeEnabled);
            }
        }


        public bool IsRunning()
        {
            return running;
        }

        public bool IsConnected()
        {
            return clientsCount > 0;
        }

        public string getLastMessage()
        {
            return lastMessage;
        }

        public void Stop()
        {
            try
            {
                udpClient.Close();
            }
            catch (Exception) {}
            running = false;
        }
        }
}