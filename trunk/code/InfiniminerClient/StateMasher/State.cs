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

namespace StateMasher
{
    public enum MouseButton
    {
        LeftButton,
        MiddleButton,
        RightButton
    }

    public class State
    {
        public StateMachine _SM = null;
        public Infiniminer.PropertyBag _P = null;

        public virtual void OnEnter(string oldState)
        {
        }

        public virtual void OnLeave(string newState)
        {
        }

        public virtual string OnUpdate(GameTime gameTime, KeyboardState keyState, MouseState mouseState)
        {
            return null;
        }

        public virtual void OnRenderAtEnter(GraphicsDevice graphicsDevice)
        {
        }

        public virtual void OnRenderAtUpdate(GraphicsDevice graphicsDevice, GameTime gameTime)
        {
        }

        public virtual void OnKeyDown(Keys key)
        {
        }

        public virtual void OnKeyUp(Keys key)
        {
        }

        public virtual void OnMouseDown(MouseButton button, int x, int y)
        {
        }

        public virtual void OnMouseUp(MouseButton button, int x, int y)
        {
        }

        public virtual void OnMouseScroll(int scrollWheelValue)
        {
        }

        //public virtual void OnStatusChange(NetConnectionStatus status)
        //{
        //}

        //public virtual void OnPacket(NetBuffer buffer, NetMessageType type)
        //{
        //}
    }
}
