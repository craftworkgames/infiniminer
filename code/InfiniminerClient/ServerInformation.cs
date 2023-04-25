using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Lidgren.Network;

namespace Infiniminer
{
    public class ServerInformation
    {
        public IPEndPoint IpEndPoint { get; private set; }
        public string ServerName { get; private set; }
        public string ServerExtra { get; private set; }
        public int NumPlayers { get; private set; }
        public int MaxPlayers { get; private set; }
        public bool LanServer { get; private set; }

        public ServerInformation(NetBuffer netBuffer)
        {
            if (netBuffer != null)
            {
                IpEndPoint = netBuffer.ReadIPEndPoint();
                if (IpEndPoint != null)
                {
                    ServerName = IpEndPoint.Address.ToString();
                    LanServer = true;
                }
            }
        }

        public ServerInformation(IPAddress ip, string name, string extra, int numPlayers, int maxPlayers)
        {
            IpEndPoint = new IPEndPoint(ip, 5565);
            ServerName = name;
            ServerExtra = extra;
            NumPlayers = numPlayers;
            MaxPlayers = maxPlayers;
            LanServer = false;
        }


        public string GetServerDesc()
        {
            string serverDesc = "";

            if (LanServer)
            {
                serverDesc = ServerName.Trim() + " ( LAN SERVER )";
            }
            else
            {
                serverDesc = ServerName.Trim() + " ( " + NumPlayers + " / " + MaxPlayers + " )";
                if (ServerExtra.Trim() != "")
                    serverDesc += " - " + ServerExtra.Trim();
            }

            return serverDesc;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this.GetType() != obj.GetType())
                return false;

            ServerInformation serverInfo = obj as ServerInformation;

            if (!IpEndPoint.Equals(serverInfo.IpEndPoint))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}