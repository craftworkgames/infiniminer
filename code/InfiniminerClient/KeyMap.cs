using System;
using System.Collections.Generic;
using System.Runtime.InteropServices; 
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Infiniminer1
{
    public class KeyMap
    {
        Dictionary<Keys, string> keyMap;

        public KeyMap()
        {
            keyMap = new Dictionary<Keys, string>();
            keyMap.Add(Keys.A, "aA");
            keyMap.Add(Keys.B, "bB");
            keyMap.Add(Keys.C, "cC");
            keyMap.Add(Keys.D, "dD");
            keyMap.Add(Keys.E, "eE");
            keyMap.Add(Keys.F, "fF");
            keyMap.Add(Keys.G, "gG");
            keyMap.Add(Keys.H, "hH");
            keyMap.Add(Keys.I, "iI");
            keyMap.Add(Keys.J, "jJ");
            keyMap.Add(Keys.K, "kK");
            keyMap.Add(Keys.L, "lL");
            keyMap.Add(Keys.M, "mM");
            keyMap.Add(Keys.N, "nN");
            keyMap.Add(Keys.O, "oO");
            keyMap.Add(Keys.P, "pP");
            keyMap.Add(Keys.Q, "qQ");
            keyMap.Add(Keys.R, "rR");
            keyMap.Add(Keys.S, "sS");
            keyMap.Add(Keys.T, "tT");
            keyMap.Add(Keys.U, "uU");
            keyMap.Add(Keys.V, "vV");
            keyMap.Add(Keys.W, "wW");
            keyMap.Add(Keys.X, "xX");
            keyMap.Add(Keys.Y, "yY");
            keyMap.Add(Keys.Z, "zZ");
            keyMap.Add(Keys.D0, "0)");
            keyMap.Add(Keys.D1, "1!");
            keyMap.Add(Keys.D2, "2@");
            keyMap.Add(Keys.D3, "3#");
            keyMap.Add(Keys.D4, "4$");
            keyMap.Add(Keys.D5, "5%");
            keyMap.Add(Keys.D6, "6^");
            keyMap.Add(Keys.D7, "7&");
            keyMap.Add(Keys.D8, "8*");
            keyMap.Add(Keys.D9, "9(");
            keyMap.Add(Keys.NumPad0, "00");
            keyMap.Add(Keys.NumPad1, "11");
            keyMap.Add(Keys.NumPad2, "22");
            keyMap.Add(Keys.NumPad3, "33");
            keyMap.Add(Keys.NumPad4, "44");
            keyMap.Add(Keys.NumPad5, "55");
            keyMap.Add(Keys.NumPad6, "66");
            keyMap.Add(Keys.NumPad7, "77");
            keyMap.Add(Keys.NumPad8, "88");
            keyMap.Add(Keys.NumPad9, "99");
            keyMap.Add(Keys.Space, "  ");
            keyMap.Add(Keys.Subtract, "-_");
            keyMap.Add(Keys.Add, "=+");
            keyMap.Add(Keys.OemBackslash, "\\|");
            keyMap.Add(Keys.OemCloseBrackets, "]}");
            keyMap.Add(Keys.OemComma, ",<");
            keyMap.Add(Keys.OemMinus, "-_");
            keyMap.Add(Keys.OemOpenBrackets, "[{");
            keyMap.Add(Keys.OemPeriod, ".>");
            keyMap.Add(Keys.OemPipe, "\\|");
            keyMap.Add(Keys.OemPlus, "=+");
            keyMap.Add(Keys.OemQuestion, "/?");
            keyMap.Add(Keys.OemQuotes, "'\"");
            keyMap.Add(Keys.OemSemicolon, ";:");
            keyMap.Add(Keys.OemTilde, "`~");
        }

        public bool IsKeyMapped(Keys key)
        {
            return keyMap.ContainsKey(key);
        }

        public string TranslateKey(Keys key, bool shiftDown)
        {
            if (!IsKeyMapped(key))
                return "";

            if (shiftDown)
                return keyMap[key].Substring(1, 1);
            else
                return keyMap[key].Substring(0, 1);
        }
    }
}