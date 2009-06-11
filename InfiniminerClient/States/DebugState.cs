using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DebugState : State
    {
        private double flashCounter = 0;

        public override void OnEnter(string oldState)
        {
        }

        public override void OnLeave(string newState)
        {
        }

        public override string OnUpdate(GameTime gameTime, KeyboardState keyState, MouseState mouseState)
        {
            flashCounter += gameTime.ElapsedGameTime.TotalSeconds;
            if (flashCounter > 0.5)
                flashCounter = 0;
            return null;
        }

        public override void OnRenderAtEnter(GraphicsDeviceManager graphicsDevice)
        {
        }

        public override void OnRenderAtUpdate(GraphicsDeviceManager graphicsDevice)
        {
            if (flashCounter < 0.25)
                graphicsDevice.GraphicsDevice.Clear(Color.Blue);
            else
                graphicsDevice.GraphicsDevice.Clear(Color.Red);
        }

        public override void OnKeyDown(Keys key)
        {
            Debug.Print("OnKeyDown(" + key.ToString() + ")");
        }

        public override void OnKeyUp(Keys key)
        {
            Debug.Print("OnKeyUp(" + key.ToString() + ")");
        }

        public override void OnMouseDown(MouseButton button, int x, int y)
        {
            Debug.Print("OnMouseDown(" + button + ", " + x + ", " + y + ")");
        }

        public override void OnMouseUp(MouseButton button, int x, int y)
        {
            Debug.Print("OnMouseUp(" + button + ", " + x + ", " + y + ")");
        }

        public override void OnMouseScroll(int scrollDelta)
        {
            Debug.Print("OnMouseScroll(" + scrollDelta + ")");
        }

        //public override void OnStatusChange(NetConnectionStatus status)
        //{
        //}

        //public override void OnPacket(NetBuffer buffer, NetMessageType type)
        //{
        //}
    }
}
