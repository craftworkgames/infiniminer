using System;
using System.Collections.Generic;
using System.Text;
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
    public enum PlayerClass
    {
        Prospector,
        Miner,
        Engineer,
        Sapper
    }

    public enum PlayerTools
    {
        Pickaxe,
        ConstructionGun,
        DeconstructionGun,
        ProspectingRadar,
        Detonator,
    }

    public enum PlayerTeam
    {
        None,
        Red,
        Blue
    }

    public enum ScreenEffect
    {
        None,
        Death,
        Teleport,
        Fall,
        Explosion,
    }

    public enum ChatMessageType
    {
        None,
        SayAll,
        SayRedTeam,
        SayBlueTeam,
    }

    public class ChatMessage
    {
        public string message;
        public ChatMessageType type;
        public float timestamp;

        public ChatMessage(string message, ChatMessageType type, float timestamp)
        {
            this.message = message;
            this.type = type;
            this.timestamp = timestamp;
        }
    }

    public enum InfiniminerSound
    {
        DigDirt,
        DigMetal,
        Ping,
        ConstructionGun,
        Death,
        CashDeposit,
        ClickHigh,
        ClickLow,
        GroundHit,
        Teleporter,
        Jumpblock,
        Explosion,
        RadarLow,
        RadarHigh,
        RadarSwitch,
    }

    public enum InfiniminerMessage : byte
    {
        BlockBulkTransfer,      // x-value, y-value, followed by 64 bytes of blocktype ; 
        BlockSet,               // x, y, z, type
        UseTool,                // position, heading, tool, blocktype 
        SelectClass,            // class
        ResourceUpdate,         // ore, cash, weight, max ore, max weight, team ore, red cash, blue cash: ReliableInOrder1
        DepositOre,
        DepositCash,
        WithdrawOre,
        TriggerExplosion,       // position
        
        PlayerUpdate,           // (uint id for server), position, heading, current tool, animate using (bool): UnreliableInOrder1
        PlayerJoined,           // uint id, player name :ReliableInOrder2
        PlayerLeft,             // uint id              :ReliableInOrder2
        PlayerSetTeam,          // (uint id for server), byte team   :ReliableInOrder2
        PlayerDead,             // (uint id for server) :ReliableInOrder2
        PlayerAlive,            // (uint id for server) :ReliableInOrder2
        PlayerPing,             // uint id

        ChatMessage,            // byte type, string message : ReliableInOrder3
        GameOver,               // byte team
        PlaySound,              // byte sound, bool isPositional, ?Vector3 location : ReliableUnordered
        TriggerConstructionGunAnimation,
        SetBeacon,              // vector3 position, string text ("" means remove)
    }

    public class Beacon
    {
        public string ID;
        public PlayerTeam Team;
    }

	public class PropertyBag
	{
        // Game engines.
        public BlockEngine blockEngine = null;
        public InterfaceEngine interfaceEngine = null;
        public PlayerEngine playerEngine = null;
        public SkyplaneEngine skyplaneEngine = null;
        public ParticleEngine particleEngine = null;

        // Network stuff.
        public NetClient netClient = null;
        public Dictionary<uint, Player> playerList = new Dictionary<uint, Player>();
        public bool[,] mapLoadProgress = null;

        // Player variables.
        public Camera playerCamera = null;
        public Vector3 playerPosition = Vector3.Zero;
        public Vector3 playerVelocity = Vector3.Zero;
        public PlayerClass playerClass;
        public PlayerTools[] playerTools = new PlayerTools[1] { PlayerTools.Pickaxe };
        public int playerToolSelected = 0;
        public BlockType[] playerBlocks = new BlockType[1] {BlockType.None};
        public int playerBlockSelected = 0;
        public PlayerTeam playerTeam = PlayerTeam.Red;
        public bool playerDead = true;
        public uint playerOre = 0;
        public uint playerCash = 0;
        public uint playerWeight = 0;
        public uint playerOreMax = 0;
        public uint playerWeightMax = 0;
        public bool playerRadarMute = false;
        public float playerToolCooldown = 0;
        public string playerHandle = "Player";
        public float volumeLevel = 1.0f;
        public uint playerMyId = 0;
        public float radarCooldown = 0;
        public float radarDistance = 0;
        public float radarValue = 0;
        public float constructionGunAnimation = 0;

        // Team variables.
        public uint teamOre = 0;
        public uint teamRedCash = 0;
        public uint teamBlueCash = 0;
        public PlayerTeam teamWinners = PlayerTeam.None;
        public Dictionary<Vector3, Beacon> beaconList = new Dictionary<Vector3, Beacon>();

        // Screen effect stuff.
        private Random randGen = new Random();
        public ScreenEffect screenEffect = ScreenEffect.None;
        public double screenEffectCounter = 0;

        // Sound stuff.
        public Dictionary<InfiniminerSound, SoundEffect> soundList = new Dictionary<InfiniminerSound, SoundEffect>();

        // Chat stuff.
        public ChatMessageType chatMode = ChatMessageType.None;
        public int chatMaxBuffer = 5;
        public List<ChatMessage> chatBuffer = new List<ChatMessage>(); // chatBuffer[0] is most recent
        public string chatEntryBuffer = "";

        public PropertyBag(InfiniminerGame gameInstance)
        {
            // Initialize our network device.
            NetConfiguration netConfig = new NetConfiguration("InfiniminerPlus");
            
            netClient = new NetClient(netConfig);
            netClient.SetMessageTypeEnabled(NetMessageType.ConnectionRejected, true);
            //netClient.SimulatedMinimumLatency = 0.1f;
            //netClient.SimulatedLatencyVariance = 0.05f;
            //netClient.SimulatedLoss = 0.1f;
            //netClient.SimulatedDuplicates = 0.05f;
            netClient.Start();

            // Initialize engines.
            blockEngine = new BlockEngine(gameInstance);
            interfaceEngine = new InterfaceEngine(gameInstance);
            playerEngine = new PlayerEngine(gameInstance);
            skyplaneEngine = new SkyplaneEngine(gameInstance);
            particleEngine = new ParticleEngine(gameInstance);

            // Create a camera.
            playerCamera = new Camera(gameInstance.GraphicsDevice);
            UpdateCamera();

            // Load sounds.
            if (!gameInstance.NoSound)
            {
                soundList[InfiniminerSound.DigDirt] = gameInstance.Content.Load<SoundEffect>("sounds/dig-dirt");
                soundList[InfiniminerSound.DigMetal] = gameInstance.Content.Load<SoundEffect>("sounds/dig-metal");
                soundList[InfiniminerSound.Ping] = gameInstance.Content.Load<SoundEffect>("sounds/ping");
                soundList[InfiniminerSound.ConstructionGun] = gameInstance.Content.Load<SoundEffect>("sounds/build");
                soundList[InfiniminerSound.Death] = gameInstance.Content.Load<SoundEffect>("sounds/death");
                soundList[InfiniminerSound.CashDeposit] = gameInstance.Content.Load<SoundEffect>("sounds/cash");
                soundList[InfiniminerSound.ClickHigh] = gameInstance.Content.Load<SoundEffect>("sounds/click-loud");
                soundList[InfiniminerSound.ClickLow] = gameInstance.Content.Load<SoundEffect>("sounds/click-quiet");
                soundList[InfiniminerSound.GroundHit] = gameInstance.Content.Load<SoundEffect>("sounds/hitground");
                soundList[InfiniminerSound.Teleporter] = gameInstance.Content.Load<SoundEffect>("sounds/teleport");
                soundList[InfiniminerSound.Jumpblock] = gameInstance.Content.Load<SoundEffect>("sounds/jumpblock");
                soundList[InfiniminerSound.Explosion] = gameInstance.Content.Load<SoundEffect>("sounds/explosion");
                soundList[InfiniminerSound.RadarHigh] = gameInstance.Content.Load<SoundEffect>("sounds/radar-high");
                soundList[InfiniminerSound.RadarLow] = gameInstance.Content.Load<SoundEffect>("sounds/radar-low");
                soundList[InfiniminerSound.RadarSwitch] = gameInstance.Content.Load<SoundEffect>("sounds/switch");
            }
        }

        public void KillPlayer(string deathMessage)
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            PlaySound(InfiniminerSound.Death);
            playerPosition = new Vector3(randGen.Next(2, 62), 66, randGen.Next(2, 62));
            playerVelocity = Vector3.Zero;
            playerDead = true;
            screenEffect = ScreenEffect.Death;
            screenEffectCounter = 0;

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerDead);
            msgBuffer.Write(deathMessage);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void RespawnPlayer()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            playerDead = false;

            // Respawn a few blocks above a safe position above altitude 0.
            bool positionFound = false;

            // Try 20 times; use a potentially invalid position if we fail.
            for (int i = 0; i < 20; i++)
            {
                // Pick a random starting point.
                Vector3 startPos = new Vector3(randGen.Next(2, 62), 63, randGen.Next(2, 62));

                // See if this is a safe place to drop.
                for (startPos.Y = 63; startPos.Y >= 54; startPos.Y--)
                {
                    BlockType blockType = blockEngine.BlockAtPoint(startPos);
                    if (blockType == BlockType.Lava)
                        break;
                    else if (blockType != BlockType.None)
                    {
                        // We have found a valid place to spawn, so spawn a few above it.
                        playerPosition = startPos + Vector3.UnitY * 5;
                        positionFound = true;
                        break;
                    }
                }

                // If we found a position, no need to try anymore!
                if (positionFound)
                    break;
            }

            // If we failed to find a spawn point, drop randomly.
            if (!positionFound)
                playerPosition = new Vector3(randGen.Next(2, 62), 66, randGen.Next(2, 62));

            // Drop the player on the middle of the block, not at the corner.
            playerPosition += new Vector3(0.5f, 0, 0.5f);

            // Zero out velocity and reset camera and screen effects.
            playerVelocity = Vector3.Zero;
            screenEffect = ScreenEffect.None;
            screenEffectCounter = 0;
            UpdateCamera();

            // Tell the server we have respawned.
            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerAlive);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void PlaySound(InfiniminerSound sound)
        {
            if (soundList.Count == 0)
                return;

            soundList[sound].Play(volumeLevel);
        }

        public void PlaySound(InfiniminerSound sound, Vector3 position)
        {
            if (soundList.Count == 0)
                return;

            float distance = (position - playerPosition).Length();
            float volume = Math.Max(0, 10 - distance) / 10.0f * volumeLevel;
            soundList[sound].Play(volume);
        }

        public void PlaySoundForEveryone(InfiniminerSound sound, Vector3 position)
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            // The PlaySound message can be used to instruct the server to have all clients play a directional sound.
            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlaySound);
            msgBuffer.Write((byte)sound);
            msgBuffer.Write(position);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        //public void Teleport()
        //{
        //    float x = (float)randGen.NextDouble() * 74 - 5;
        //    float z = (float)randGen.NextDouble() * 74 - 5;
        //    //playerPosition = playerHomeBlock + new Vector3(0.5f, 3, 0.5f);
        //    playerPosition = new Vector3(x, 74, z);
        //    screenEffect = ScreenEffect.Teleport;
        //    screenEffectCounter = 0;
        //    UpdateCamera();
        //}

        // Version used during updates.
        public void UpdateCamera(GameTime gameTime)
        {
            // If we have a gameTime object, apply screen jitter.
            if (screenEffect == ScreenEffect.Explosion)
            {
                if (gameTime != null)
                {
                    screenEffectCounter += gameTime.ElapsedGameTime.TotalSeconds;
                    // For 0 to 2, shake the camera.
                    if (screenEffectCounter < 2)
                    {
                        Vector3 newPosition = playerCamera.Position;
                        newPosition.X += (float)(2 - screenEffectCounter) * (float)(randGen.NextDouble() - 0.5) * 0.5f;
                        newPosition.Y += (float)(2 - screenEffectCounter) * (float)(randGen.NextDouble() - 0.5) * 0.5f;
                        newPosition.Z += (float)(2 - screenEffectCounter) * (float)(randGen.NextDouble() - 0.5) * 0.5f;
                        if (!blockEngine.SolidAtPointForPlayer(newPosition) && (newPosition-playerPosition).Length() < 0.7f)
                            playerCamera.Position = newPosition;
                    }
                    // For 2 to 3, move the camera back.
                    else if (screenEffectCounter < 3)
                    {
                        Vector3 lerpVector = playerPosition - playerCamera.Position;
                        playerCamera.Position += 0.5f * lerpVector;
                    }
                    else
                    {
                        screenEffect = ScreenEffect.None;
                        screenEffectCounter = 0;
                        playerCamera.Position = playerPosition;
                    }
                }
            }
            else
            {
                playerCamera.Position = playerPosition;
            }
            playerCamera.Update();
        }

        public void UpdateCamera()
        {
            UpdateCamera(null);
        }

        public void DepositLoot()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.DepositCash);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void DepositOre()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.DepositOre);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void WithdrawOre()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.WithdrawOre);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void SetPlayerTeam(PlayerTeam playerTeam)
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            this.playerTeam = playerTeam;

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerSetTeam);
            msgBuffer.Write((byte)playerTeam);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void SetPlayerClass(PlayerClass playerClass)
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            this.playerClass = playerClass;

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.SelectClass);
            msgBuffer.Write((byte)playerClass);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);

            playerToolSelected = 0;
            playerBlockSelected = 0;

            switch (playerClass)
            {
                case PlayerClass.Prospector:
                    playerTools = new PlayerTools[3] {  PlayerTools.Pickaxe,
                                                        PlayerTools.ConstructionGun,
                                                        PlayerTools.ProspectingRadar     };
                    playerBlocks = new BlockType[4] {   playerTeam == PlayerTeam.Red ? BlockType.SolidRed : BlockType.SolidBlue,
                                                        playerTeam == PlayerTeam.Red ? BlockType.TransRed : BlockType.TransBlue,
                                                        playerTeam == PlayerTeam.Red ? BlockType.BeaconRed : BlockType.BeaconBlue,
                                                        BlockType.Ladder    };
                    break;

                case PlayerClass.Miner:
                    playerTools = new PlayerTools[2] {  PlayerTools.Pickaxe,
                                                        PlayerTools.ConstructionGun     };
                    playerBlocks = new BlockType[3] {   playerTeam == PlayerTeam.Red ? BlockType.SolidRed : BlockType.SolidBlue,
                                                        playerTeam == PlayerTeam.Red ? BlockType.TransRed : BlockType.TransBlue,
                                                        BlockType.Ladder    };
                    break;

                case PlayerClass.Engineer:
                    playerTools = new PlayerTools[3] {  PlayerTools.Pickaxe,
                                                        PlayerTools.ConstructionGun,     
                                                        PlayerTools.DeconstructionGun   };
                    playerBlocks = new BlockType[9] {   playerTeam == PlayerTeam.Red ? BlockType.SolidRed : BlockType.SolidBlue,
                                                        BlockType.TransRed,
                                                        BlockType.TransBlue,
                                                        BlockType.Road,
                                                        BlockType.Ladder,
                                                        BlockType.Jump,
                                                        BlockType.Shock,
                                                        playerTeam == PlayerTeam.Red ? BlockType.BeaconRed : BlockType.BeaconBlue,
                                                        playerTeam == PlayerTeam.Red ? BlockType.BankRed : BlockType.BankBlue  };
                    break;

                case PlayerClass.Sapper:
                    playerTools = new PlayerTools[3] {  PlayerTools.Pickaxe,
                                                        PlayerTools.ConstructionGun,
                                                        PlayerTools.Detonator     };
                    playerBlocks = new BlockType[4] {   playerTeam == PlayerTeam.Red ? BlockType.SolidRed : BlockType.SolidBlue,
                                                        playerTeam == PlayerTeam.Red ? BlockType.TransRed : BlockType.TransBlue,
                                                        BlockType.Ladder,
                                                        BlockType.Explosive     };
                    break;
            }
        }

        public void FireRadar()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            playerToolCooldown = GetToolCooldown(PlayerTools.ProspectingRadar);

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.UseTool);
            msgBuffer.Write(playerPosition);
            msgBuffer.Write(playerCamera.GetLookVector());
            msgBuffer.Write((byte)PlayerTools.ProspectingRadar);
            msgBuffer.Write((byte)BlockType.None);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void FirePickaxe()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            playerToolCooldown = GetToolCooldown(PlayerTools.Pickaxe);

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.UseTool);
            msgBuffer.Write(playerPosition);
            msgBuffer.Write(playerCamera.GetLookVector());
            msgBuffer.Write((byte)PlayerTools.Pickaxe);
            msgBuffer.Write((byte)BlockType.None);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered); 
        }

        public void FireConstructionGun(BlockType blockType)
        {
            playerToolCooldown = GetToolCooldown(PlayerTools.ConstructionGun);
            constructionGunAnimation = -5;

            // Send the message.
            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.UseTool);
            msgBuffer.Write(playerPosition);
            msgBuffer.Write(playerCamera.GetLookVector());
            msgBuffer.Write((byte)PlayerTools.ConstructionGun);
            msgBuffer.Write((byte)blockType);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void FireDeconstructionGun()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            playerToolCooldown = GetToolCooldown(PlayerTools.DeconstructionGun);
            constructionGunAnimation = -5;

            // Send the message.
            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.UseTool);
            msgBuffer.Write(playerPosition);
            msgBuffer.Write(playerCamera.GetLookVector());
            msgBuffer.Write((byte)PlayerTools.DeconstructionGun);
            msgBuffer.Write((byte)BlockType.None);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
        }

        public void FireDetonator()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            playerToolCooldown = GetToolCooldown(PlayerTools.Detonator);

            // Send the message.
            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.UseTool);
            msgBuffer.Write(playerPosition);
            msgBuffer.Write(playerCamera.GetLookVector());
            msgBuffer.Write((byte)PlayerTools.Detonator);
            msgBuffer.Write((byte)BlockType.None);
            netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered); 
        }

        public void ToggleRadar()
        {
            playerRadarMute = !playerRadarMute;
            PlaySound(InfiniminerSound.RadarSwitch);
        }

        public void ReadRadar(ref float distanceReading, ref float valueReading)
        {
            valueReading = 0;
            distanceReading = 30;

            // Scan out along the camera axis for 30 meters.
            for (int i=-3;i<=3; i++)
                for (int j = -3; j <= 3; j++)
                {
                    Matrix rotation = Matrix.CreateRotationX((float)(i * Math.PI / 128)) * Matrix.CreateRotationY((float)(j * Math.PI / 128));
                    Vector3 scanPoint = playerPosition;
                    Vector3 lookVector = Vector3.Transform(playerCamera.GetLookVector(), rotation);
                    for (int k = 0; k < 60; k++)
                    {
                        BlockType blockType = blockEngine.BlockAtPoint(scanPoint);
                        if (blockType == BlockType.Gold)
                        {
                            distanceReading = Math.Min(distanceReading, 0.5f * k);
                            valueReading = Math.Max(valueReading, 200);
                        }
                        else if (blockType == BlockType.Diamond)
                        {
                            distanceReading = Math.Min(distanceReading, 0.5f * k);
                            valueReading = Math.Max(valueReading, 1000);
                        }
                        scanPoint += 0.5f * lookVector;
                    }
                }
        }

        // Returns true if the player is able to use a bank right now.
        public bool AtBankTerminal()
        {
            // Figure out what we're looking at.
            Vector3 hitPoint = Vector3.Zero;
            Vector3 buildPoint = Vector3.Zero;
            if (!blockEngine.RayCollision(playerPosition, playerCamera.GetLookVector(), 2.5f, 25, ref hitPoint, ref buildPoint))
                return false;

            // If it's a valid bank object, we're good!
            BlockType blockType = blockEngine.BlockAtPoint(hitPoint);
            if (blockType == BlockType.BankRed && playerTeam == PlayerTeam.Red)
                return true;
            if (blockType == BlockType.BankBlue && playerTeam == PlayerTeam.Blue)
                return true;
            return false;
        }

        public float GetToolCooldown(PlayerTools tool)
        {
            switch (tool)
            {
                case PlayerTools.Pickaxe: return 0.55f;
                case PlayerTools.Detonator: return 0.01f;
                case PlayerTools.ConstructionGun: return 0.5f;
                case PlayerTools.DeconstructionGun: return 0.5f;
                case PlayerTools.ProspectingRadar: return 0.5f;
                default: return 0;
            }
        }

        public void SendPlayerUpdate()
        {
            if (netClient.Status != NetConnectionStatus.Connected)
                return;

            NetBuffer msgBuffer = netClient.CreateBuffer();
            msgBuffer.Write((byte)InfiniminerMessage.PlayerUpdate);
            msgBuffer.Write(playerPosition);
            msgBuffer.Write(playerCamera.GetLookVector());
            msgBuffer.Write((byte)playerTools[playerToolSelected]);
            msgBuffer.Write(playerToolCooldown > 0.001f);
            netClient.SendMessage(msgBuffer, NetChannel.UnreliableInOrder1);
        }
    }
}
