using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace Infiniminer
{
    public class KeyBindHandler
    {
        //Cases where there are multiple keys under the same name
        enum SpecialKeys
        {
            Control,
            Alt,
            Shift
        }

        Dictionary<Keys, Buttons> keyBinds = new Dictionary<Keys, Buttons>();
        Dictionary<MouseButton, Buttons> mouseBinds = new Dictionary<MouseButton, Buttons>();
        Dictionary<SpecialKeys, Buttons> specialKeyBinds = new Dictionary<SpecialKeys, Buttons>();

        public bool IsBound(Buttons button, Keys theKey)
        {
            if (keyBinds.ContainsKey(theKey) && keyBinds[theKey] == button)
                return true;
            else if ((theKey == Keys.LeftAlt || theKey == Keys.RightAlt) && specialKeyBinds.ContainsKey(SpecialKeys.Alt) && specialKeyBinds[SpecialKeys.Alt] == button)
                return true;
            else if ((theKey == Keys.LeftShift || theKey == Keys.RightShift) && specialKeyBinds.ContainsKey(SpecialKeys.Shift) && specialKeyBinds[SpecialKeys.Shift] == button)
                return true;
            else if ((theKey == Keys.LeftControl || theKey == Keys.RightControl) && specialKeyBinds.ContainsKey(SpecialKeys.Control) && specialKeyBinds[SpecialKeys.Control] == button)
                return true;
            return false;
        }

        public bool IsPressed(Buttons button)
        {
            KeyboardState state = Keyboard.GetState();
            foreach (Keys key in keyBinds.Keys)
            {
                if (keyBinds[key] == button)
                {
                    if (state.IsKeyDown(key))
                        return true;
                }
            }
            MouseState ms = Mouse.GetState();
            foreach (MouseButton mb in mouseBinds.Keys)
            {
                if (mouseBinds[mb] == button)
                {
                    switch (mb)
                    {
                        case MouseButton.LeftButton:
                            if (ms.LeftButton == ButtonState.Pressed)
                                return true;
                            break;
                        case MouseButton.MiddleButton:
                            if (ms.MiddleButton == ButtonState.Pressed)
                                return true;
                            break;
                        case MouseButton.RightButton:
                            if (ms.RightButton == ButtonState.Pressed)
                                return true;
                            break;
                    }
                }
            }
            foreach (SpecialKeys key in specialKeyBinds.Keys)
            {
                if (specialKeyBinds[key] == button)
                {
                    switch (key)
                    {
                        case SpecialKeys.Alt:
                            if (state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt))
                                return true;
                            break;
                        case SpecialKeys.Control:
                            if (state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl))
                                return true;
                            break;
                        case SpecialKeys.Shift:
                            if (state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift))
                                return true;
                            break;
                    }
                }
            }
            return false;
        }

        public bool IsBound(Buttons button, MouseButton mb)
        {
            if (mouseBinds.ContainsKey(mb) && mouseBinds[mb] == button)
                return true;
            return false;
        }

        public Buttons GetBound(Keys theKey)
        {
            if (keyBinds.ContainsKey(theKey))
                return keyBinds[theKey];
            else if ((theKey == Keys.LeftAlt || theKey == Keys.RightAlt) && specialKeyBinds.ContainsKey(SpecialKeys.Alt))
                return specialKeyBinds[SpecialKeys.Alt];
            else if ((theKey == Keys.LeftShift || theKey == Keys.RightShift) && specialKeyBinds.ContainsKey(SpecialKeys.Shift))
                return specialKeyBinds[SpecialKeys.Shift];
            else if ((theKey == Keys.LeftControl || theKey == Keys.RightControl) && specialKeyBinds.ContainsKey(SpecialKeys.Control))
                return specialKeyBinds[SpecialKeys.Control];
            return Buttons.None;
        }

        public Buttons GetBound(MouseButton theButton)
        {
            if (mouseBinds.ContainsKey(theButton))
                return mouseBinds[theButton];
            return Buttons.None;
        }

        //If overwrite is true then the previous entry for that button will be removed
        public bool BindKey(Buttons button, string key, bool overwrite)
        {
            try
            {
                //Key bind
                Keys actualKey = (Keys)Enum.Parse(typeof(Keys), key, true);
                if (Enum.IsDefined(typeof(Keys), actualKey))
                {
                    keyBinds.Add(actualKey, (Buttons)button);
                    return true;
                }
            }
            catch { }
            try
            {
                //Mouse bind
                MouseButton actualMB = (MouseButton)Enum.Parse(typeof(MouseButton), key, true);
                if (Enum.IsDefined(typeof(MouseButton), actualMB))
                {
                    mouseBinds.Add(actualMB, (Buttons)button);
                    return true;
                }
            }
            catch { }
            //Special cases
            if (key.Equals("control", StringComparison.OrdinalIgnoreCase) || key.Equals("ctrl", StringComparison.OrdinalIgnoreCase))
            {
                specialKeyBinds.Add(SpecialKeys.Control, (Buttons)button);
                return true;
            }
            if (key.Equals("shift", StringComparison.OrdinalIgnoreCase))
            {
                specialKeyBinds.Add(SpecialKeys.Shift, (Buttons)button);
                return true;
            }
            if (key.Equals("alt", StringComparison.OrdinalIgnoreCase))
            {
                specialKeyBinds.Add(SpecialKeys.Alt, (Buttons)button);
                return true;
            }
            return false;
        }

        //Note that multiple binds to the same key won't work right now due to the way DatafileWriter handles input and how Dictionary works
        //Macro support is a future goal
        public void SaveBinds(DatafileWriter output, string filename)
        {
            foreach (Keys key in keyBinds.Keys)
            {
                output.Data[key.ToString()] = keyBinds[key].ToString();
            }
            foreach (MouseButton button in mouseBinds.Keys)
            {
                output.Data[button.ToString()] = mouseBinds[button].ToString();
            }
            foreach (SpecialKeys key in specialKeyBinds.Keys)
            {
                output.Data[key.ToString()] = specialKeyBinds[key].ToString();
            }
            output.WriteChanges(filename);
        }

        public void CreateDefaultSet()
        {
            mouseBinds.Add(MouseButton.LeftButton, Buttons.Fire);

            keyBinds.Add(Keys.W, Buttons.Forward);
            keyBinds.Add(Keys.S, Buttons.Backward);
            keyBinds.Add(Keys.A, Buttons.Left);
            keyBinds.Add(Keys.D, Buttons.Right);
            specialKeyBinds.Add(SpecialKeys.Shift, Buttons.Sprint);
            specialKeyBinds.Add(SpecialKeys.Control, Buttons.Crouch);
            keyBinds.Add(Keys.Space, Buttons.Jump);

            keyBinds.Add(Keys.Q, Buttons.Ping);
            keyBinds.Add(Keys.D8, Buttons.Deposit);
            keyBinds.Add(Keys.D9, Buttons.Withdraw);

            keyBinds.Add(Keys.Y, Buttons.SayAll);
            keyBinds.Add(Keys.U, Buttons.SayTeam);

            keyBinds.Add(Keys.M, Buttons.ChangeClass);
            keyBinds.Add(Keys.N, Buttons.ChangeTeam);

            keyBinds.Add(Keys.D1, Buttons.Tool1);
            keyBinds.Add(Keys.D2, Buttons.Tool2);
            keyBinds.Add(Keys.D3, Buttons.Tool3);
            keyBinds.Add(Keys.D4, Buttons.Tool4);
            keyBinds.Add(Keys.D5, Buttons.Tool5);

            keyBinds.Add(Keys.E, Buttons.ToolUp);

            keyBinds.Add(Keys.R, Buttons.BlockUp);
            mouseBinds.Add(MouseButton.WheelUp, Buttons.BlockUp);
            mouseBinds.Add(MouseButton.WheelDown, Buttons.BlockDown);
        }
    }
}
