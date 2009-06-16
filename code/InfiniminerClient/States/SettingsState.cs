using System;
using System.Collections.Generic;

using System.Text;
using System.Diagnostics;
using StateMasher;
using InterfaceItems;
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
    class SettingsState : State
    {
        Texture2D texSettings;
        List<InterfaceElement> elements = new List<InterfaceElement>();
        Rectangle drawRect;
        int baseHeight = 14;
        //int sliderFullHeight = 52;
        int sliderFullWidth = 200;
        /*int buttonFullHeight = 36;
        int textFullHeight = 36;*/
        Vector2 currentPos = new Vector2(0, 0);
        int originalY = 0;

        ClickRegion[] clkMenuSettings = new ClickRegion[2] {
            new ClickRegion(new Rectangle(0,713,255,42),"cancel"),
            new ClickRegion(new Rectangle(524,713,500,42),"accept")
            //new ClickRegion(new Rectangle(0,0,0,0),"keylayout")
        };

        protected string nextState = null;

        public void addSpace(int amount)
        {
            currentPos.Y += amount;
        }

        public void shiftColumn()
        {
            shiftColumn(350);
        }

        public void shiftColumn(int amount)
        {
            currentPos.X += amount;
            currentPos.Y = originalY;
        }

        public void addSliderAutomatic(string text, float minVal, float maxVal, float initVal, bool integerOnly)
        {
            int height = 38; //basic
            if (text != "")
                height += 20;
            currentPos.Y += height;
            addSlider(new Rectangle((int)currentPos.X, (int)currentPos.Y, sliderFullWidth, baseHeight), true, true, text, minVal, maxVal, initVal, integerOnly);
        }

        public void addSlider(Rectangle size, bool enabled, bool visible, string text, float minVal, float maxVal, float initVal, bool integerOnly)
        {
            InterfaceSlider temp = new InterfaceSlider((_SM as InfiniminerGame), _P);
            temp.size=size;
            temp.enabled=enabled;
            temp.visible=visible;
            temp.text=text;
            temp.minVal=minVal;
            temp.maxVal=maxVal;
            temp.setValue(initVal);
            temp.integers=integerOnly;
            elements.Add(temp);
        }

        public void addButtonAutomatic(string text, string onText, string offText, bool clicked)
        {
            int height = 22; //basic
            if (text != "")
                height += 20;
            currentPos.Y += height;
            addButton(new Rectangle((int)currentPos.X, (int)currentPos.Y, sliderFullWidth, baseHeight), true, true, text, onText, offText, clicked);
        }

        public void addButton(Rectangle size, bool enabled, bool visible, string text, string onText, string offText, bool clicked)
        {
            InterfaceButtonToggle temp = new InterfaceButtonToggle((_SM as InfiniminerGame), _P);
            temp.size = size;
            temp.enabled = enabled;
            temp.visible = visible;
            temp.text = text;
            temp.onText = onText;
            temp.offText = offText;
            temp.clicked = clicked;
            elements.Add(temp);
        }

        public void addTextInputAutomatic(string text, string initVal)
        {
            int height = 22; //basic
            if (text != "")
                height += 20;
            currentPos.Y += height;
            addTextInput(new Rectangle((int)currentPos.X, (int)currentPos.Y, sliderFullWidth, baseHeight), true, true, text, initVal);
        }

        public void addTextInput(Rectangle size, bool enabled, bool visible, string text, string initVal)
        {
            InterfaceTextInput temp = new InterfaceTextInput((_SM as InfiniminerGame), _P);
            temp.size = size;
            temp.enabled = enabled;
            temp.visible = visible;
            temp.text = text;
            temp.value = initVal;
            elements.Add(temp);
        }

        public void addLabelAutomatic(string text)
        {
            currentPos.Y += 20;
            addLabel(new Rectangle((int)currentPos.X-100, (int)currentPos.Y, 0, 0), true, text);
        }

        public void addLabel(Rectangle size, bool visible, string text)
        {
            InterfaceLabel temp = new InterfaceLabel((_SM as InfiniminerGame), _P);
            temp.size = size;
            temp.visible = visible;
            temp.text = text;
            elements.Add(temp);
        }

        public override void OnEnter(string oldState)
        {
            _SM.IsMouseVisible = true;

            //Load the background
            texSettings = _SM.Content.Load<Texture2D>("menus/tex_menu_settings");
            drawRect = new Rectangle(_SM.GraphicsDevice.Viewport.Width / 2 - 1024 / 2,
                                                 _SM.GraphicsDevice.Viewport.Height / 2 - 768 / 2,
                                                 1024,
                                                 1024);

            //Read the data from file
            DatafileWriter dw = new DatafileWriter("client.config.txt");

            currentPos = new Vector2(200, 100);
            originalY = (int)currentPos.Y;

            addLabelAutomatic("User Settings");
                addTextInputAutomatic("Username", dw.Data.ContainsKey("handle") ? dw.Data["handle"] : "Player");
            addSpace(16);

            addLabelAutomatic("Screen Settings");
                addTextInputAutomatic("Scrn  Width", dw.Data.ContainsKey("width") ? dw.Data["width"] : "1024");
                addTextInputAutomatic("Scrn Height", dw.Data.ContainsKey("height") ? dw.Data["height"] : "780");
                addButtonAutomatic("Screen Mode", "Fullscreen", "Windowed", dw.Data.ContainsKey("fullscreen") ? bool.Parse(dw.Data["fullscreen"]) : false);
            addSpace(16);

            addLabelAutomatic("Sound Settings");
                addSliderAutomatic("Volume", 1f, 100f, dw.Data.ContainsKey("volume") ? float.Parse(dw.Data["volume"])*100 : 100f, true);
                addButtonAutomatic("Enable Sound", "On", "NoSound", dw.Data.ContainsKey("nosound") ? !bool.Parse(dw.Data["nosound"]) : true);
            addSpace(16);

            shiftColumn();

            addLabelAutomatic("Mouse Settings");
                addButtonAutomatic("Invert Mouse", "Yes", "No", dw.Data.ContainsKey("yinvert") ? bool.Parse(dw.Data["yinvert"]) : false);
                addSliderAutomatic("Mouse Sensitivity", 1f, 10f, dw.Data.ContainsKey("sensitivity") ? float.Parse(dw.Data["sensitivity"]) : 5f, true);
            addSpace(16);

            addLabelAutomatic("Misc Settings");
                addButtonAutomatic("Bloom", "Pretty", "Boring", dw.Data.ContainsKey("pretty") ? bool.Parse(dw.Data["pretty"]) : true);
                addButtonAutomatic("Show FPS", "Yes", "No", dw.Data.ContainsKey("showfps") ? bool.Parse(dw.Data["showfps"]) : true);
            addSpace(16);

            
            //_P.KillPlayer("");
        }

        public override void OnLeave(string newState)
        {
            base.OnLeave(newState);
        }

        public override void OnMouseDown(MouseButton button, int x, int y)
        {
            base.OnMouseDown(button, x, y);
            foreach (InterfaceElement element in elements)
            {
                element.OnMouseDown(button, x, y);
            }
            switch(ClickRegion.HitTest(clkMenuSettings,new Point(x,y)))
            {
                case "cancel":
                    nextState = "Infiniminer.States.ServerBrowserState";
                    break;
                case "accept":
                    if (saveData()>=1)
                        _SM.Exit();
                    break;
                /*case "keylayout":
                    saveData();
                    nextState = "Infiniminer.States.KeySettingsState";
                    break;*/
            }
        }

        public override void OnMouseUp(MouseButton button, int x, int y)
        {
            base.OnMouseUp(button, x, y);
            foreach (InterfaceElement element in elements)
            {
                element.OnMouseUp(button, x, y);
            }
        }

        public int saveData()
        {
            DatafileWriter dw = new DatafileWriter("client.config.txt");
            foreach (InterfaceElement element in elements)
            {
                switch (element.text)
                {
                    case "Username": dw.Data["handle"] = (element as InterfaceTextInput).value;
                        break;
                    case "Scrn  Width": dw.Data["width"] = (element as InterfaceTextInput).value;
                        break;
                    case "Scrn Height": dw.Data["height"] = (element as InterfaceTextInput).value;
                        break;
                    case "Screen Mode": dw.Data["fullscreen"] = (element as InterfaceButtonToggle).clicked.ToString().ToLower();
                        break;
                    case "Volume": dw.Data["volume"] = ((element as InterfaceSlider).value / 100).ToString();
                        break;
                    case "Enable Sound": dw.Data["nosound"] = (!(element as InterfaceButtonToggle).clicked).ToString().ToLower();
                        break;
                    case "Invert Mouse": dw.Data["yinvert"] = (element as InterfaceButtonToggle).clicked.ToString().ToLower();
                        break;
                    case "Mouse Sensitivity": dw.Data["sensitivity"] = (element as InterfaceSlider).value.ToString();
                        break;
                    case "Bloom": dw.Data["pretty"] = (element as InterfaceButtonToggle).clicked.ToString().ToLower();
                        break;
                    case "Show FPS": dw.Data["showfps"] = (element as InterfaceButtonToggle).clicked.ToString().ToLower();
                        break;
                    default: break;
                }
            }
            return dw.WriteChanges("client.config.txt");
        }

        public override void OnCharEntered(EventInput.CharacterEventArgs e)
        {
            base.OnCharEntered(e);
            foreach (InterfaceElement element in elements)
            {
                element.OnCharEntered(e);
            }
        }

        public override void OnKeyDown(Keys key)
        {
            base.OnKeyDown(key);
            if (key == Keys.Escape)
                nextState = "Infiniminer.States.ServerBrowserState";
            else
            {
                foreach (InterfaceElement element in elements)
                {
                    element.OnKeyDown(key);
                }
            }
        }

        public override void OnKeyUp(Keys key)
        {
            base.OnKeyUp(key);
            foreach (InterfaceElement element in elements)
            {
                element.OnKeyUp(key);
            }
        }

        public override string OnUpdate(GameTime gameTime, KeyboardState keyState, MouseState mouseState)
        {
            return nextState;
        }

        public override void OnRenderAtUpdate(GraphicsDevice graphicsDevice, GameTime gameTime)
        {
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            spriteBatch.Draw(texSettings, drawRect, Color.White);
            spriteBatch.End();
            foreach (InterfaceElement element in elements)
            {
                element.Render(graphicsDevice);
            }
        }
    }
}
