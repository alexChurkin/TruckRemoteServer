using System;
using System.IO;
using System.Threading.Tasks;

namespace TruckRemoteServer
{
    public static class Logger
    {
        public static bool Enabled = false;
        private static string LOG_FILE_NAME = "TruckRemoteLogs.txt";

        public static void Log(string msg)
        {
            try
            {
                if (Enabled) File.AppendAllText(LOG_FILE_NAME, msg + "\n");
            } catch(Exception)
            {
            }
        }

        public static void ClearFile()
        {
            File.Delete(LOG_FILE_NAME);
        }
    }
}
