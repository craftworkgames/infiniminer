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

namespace Infiniminer
{
    public class ClickRegion
    {
        public Rectangle Rectangle;
        public string Tag;

        public ClickRegion(Rectangle rectangle, string tag)
        {
            Rectangle = rectangle;
            Tag = tag;
        }

        /// <summary>
        /// Returns the tag, if any, of the region that contains point.
        /// </summary>
        public static string HitTest(ClickRegion[] regionList, Point point)
        {
            foreach (ClickRegion r in regionList)
            {
                if (r.Rectangle.Contains(point))
                    return r.Tag;
            }
            return null;
        }
    }
}
