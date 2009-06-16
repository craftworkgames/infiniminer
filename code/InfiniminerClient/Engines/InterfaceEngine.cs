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

namespace Infiniminer
{
    public class InterfaceEngine
    {
        InfiniminerGame gameInstance;
        PropertyBag _P;
        SpriteBatch spriteBatch;
        public SpriteFont uiFont, radarFont;
        Rectangle drawRect;

        Texture2D texCrosshairs, texBlank, texHelp;
        Texture2D texRadarBackground, texRadarForeground, texRadarPlayerSame, texRadarPlayerAbove, texRadarPlayerBelow, texRadarPlayerPing, texRadarNorth;
        Texture2D texToolRadarRed, texToolRadarBlue, texToolRadarGold, texToolRadarDiamond, texToolRadarLED, texToolRadarPointer, texToolRadarFlash;
        Texture2D texToolDetonatorDownRed, texToolDetonatorUpRed, texToolDetonatorDownBlue, texToolDetonatorUpBlue;
        Texture2D texToolBuild, texToolBuildCharge, texToolBuildBlast, texToolBuildSmoke;

        Dictionary<BlockType, Texture2D> blockIcons = new Dictionary<BlockType, Texture2D>();

        public InterfaceEngine(InfiniminerGame gameInstance)
        {
            this.gameInstance = gameInstance;
            spriteBatch = new SpriteBatch(gameInstance.GraphicsDevice);

            // Load textures.
            texCrosshairs = gameInstance.Content.Load<Texture2D>("ui/tex_ui_crosshair");
            texBlank = new Texture2D(gameInstance.GraphicsDevice, 1, 1);
            texBlank.SetData(new uint[1] { 0xFFFFFFFF });
            texRadarBackground = gameInstance.Content.Load<Texture2D>("ui/tex_radar_background");
            texRadarForeground = gameInstance.Content.Load<Texture2D>("ui/tex_radar_foreground");
            texRadarPlayerSame = gameInstance.Content.Load<Texture2D>("ui/tex_radar_player_same");
            texRadarPlayerAbove = gameInstance.Content.Load<Texture2D>("ui/tex_radar_player_above");
            texRadarPlayerBelow = gameInstance.Content.Load<Texture2D>("ui/tex_radar_player_below");
            texRadarPlayerPing = gameInstance.Content.Load<Texture2D>("ui/tex_radar_player_ping");
            texRadarNorth = gameInstance.Content.Load<Texture2D>("ui/tex_radar_north");
            texHelp = gameInstance.Content.Load<Texture2D>("menus/tex_menu_help");

            texToolRadarRed = gameInstance.Content.Load<Texture2D>("tools/tex_tool_radar_red");
            texToolRadarBlue = gameInstance.Content.Load<Texture2D>("tools/tex_tool_radar_blue");
            texToolRadarGold = gameInstance.Content.Load<Texture2D>("tools/tex_tool_radar_screen_gold");
            texToolRadarDiamond = gameInstance.Content.Load<Texture2D>("tools/tex_tool_radar_screen_diamond");
            texToolRadarLED = gameInstance.Content.Load<Texture2D>("tools/tex_tool_radar_led");
            texToolRadarPointer = gameInstance.Content.Load<Texture2D>("tools/tex_tool_radar_pointer");
            texToolRadarFlash = gameInstance.Content.Load<Texture2D>("tools/tex_tool_radar_flash");

            texToolBuild = gameInstance.Content.Load<Texture2D>("tools/tex_tool_build");
            texToolBuildCharge = gameInstance.Content.Load<Texture2D>("tools/tex_tool_build_charge");
            texToolBuildBlast = gameInstance.Content.Load<Texture2D>("tools/tex_tool_build_blast");
            texToolBuildSmoke = gameInstance.Content.Load<Texture2D>("tools/tex_tool_build_smoke");

            texToolDetonatorDownRed = gameInstance.Content.Load<Texture2D>("tools/tex_tool_detonator_down_red");
            texToolDetonatorUpRed = gameInstance.Content.Load<Texture2D>("tools/tex_tool_detonator_up_red");
            texToolDetonatorDownBlue = gameInstance.Content.Load<Texture2D>("tools/tex_tool_detonator_down_blue");
            texToolDetonatorUpBlue = gameInstance.Content.Load<Texture2D>("tools/tex_tool_detonator_up_blue");

            drawRect = new Rectangle(gameInstance.GraphicsDevice.Viewport.Width / 2 - 1024 / 2,
                                     gameInstance.GraphicsDevice.Viewport.Height / 2 - 768 / 2,
                                     1024,
                                     1024);

            // Load icons.
            blockIcons[BlockType.BankBlue] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_bank_blue");
            blockIcons[BlockType.BankRed] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_bank_red");
            blockIcons[BlockType.Explosive] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_explosive");
            blockIcons[BlockType.Jump] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_jump");
            blockIcons[BlockType.Ladder] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_ladder");
            blockIcons[BlockType.SolidBlue] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_solid_blue");
            blockIcons[BlockType.SolidRed] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_solid_red");
            blockIcons[BlockType.Shock] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_spikes");
            blockIcons[BlockType.TransBlue] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_translucent_blue");
            blockIcons[BlockType.TransRed] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_translucent_red");
            blockIcons[BlockType.BeaconRed] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_beacon");
            blockIcons[BlockType.BeaconBlue] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_beacon");
            blockIcons[BlockType.Road] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_road");
            blockIcons[BlockType.None] = gameInstance.Content.Load<Texture2D>("icons/tex_icon_deconstruction");

            // Load fonts.
            uiFont = gameInstance.Content.Load<SpriteFont>("font_04b08");
            radarFont = gameInstance.Content.Load<SpriteFont>("font_04b03b");
        }

        public void RenderMessageCenter(SpriteBatch spriteBatch, string text, Vector2 pointCenter, Color colorText, Color colorBackground)
        {
            Vector2 textSize = uiFont.MeasureString(text);
            spriteBatch.Draw(texBlank, new Rectangle((int)(pointCenter.X - textSize.X / 2 - 10), (int)(pointCenter.Y - textSize.Y / 2 - 10), (int)(textSize.X + 20), (int)(textSize.Y + 20)), colorBackground);
            spriteBatch.DrawString(uiFont, text, pointCenter - textSize / 2, colorText);
        }

        private static bool MessageExpired(ChatMessage msg)
        {
            return msg.timestamp <= 0;
        }

        public void Update(GameTime gameTime)
        {
            if (_P == null)
                return;

            foreach (ChatMessage msg in _P.chatBuffer)
                msg.timestamp -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            _P.chatBuffer.RemoveAll(MessageExpired);

            int bufferSize = 10;
            if (_P.chatFullBuffer.Count > bufferSize)
                _P.chatFullBuffer.RemoveRange(bufferSize, _P.chatFullBuffer.Count - bufferSize);

            if (_P.constructionGunAnimation > 0)
            {
                if (_P.constructionGunAnimation > gameTime.ElapsedGameTime.TotalSeconds)
                    _P.constructionGunAnimation -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                else
                    _P.constructionGunAnimation = 0;
            }
            else
            {
                if (_P.constructionGunAnimation < -gameTime.ElapsedGameTime.TotalSeconds)
                    _P.constructionGunAnimation += (float)gameTime.ElapsedGameTime.TotalSeconds;
                else
                    _P.constructionGunAnimation = 0;
            }
        }

        public void RenderRadarBlip(SpriteBatch spriteBatch, Vector3 position, Color color, bool ping, string text)
        {
            // Figure out the relative position for the radar blip.
            Vector3 relativePosition = position - _P.playerPosition;
            float relativeAltitude = relativePosition.Y;
            relativePosition.Y = 0;
            Matrix rotationMatrix = Matrix.CreateRotationY(-_P.playerCamera.Yaw);
            relativePosition = Vector3.Transform(relativePosition, rotationMatrix) * 10;
            float relativeLength = Math.Min(relativePosition.Length(), 93);
            if (relativeLength != 0)
                relativePosition.Normalize();
            relativePosition *= relativeLength;

            // Draw the radar blip.
            if (text == "")
            {
                relativePosition.X = (int)relativePosition.X;
                relativePosition.Z = (int)relativePosition.Z;
                Texture2D texRadarSprite = texRadarPlayerSame;
                if (relativeAltitude > 2)
                    texRadarSprite = texRadarPlayerAbove;
                else if (relativeAltitude < -2)
                    texRadarSprite = texRadarPlayerBelow;
                spriteBatch.Draw(texRadarSprite, new Vector2(10 + 99 + relativePosition.X - texRadarSprite.Width / 2, 30 + 99 + relativePosition.Z - texRadarSprite.Height / 2), color);
                if (ping)
                    spriteBatch.Draw(texRadarPlayerPing, new Vector2(10 + 99 + relativePosition.X - texRadarPlayerPing.Width / 2, 30 + 99 + relativePosition.Z - texRadarPlayerPing.Height / 2), color);
            }

            // Render text.
            if (text != "")
            {
                relativePosition *= 0.9f;
                relativePosition.X = (int)relativePosition.X;
                relativePosition.Z = (int)relativePosition.Z;

                if (text == "NORTH")
                {
                    spriteBatch.Draw(texRadarNorth, new Vector2(10 + 99 + relativePosition.X - texRadarNorth.Width / 2, 30 + 99 + relativePosition.Z - texRadarNorth.Height / 2), color);
                }
                else
                {
                    if (relativeAltitude > 2)
                        text += " ^";
                    else if (relativeAltitude < -2)
                        text += " v";
                    Vector2 textSize = radarFont.MeasureString(text);
                    spriteBatch.DrawString(radarFont, text, new Vector2(10 + 99 + relativePosition.X - textSize.X / 2, 30 + 99 + relativePosition.Z - textSize.Y / 2), color);
                }
            }
        }

        public void RenderDetonator(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;
            graphicsDevice.SamplerStates[0].MagFilter = TextureFilter.Point;

            Texture2D textureToUse;
            if (Mouse.GetState().LeftButton == ButtonState.Pressed || Mouse.GetState().MiddleButton == ButtonState.Pressed || Mouse.GetState().RightButton == ButtonState.Pressed)
                textureToUse = _P.playerTeam == PlayerTeam.Red ? texToolDetonatorDownRed : texToolDetonatorDownBlue;
            else
                textureToUse = _P.playerTeam == PlayerTeam.Red ? texToolDetonatorUpRed : texToolDetonatorUpBlue;

            spriteBatch.Draw(textureToUse, new Rectangle(screenWidth / 2 /*- 22 * 3*/, screenHeight - 77 * 3 + 14 * 3, 75 * 3, 77 * 3), Color.White);
        }

        public void RenderProspectron(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;
            graphicsDevice.SamplerStates[0].MagFilter = TextureFilter.Point;

            int drawX = screenWidth / 2 - 32 * 3;
            int drawY = screenHeight - 102 * 3;

            spriteBatch.Draw(_P.playerTeam == PlayerTeam.Red ? texToolRadarRed : texToolRadarBlue, new Rectangle(drawX, drawY, 70 * 3, 102 * 3), Color.White);

            if (_P.radarValue > 0)
                spriteBatch.Draw(texToolRadarLED, new Rectangle(drawX, drawY, 70 * 3, 102 * 3), Color.White);
            if (_P.radarValue == 200)
                spriteBatch.Draw(texToolRadarGold, new Rectangle(drawX, drawY, 70 * 3, 102 * 3), Color.White);
            if (_P.radarValue == 1000)
                spriteBatch.Draw(texToolRadarDiamond, new Rectangle(drawX, drawY, 70 * 3, 102 * 3), Color.White);
            if (_P.playerToolCooldown > 0.2f)
                spriteBatch.Draw(texToolRadarFlash, new Rectangle(drawX, drawY, 70 * 3, 102 * 3), Color.White);

            int pointerOffset = (int)(30 - _P.radarDistance) / 2;  // ranges from 0 to 15 inclusive
            if (_P.radarDistance == 30)
                pointerOffset = 15;
            spriteBatch.Draw(texToolRadarPointer, new Rectangle(drawX + 54 * 3, drawY + 20 * 3 + pointerOffset * 3, 4 * 3, 5 * 3), Color.White);
        }

        public void RenderConstructionGun(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, BlockType blockType)
        {
            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;
            graphicsDevice.SamplerStates[0].MagFilter = TextureFilter.Point;

            int drawX = screenWidth / 2 - 60 * 3;
            int drawY = screenHeight - 91 * 3;

            Texture2D gunSprite = texToolBuild;
            if (_P.constructionGunAnimation < -0.001)
                gunSprite = texToolBuildCharge;
            else if (_P.constructionGunAnimation > 0.3)
                gunSprite = texToolBuildBlast;
            else if (_P.constructionGunAnimation > 0.001)
                gunSprite = texToolBuildSmoke;
            spriteBatch.Draw(gunSprite, new Rectangle(drawX, drawY, 120 * 3, 126 * 3), Color.White);
            spriteBatch.Draw(blockIcons[blockType], new Rectangle(drawX + 37 * 3, drawY + 50 * 3, 117, 63), Color.White);
        }

        public void drawChat(List<ChatMessage>messages, GraphicsDevice graphicsDevice)
        {
            int newlines = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                Color chatColor = Color.White;
                if (messages[i].type == ChatMessageType.SayRedTeam)
                    chatColor = _P.red;// Defines.IM_RED;
                if (messages[i].type == ChatMessageType.SayBlueTeam)
                    chatColor = _P.blue;// Defines.IM_BLUE;

                int y = graphicsDevice.Viewport.Height - 114;
                newlines += messages[i].newlines;
                y -= 16 * newlines;
                //y -= 16 * i;

                spriteBatch.DrawString(uiFont, messages[i].message, new Vector2(22, y), Color.Black);
                spriteBatch.DrawString(uiFont, messages[i].message, new Vector2(20, y-2), chatColor);//graphicsDevice.Viewport.Height - 116 - 16 * i), chatColor);
            }
        }

        public void Render(GraphicsDevice graphicsDevice)
        {
            // If we don't have _P, grab it from the current gameInstance.
            // We can't do this in the constructor because we are created in the property bag's constructor!
            if (_P == null)
                _P = gameInstance.propertyBag;

            // Draw the UI.
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);

            // Draw the crosshair.
            spriteBatch.Draw(texCrosshairs, new Rectangle(graphicsDevice.Viewport.Width / 2 - texCrosshairs.Width / 2,
                                                            graphicsDevice.Viewport.Height / 2 - texCrosshairs.Height / 2,
                                                            texCrosshairs.Width,
                                                            texCrosshairs.Height), Color.White);

            // If equipped, draw the tool.
            switch (_P.playerTools[_P.playerToolSelected])
            {
                case PlayerTools.Detonator:
                    RenderDetonator(graphicsDevice, spriteBatch);
                    break;

                case PlayerTools.ProspectingRadar:
                    RenderProspectron(graphicsDevice, spriteBatch);
                    break;

                case PlayerTools.ConstructionGun:
                    RenderConstructionGun(graphicsDevice, spriteBatch, _P.playerBlocks[_P.playerBlockSelected]);
                    break;

                case PlayerTools.DeconstructionGun:
                    RenderConstructionGun(graphicsDevice, spriteBatch, BlockType.None);
                    break;

                default:
                    {
                        // Draw info about what we have equipped.
                        PlayerTools currentTool = _P.playerTools[_P.playerToolSelected];
                        BlockType currentBlock = _P.playerBlocks[_P.playerBlockSelected];
                        string equipment = currentTool.ToString();
                        if (currentTool == PlayerTools.ConstructionGun)
                            equipment += " - " + currentBlock.ToString() + " (" + BlockInformation.GetCost(currentBlock) + ")";
                        RenderMessageCenter(spriteBatch, equipment, new Vector2(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height - 20), Color.White, Color.Black);
                    }
                    break;
            }

            if (gameInstance.DrawFrameRate)
                RenderMessageCenter(spriteBatch, String.Format("FPS: {0:000}", gameInstance.FrameRate), new Vector2(60, graphicsDevice.Viewport.Height - 20), Color.Gray, Color.Black);

            // Show the altimeter.
            int altitude = (int)(_P.playerPosition.Y - 64 + Defines.GROUND_LEVEL);
            RenderMessageCenter(spriteBatch, String.Format("ALTITUDE: {0:00}", altitude), new Vector2(graphicsDevice.Viewport.Width - 90, graphicsDevice.Viewport.Height - 20), altitude >= 0 ? Color.Gray : Defines.IM_RED, Color.Black);

            // Draw bank instructions.
            if (_P.AtBankTerminal())
                RenderMessageCenter(spriteBatch, "8: DEPOSIT 50 ORE  9: WITHDRAW 50 ORE", new Vector2(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2 + 60), Color.White, Color.Black);

            // Are they trying to change class when they cannot?
            //if (Keyboard.GetState().IsKeyDown(Keys.M) && _P.playerPosition.Y <= 64 - Defines.GROUND_LEVEL && _P.chatMode == ChatMessageType.None)
            //    RenderMessageCenter(spriteBatch, "YOU CANNOT CHANGE YOUR CLASS BELOW THE SURFACE", new Vector2(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2 + 90), Color.White, Color.Black);

            // Draw the text-based information panel.
            int textStart = (graphicsDevice.Viewport.Width - 1024) / 2;
            spriteBatch.Draw(texBlank, new Rectangle(0, 0, graphicsDevice.Viewport.Width, 20), Color.Black);
            spriteBatch.DrawString(uiFont, "ORE: " + _P.playerOre + "/" + _P.playerOreMax, new Vector2(textStart + 3, 2), Color.White);
            spriteBatch.DrawString(uiFont, "LOOT: $" + _P.playerCash, new Vector2(textStart + 170, 2), Color.White);
            spriteBatch.DrawString(uiFont, "WEIGHT: " + _P.playerWeight + "/" + _P.playerWeightMax, new Vector2(textStart + 340, 2), Color.White);
            spriteBatch.DrawString(uiFont, "TEAM ORE: " + _P.teamOre, new Vector2(textStart + 515, 2), Color.White);
            spriteBatch.DrawString(uiFont, _P.redName + ": $" + _P.teamRedCash, new Vector2(textStart + 700, 2), _P.red);// Defines.IM_RED);
            spriteBatch.DrawString(uiFont, _P.blueName + ": $" + _P.teamBlueCash, new Vector2(textStart + 860, 2), _P.blue);// Defines.IM_BLUE);

            // Draw player information.
            if ((Keyboard.GetState().IsKeyDown(Keys.Tab) && _P.screenEffect == ScreenEffect.None) || _P.teamWinners != PlayerTeam.None)
            {
                spriteBatch.Draw(texBlank, new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height), new Color(Color.Black, 0.7f));

                //Server name
                RenderMessageCenter(spriteBatch, _P.serverName, new Vector2(graphicsDevice.Viewport.Width / 2, 32), _P.playerTeam == PlayerTeam.Blue ? _P.blue : _P.red, Color.Black);//Defines.IM_BLUE : Defines.IM_RED, Color.Black);
                
                if (_P.teamWinners != PlayerTeam.None)
                {
                    string teamName = _P.teamWinners == PlayerTeam.Red ? "RED" : "BLUE";
                    Color teamColor = _P.teamWinners == PlayerTeam.Red ? _P.red : _P.blue;//Defines.IM_RED : Defines.IM_BLUE;
                    string gameOverMessage = "GAME OVER - " + teamName + " TEAM WINS!";
                    RenderMessageCenter(spriteBatch, gameOverMessage, new Vector2(graphicsDevice.Viewport.Width / 2, 150), teamColor, new Color(0, 0, 0, 0));
                }

                int drawY = 200;
                foreach (Player p in _P.playerList.Values)
                {
                    if (p.Team != PlayerTeam.Red)
                        continue;
                    RenderMessageCenter(spriteBatch, p.Handle + " ( $" + p.Score + " )", new Vector2(graphicsDevice.Viewport.Width / 4, drawY), _P.red, new Color(0, 0, 0, 0));//Defines.IM_RED
                    drawY += 35;
                }
                drawY = 200;
                foreach (Player p in _P.playerList.Values)
                {
                    if (p.Team != PlayerTeam.Blue)
                        continue;
                    RenderMessageCenter(spriteBatch, p.Handle + " ( $" + p.Score + " )", new Vector2(graphicsDevice.Viewport.Width * 3 / 4, drawY), _P.blue, new Color(0, 0, 0, 0)); //Defines.IM_BLUE
                    drawY += 35;
                }
            }

            // Draw the chat buffer.
            if (_P.chatMode == ChatMessageType.SayAll)
            {
                spriteBatch.DrawString(uiFont, "ALL> " + _P.chatEntryBuffer, new Vector2(22, graphicsDevice.Viewport.Height - 98), Color.Black);
                spriteBatch.DrawString(uiFont, "ALL> " + _P.chatEntryBuffer, new Vector2(20, graphicsDevice.Viewport.Height - 100), Color.White);
            }
            else if (_P.chatMode == ChatMessageType.SayBlueTeam || _P.chatMode == ChatMessageType.SayRedTeam)
            {
                spriteBatch.DrawString(uiFont, "TEAM> " + _P.chatEntryBuffer, new Vector2(22, graphicsDevice.Viewport.Height - 98), Color.Black);
                spriteBatch.DrawString(uiFont, "TEAM> " + _P.chatEntryBuffer, new Vector2(20, graphicsDevice.Viewport.Height - 100), Color.White);
            }
            if (_P.chatMode != ChatMessageType.None)
            {
                drawChat(_P.chatFullBuffer,graphicsDevice);
                /*for (int i = 0; i < _P.chatFullBuffer.Count; i++)
                {
                    Color chatColor = Color.White;
                    chatColor = _P.chatFullBuffer[i].type == ChatMessageType.SayAll ? Color.White : _P.chatFullBuffer[i].type == ChatMessageType.SayRedTeam ? InfiniminerGame.IM_RED : InfiniminerGame.IM_BLUE;
                    
                    spriteBatch.DrawString(uiFont, _P.chatFullBuffer[i].message, new Vector2(22, graphicsDevice.Viewport.Height - 114 - 16 * i), Color.Black);
                    spriteBatch.DrawString(uiFont, _P.chatFullBuffer[i].message, new Vector2(20, graphicsDevice.Viewport.Height - 116 - 16 * i), chatColor);
                }*/
            }
            else
            {
                drawChat(_P.chatBuffer,graphicsDevice);
            }

            // Draw the player radar.
            spriteBatch.Draw(texRadarBackground, new Vector2(10, 30), Color.White);
            foreach (Player p in _P.playerList.Values)
                if (p.Team == _P.playerTeam && p.Alive)
                    RenderRadarBlip(spriteBatch, p.ID == _P.playerMyId ? _P.playerPosition : p.Position, p.Team == PlayerTeam.Red ? _P.red : _P.blue, p.Ping > 0, ""); //Defines.IM_RED : Defines.IM_BLUE, p.Ping > 0, "");
            foreach (KeyValuePair<Vector3, Beacon> bPair in _P.beaconList)
                if (bPair.Value.Team == _P.playerTeam)
                    RenderRadarBlip(spriteBatch, bPair.Key, Color.White, false, bPair.Value.ID);
            RenderRadarBlip(spriteBatch, new Vector3(100000, 0, 32), Color.White, false, "NORTH");

            spriteBatch.Draw(texRadarForeground, new Vector2(10, 30), Color.White);

            // Draw escape message.
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                RenderMessageCenter(spriteBatch, "PRESS Y TO CONFIRM THAT YOU WANT TO QUIT.", new Vector2(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2 + 30), Color.White, Color.Black);
                RenderMessageCenter(spriteBatch, "PRESS K TO COMMIT PIXELCIDE.", new Vector2(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2 + 80), Color.White, Color.Black);
            }

            // Draw the current screen effect.
            if (_P.screenEffect == ScreenEffect.Death)
            {
                Color drawColor = new Color(1 - (float)_P.screenEffectCounter * 0.5f, 0f, 0f);
                spriteBatch.Draw(texBlank, new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height), drawColor);
                if (_P.screenEffectCounter >= 2)
                    RenderMessageCenter(spriteBatch, "You have died. Click to respawn.", new Vector2(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2), Color.White, Color.Black);
            }
            if (_P.screenEffect == ScreenEffect.Teleport || _P.screenEffect == ScreenEffect.Explosion)
            {
                Color drawColor = new Color(1, 1, 1, 1 - (float)_P.screenEffectCounter * 0.5f);
                spriteBatch.Draw(texBlank, new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height), drawColor);
                if (_P.screenEffectCounter > 2)
                    _P.screenEffect = ScreenEffect.None;
            }
            if (_P.screenEffect == ScreenEffect.Fall)
            {
                Color drawColor = new Color(1, 0, 0, 1 - (float)_P.screenEffectCounter * 0.5f);
                spriteBatch.Draw(texBlank, new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height), drawColor);
                if (_P.screenEffectCounter > 2)
                    _P.screenEffect = ScreenEffect.None;
            }

            // Draw the help screen.
            if (Keyboard.GetState().IsKeyDown(Keys.F1))
            {
                spriteBatch.Draw(texBlank, new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height), Color.Black);
                spriteBatch.Draw(texHelp, drawRect, Color.White);
            }

            spriteBatch.End();
        }
    }
}
