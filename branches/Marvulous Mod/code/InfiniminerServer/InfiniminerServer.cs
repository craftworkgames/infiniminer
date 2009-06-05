using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Lidgren.Network;
using Lidgren.Network.Xna;
using Microsoft.Xna.Framework;

namespace Infiniminer
{
    public class InfiniminerServer
    {
        InfiniminerNetServer netServer = null;
        BlockType[, ,] blockList = null;    // In game coordinates, where Y points up.
        PlayerTeam[, ,] blockCreatorTeam = null;
        const int MAPSIZE = 64;
        Dictionary<NetConnection, Player> playerList = new Dictionary<NetConnection, Player>();
        int lavaBlockCount = 0;
        uint oreFactor = 10;
        bool publicServer = false;
        uint maxPlayers = 16;
        string serverName = "Unnamed Server";
        bool sandboxMode = false;
        bool includeLava = true;
        DateTime lastServerListUpdate = DateTime.Now;
        List<string> banList = null;

        const int CONSOLE_SIZE = 30;
        List<string> consoleText = new List<string>();
        string consoleInput = "";

        bool keepRunning = true;

        uint teamCashRed = 0;
        uint teamCashBlue = 0;
        uint teamOreRed = 0;
        uint teamOreBlue = 0;

        uint winningCashAmount = 10000;
        PlayerTeam winningTeam = PlayerTeam.None;

        // Server restarting variables.
        DateTime restartTime = DateTime.Now;
        bool restartTriggered = false;

        public InfiniminerServer()
        {
            Console.SetWindowSize(1, 1);
            Console.SetBufferSize(80, CONSOLE_SIZE + 4);
            Console.SetWindowSize(80, CONSOLE_SIZE + 4);
        }

        public string GetExtraInfo()
        {
            string extraInfo = "";
            if (sandboxMode)
                extraInfo += "sandbox";
            else
                extraInfo += string.Format("{0:#.##k}", winningCashAmount / 1000);
            if (!includeLava)
                extraInfo += ", !lava";
            return extraInfo;
        }

        public void PublicServerListUpdate()
        {
            if (!publicServer)
                return;

            Dictionary<string, string> postDict = new Dictionary<string, string>();
            postDict["name"] = serverName;
            postDict["game"] = "INFINIMINER";
            postDict["player_count"] = "" + playerList.Keys.Count;
            postDict["player_capacity"] = "" + maxPlayers;
            postDict["extra"] = GetExtraInfo();

            try
            {
                HttpRequest.Post("http://apps.keithholman.net/post", postDict);
                ConsoleWrite("PUBLICLIST: UPDATING SERVER LISTING");
            }
            catch (Exception)
            {
                ConsoleWrite("PUBLICLIST: ERROR CONTACTING SERVER");
            }

            lastServerListUpdate = DateTime.Now;
        }

        public void ConsoleWrite(string text)
        {
            consoleText.Add(text);
            if (consoleText.Count > CONSOLE_SIZE)
                consoleText.RemoveAt(0);
            ConsoleRedraw();
        }

        public List<string> LoadBanList()
        {
            List<string> retList = new List<string>();

            try
            {
                FileStream file = new FileStream("banlist.txt", FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(file);
                string line = sr.ReadLine();
                while (line != null)
                {
                    retList.Add(line.Trim());
                    line = sr.ReadLine();
                }
                sr.Close();
                file.Close();
            }
            catch (Exception e)
            {
            }

            return retList;
        }

        public void SaveBanList(List<string> banList)
        {
            try
            {
                FileStream file = new FileStream("banlist.txt", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(file);
                foreach (string ip in banList)
                    sw.WriteLine(ip);
                sw.Close();
                file.Close();
            }
            catch (Exception e)
            {
            }
        }

        public void KickPlayer(string ip)
        {
            List<Player> playersToKick = new List<Player>();
            foreach (Player p in playerList.Values)
            {
                if (p.IP == ip)
                    playersToKick.Add(p);
            }
            foreach (Player p in playersToKick)
            {
                p.NetConn.Disconnect("", 0);
                p.Kicked = true;
            }
        }

        public void BanPlayer(string ip)
        {
            if (!banList.Contains(ip))
            {
                banList.Add(ip);
                SaveBanList(banList);
            }
        }

        public void ConsoleProcessInput()
        {
            string[] args = consoleInput.Split(" ".ToCharArray());

            ConsoleWrite("> " + consoleInput);

            switch (args[0].ToLower().Trim())
            {
                case "help":
                    {
                        ConsoleWrite("SERVER CONSOLE COMMANDS:");
                        ConsoleWrite(" players");
                        ConsoleWrite(" kick <ip>");
                        ConsoleWrite(" ban <ip>");
                        ConsoleWrite(" say <message>");
                        ConsoleWrite(" save <mapfile>");
                        ConsoleWrite(" load <mapfile>");
                        ConsoleWrite(" restart");
                        ConsoleWrite(" quit");
                    }
                    break;

                case "players":
                    {
                        foreach (Player p in playerList.Values)
                        {
                            string teamIdent = "";
                            if (p.Team == PlayerTeam.Red)
                                teamIdent = " (R)";
                            else if (p.Team == PlayerTeam.Blue)
                                teamIdent = " (B)";
                            ConsoleWrite(p.Handle + teamIdent + " - " + p.IP);
                        }
                    }
                    break;

                case "kick":
                    {
                        if (args.Length == 2)
                        {
                            KickPlayer(args[1]);
                        }
                    }
                    break;

                case "ban":
                    {
                        if (args.Length == 2)
                        {
                            KickPlayer(args[1]);
                            BanPlayer(args[1]);
                        }
                    }
                    break;

                case "quit":
                    {
                        keepRunning = false;
                    }
                    break;

                case "restart":
                    {
                        restartTriggered = true;
                        restartTime = DateTime.Now;
                    }
                    break;

                case "say":
                    {
                        string message = "SERVER: ";
                        for (int i = 1; i < args.Length; i++)
                            message += args[i] + " ";
                        SendServerMessage(message);
                    }
                    break;

                case "save":
                    {
                        if (args.Length >= 2)
                        {
                            SaveLevel(args[1]);
                        }
                    }
                    break;

                case "load":
                    {
                        if (args.Length >= 2)
                        {
                            try
                            {
                                FileStream fs = new FileStream(args[1], FileMode.Open);
                                StreamReader sr = new StreamReader(fs);
                                for (int x = 0; x < 64; x++)
                                    for (int y = 0; y < 64; y++)
                                        for (int z = 0; z < 64; z++)
                                        {
                                            string line = sr.ReadLine();
                                            string[] fileArgs = line.Split(",".ToCharArray());
                                            if (fileArgs.Length == 2)
                                            {
                                                blockList[x, y, z] = (BlockType)int.Parse(fileArgs[0], System.Globalization.CultureInfo.InvariantCulture);
                                                blockCreatorTeam[x, y, z] = (PlayerTeam)int.Parse(fileArgs[1], System.Globalization.CultureInfo.InvariantCulture);
                                            }
                                        }
                                sr.Close();
                                fs.Close();
                            }
                            catch (FileNotFoundException e)
                            {
                                ConsoleWrite("ERROR: File not found!");
                            }
                        }
                    }
                    break;
            }

            consoleInput = "";
            ConsoleRedraw();
        }

        public void SaveLevel(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            for (int x = 0; x < 64; x++)
                for (int y = 0; y < 64; y++)
                    for (int z = 0; z < 64; z++)
                        sw.WriteLine((byte)blockList[x, y, z] + "," + (byte)blockCreatorTeam[x, y, z]);
            sw.Close();
            fs.Close();
        }

        public void ConsoleRedraw()
        {
            Console.Clear();
            ConsoleDrawCentered("INFINIMINER SERVER " + InfiniminerGame.INFINIMINER_VERSION, 0);
            ConsoleDraw("================================================================================", 0, 1);
            for (int i = 0; i < consoleText.Count; i++)
                ConsoleDraw(consoleText[i], 0, i + 2);
            ConsoleDraw("================================================================================", 0, CONSOLE_SIZE + 2);
            ConsoleDraw("> " + consoleInput, 0, CONSOLE_SIZE + 3);
        }

        public void ConsoleDraw(string text, int x, int y)
        {
            Console.SetCursorPosition(x, y);
            Console.Write(text);
        }

        public void ConsoleDrawCentered(string text, int y)
        {
            Console.SetCursorPosition(40 - text.Length / 2, y);
            Console.Write(text);
        }

        List<string> beaconIDList = new List<string>();
        Dictionary<Vector3, Beacon> beaconList = new Dictionary<Vector3, Beacon>();
        Random randGen = new Random();
        public string _GenerateBeaconID()
        {
            string id = "K";
            for (int i = 0; i < 3; i++)
                id += (char)randGen.Next(48, 58);
            return id;
        }
        public string GenerateBeaconID()
        {
            string newId = _GenerateBeaconID();
            while (beaconIDList.Contains(newId))
                newId = _GenerateBeaconID();
            beaconIDList.Add(newId);
            return newId;
        }

        public void SetBlock(ushort x, ushort y, ushort z, BlockType blockType, PlayerTeam team)
        {
            if (x <= 0 || y <= 0 || z <= 0 || x >= MAPSIZE - 1 || y >= MAPSIZE - 1 || z >= MAPSIZE - 1)
                return;

            if (blockType == BlockType.BeaconRed || blockType == BlockType.BeaconBlue)
            {
                Beacon newBeacon = new Beacon();
                newBeacon.ID = GenerateBeaconID();
                newBeacon.Team = blockType == BlockType.BeaconRed ? PlayerTeam.Red : PlayerTeam.Blue;
                beaconList[new Vector3(x, y, z)] = newBeacon;
                SendSetBeacon(new Vector3(x, y+1, z), newBeacon.ID, newBeacon.Team);
            }

            if (blockType == BlockType.None && (blockList[x, y, z] == BlockType.BeaconRed || blockList[x, y, z] == BlockType.BeaconBlue))
            {
                if (beaconList.ContainsKey(new Vector3(x,y,z)))
                    beaconList.Remove(new Vector3(x,y,z));
                SendSetBeacon(new Vector3(x, y+1, z), "", PlayerTeam.None);
            }
            
            blockList[x, y, z] = blockType;
            blockCreatorTeam[x, y, z] = team;

            // x, y, z, type, all bytes
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.BlockSet);
            msgBuffer.Write((byte)x);
            msgBuffer.Write((byte)y);
            msgBuffer.Write((byte)z);
            msgBuffer.Write((byte)blockType);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableUnordered);

            if (blockType == BlockType.Lava)
                lavaBlockCount += 1;
            
            //ConsoleWrite("BLOCKSET: " + x + " " + y + " " + z + " " + blockType.ToString());
        }

        public bool Start()
        {
            // Read in from the config file.
            DatafileLoader dataFile = new DatafileLoader("server.config.txt");
            if (dataFile.Data.ContainsKey("winningcash"))
                winningCashAmount = uint.Parse(dataFile.Data["winningcash"], System.Globalization.CultureInfo.InvariantCulture);
            if (dataFile.Data.ContainsKey("includelava"))
                includeLava = bool.Parse(dataFile.Data["includelava"]);
            if (dataFile.Data.ContainsKey("orefactor"))
                oreFactor = uint.Parse(dataFile.Data["orefactor"], System.Globalization.CultureInfo.InvariantCulture);
            if (dataFile.Data.ContainsKey("maxplayers"))
                maxPlayers = Math.Min(32, uint.Parse(dataFile.Data["maxplayers"], System.Globalization.CultureInfo.InvariantCulture));
            if (dataFile.Data.ContainsKey("public"))
                publicServer = bool.Parse(dataFile.Data["public"]);
            if (dataFile.Data.ContainsKey("servername"))
                serverName = dataFile.Data["servername"];
            if (dataFile.Data.ContainsKey("sandbox"))
                sandboxMode = bool.Parse(dataFile.Data["sandbox"]);

            // Load the ban-list.
            banList = LoadBanList();

            // Create our block world, translating the coordinates out of the cave generator (where Z points down)
            BlockType[, ,] worldData = CaveGenerator.GenerateCaveSystem(MAPSIZE, includeLava, oreFactor);
            blockList = new BlockType[MAPSIZE, MAPSIZE, MAPSIZE];
            blockCreatorTeam = new PlayerTeam[MAPSIZE, MAPSIZE, MAPSIZE];
            for (ushort i = 0; i < MAPSIZE; i++)
                for (ushort j = 0; j < MAPSIZE; j++)
                    for (ushort k = 0; k < MAPSIZE; k++)
                    {
                        blockList[i, (ushort)(MAPSIZE - 1 - k), j] = worldData[i, j, k];
                        blockCreatorTeam[i, j, k] = PlayerTeam.None;
                    }

            // Initialize the server.
            NetConfiguration netConfig = new NetConfiguration("InfiniminerPlus");
            netConfig.MaxConnections = (int)maxPlayers;
            netConfig.Port = 5565;
            netServer = new InfiniminerNetServer(netConfig);
            netServer.SetMessageTypeEnabled(NetMessageType.ConnectionApproval, true);
            //netServer.SimulatedMinimumLatency = 0.1f;
            //netServer.SimulatedLatencyVariance = 0.05f;
            //netServer.SimulatedLoss = 0.1f;
            //netServer.SimulatedDuplicates = 0.05f;
            netServer.Start();

            // Initialize variables we'll use.
            NetBuffer msgBuffer = netServer.CreateBuffer();
            NetMessageType msgType;
            NetConnection msgSender;

            // Store the last time that we did a flow calculation.
            DateTime lastFlowCalc = DateTime.Now;

            // Calculate initial lava flows.
            ConsoleWrite("CALCULATING INITIAL LAVA FLOWS");
            for (int i=0; i<MAPSIZE*2; i++)
                DoLavaStuff();
            ConsoleWrite("TOTAL LAVA BLOCKS = " + lavaBlockCount);

            // Send the initial server list update.
            PublicServerListUpdate();

            // Main server loop!
            ConsoleWrite("SERVER READY");
            while (keepRunning)
            {
                // Process any messages that are here.
                while (netServer.ReadMessage(msgBuffer, out msgType, out msgSender))
                {
                    switch (msgType)
                    {
                        case NetMessageType.ConnectionApproval:
                            {
                                Player newPlayer = new Player(msgSender, null);
                                newPlayer.Handle = InfiniminerGame.Sanitize(msgBuffer.ReadString()).Trim();
                                if (newPlayer.Handle.Length == 0)
                                {
                                    newPlayer.Handle = "Player";
                                }

                                string clientVersion = msgBuffer.ReadString();
                                if (clientVersion != InfiniminerGame.INFINIMINER_VERSION)
                                {
                                    msgSender.Disapprove("VER;" + InfiniminerGame.INFINIMINER_VERSION);
                                }
                                else if (banList.Contains(newPlayer.IP))
                                {
                                    msgSender.Disapprove("BAN;");
                                }
                                else
                                {
                                    playerList[msgSender] = newPlayer;
                                    this.netServer.SanityCheck(msgSender);
                                    msgSender.Approve();
                                }
                            }
                            break;

                        case NetMessageType.StatusChanged:
                            {
                                if (!this.playerList.ContainsKey(msgSender))
                                {
                                    break;
                                }

                                Player player = playerList[msgSender];

                                if (msgSender.Status == NetConnectionStatus.Connected)
                                {
                                    ConsoleWrite("CONNECT: " + playerList[msgSender].Handle);
                                    SendCurrentMap(msgSender);
                                    SendPlayerJoined(player);
                                    PublicServerListUpdate();
                                }

                                else if (msgSender.Status == NetConnectionStatus.Disconnected)
                                {
                                    ConsoleWrite("DISCONNECT: " + playerList[msgSender].Handle);
                                    SendPlayerLeft(player, player.Kicked ? "WAS KICKED FROM THE GAME!" : "HAS ABANDONED THEIR DUTIES!");
                                    if (playerList.ContainsKey(msgSender))
                                        playerList.Remove(msgSender);
                                    PublicServerListUpdate();
                                }
                            }
                            break;

                        case NetMessageType.Data:
							{
                                if (!this.playerList.ContainsKey(msgSender))
                                {
                                    break;
                                }

                                Player player = playerList[msgSender];
                                InfiniminerMessage dataType = (InfiniminerMessage)msgBuffer.ReadByte();
                                switch (dataType)
                                {
                                    case InfiniminerMessage.ChatMessage:
                                        {
                                            // Read the data from the packet.
                                            ChatMessageType chatType = (ChatMessageType)msgBuffer.ReadByte();
                                            string chatString = InfiniminerGame.Sanitize(msgBuffer.ReadString());
                                            ConsoleWrite("CHAT: (" + player.Handle + ") " + chatString);

                                            // Append identifier information.
                                            if (chatType == ChatMessageType.SayAll)
                                                chatString = player.Handle + " (ALL): " + chatString;
                                            else
                                                chatString = player.Handle + " (TEAM): " + chatString;

                                            // Construct the message packet.
                                            NetBuffer chatPacket = netServer.CreateBuffer();
                                            chatPacket.Write((byte)InfiniminerMessage.ChatMessage);
                                            chatPacket.Write((byte)((player.Team == PlayerTeam.Red) ? ChatMessageType.SayRedTeam : ChatMessageType.SayBlueTeam));
                                            chatPacket.Write(chatString);

                                            // Send the packet to people who should recieve it.
                                            foreach (Player p in playerList.Values)
                                            {
                                                if (chatType == ChatMessageType.SayAll ||
                                                    chatType == ChatMessageType.SayBlueTeam && p.Team == PlayerTeam.Blue ||
                                                    chatType == ChatMessageType.SayRedTeam && p.Team == PlayerTeam.Red)
                                                    if (p.NetConn.Status == NetConnectionStatus.Connected)
                                                        netServer.SendMessage(chatPacket, p.NetConn, NetChannel.ReliableInOrder3);
                                            }
                                        }
                                        break;

                                    case InfiniminerMessage.UseTool:
                                        {
                                            Vector3 playerPosition = msgBuffer.ReadVector3();
                                            Vector3 playerHeading = msgBuffer.ReadVector3();
                                            PlayerTools playerTool = (PlayerTools)msgBuffer.ReadByte();
                                            BlockType blockType = (BlockType)msgBuffer.ReadByte();
                                            switch (playerTool)
                                            {
                                                case PlayerTools.Pickaxe:
                                                    UsePickaxe(player, playerPosition, playerHeading);
                                                    break;
                                                case PlayerTools.ConstructionGun:
                                                    UseConstructionGun(player, playerPosition, playerHeading, blockType);
                                                    break;
                                                case PlayerTools.DeconstructionGun:
                                                    UseDeconstructionGun(player, playerPosition, playerHeading);
                                                    break;
                                                case PlayerTools.ProspectingRadar:
                                                    UseSignPainter(player, playerPosition, playerHeading);
                                                    break;
                                                case PlayerTools.Detonator:
                                                    UseDetonator(player);
                                                    break;
                                            }
                                        }
                                        break;

                                    case InfiniminerMessage.SelectClass:
                                        {
                                            PlayerClass playerClass = (PlayerClass)msgBuffer.ReadByte();
                                            ConsoleWrite("SELECT_CLASS: " + player.Handle + ", " + playerClass.ToString());
                                            switch (playerClass)
                                            {
                                                case PlayerClass.Engineer:
                                                    player.OreMax = 350;
                                                    player.WeightMax = 4;
                                                    break;
                                                case PlayerClass.Miner:
                                                    player.OreMax = 200;
                                                    player.WeightMax = 8;
                                                    break;
                                                case PlayerClass.Prospector:
                                                    player.OreMax = 200;
                                                    player.WeightMax = 4;
                                                    break;
                                                case PlayerClass.Sapper:
                                                    player.OreMax = 200;
                                                    player.WeightMax = 4;
                                                    break;
                                            }
                                            SendResourceUpdate(player);
                                        }
                                        break;

                                    case InfiniminerMessage.PlayerSetTeam:
                                        {
                                            PlayerTeam playerTeam = (PlayerTeam)msgBuffer.ReadByte();
                                            ConsoleWrite("SELECT_TEAM: " + player.Handle + ", " + playerTeam.ToString());
                                            player.Team = playerTeam;
                                            SendResourceUpdate(player);
                                            SendPlayerSetTeam(player);
                                        }
                                        break;

                                    case InfiniminerMessage.PlayerDead:
                                        {
                                            ConsoleWrite("PLAYER_DEAD: " + player.Handle);
                                            player.Ore = 0;
                                            player.Cash = 0;
                                            player.Weight = 0;
                                            player.Alive = false;
                                            SendResourceUpdate(player);
                                            SendPlayerDead(player);

                                            string deathMessage = msgBuffer.ReadString();
                                            if (deathMessage != "")
                                            {
                                                msgBuffer = netServer.CreateBuffer();
                                                msgBuffer.Write((byte)InfiniminerMessage.ChatMessage);
                                                msgBuffer.Write((byte)(player.Team == PlayerTeam.Red ? ChatMessageType.SayRedTeam : ChatMessageType.SayBlueTeam));
                                                msgBuffer.Write(player.Handle + " " + deathMessage);
                                                foreach (NetConnection netConn in playerList.Keys)
                                                    if (netConn.Status == NetConnectionStatus.Connected)
                                                        netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder3);
                                            }
                                        }
                                        break;

                                    case InfiniminerMessage.PlayerAlive:
                                        {
                                            ConsoleWrite("PLAYER_ALIVE: " + player.Handle);
                                            player.Ore = 0;
                                            player.Cash = 0;
                                            player.Weight = 0;
                                            player.Alive = true;
                                            SendResourceUpdate(player);
                                            SendPlayerAlive(player);
                                        }
                                        break;

                                    case InfiniminerMessage.PlayerUpdate:
                                        {
                                            player.Position = msgBuffer.ReadVector3();
                                            player.Heading = msgBuffer.ReadVector3();
                                            player.Tool = (PlayerTools)msgBuffer.ReadByte();
                                            player.UsingTool = msgBuffer.ReadBoolean();
                                            SendPlayerUpdate(player);
                                        }
                                        break;

                                    case InfiniminerMessage.DepositOre:
                                        {
                                            DepositOre(player);
                                            foreach (Player p in playerList.Values)
                                                SendResourceUpdate(p);
                                        }
                                        break;

                                    case InfiniminerMessage.WithdrawOre:
                                        {
                                            WithdrawOre(player);
                                            foreach (Player p in playerList.Values)
                                                SendResourceUpdate(p);
                                        }
                                        break;

                                    case InfiniminerMessage.PlayerPing:
                                        {
                                            SendPlayerPing((uint)msgBuffer.ReadInt32());
                                        }
                                        break;

                                    case InfiniminerMessage.PlaySound:
                                        {
                                            InfiniminerSound sound = (InfiniminerSound)msgBuffer.ReadByte();
                                            Vector3 position = msgBuffer.ReadVector3();
                                            PlaySound(sound, position);
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }

                // Time to send a new server update?
                TimeSpan updateTimeSpan = DateTime.Now - lastServerListUpdate;
                if (updateTimeSpan.TotalMinutes > 5)
                    PublicServerListUpdate();

                // Check for players who are in the zone to deposit.
                DepositForPlayers();

                // Is it time to do a lava calculation? If so, do it!
                TimeSpan timeSpan = DateTime.Now - lastFlowCalc;
                if (timeSpan.TotalMilliseconds > 500)
                {
                    DoLavaStuff();
                    lastFlowCalc = DateTime.Now;
                }

                // Handle console keypresses.
                while (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey();
                    if (keyInfo.Key == ConsoleKey.Enter)
                        ConsoleProcessInput();
                    else if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (consoleInput.Length > 0)
                            consoleInput = consoleInput.Substring(0, consoleInput.Length - 1);
                        ConsoleRedraw();
                    }
                    else
                    {
                        consoleInput += keyInfo.KeyChar;
                        ConsoleRedraw();
                    }
                }

                // Is the game over?
                if (winningTeam != PlayerTeam.None && !restartTriggered)
                {
                    BroadcastGameOver();
                    restartTriggered = true;
                    restartTime = DateTime.Now.AddSeconds(10);
                }

                // Restart the server?
                if (restartTriggered && DateTime.Now > restartTime)
                {
                    SaveLevel("autosave_" + (UInt64)DateTime.Now.ToBinary() + ".lvl");
                    netServer.Shutdown("The server is restarting.");
                    return true;
                }

                // Pass control over to waiting threads.
                Thread.Sleep(1);
            }

            netServer.Shutdown("The server was terminated.");
            return false;
        }

        public void DepositForPlayers()
        {
            foreach (Player p in playerList.Values)
            {
                if (p.Position.Y > 64 - InfiniminerGame.GROUND_LEVEL)
                    DepositCash(p);
            }

            if (sandboxMode)
                return;
            if (teamCashBlue >= winningCashAmount && winningTeam == PlayerTeam.None)
                winningTeam = PlayerTeam.Blue;
            if (teamCashRed >= winningCashAmount && winningTeam == PlayerTeam.None)
                winningTeam = PlayerTeam.Red;
        }

        public void DoLavaStuff()
        {
            bool[, ,] flowSleep = new bool[MAPSIZE, MAPSIZE, MAPSIZE]; //if true, do not calculate this turn

            for (ushort i = 0; i < MAPSIZE; i++)
                for (ushort j = 0; j < MAPSIZE; j++)
                    for (ushort k = 0; k < MAPSIZE; k++)
                        flowSleep[i, j, k] = false;

            for (ushort i = 0; i < MAPSIZE; i++)
                for (ushort j = 0; j < MAPSIZE; j++)
                    for (ushort k = 0; k < MAPSIZE; k++)
                        if (blockList[i, j, k] == BlockType.Lava && !flowSleep[i, j, k])
                        {
                            // RULES FOR LAVA EXPANSION:
                            // if the block below is lava, do nothing
                            // if the block below is empty space, add lava there
                            // if the block below is something solid, add lava to the sides
                            BlockType typeBelow = (j == 0) ? BlockType.Lava : blockList[i, j - 1, k];
                            if (typeBelow == BlockType.None)
                            {
                                if (j > 0)
                                {
                                    SetBlock(i, (ushort)(j - 1), k, BlockType.Lava, PlayerTeam.None);
                                    flowSleep[i, j - 1, k] = true;
                                }
                            }
                            else if (typeBelow != BlockType.Lava)
                            {
                                if (i > 0 && blockList[i-1, j, k] == BlockType.None)
                                {
                                    SetBlock((ushort)(i - 1), j, k, BlockType.Lava, PlayerTeam.None);
                                    flowSleep[i - 1, j, k] = true;
                                }
                                if (k > 0 && blockList[i, j, k-1] == BlockType.None)
                                {
                                    SetBlock(i, j, (ushort)(k - 1), BlockType.Lava, PlayerTeam.None);
                                    flowSleep[i, j, k - 1] = true;
                                }
                                if (i < MAPSIZE - 1 && blockList[i + 1, j, k] == BlockType.None)
                                {
                                    SetBlock((ushort)(i + 1), j, k, BlockType.Lava, PlayerTeam.None);
                                    flowSleep[i + 1, j, k] = true;
                                }
                                if (k < MAPSIZE - 1 && blockList[i, j, k+1] == BlockType.None)
                                {
                                    SetBlock(i, j, (ushort)(k + 1), BlockType.Lava, PlayerTeam.None);
                                    flowSleep[i, j, k + 1] = true;
                                }
                            }
                        }
        }

        public BlockType BlockAtPoint(Vector3 point)
        {
            ushort x = (ushort)point.X;
            ushort y = (ushort)point.Y;
            ushort z = (ushort)point.Z;
            if (x <= 0 || y <= 0 || z <= 0 || x >= MAPSIZE - 1 || y >= MAPSIZE - 1 || z >= MAPSIZE - 1)
                return BlockType.None;
            return blockList[x, y, z];
        }

        public bool RayCollision(Vector3 startPosition, Vector3 rayDirection, float distance, int searchGranularity, ref Vector3 hitPoint, ref Vector3 buildPoint)
        {
            Vector3 testPos = startPosition;
            Vector3 buildPos = startPosition;
            for (int i = 0; i < searchGranularity; i++)
            {
                testPos += rayDirection * distance / searchGranularity;
                BlockType testBlock = BlockAtPoint(testPos);
                if (testBlock != BlockType.None)
                {
                    hitPoint = testPos;
                    buildPoint = buildPos;
                    return true;
                }
                buildPos = testPos;
            }
            return false;
        }

        public void UsePickaxe(Player player, Vector3 playerPosition, Vector3 playerHeading)
        {
            player.QueueAnimationBreak = true;
            
            // Figure out what we're hitting.
            Vector3 hitPoint = Vector3.Zero;
            Vector3 buildPoint = Vector3.Zero;
            if (!RayCollision(playerPosition, playerHeading, 2, 10, ref hitPoint, ref buildPoint))
                return;
            ushort x = (ushort)hitPoint.X;
            ushort y = (ushort)hitPoint.Y;
            ushort z = (ushort)hitPoint.Z;

            // Figure out what the result is.
            bool removeBlock = false;
            uint giveOre = 0;
            uint giveCash = 0;
            uint giveWeight = 0;
            InfiniminerSound sound = InfiniminerSound.DigDirt;

            switch (BlockAtPoint(hitPoint))
            {
                case BlockType.Dirt:
                case BlockType.DirtSign:
                    removeBlock = true;
                    sound = InfiniminerSound.DigDirt;
                    break;

                case BlockType.Ore:
                    removeBlock = true;
                    giveOre = 20;
                    sound = InfiniminerSound.DigMetal;
                    break;

                case BlockType.Gold:
                    removeBlock = true;
                    giveWeight = 1;
                    giveCash = 100;
                    sound = InfiniminerSound.DigMetal;
                    break;

                case BlockType.Diamond:
                    removeBlock = true;
                    giveWeight = 1;
                    giveCash = 1000;
                    sound = InfiniminerSound.DigMetal;
                    break;
            }

            if (giveOre > 0)
            {
                if (player.Ore < player.OreMax)
                {
                    player.Ore = Math.Min(player.Ore + giveOre, player.OreMax);
                    SendResourceUpdate(player);
                }
            }

            if (giveWeight > 0)
            {
                if (player.Weight < player.WeightMax)
                {
                    player.Weight = Math.Min(player.Weight + giveWeight, player.WeightMax);
                    player.Cash += giveCash;
                    SendResourceUpdate(player);
                }
                else
                    removeBlock = false;
            }

            if (removeBlock)
            {
                SetBlock(x, y, z, BlockType.None, PlayerTeam.None);
                PlaySound(sound, player.Position);
            }
        }

        //private bool LocationNearBase(ushort x, ushort y, ushort z)
        //{
        //    for (int i=0; i<MAPSIZE; i++)
        //        for (int j=0; j<MAPSIZE; j++)
        //            for (int k = 0; k < MAPSIZE; k++)
        //                if (blockList[i, j, k] == BlockType.HomeBlue || blockList[i, j, k] == BlockType.HomeRed)
        //                {
        //                    double dist = Math.Sqrt(Math.Pow(x - i, 2) + Math.Pow(y - j, 2) + Math.Pow(z - k, 2));
        //                    if (dist < 3)
        //                        return true;
        //                }
        //    return false;
        //}

        public void UseConstructionGun(Player player, Vector3 playerPosition, Vector3 playerHeading, BlockType blockType)
        {
            bool actionFailed = false;

            // If there's no surface within range, bail.
            Vector3 hitPoint = Vector3.Zero;
            Vector3 buildPoint = Vector3.Zero;
            if (!RayCollision(playerPosition, playerHeading, 6, 25, ref hitPoint, ref buildPoint))
                actionFailed = true;

            // If the block is too expensive, bail.
            uint blockCost = BlockInformation.GetCost(blockType);
            if (sandboxMode && blockCost <= player.OreMax)
                blockCost = 0;
            if (blockCost > player.Ore)
                actionFailed = true;

            // If there's someone there currently, bail.
            ushort x = (ushort)buildPoint.X;
            ushort y = (ushort)buildPoint.Y;
            ushort z = (ushort)buildPoint.Z;
            foreach (Player p in playerList.Values)
            {
                if ((int)p.Position.X == x && (int)p.Position.Z == z && ((int)p.Position.Y == y || (int)p.Position.Y - 1 == y))
                    actionFailed = true;
            }

            // If it's out of bounds, bail.
            if (x <= 0 || y <= 0 || z <= 0 || x >= MAPSIZE - 1 || y >= MAPSIZE - 1 || z >= MAPSIZE - 1)
                actionFailed = true;

            // If it's near a base, bail.
            //if (LocationNearBase(x, y, z))
            //    actionFailed = true;

            // If it's lava, don't let them build off of lava.
            if (blockList[(ushort)hitPoint.X, (ushort)hitPoint.Y, (ushort)hitPoint.Z] == BlockType.Lava)
                actionFailed = true;

            if (actionFailed)
            {
                // Decharge the player's gun.
                TriggerConstructionGunAnimation(player, -0.2f);
            }
            else
            {
                // Fire the player's gun.
                TriggerConstructionGunAnimation(player, 0.5f);

                // Build the block.
                SetBlock(x, y, z, blockType, player.Team);
                player.Ore -= blockCost;
                SendResourceUpdate(player);

                // Play the sound.
                PlaySound(InfiniminerSound.ConstructionGun, player.Position);

                // If it's an explosive block, add it to our list.
                if (blockType == BlockType.Explosive)
                    player.ExplosiveList.Add(buildPoint);
            }            
        }

        public void UseDeconstructionGun(Player player, Vector3 playerPosition, Vector3 playerHeading)
        {
            bool actionFailed = false;

            // If there's no surface within range, bail.
            Vector3 hitPoint = Vector3.Zero;
            Vector3 buildPoint = Vector3.Zero;
            if (!RayCollision(playerPosition, playerHeading, 6, 25, ref hitPoint, ref buildPoint))
                actionFailed = true;
            ushort x = (ushort)hitPoint.X;
            ushort y = (ushort)hitPoint.Y;
            ushort z = (ushort)hitPoint.Z;

            // If this is another team's block, bail.
            if (blockCreatorTeam[x, y, z] != player.Team)
                actionFailed = true;

            BlockType blockType = blockList[x, y, z];
            if (!(blockType == BlockType.SolidBlue ||
                blockType == BlockType.SolidRed ||
                blockType == BlockType.BankBlue ||
                blockType == BlockType.BankRed ||
                blockType == BlockType.Jump ||
                blockType == BlockType.Ladder ||
                blockType == BlockType.Road ||
                blockType == BlockType.Shock ||
                blockType == BlockType.BeaconRed ||
                blockType == BlockType.BeaconBlue ||
                blockType == BlockType.TransBlue ||
                blockType == BlockType.TransRed))
                actionFailed = true;

            if (actionFailed)
            {
                // Decharge the player's gun.
                TriggerConstructionGunAnimation(player, -0.2f);
            }
            else
            {
                // Fire the player's gun.
                TriggerConstructionGunAnimation(player, 0.5f);

                // Remove the block.
                SetBlock(x, y, z, BlockType.None, PlayerTeam.None);
                PlaySound(InfiniminerSound.ConstructionGun, player.Position);
            }
        }

        public void TriggerConstructionGunAnimation(Player player, float animationValue)
        {
            if (player.NetConn.Status != NetConnectionStatus.Connected)
                return;

            // ore, cash, weight, max ore, max weight, team ore, red cash, blue cash, all uint
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.TriggerConstructionGunAnimation);
            msgBuffer.Write(animationValue);
            netServer.SendMessage(msgBuffer, player.NetConn, NetChannel.ReliableInOrder1);
        }

        public void UseSignPainter(Player player, Vector3 playerPosition, Vector3 playerHeading)
        {
            // If there's no surface within range, bail.
            Vector3 hitPoint = Vector3.Zero;
            Vector3 buildPoint = Vector3.Zero;
            if (!RayCollision(playerPosition, playerHeading, 4, 25, ref hitPoint, ref buildPoint))
                return;
            ushort x = (ushort)hitPoint.X;
            ushort y = (ushort)hitPoint.Y;
            ushort z = (ushort)hitPoint.Z;

            if (blockList[x, y, z] == BlockType.Dirt)
            {
                SetBlock(x, y, z, BlockType.DirtSign, PlayerTeam.None);
                PlaySound(InfiniminerSound.ConstructionGun, player.Position);
            }
            else if (blockList[x, y, z] == BlockType.DirtSign)
            {
                SetBlock(x, y, z, BlockType.Dirt, PlayerTeam.None);
                PlaySound(InfiniminerSound.ConstructionGun, player.Position);
            }
        }

        public void DetonateAtPoint(int x, int y, int z)
        {
            // Remove the block that is detonating.
            SetBlock((ushort)(x), (ushort)(y), (ushort)(z), BlockType.None, PlayerTeam.None);

            // Remove this from any explosive lists it may be in.
            foreach (Player p in playerList.Values)
                p.ExplosiveList.Remove(new Vector3(x, y, z));

            // Detonate the block.
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        // Check that this is a sane block position.
                        if (x + dx <= 0 || y + dy <= 0 || z + dz <= 0 || x + dx >= MAPSIZE - 1 || y + dy >= MAPSIZE - 1 || z + dz >= MAPSIZE - 1)
                            continue;

                        // Chain reactions!
                        if (blockList[x + dx, y + dy, z + dz] == BlockType.Explosive)
                            DetonateAtPoint(x + dx, y + dy, z + dz);

                        // Detonation of normal blocks.
                        bool destroyBlock = false;
                        switch (blockList[x + dx, y + dy, z + dz])
                        {
                            case BlockType.Rock:
                            case BlockType.Dirt:
                            case BlockType.DirtSign:
                            case BlockType.Ore:
                            case BlockType.SolidRed:
                            case BlockType.SolidBlue:
                            case BlockType.TransRed:
                            case BlockType.TransBlue:
                            case BlockType.Ladder:
                            case BlockType.Shock:
                            case BlockType.Jump:
                            case BlockType.Explosive:
                            case BlockType.Lava:
                            case BlockType.Road:
                                destroyBlock = true;
                                break;
                        }
                        if (destroyBlock)
                            SetBlock((ushort)(x + dx), (ushort)(y + dy), (ushort)(z + dz), BlockType.None, PlayerTeam.None);
                    }

            // Send off the explosion to clients.
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.TriggerExplosion);
            msgBuffer.Write(new Vector3(x,y,z));
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableUnordered);
        }

        public void UseDetonator(Player player)
        {
            while (player.ExplosiveList.Count > 0)
            {
                Vector3 blockPos = player.ExplosiveList[0];
                ushort x = (ushort)blockPos.X;
                ushort y = (ushort)blockPos.Y;
                ushort z = (ushort)blockPos.Z;

                if (blockList[x, y, z] != BlockType.Explosive)
                    player.ExplosiveList.RemoveAt(0);
                else
                    DetonateAtPoint(x, y, z);
            }
        }

        public void DepositOre(Player player)
        {
            uint depositAmount = Math.Min(50, player.Ore);
            player.Ore -= depositAmount;
            if (player.Team == PlayerTeam.Red)
                teamOreRed = Math.Min(teamOreRed + depositAmount, 9999);
            else
                teamOreBlue = Math.Min(teamOreBlue + depositAmount, 9999);
        }

        public void WithdrawOre(Player player)
        {
            if (player.Team == PlayerTeam.Red)
            {
                uint withdrawAmount = Math.Min(player.OreMax - player.Ore, Math.Min(50, teamOreRed));
                player.Ore += withdrawAmount;
                teamOreRed -= withdrawAmount;
            }
            else
            {
                uint withdrawAmount = Math.Min(player.OreMax - player.Ore, Math.Min(50, teamOreBlue));
                player.Ore += withdrawAmount;
                teamOreBlue -= withdrawAmount;
            }
        }

        public void DepositCash(Player player)
        {
            if (player.Cash <= 0)
                return;

            player.Score += player.Cash;

            if (!sandboxMode)
            {
                if (player.Team == PlayerTeam.Red)
                    teamCashRed += player.Cash;
                else
                    teamCashBlue += player.Cash;
                SendServerMessage("SERVER: " + player.Handle + " HAS EARNED $" + player.Cash + " FOR THE " + GetTeamName(player.Team) + " TEAM!");
            }

            PlaySound(InfiniminerSound.CashDeposit, player.Position);
            ConsoleWrite("DEPOSIT_CASH: " + player.Handle + ", " + player.Cash);
            
            player.Cash = 0;
            player.Weight = 0;

            foreach (Player p in playerList.Values)
                SendResourceUpdate(p);
        }

        public string GetTeamName(PlayerTeam team)
        {
            switch (team)
            {
                case PlayerTeam.Red:
                    return "RED";
                case PlayerTeam.Blue:
                    return "BLUE";
            }
            return "";
        }

        public void SendServerMessage(string message)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.ChatMessage);
            msgBuffer.Write((byte)ChatMessageType.SayAll);
            msgBuffer.Write(message);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder3);
        }

        // Lets a player know about their resources.
        public void SendResourceUpdate(Player player)
        {
            if (player.NetConn.Status != NetConnectionStatus.Connected)
                return;

            // ore, cash, weight, max ore, max weight, team ore, red cash, blue cash, all uint
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.ResourceUpdate);
            msgBuffer.Write((uint)player.Ore);
            msgBuffer.Write((uint)player.Cash);
            msgBuffer.Write((uint)player.Weight);
            msgBuffer.Write((uint)player.OreMax);
            msgBuffer.Write((uint)player.WeightMax);
            msgBuffer.Write((uint)(player.Team == PlayerTeam.Red ? teamOreRed : teamOreBlue));
            msgBuffer.Write((uint)teamCashRed);
            msgBuffer.Write((uint)teamCashBlue);
            netServer.SendMessage(msgBuffer, player.NetConn, NetChannel.ReliableInOrder1);
        }

        public void SendCurrentMap(NetConnection client)
        {
            Debug.Assert(MAPSIZE == 64, "The BlockBulkTransfer message requires a map size of 64.");
            
            for (byte x = 0; x < MAPSIZE; x++)
                for (byte y=0; y<MAPSIZE; y+=16)
                {
                    NetBuffer msgBuffer = netServer.CreateBuffer();
                    msgBuffer.Write((byte)InfiniminerMessage.BlockBulkTransfer);
                    msgBuffer.Write(x);
                    msgBuffer.Write(y);
                    for (byte dy=0; dy<16; dy++)
                        for (byte z = 0; z < MAPSIZE; z++)
                            msgBuffer.Write((byte)(blockList[x, y+dy, z]));
                    if (client.Status == NetConnectionStatus.Connected)
                        netServer.SendMessage(msgBuffer, client, NetChannel.ReliableUnordered);
                }
        }

        public void SendPlayerPing(uint playerId)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerPing);
            msgBuffer.Write(playerId);

            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableUnordered);
        }

        public void SendPlayerUpdate(Player player)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerUpdate);
            msgBuffer.Write((uint)player.ID);
            msgBuffer.Write(player.Position);
            msgBuffer.Write(player.Heading);
            msgBuffer.Write((byte)player.Tool);

            if (player.QueueAnimationBreak)
            {
                player.QueueAnimationBreak = false;
                msgBuffer.Write(false);
            }
            else
                msgBuffer.Write(player.UsingTool);

            msgBuffer.Write((ushort)player.Score / 100);

            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.UnreliableInOrder1);
        }

        public void SendSetBeacon(Vector3 position, string text, PlayerTeam team)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.SetBeacon);
            msgBuffer.Write(position);
            msgBuffer.Write(text);
            msgBuffer.Write((byte)team);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder2);
        }

        public void SendPlayerJoined(Player player)
        {
            NetBuffer msgBuffer;

            // Let this player know about other players.
            foreach (Player p in playerList.Values)
            {
                msgBuffer = netServer.CreateBuffer();
                msgBuffer.Write((byte)InfiniminerMessage.PlayerJoined);
                msgBuffer.Write((uint)p.ID);
                msgBuffer.Write(p.Handle);
                msgBuffer.Write(p == player);
                msgBuffer.Write(p.Alive);
                if (player.NetConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, player.NetConn, NetChannel.ReliableInOrder2);

                msgBuffer = netServer.CreateBuffer();
                msgBuffer.Write((byte)InfiniminerMessage.PlayerSetTeam);
                msgBuffer.Write((uint)p.ID);
                msgBuffer.Write((byte)p.Team);
                if (player.NetConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, player.NetConn, NetChannel.ReliableInOrder2);
            }

            // Let this player know about all placed beacons.
            foreach (KeyValuePair<Vector3, Beacon> bPair in beaconList)
            {
                Vector3 position = bPair.Key;
                position.Y += 1; // beacon is shown a block below its actually position to make altitude show up right
                msgBuffer = netServer.CreateBuffer();
                msgBuffer.Write((byte)InfiniminerMessage.SetBeacon);
                msgBuffer.Write(position);
                msgBuffer.Write(bPair.Value.ID);
                msgBuffer.Write((byte)bPair.Value.Team);
                netServer.SendMessage(msgBuffer, player.NetConn, NetChannel.ReliableInOrder2);
            }

            // Let other players know about this player.
            msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerJoined);
            msgBuffer.Write((uint)player.ID);
            msgBuffer.Write(player.Handle);
            msgBuffer.Write(false);
            msgBuffer.Write(player.Alive);

            foreach (NetConnection netConn in playerList.Keys)
                if (netConn != player.NetConn && netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder2);

            // Send this out just incase someone is joining at the last minute.
            if (winningTeam != PlayerTeam.None)
                BroadcastGameOver();

            // Send out a chat message.
            msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.ChatMessage);
            msgBuffer.Write((byte)ChatMessageType.SayAll);
            msgBuffer.Write(player.Handle + " HAS JOINED THE ADVENTURE!");
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder3);
        }

        public void BroadcastGameOver()
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.GameOver);
            msgBuffer.Write((byte)winningTeam);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableUnordered);     
        }

        public void SendPlayerLeft(Player player, string reason)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerLeft);
            msgBuffer.Write((uint)player.ID);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn != player.NetConn && netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder2);

            // Send out a chat message.
            msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.ChatMessage);
            msgBuffer.Write((byte)ChatMessageType.SayAll);
            msgBuffer.Write(player.Handle + " " + reason);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder3);
        }

        public void SendPlayerSetTeam(Player player)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerSetTeam);
            msgBuffer.Write((uint)player.ID);
            msgBuffer.Write((byte)player.Team);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder2);
        }

        public void SendPlayerDead(Player player)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerDead);
            msgBuffer.Write((uint)player.ID);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder2);
        }

        public void SendPlayerAlive(Player player)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerAlive);
            msgBuffer.Write((uint)player.ID);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableInOrder2);
        }

        public void PlaySound(InfiniminerSound sound, Vector3 position)
        {
            NetBuffer msgBuffer = netServer.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlaySound);
            msgBuffer.Write((byte)sound);
            msgBuffer.Write(true);
            msgBuffer.Write(position);
            foreach (NetConnection netConn in playerList.Keys)
                if (netConn.Status == NetConnectionStatus.Connected)
                    netServer.SendMessage(msgBuffer, netConn, NetChannel.ReliableUnordered);
        }
    }
}
