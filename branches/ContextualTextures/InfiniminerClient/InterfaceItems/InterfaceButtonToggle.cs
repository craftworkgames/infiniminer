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
using Microsoft.Xna.Framework.Design;
using Infiniminer;

namespace InterfaceItems
{
    class InterfaceButtonToggle : InterfaceElement
    {
        private bool midClick = false;
        public bool clicked = false;
        public string offText = "Off";
        public string onText = "On";

        public InterfaceButtonToggle()
        {
        }

        public InterfaceButtonToggle(Infiniminer.InfiniminerGame gameInstance)
        {
            uiFont = gameInstance.Content.Load<SpriteFont>("font_04b08");
        }

        public InterfaceButtonToggle(Infiniminer.InfiniminerGame gameInstance, Infiniminer.PropertyBag pb)
        {
            uiFont = gameInstance.Content.Load<SpriteFont>("font_04b08");
            _P = pb;
        }

        public override void OnMouseDown(MouseButton button, int x, int y)
        {
            if (enabled && size.Contains(x, y))
            {
                midClick = true;
            }
            else
                midClick = false;
        }

        public override void OnMouseUp(MouseButton button, int x, int y)
        {
            if (enabled && midClick && size.Contains(x, y))
            {
                clicked = !clicked;
                _P.PlaySound(Infiniminer.InfiniminerSound.ClickLow);
            }
            midClick = false;
        }

        public override void Render(GraphicsDevice graphicsDevice)
        {
            if (visible && size.Width > 0 && size.Height > 0)
            {
                Color drawColour = new Color(1f, 1f, 1f);

                if (!enabled)
                    drawColour = new Color(.7f, .7f, .7f);
                else if (midClick)
                    drawColour = new Color(.85f, .85f, .85f);

                //Generate 1px white texture
                Texture2D shade = new Texture2D(graphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
                shade.SetData(new Color[] { Color.White });
                
                //Draw base button
                SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
                spriteBatch.Begin();
                spriteBatch.Draw(shade, size, drawColour);

                //Draw button text
                string dispText = offText;
                if (clicked)
                    dispText = onText;

                spriteBatch.DrawString(uiFont, dispText, new Vector2(size.X + size.Width / 2 - uiFont.MeasureString(dispText).X / 2, size.Y + size.Height / 2 - 8), Color.Black);

                if (text != "")
                {
                    //Draw text
                    spriteBatch.DrawString(uiFont, text, new Vector2(size.X, size.Y - 20), enabled ? Color.White : new Color(.7f, .7f, .7f));//drawColour);
                }

                /*spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
                spriteBatch.Draw(shade, new Rectangle(size.X, size.Y, size.Height, size.Height), drawColour);
                spriteBatch.Draw(shade, new Rectangle(size.X + size.Width - size.Height, size.Y, size.Height, size.Height), drawColour);

                //Draw line
                float sliderPercent = getPercent();
                int sliderPartialWidth = size.Height / 4;
                int midHeight = (int)(size.Height / 2) - 1;
                int actualWidth = size.Width - 2 * size.Height;
                int actualPosition = (int)(sliderPercent * actualWidth);
                spriteBatch.Draw(shade, new Rectangle(size.X, size.Y + midHeight, size.Width, 1), drawColour);

                //Draw slider
                spriteBatch.Draw(shade, new Rectangle(size.X + size.Height + actualPosition - sliderPartialWidth, size.Y + midHeight - sliderPartialWidth, size.Height / 2, size.Height / 2), drawColour);
                
                //Draw amount
                spriteBatch.DrawString(uiFont, (((float)(int)(value * 10)) / 10).ToString(), new Vector2(size.X, size.Y - 20), drawColour);
                */

                spriteBatch.End();
                shade.Dispose();
            }
        }
    }
}
