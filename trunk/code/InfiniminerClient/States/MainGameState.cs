using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using StateMasher;
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

namespace Infiniminer.States
{
    public class MainGameState : State
    {
        const float MOVESPEED = 3.5f;
        const float GRAVITY = -8.0f;
        const float JUMPVELOCITY = 4.0f;
        const float CLIMBVELOCITY = 2.5f;
        const float DIEVELOCITY = 15.0f;

        string nextState = null;
        bool mouseInitialized = false;

        public override void OnEnter(string oldState)
        {
            _SM.IsMouseVisible = false;
        }

        public override void OnLeave(string newState)
        {
            _P.chatEntryBuffer = "";
            _P.chatMode = ChatMessageType.None;
        }

        public override string OnUpdate(GameTime gameTime, KeyboardState keyState, MouseState mouseState)
        {
            // Update network stuff.
            (_SM as InfiniminerGame).UpdateNetwork(gameTime);

            // Update the current screen effect.
            _P.screenEffectCounter += gameTime.ElapsedGameTime.TotalSeconds;

            // Update engines.
            _P.skyplaneEngine.Update(gameTime);
            _P.playerEngine.Update(gameTime);
            _P.interfaceEngine.Update(gameTime);
            _P.particleEngine.Update(gameTime);

            // Count down the tool cooldown.
            if (_P.playerToolCooldown > 0)
            {
                _P.playerToolCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_P.playerToolCooldown <= 0)
                    _P.playerToolCooldown = 0;
            }

            // Moving the mouse changes where we look.
            if (_SM.WindowHasFocus())
            {
                if (mouseInitialized)
                {
                    int dx = mouseState.X - _SM.GraphicsDevice.Viewport.Width / 2;
                    int dy = mouseState.Y - _SM.GraphicsDevice.Viewport.Height / 2;

                    if ((_SM as InfiniminerGame).InvertMouseYAxis)
                        dy = -dy;

                    _P.playerCamera.Yaw -= dx * _P.mouseSensitivity;
                    _P.playerCamera.Pitch = (float)Math.Min(Math.PI * 0.49, Math.Max(-Math.PI * 0.49, _P.playerCamera.Pitch - dy * _P.mouseSensitivity));
                }
                else
                {
                    mouseInitialized = true;
                }
                Mouse.SetPosition(_SM.GraphicsDevice.Viewport.Width / 2, _SM.GraphicsDevice.Viewport.Height / 2);
            }
            else
                mouseInitialized = false;

            // Digging like a freaking terrier! Now for everyone!
            if (mouseInitialized && mouseState.LeftButton == ButtonState.Pressed && !_P.playerDead && _P.playerToolCooldown == 0 && _P.playerTools[_P.playerToolSelected] == PlayerTools.Pickaxe)
            {
                _P.FirePickaxe();
                _P.playerToolCooldown = _P.GetToolCooldown(PlayerTools.Pickaxe) * (_P.playerClass == PlayerClass.Miner ? 0.4f : 1.0f);
            }

            // Prospector radar stuff.
            if (!_P.playerDead && _P.playerToolCooldown == 0 && _P.playerTools[_P.playerToolSelected] == PlayerTools.ProspectingRadar)
            {
                float oldValue = _P.radarValue;
                _P.ReadRadar(ref _P.radarDistance, ref _P.radarValue);
                if (_P.radarValue != oldValue)
                {
                    if (_P.radarValue == 200)
                        _P.PlaySound(InfiniminerSound.RadarLow);
                    if (_P.radarValue == 1000)
                        _P.PlaySound(InfiniminerSound.RadarHigh);
                }
            }

            // Update the player's position.
            if (!_P.playerDead)
                UpdatePlayerPosition(gameTime, keyState);

            // Update the camera regardless of if we're alive or not.
            _P.UpdateCamera(gameTime);

            return nextState;
        }

        private void UpdatePlayerPosition(GameTime gameTime, KeyboardState keyState)
        {
            // Double-speed move flag, set if we're on road.
            bool movingOnRoad = false;
            bool sprinting = false;

            // Apply "gravity".
            _P.playerVelocity.Y += GRAVITY * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Vector3 footPosition = _P.playerPosition + new Vector3(0f, -1.5f, 0f);
            Vector3 headPosition = _P.playerPosition + new Vector3(0f, 0.1f, 0f);
            if (_P.blockEngine.SolidAtPointForPlayer(footPosition) || _P.blockEngine.SolidAtPointForPlayer(headPosition))
            {
                BlockType standingOnBlock = _P.blockEngine.BlockAtPoint(footPosition);
                BlockType hittingHeadOnBlock = _P.blockEngine.BlockAtPoint(headPosition);

                // If we"re hitting the ground with a high velocity, die!
                if (standingOnBlock != BlockType.None && _P.playerVelocity.Y < 0)
                {
                    float fallDamage = Math.Abs(_P.playerVelocity.Y) / DIEVELOCITY;
                    if (fallDamage >= 1)
                    {
                        _P.PlaySoundForEveryone(InfiniminerSound.GroundHit, _P.playerPosition);
                        _P.KillPlayer(Defines.deathByFall);//"WAS KILLED BY GRAVITY!");
                        return;
                    }
                    else if (fallDamage > 0.5)
                    {
                        // Fall damage of 0.5 maps to a screenEffectCounter value of 2, meaning that the effect doesn't appear.
                        // Fall damage of 1.0 maps to a screenEffectCounter value of 0, making the effect very strong.
                        if (standingOnBlock != BlockType.Jump)
                        {
                            _P.screenEffect = ScreenEffect.Fall;
                            _P.screenEffectCounter = 2 - (fallDamage - 0.5) * 4;
                            _P.PlaySoundForEveryone(InfiniminerSound.GroundHit, _P.playerPosition);
                        }
                    }
                }

                // If the player has their head stuck in a block, push them down.
                if (_P.blockEngine.SolidAtPointForPlayer(headPosition))
                {
                    int blockIn = (int)(headPosition.Y);
                    _P.playerPosition.Y = (float)(blockIn - 0.15f);
                }

                // If the player is stuck in the ground, bring them out.
                // This happens because we're standing on a block at -1.5, but stuck in it at -1.4, so -1.45 is the sweet spot.
                if (_P.blockEngine.SolidAtPointForPlayer(footPosition))
                {
                    int blockOn = (int)(footPosition.Y);
                    _P.playerPosition.Y = (float)(blockOn + 1 + 1.45);
                }

                _P.playerVelocity.Y = 0;

                // Logic for standing on a block.
                switch (standingOnBlock)
                {
                    case BlockType.Jump:
                        _P.playerVelocity.Y = 2.5f * JUMPVELOCITY;
                        _P.PlaySoundForEveryone(InfiniminerSound.Jumpblock, _P.playerPosition);
                        break;

                    case BlockType.Road:
                        movingOnRoad = true;
                        break;

                    case BlockType.Lava:
                        _P.KillPlayer(Defines.deathByLava);
                        return;
                }

                // Logic for bumping your head on a block.
                switch (hittingHeadOnBlock)
                {
                    case BlockType.Shock:
                        _P.KillPlayer(Defines.deathByElec);
                        return;

                    case BlockType.Lava:
                        _P.KillPlayer(Defines.deathByLava);
                        return;
                }
            }
            _P.playerPosition += _P.playerVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Death by falling off the map.
            if (_P.playerPosition.Y < -30)
            {
                _P.KillPlayer(Defines.deathByMiss);
                return;
            }

            // Pressing forward moves us in the direction we"re looking.
            Vector3 moveVector = Vector3.Zero;

            if (_P.chatMode == ChatMessageType.None)
            {
                if ((_SM as InfiniminerGame).keyBinds.IsPressed(Buttons.Forward))//keyState.IsKeyDown(Keys.W))
                    moveVector += _P.playerCamera.GetLookVector();
                if ((_SM as InfiniminerGame).keyBinds.IsPressed(Buttons.Backward))//keyState.IsKeyDown(Keys.S))
                    moveVector -= _P.playerCamera.GetLookVector();
                if ((_SM as InfiniminerGame).keyBinds.IsPressed(Buttons.Right))//keyState.IsKeyDown(Keys.D))
                    moveVector += _P.playerCamera.GetRightVector();
                if ((_SM as InfiniminerGame).keyBinds.IsPressed(Buttons.Left))//keyState.IsKeyDown(Keys.A))
                    moveVector -= _P.playerCamera.GetRightVector();
                //Sprinting
                if ((_SM as InfiniminerGame).keyBinds.IsPressed(Buttons.Sprint))//keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift))
                    sprinting = true;
            }

            if (moveVector.X != 0 || moveVector.Z != 0)
            {
                // "Flatten" the movement vector so that we don"t move up/down.
                moveVector.Y = 0;
                moveVector.Normalize();
                moveVector *= MOVESPEED * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (movingOnRoad)
                    moveVector *= 2;
                // Sprinting doubles speed, even if already on road
                if (sprinting)
                    moveVector *= 1.5f;

                // Attempt to move, doing collision stuff.
                if (TryToMoveTo(moveVector, gameTime)) { }
                else if (!TryToMoveTo(new Vector3(0, 0, moveVector.Z), gameTime)) { }
                else if (!TryToMoveTo(new Vector3(moveVector.X, 0, 0), gameTime)) { }
            }
        }

        private bool TryToMoveTo(Vector3 moveVector, GameTime gameTime)
        {
            // Build a "test vector" that is a little longer than the move vector.
            float moveLength = moveVector.Length();
            Vector3 testVector = moveVector;
            testVector.Normalize();
            testVector = testVector * (moveLength + 0.1f);

            // Apply this test vector.
            Vector3 movePosition = _P.playerPosition + testVector;
            Vector3 midBodyPoint = movePosition + new Vector3(0, -0.7f, 0);
            Vector3 lowerBodyPoint = movePosition + new Vector3(0, -1.4f, 0);

            if (!_P.blockEngine.SolidAtPointForPlayer(movePosition) && !_P.blockEngine.SolidAtPointForPlayer(lowerBodyPoint) && !_P.blockEngine.SolidAtPointForPlayer(midBodyPoint))
            {
                _P.playerPosition = _P.playerPosition + moveVector;
                return true;
            }

            // It's solid there, so while we can't move we have officially collided with it.
            BlockType lowerBlock = _P.blockEngine.BlockAtPoint(lowerBodyPoint);
            BlockType midBlock = _P.blockEngine.BlockAtPoint(midBodyPoint);
            BlockType upperBlock = _P.blockEngine.BlockAtPoint(movePosition);

            // It's solid there, so see if it's a lava block. If so, touching it will kill us!
            if (upperBlock == BlockType.Lava || lowerBlock == BlockType.Lava || midBlock == BlockType.Lava)
            {
                _P.KillPlayer(Defines.deathByLava);
                return true;
            }

            // If it's a ladder, move up.
            if (upperBlock == BlockType.Ladder || lowerBlock == BlockType.Ladder || midBlock == BlockType.Ladder)
            {
                _P.playerVelocity.Y = CLIMBVELOCITY;
                Vector3 footPosition = _P.playerPosition + new Vector3(0f, -1.5f, 0f);
                if (_P.blockEngine.SolidAtPointForPlayer(footPosition))
                    _P.playerPosition.Y += 0.1f;
                return true;
            }

            return false;
        }

        public override void OnRenderAtEnter(GraphicsDevice graphicsDevice)
        {

        }

        public override void OnRenderAtUpdate(GraphicsDevice graphicsDevice, GameTime gameTime)
        {
            _P.skyplaneEngine.Render(graphicsDevice);
            _P.particleEngine.Render(graphicsDevice);
            _P.playerEngine.Render(graphicsDevice);
            _P.blockEngine.Render(graphicsDevice, gameTime);
            _P.playerEngine.RenderPlayerNames(graphicsDevice);
            _P.interfaceEngine.Render(graphicsDevice);

            _SM.Window.Title = "Infiniminer";
        }

        DateTime startChat = DateTime.Now;
        public override void OnCharEntered(EventInput.CharacterEventArgs e)
        {
            if ((int)e.Character < 32 || (int)e.Character > 126) //From space to tilde
                return; //Do nothing
            if (_P.chatMode != ChatMessageType.None)
            {
                //Chat delay to avoid entering the "start chat" key, an unfortunate side effect of the new key bind system
                TimeSpan diff = DateTime.Now - startChat;
                if (diff.Milliseconds >= 2)
                    if (!(Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)))
                    {
                        _P.chatEntryBuffer += e.Character;
                    }
            }
        }

        private void HandleInput(Buttons input)
        {
            switch (input)
            {
                case Buttons.Fire:
                    if (_P.playerToolCooldown <= 0)
                    {
                        switch (_P.playerTools[_P.playerToolSelected])
                        {
                            // Disabled as everyone speed-mines now.
                            //case PlayerTools.Pickaxe:
                            //    if (_P.playerClass != PlayerClass.Miner)
                            //        _P.FirePickaxe();
                            //    break;

                            case PlayerTools.ConstructionGun:
                                _P.FireConstructionGun(_P.playerBlocks[_P.playerBlockSelected]);//, !(button == MouseButton.LeftButton));//_P.FireConstructionGun(_P.playerBlocks[_P.playerBlockSelected]);
                                break;

                            case PlayerTools.DeconstructionGun:
                                _P.FireDeconstructionGun();
                                break;

                            case PlayerTools.Detonator:
                                _P.PlaySound(InfiniminerSound.ClickHigh);
                                _P.FireDetonator();
                                break;

                            case PlayerTools.ProspectingRadar:
                                _P.FireRadar();
                                break;
                        }
                    }
                    break;
                case Buttons.Jump:
                    {
                        Vector3 footPosition = _P.playerPosition + new Vector3(0f, -1.5f, 0f);
                        if (_P.blockEngine.SolidAtPointForPlayer(footPosition) && _P.playerVelocity.Y == 0)
                        {
                            _P.playerVelocity.Y = JUMPVELOCITY;
                            float amountBelowSurface = ((ushort)footPosition.Y) + 1 - footPosition.Y;
                            _P.playerPosition.Y += amountBelowSurface + 0.01f;
                        }
                    }
                    break;
                case Buttons.ToolUp:
                    _P.PlaySound(InfiniminerSound.ClickLow);
                    _P.playerToolSelected += 1;
                    if (_P.playerToolSelected >= _P.playerTools.Length)
                        _P.playerToolSelected = 0;
                    break;
                case Buttons.ToolDown:
                    _P.PlaySound(InfiniminerSound.ClickLow);
                    _P.playerToolSelected -= 1;
                    if (_P.playerToolSelected < 0)
                        _P.playerToolSelected = _P.playerTools.Length;
                    break;
                case Buttons.Tool1:
                    _P.playerToolSelected = 0;
                    _P.PlaySound(InfiniminerSound.ClickLow);
                    if (_P.playerToolSelected >= _P.playerTools.Length)
                        _P.playerToolSelected = _P.playerTools.Length - 1;
                    break;
                case Buttons.Tool2:
                    _P.playerToolSelected = 1;
                    _P.PlaySound(InfiniminerSound.ClickLow);
                    if (_P.playerToolSelected >= _P.playerTools.Length)
                        _P.playerToolSelected = _P.playerTools.Length - 1;
                    break;
                case Buttons.Tool3:
                    _P.playerToolSelected = 2;
                    _P.PlaySound(InfiniminerSound.ClickLow);
                    if (_P.playerToolSelected >= _P.playerTools.Length)
                        _P.playerToolSelected = _P.playerTools.Length - 1;
                    break;
                case Buttons.Tool4:
                    _P.playerToolSelected = 3;
                    _P.PlaySound(InfiniminerSound.ClickLow);
                    if (_P.playerToolSelected >= _P.playerTools.Length)
                        _P.playerToolSelected = _P.playerTools.Length - 1;
                    break;
                case Buttons.Tool5:
                    _P.playerToolSelected = 4;
                    _P.PlaySound(InfiniminerSound.ClickLow);
                    if (_P.playerToolSelected >= _P.playerTools.Length)
                        _P.playerToolSelected = _P.playerTools.Length - 1;
                    break;
                case Buttons.BlockUp:
                    if (_P.playerTools[_P.playerToolSelected] == PlayerTools.ConstructionGun)
                    {
                        _P.PlaySound(InfiniminerSound.ClickLow);
                        _P.playerBlockSelected += 1;
                        if (_P.playerBlockSelected >= _P.playerBlocks.Length)
                            _P.playerBlockSelected = 0;
                    }
                    break;
                case Buttons.BlockDown:
                    if (_P.playerTools[_P.playerToolSelected] == PlayerTools.ConstructionGun)
                    {
                        _P.PlaySound(InfiniminerSound.ClickLow);
                        _P.playerBlockSelected -= 1;
                        if (_P.playerBlockSelected < 0)
                            _P.playerBlockSelected = _P.playerBlocks.Length-1;
                    }
                    break;
                case Buttons.Deposit:
                    if (_P.AtBankTerminal())
                    {
                        _P.DepositOre();
                        _P.PlaySound(InfiniminerSound.ClickHigh);
                    }
                    break;
                case Buttons.Withdraw:
                    if (_P.AtBankTerminal())
                    {
                        _P.WithdrawOre();
                        _P.PlaySound(InfiniminerSound.ClickHigh);
                    }
                    break;
                case Buttons.Ping:
                    {
                        NetBuffer msgBuffer = _P.netClient.CreateBuffer();
                        msgBuffer.Write((byte)InfiniminerMessage.PlayerPing);
                        msgBuffer.Write(_P.playerMyId);
                        _P.netClient.SendMessage(msgBuffer, NetChannel.ReliableUnordered);
                    }
                    break;
                case Buttons.ChangeClass:
                    nextState = "Infiniminer.States.ClassSelectionState";
                    break;
                case Buttons.ChangeTeam:
                    nextState = "Infiniminer.States.TeamSelectionState";
                    break;
                case Buttons.SayAll:
                    _P.chatMode = ChatMessageType.SayAll;
                    startChat = DateTime.Now;
                    break;
                case Buttons.SayTeam:
                    _P.chatMode = _P.playerTeam == PlayerTeam.Red ? ChatMessageType.SayRedTeam : ChatMessageType.SayBlueTeam;
                    startChat = DateTime.Now;
                    break;
            }
        }

        public override void OnKeyDown(Keys key)
        {
            // Exit!
            if (key == Keys.Y && Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                _P.netClient.Disconnect("Client disconnected.");
                nextState = "Infiniminer.States.ServerBrowserState";
            }

            // Pixelcide!
            if (key == Keys.K && Keyboard.GetState().IsKeyDown(Keys.Escape) && !_P.playerDead)
            {
                _P.KillPlayer(Defines.deathBySuic);//"HAS COMMMITTED PIXELCIDE!");
                return;
            }

            //Map saving!
            if ((Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)) && key == Keys.S)
            {
                _P.SaveMap();
                return;
            }

            if (_P.chatMode != ChatMessageType.None)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl))
                {
                    if (key == Keys.V)
                    {
                        _P.chatEntryBuffer += System.Windows.Forms.Clipboard.GetText();
                        return;
                    }
                    else if (key == Keys.C)
                    {
                        System.Windows.Forms.Clipboard.SetText(_P.chatEntryBuffer);
                        return;
                    }
                    else if (key == Keys.X)
                    {
                        System.Windows.Forms.Clipboard.SetText(_P.chatEntryBuffer);
                        _P.chatEntryBuffer = "";
                        return;
                    }
                }
                // Put the characters in the chat buffer.
                if (key == Keys.Enter)
                {
                    // If we have an actual message to send, fire it off at the server.
                    if (_P.chatEntryBuffer.Length > 0)
                    {
                        if (_P.netClient.Status == NetConnectionStatus.Connected)
                        {
                            NetBuffer msgBuffer = _P.netClient.CreateBuffer();
                            msgBuffer.Write((byte)InfiniminerMessage.ChatMessage);
                            msgBuffer.Write((byte)_P.chatMode);
                            msgBuffer.Write(_P.chatEntryBuffer);
                            _P.netClient.SendMessage(msgBuffer, NetChannel.ReliableInOrder3);
                        }
                        else
                        {
                            _P.addChatMessage("Not connected to server.", ChatMessageType.SayAll, 10);
                        }
                    }

                    _P.chatEntryBuffer = "";
                    _P.chatMode = ChatMessageType.None;
                }
                else if (key == Keys.Back)
                {
                    if (_P.chatEntryBuffer.Length > 0)
                        _P.chatEntryBuffer = _P.chatEntryBuffer.Substring(0, _P.chatEntryBuffer.Length - 1);
                }
                else if (key == Keys.Escape)
                {
                    _P.chatEntryBuffer = "";
                    _P.chatMode = ChatMessageType.None;
                }
                return;
            }else if (!_P.playerDead)
                HandleInput((_SM as InfiniminerGame).keyBinds.GetBound(key));
            
        }

        public override void OnKeyUp(Keys key)
        {

        }

        public override void OnMouseDown(MouseButton button, int x, int y)
        {
            // If we're dead, come back to life.
            if (_P.playerDead && _P.screenEffectCounter > 2)
                _P.RespawnPlayer();
            else if (!_P.playerDead)
                HandleInput((_SM as InfiniminerGame).keyBinds.GetBound(button));
        }

        public override void OnMouseUp(MouseButton button, int x, int y)
        {

        }

        public override void OnMouseScroll(int scrollDelta)
        {
            if (_P.playerDead)
                return;
            else
            {
                if (scrollDelta >= 120)
                {
                    Console.WriteLine("Handling input for scroll up...");
                    HandleInput((_SM as InfiniminerGame).keyBinds.GetBound(MouseButton.WheelUp));//.keyBinds.GetBound(button));
                }
                else if (scrollDelta <= -120)
                {
                    HandleInput((_SM as InfiniminerGame).keyBinds.GetBound(MouseButton.WheelDown));
                }
            }
        }
    }
}
