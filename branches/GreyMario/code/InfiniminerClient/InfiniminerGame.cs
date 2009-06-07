using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using Lidgren.Network;
using Lidgren.Network.Xna;

namespace Infiniminer
{
    public class InfiniminerGame : StateMasher.StateMachine
    {
        double timeSinceLastUpdate = 0;
        string playerHandle = "Player";
        float volumeLevel = 1.0f;
        NetBuffer msgBuffer = null;
        Song songTitle = null;

        public bool RenderPretty = true;
        public bool DrawFrameRate = false;
        public bool InvertMouseYAxis = false;
        public bool NoSound = false;

        public const string INFINIMINER_VERSION = "v1.5";
        public const int GROUND_LEVEL = 8;
        public static Color IM_BLUE = new Color(80, 150, 255);
        public static Color IM_RED = new Color(222, 24, 24);

        public InfiniminerGame(string[] args)
        {
        }

        public static string Sanitize(string input)
        {
            string output = "";
            for (int i = 0; i < input.Length; i++)
            {
                char c = (char)input[i];
                if (c >= 32 && c <= 126)
                    output += c;
            }
            return output;
        }

        public void JoinGame(IPEndPoint serverEndPoint)
        {
            // Clear out the map load progress indicator.
            propertyBag.mapLoadProgress = new bool[64,64];
            for (int i = 0; i < 64; i++)
                for (int j=0; j<64; j++)
                    propertyBag.mapLoadProgress[i,j] = false;

            // Create our connect message.
            NetBuffer connectBuffer = propertyBag.netClient.CreateBuffer();
            connectBuffer.Write(propertyBag.playerHandle);
            connectBuffer.Write(INFINIMINER_VERSION);

            // Connect to the server.
            propertyBag.netClient.Connect(serverEndPoint, connectBuffer.ToArray());
        }

        public List<ServerInformation> EnumerateServers(float discoveryTime)
        {
            List<ServerInformation> serverList = new List<ServerInformation>();
            
            // Discover local servers.
            propertyBag.netClient.DiscoverLocalServers(5565);
            NetBuffer msgBuffer = propertyBag.netClient.CreateBuffer();
            NetMessageType msgType;
            float timeTaken = 0;
            while (timeTaken < discoveryTime)
            {
                while (propertyBag.netClient.ReadMessage(msgBuffer, out msgType))
                {
                    if (msgType == NetMessageType.ServerDiscovered)
                    {
                        bool serverFound = false;
                        ServerInformation serverInfo = new ServerInformation(msgBuffer);
                        foreach (ServerInformation si in serverList)
                            if (si.Equals(serverInfo))
                                serverFound = true;
                        if (!serverFound)
                            serverList.Add(serverInfo);
                    }
                }

                timeTaken += 0.1f;
                Thread.Sleep(100);
            }

            // Discover remote servers.
            try
            {
                string publicList = HttpRequest.Get("http://apps.keithholman.net/plain", null);
                foreach (string s in publicList.Split("\r\n".ToCharArray()))
                {
                    string[] args = s.Split(";".ToCharArray());
                    if (args.Length == 6)
                    {
                        IPAddress serverIp;
                        if (IPAddress.TryParse(args[1], out serverIp) && args[2] == "INFINIMINER")
                        {
                            ServerInformation serverInfo = new ServerInformation(serverIp, args[0], args[5], args[3], args[4]);
                            serverList.Add(serverInfo);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return serverList;
        }

        public void UpdateNetwork(GameTime gameTime)
        {
            // Update the server with our status.
            timeSinceLastUpdate += gameTime.ElapsedGameTime.TotalSeconds;
            if (timeSinceLastUpdate > 0.05)
            {
                timeSinceLastUpdate = 0;
                if (CurrentStateType == "Infiniminer.States.MainGameState")
                    propertyBag.SendPlayerUpdate();
            }

            // Recieve messages from the server.
            NetMessageType msgType;
            while (propertyBag.netClient.ReadMessage(msgBuffer, out msgType))
            {
                switch (msgType)
                {
                    case NetMessageType.StatusChanged:
                        {
                            if (propertyBag.netClient.Status == NetConnectionStatus.Disconnected)
                                ChangeState("Infiniminer.States.ServerBrowserState");
                        }
                        break;

                    case NetMessageType.ConnectionRejected:
                        {
                            string[] reason = msgBuffer.ReadString().Split(";".ToCharArray());
                            if (reason.Length < 2 || reason[0] == "VER")
                                MessageBox.Show("Error: client/server version incompability!\r\nServer: " + msgBuffer.ReadString() + "\r\nClient: " + INFINIMINER_VERSION);
                            else
                                MessageBox.Show("Error: you are banned from this server!");
                            ChangeState("Infiniminer.States.ServerBrowserState");
                        }
                        break;

                    case NetMessageType.Data:
                        {
                            InfiniminerMessage dataType = (InfiniminerMessage)msgBuffer.ReadByte();
                            switch (dataType)
                            {
                                case InfiniminerMessage.BlockBulkTransfer:
                                    {
                                        byte x = msgBuffer.ReadByte();
                                        byte y = msgBuffer.ReadByte();
                                        propertyBag.mapLoadProgress[x,y] = true;
                                        for (byte dy=0; dy<16; dy++)
                                            for (byte z=0; z<64; z++)
                                            {
                                                BlockType blockType = (BlockType)msgBuffer.ReadByte();
                                                if (blockType != BlockType.None)
                                                    propertyBag.blockEngine.downloadList[x, y+dy, z] = blockType;
                                            }
                                        bool downloadComplete = true;
                                        for (x=0; x<64; x++)
                                            for (y=0; y<64; y+=16)
                                                if (propertyBag.mapLoadProgress[x,y] == false)
                                                {
                                                    downloadComplete = false;
                                                    break;
                                                }
                                        if (downloadComplete)
                                        {
                                            ChangeState("Infiniminer.States.TeamSelectionState");
                                            if (!NoSound)
                                                MediaPlayer.Stop();
                                            propertyBag.blockEngine.DownloadComplete();
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.SetBeacon:
                                    {
                                        Vector3 position = msgBuffer.ReadVector3();
                                        string text = msgBuffer.ReadString();
                                        PlayerTeam team = (PlayerTeam)msgBuffer.ReadByte();

                                        if (text == "")
                                        {
                                            if (propertyBag.beaconList.ContainsKey(position))
                                                propertyBag.beaconList.Remove(position);
                                        }
                                        else
                                        {
                                            Beacon newBeacon = new Beacon();
                                            newBeacon.ID = text;
                                            newBeacon.Team = team;
                                            propertyBag.beaconList.Add(position, newBeacon);
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.TriggerConstructionGunAnimation:
                                    {
                                        propertyBag.constructionGunAnimation = msgBuffer.ReadFloat();
                                        if (propertyBag.constructionGunAnimation <= -0.1)
                                            propertyBag.PlaySound(InfiniminerSound.RadarSwitch);
                                    }
                                    break;

                                case InfiniminerMessage.ResourceUpdate:
                                    {
                                        // ore, cash, weight, max ore, max weight, team ore, red cash, blue cash, all uint
                                        propertyBag.playerOre = msgBuffer.ReadUInt32();
                                        propertyBag.playerCash = msgBuffer.ReadUInt32();
                                        propertyBag.playerWeight = msgBuffer.ReadUInt32();
                                        propertyBag.playerOreMax = msgBuffer.ReadUInt32();
                                        propertyBag.playerWeightMax = msgBuffer.ReadUInt32();
                                        propertyBag.teamOre = msgBuffer.ReadUInt32();
                                        propertyBag.teamRedCash = msgBuffer.ReadUInt32();
                                        propertyBag.teamBlueCash = msgBuffer.ReadUInt32();
                                    }
                                    break;

                                case InfiniminerMessage.BlockSet:
                                    {
                                        // x, y, z, type, all bytes
                                        byte x = msgBuffer.ReadByte();
                                        byte y = msgBuffer.ReadByte();
                                        byte z = msgBuffer.ReadByte();
                                        BlockType blockType = (BlockType)msgBuffer.ReadByte();
                                        if (blockType == BlockType.None)
                                        {
                                            if (propertyBag.blockEngine.BlockAtPoint(new Vector3(x, y, z)) != BlockType.None)
                                                propertyBag.blockEngine.RemoveBlock(x, y, z);
                                        }
                                        else
                                        {
                                            if (propertyBag.blockEngine.BlockAtPoint(new Vector3(x, y, z)) != BlockType.None)
                                                propertyBag.blockEngine.RemoveBlock(x, y, z);
                                            propertyBag.blockEngine.AddBlock(x, y, z, blockType);
                                            CheckForStandingInLava();                                          
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.TriggerExplosion:
                                    {
                                        Vector3 blockPos = msgBuffer.ReadVector3();
                                        
                                        // Play the explosion sound.
                                        propertyBag.PlaySound(InfiniminerSound.Explosion, blockPos);

                                        // Create some particles.
                                        propertyBag.particleEngine.CreateExplosionDebris(blockPos);

                                        // Figure out what the effect is.
                                        float distFromExplosive = (blockPos + 0.5f * Vector3.One - propertyBag.playerPosition).Length();
                                        if (distFromExplosive < 3)
                                            propertyBag.KillPlayer("WAS KILLED IN AN EXPLOSION!");
                                        else if (distFromExplosive < 8)
                                        {
                                            // If we're not in explosion mode, turn it on with the minimum ammount of shakiness.
                                            if (propertyBag.screenEffect != ScreenEffect.Explosion)
                                            {
                                                propertyBag.screenEffect = ScreenEffect.Explosion;
                                                propertyBag.screenEffectCounter = 2;
                                            }
                                            // If this bomb would result in a bigger shake, use its value.
                                            propertyBag.screenEffectCounter = Math.Min(propertyBag.screenEffectCounter, (distFromExplosive - 2) / 5);
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.PlayerSetTeam:
                                    {
                                        uint playerId = msgBuffer.ReadUInt32();
                                        if (propertyBag.playerList.ContainsKey(playerId))
                                        {
                                            Player player = propertyBag.playerList[playerId];
                                            player.Team = (PlayerTeam)msgBuffer.ReadByte();
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.PlayerJoined:
                                    {
                                        uint playerId = msgBuffer.ReadUInt32();
                                        string playerName = msgBuffer.ReadString();
                                        bool thisIsMe = msgBuffer.ReadBoolean();
                                        bool playerAlive = msgBuffer.ReadBoolean();
                                        propertyBag.playerList[playerId] = new Player(null, (Game)this);
                                        propertyBag.playerList[playerId].Handle = playerName;
                                        propertyBag.playerList[playerId].ID = playerId;
                                        propertyBag.playerList[playerId].Alive = playerAlive;
                                        if (thisIsMe)
                                            propertyBag.playerMyId = playerId;
                                    }
                                    break;

                                case InfiniminerMessage.PlayerLeft:
                                    {
                                        uint playerId = msgBuffer.ReadUInt32();
                                        if (propertyBag.playerList.ContainsKey(playerId))
                                            propertyBag.playerList.Remove(playerId);
                                    }
                                    break;

                                case InfiniminerMessage.PlayerDead:
                                    {
                                        uint playerId = msgBuffer.ReadUInt32();
                                        if (propertyBag.playerList.ContainsKey(playerId))
                                        {
                                            Player player = propertyBag.playerList[playerId];
                                            player.Alive = false;
                                            propertyBag.particleEngine.CreateBloodSplatter(player.Position, player.Team == PlayerTeam.Red ? Color.Red : Color.Blue);
                                            if (playerId != propertyBag.playerMyId)
                                                propertyBag.PlaySound(InfiniminerSound.Death, player.Position);
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.PlayerAlive:
                                    {
                                        uint playerId = msgBuffer.ReadUInt32();
                                        if (propertyBag.playerList.ContainsKey(playerId))
                                        {
                                            Player player = propertyBag.playerList[playerId];
                                            player.Alive = true;
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.PlayerUpdate:
                                    {
                                        uint playerId = msgBuffer.ReadUInt32();
                                        if (propertyBag.playerList.ContainsKey(playerId))
                                        {
                                            Player player = propertyBag.playerList[playerId];
                                            player.UpdatePosition(msgBuffer.ReadVector3(), gameTime.TotalGameTime.TotalSeconds);
                                            player.Heading = msgBuffer.ReadVector3();
                                            player.Tool = (PlayerTools)msgBuffer.ReadByte();
                                            player.UsingTool = msgBuffer.ReadBoolean();
                                            player.Score = (uint)(msgBuffer.ReadUInt16() * 100);
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.GameOver:
                                    {
                                        propertyBag.teamWinners = (PlayerTeam)msgBuffer.ReadByte();
                                    }
                                    break;

                                case InfiniminerMessage.ChatMessage:
                                    {
                                        ChatMessageType chatType = (ChatMessageType)msgBuffer.ReadByte();
                                        string chatString = msgBuffer.ReadString();
                                        ChatMessage chatMsg = new ChatMessage(chatString, chatType, 10);
                                        propertyBag.chatBuffer.Insert(0, chatMsg);
                                        propertyBag.PlaySound(InfiniminerSound.ClickLow);
                                    }
                                    break;

                                case InfiniminerMessage.PlayerPing:
                                    {
                                        uint playerId = (uint)msgBuffer.ReadInt32();
                                        if (propertyBag.playerList.ContainsKey(playerId))
                                        {
                                            if (propertyBag.playerList[playerId].Team == propertyBag.playerTeam)
                                            {
                                                propertyBag.playerList[playerId].Ping = 1;
                                                propertyBag.PlaySound(InfiniminerSound.Ping);
                                            }
                                        }
                                    }
                                    break;

                                case InfiniminerMessage.PlaySound:
                                    {
                                        InfiniminerSound sound = (InfiniminerSound)msgBuffer.ReadByte();
                                        bool hasPosition = msgBuffer.ReadBoolean();
                                        if (hasPosition)
                                        {
                                            Vector3 soundPosition = msgBuffer.ReadVector3();
                                            propertyBag.PlaySound(sound, soundPosition);
                                        }
                                        else
                                            propertyBag.PlaySound(sound);
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            // Make sure our network thread actually gets to run.
            Thread.Sleep(1);
        }

        private void CheckForStandingInLava()
        {
            // Copied from TryToMoveTo; responsible for checking if lava has flowed over us.

            Vector3 movePosition = propertyBag.playerPosition;
            Vector3 midBodyPoint = movePosition + new Vector3(0, -0.7f, 0);
            Vector3 lowerBodyPoint = movePosition + new Vector3(0, -1.4f, 0);
            BlockType lowerBlock = propertyBag.blockEngine.BlockAtPoint(lowerBodyPoint);
            BlockType midBlock = propertyBag.blockEngine.BlockAtPoint(midBodyPoint);
            BlockType upperBlock = propertyBag.blockEngine.BlockAtPoint(movePosition);
            if (upperBlock == BlockType.Lava || lowerBlock == BlockType.Lava || midBlock == BlockType.Lava)
            {
                propertyBag.KillPlayer("WAS INCINERATED BY LAVA!");
            }
        }

        protected override void Initialize()
        {
            graphicsDeviceManager.IsFullScreen = false;
            graphicsDeviceManager.PreferredBackBufferWidth = 1024;
            graphicsDeviceManager.PreferredBackBufferHeight = 768;
            graphicsDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

            DatafileLoader dataFile = new DatafileLoader("client.config.txt");
            if (dataFile.Data.ContainsKey("width"))
                graphicsDeviceManager.PreferredBackBufferWidth = int.Parse(dataFile.Data["width"], System.Globalization.CultureInfo.InvariantCulture);
            if (dataFile.Data.ContainsKey("height"))
                graphicsDeviceManager.PreferredBackBufferHeight = int.Parse(dataFile.Data["height"], System.Globalization.CultureInfo.InvariantCulture);
            if (dataFile.Data.ContainsKey("fullscreen"))
                graphicsDeviceManager.IsFullScreen = bool.Parse(dataFile.Data["fullscreen"]);
            if (dataFile.Data.ContainsKey("handle"))
                playerHandle = dataFile.Data["handle"];
            if (dataFile.Data.ContainsKey("showfps"))
                DrawFrameRate = bool.Parse(dataFile.Data["showfps"]);
            if (dataFile.Data.ContainsKey("yinvert"))
                InvertMouseYAxis = bool.Parse(dataFile.Data["yinvert"]);
            if (dataFile.Data.ContainsKey("nosound"))
                NoSound = bool.Parse(dataFile.Data["nosound"]);
            if (dataFile.Data.ContainsKey("pretty"))
                RenderPretty = bool.Parse(dataFile.Data["pretty"]);
            if (dataFile.Data.ContainsKey("volume"))
                volumeLevel = Math.Max(0,Math.Min(1,float.Parse(dataFile.Data["volume"], System.Globalization.CultureInfo.InvariantCulture)));

            graphicsDeviceManager.ApplyChanges();
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            propertyBag.netClient.Shutdown("Client exiting.");
            
            base.OnExiting(sender, args);
        }

        public void ResetPropertyBag()
        {
            if (propertyBag != null)
                propertyBag.netClient.Shutdown("");

            propertyBag = new Infiniminer.PropertyBag(this);
            propertyBag.playerHandle = playerHandle;
            propertyBag.volumeLevel = volumeLevel;
            msgBuffer = propertyBag.netClient.CreateBuffer();
        }

        protected override void LoadContent()
        {
            // Initialize the property bag.
            ResetPropertyBag();

            // Set the initial state to team selection
            ChangeState("Infiniminer.States.TitleState");

            // Play the title music.
            if (!NoSound)
            {
                songTitle = Content.Load<Song>("song_title");
                MediaPlayer.Play(songTitle);
                MediaPlayer.Volume = propertyBag.volumeLevel;
            }
        }
    }
}
