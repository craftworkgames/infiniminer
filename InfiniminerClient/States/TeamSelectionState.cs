using System;
using System.Collections.Generic;
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

namespace Infiniminer.States
{
    public class TeamSelectionState : State
    {
        Texture2D texMenu;
        Rectangle drawRect;
        string nextState = null;
        SpriteFont uiFont;
        bool canCancel = false;

        ClickRegion[] clkTeamMenu = new ClickRegion[2] {
	        new ClickRegion(new Rectangle(229,156,572,190), "red"), 
	        new ClickRegion(new Rectangle(135,424,761,181), "blue")//,
            //new ClickRegion(new Rectangle(0,0,0,0), "cancel")
        };

        public override void OnEnter(string oldState)
        {
            _SM.IsMouseVisible = true;

            texMenu = _SM.Content.Load<Texture2D>("menus/tex_menu_team");

            drawRect = new Rectangle(_SM.GraphicsDevice.Viewport.Width / 2 - 1024 / 2,
                                     _SM.GraphicsDevice.Viewport.Height / 2 - 768 / 2,
                                     1024,
                                     1024);

            uiFont = _SM.Content.Load<SpriteFont>(GlobalVariables.fontUI);

            if (oldState == "Infiniminer.States.MainGameState")
                canCancel = true;
        }

        public override void OnLeave(string newState)
        {

        }

        public override string OnUpdate(GameTime gameTime, KeyboardState keyState, MouseState mouseState)
        {
            // Do network stuff.
            (_SM as InfiniminerGame).UpdateNetwork(gameTime);

            return nextState;
        }

        public override void OnRenderAtEnter(GraphicsDevice graphicsDevice)
        {

        }

        public void QuickDrawText(SpriteBatch spriteBatch, string text, int y, Color color)
        {
            spriteBatch.DrawString(uiFont, text, new Vector2(_SM.GraphicsDevice.Viewport.Width / 2 - uiFont.MeasureString(text).X / 2, drawRect.Y + y), color);
        }

        public override void OnRenderAtUpdate(GraphicsDevice graphicsDevice, GameTime gameTime)
        {
            int teamCountA = 0, teamCountB = 0;
            foreach (Player p in _P.playerList.Values)
            {
                if (p.Team == PlayerTeam.A)
                    teamCountA += 1;
                else if (p.Team == PlayerTeam.B)
                    teamCountB += 1;
            }

            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            spriteBatch.Draw(texMenu, drawRect, Color.White);
            QuickDrawText(spriteBatch, "" + teamCountA + " PLAYERS", 360, SessionVariables.teams[(byte)PlayerTeam.A].color);
            QuickDrawText(spriteBatch, "" + teamCountB + " PLAYERS", 620, SessionVariables.teams[(byte)PlayerTeam.B].color);
            spriteBatch.End();
        }

        public override void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape && canCancel)
                nextState = "Infiniminer.States.MainGameState";
        }

        public override void OnKeyUp(Keys key)
        {

        }

        public override void OnMouseDown(MouseButton button, int x, int y)
        {
            x -= drawRect.X;
            y -= drawRect.Y;
            switch (ClickRegion.HitTest(clkTeamMenu, new Point(x, y)))
            {
                case "A":
                    if (_P.playerTeam == PlayerTeam.A && canCancel)
                        nextState = "Infiniminer.States.MainGameState";
                    else
                    {
                        _P.SetPlayerTeam(PlayerTeam.A);
                        nextState = "Infiniminer.States.ClassSelectionState";
                    }
                    _P.PlaySound(InfiniminerSound.ClickHigh);
                    break;
                case "B":
                    if (_P.playerTeam == PlayerTeam.B && canCancel)
                        nextState = "Infiniminer.States.MainGameState";
                    else
                    {
                        _P.SetPlayerTeam(PlayerTeam.B);
                        nextState = "Infiniminer.States.ClassSelectionState";
                    }
                    _P.PlaySound(InfiniminerSound.ClickHigh);
                    break;
                case "cancel":
                    if (canCancel)
                    {
                        nextState = "Infiniminer.States.MainGameState";
                        _P.PlaySound(InfiniminerSound.ClickHigh);
                    }
                    break;
            }
        }

        public override void OnMouseUp(MouseButton button, int x, int y)
        {

        }

        public override void OnMouseScroll(int scrollDelta)
        {

        }
    }
}
