using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Infiniminer
{
    class Program
    {
        static void Main(string[] args)
        {
            bool authEnabled = true, publicServer = false;
            DatafileLoader dataFile = new DatafileLoader("server.config.txt");
            if (dataFile.Data.ContainsKey("authenabled"))
                authEnabled = bool.Parse(dataFile.Data["authenabled"]);
            if (dataFile.Data.ContainsKey("public"))
                publicServer = bool.Parse(dataFile.Data["public"]);

            try
            {
                bool restartServer = true;
                while (restartServer)
                {
                    InfiniminerServer infiniminerServer = new InfiniminerServer();
                    restartServer = infiniminerServer.Start();
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "\r\n\r\n" + e.StackTrace);
            }
        }
    }
}
