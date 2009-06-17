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
    public class AnimationCallbackEventArgs : EventArgs
    {
        public AnimationCallbackEventArgs(string animationEvent)
        {
            this.animationEvent = animationEvent;
        }
        private string animationEvent;
        public string AnimationEvent
        {
            get { return animationEvent; }
        }
    }

    public struct AnimationFrame
    {
        public int spriteColumn;
        public float length;
    }

    public class SpriteModel
    {
        /* Animation scripts provide a light-weight way to define animations. They consist of a sequence of frames
         * and callback events. The following animation (based off the above skeleton model),
         * 
         *      "1,10"
         * 
         * will set the frame to frame 1 (idle) and then wait 10 seconds. When used with SetPassiveAnimation, it will 
         * loop, causing the skeleton to stand still. The following animation,
         * 
         *      "0,0.5 ; 1,0.5 ; 2,0.5 ; 1,0.5"
         *      
         * will cause the frames to cycle between 0, 1, 2, 1, and back to 0 when repeating (with SetPassiveAnimation),
         * waiting 0.5 seconds after each change of frame. The following animation,
         * 
         *      "3,1 ; !ATTACKHIT ; 1,0.5 ; !ATTACKEND"
         *      
         * will cause the skeleton to switch to frame 3 (arm raised), wait one second, issue the string “ATTACKHIT” 
         * through the AnimationCallback event, return to frame 1 (idle), wait an additional half of a second, and
         * then issue the string “ATTACKEND” through the AnimationCallback. When used with BeginActiveAnimation, this 
         * could be used to time attacks and sync the enemy behavior with its animation.
         * 
         * Whitespace is, of course, discarded when compiling animation scripts.
         */

        Texture2D texSprite = null;
        int numColumns = 0;
        int currentColumn = 0;
        List<AnimationFrame> passiveAnimation = null;
        List<AnimationFrame> activeAnimation = null;
        int animationStep = 0;
        bool runningActive = false;
        float timeCountdown = 0;
        VertexDeclaration vertexDeclaration;
        GraphicsDevice graphicsDevice;
        Effect effect;
        Game gameInstance;
        SpriteFont nameFont = null;

        // Constructor for SpriteModel. Loads up the texture referenced by spriteSheetPath to use for drawing. 
        // Each individual sprite should be fit to a 24×32 box with the bottom center of the box corresponding to 
        // the SpriteModel's origin. A sprite sheet is expected to have a column of four sprites for every frame 
        // designated by numFrames. A sprite sheet should be 128 pixels tall and padded on the right to bring the 
        // total texture width to a power of two.
        public SpriteModel(Game gameInstance, int numFrames)
        {
            this.gameInstance = gameInstance;
            this.graphicsDevice = gameInstance.GraphicsDevice;
            this.effect = gameInstance.Content.Load<Effect>("effect_spritemodel");
            this.nameFont = gameInstance.Content.Load<SpriteFont>("font_04b08");

            this.numColumns = numFrames;

            AnimationFrame dummyFrame = new AnimationFrame();
            dummyFrame.length = 1;
            dummyFrame.spriteColumn = 1;
            passiveAnimation = new List<AnimationFrame>();
            passiveAnimation.Add(dummyFrame);
            activeAnimation = new List<AnimationFrame>();
            activeAnimation.Add(dummyFrame);

            vertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionTexture.VertexElements);
        }

        public void SetSpriteTexture(Texture2D spriteTexture)
        {
            texSprite = spriteTexture;
        }

        // Draw the SpriteModel into gamespace at drawLocation, facing drawAngle, where the camera is located at 
        // cameraLocation (used for finding the angle between the camera and the sprite's forward direction). 
        // The sprite will be drawn as 1*drawScale engine units wide and 1.5*drawScale engine units tall.
        public void Draw(Matrix viewMatrix, Matrix projectionMatrix, Vector3 cameraPosition, Vector3 cameraForward, Vector3 drawPosition, Vector3 drawHeading, float drawScale)
        {
            VertexPositionTexture[] vertices = GenerateVertices(cameraPosition, drawPosition, drawHeading, drawScale);
            //Matrix world = Matrix.CreateBillboard(drawPosition, cameraPosition, Vector3.UnitY, cameraForward);
            Matrix world = Matrix.CreateConstrainedBillboard(drawPosition, cameraPosition, Vector3.UnitY, cameraForward, null);

            effect.Parameters["xWorld"].SetValue(world);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xTexture"].SetValue(texSprite);
            effect.Begin();
            effect.Techniques[0].Passes[0].Begin();

            graphicsDevice.RenderState.CullMode = CullMode.None;
            graphicsDevice.SamplerStates[0].MagFilter = TextureFilter.Point;

            // Since the per-pixel alpha is either 0 or 1 we can use an alpha test instead of alpha blending.
            graphicsDevice.RenderState.AlphaTestEnable = true;
            graphicsDevice.RenderState.AlphaFunction = CompareFunction.Greater;
            graphicsDevice.RenderState.ReferenceAlpha = 128;

            graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);

            graphicsDevice.RenderState.AlphaTestEnable = false;

            effect.Techniques[0].Passes[0].End();
            effect.End();
        }

        public void DrawText(Matrix viewMatrix, Matrix projectionMatrix, Vector3 drawPosition, string hoverText)
        {
            DrawText(viewMatrix, projectionMatrix, drawPosition, hoverText, new Color(255, 255, 255, 255));
        }

        public void DrawText(Matrix viewMatrix, Matrix projectionMatrix, Vector3 drawPosition, string hoverText, Color color)
        {
            // Don't draw text if it's not within our frustum.
            BoundingSphere regionBounds = new BoundingSphere(drawPosition, 0.1f);
            BoundingFrustum boundingFrustum = new BoundingFrustum(viewMatrix * projectionMatrix);
            if (boundingFrustum.Contains(regionBounds) == ContainmentType.Disjoint)
                return;

            // Draw our text over the player.
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            Vector3 screenSpace = graphicsDevice.Viewport.Project(Vector3.Zero,
                                                                  projectionMatrix,
                                                                  viewMatrix,
                                                                  Matrix.CreateTranslation(drawPosition + new Vector3(0, 1.7f, 0)));
            Vector2 textPosition = new Vector2(screenSpace.X, screenSpace.Y) - nameFont.MeasureString(hoverText) * 0.5f;
            textPosition.X = (int)textPosition.X;
            textPosition.Y = (int)textPosition.Y;
            spriteBatch.DrawString(nameFont, hoverText, textPosition, Color.Black);
            textPosition.X -= 2;
            textPosition.Y -= 2;
            spriteBatch.DrawString(nameFont, hoverText, textPosition, color);//Color.White);
            spriteBatch.End();
        }

        public VertexPositionTexture[] GenerateVertices(Vector3 cameraPosition, Vector3 drawPosition, Vector3 drawHeading, float drawScale)
        {
            VertexPositionTexture[] vertices = new VertexPositionTexture[6];

            Vector2 vTexStart = new Vector2(0, 0);
            float frameWidth = 24.0f / texSprite.Width;
            float frameHeight = 32.0f / texSprite.Height;

            // Get rid of the up-down component of our vectors and normalize them.
            Vector3 vToPlayer = cameraPosition - drawPosition;
            vToPlayer.Y = 0;
            vToPlayer.X += 0.000001f;
            drawHeading.Y = 0;
            drawHeading.X += 0.000001f;
            vToPlayer.Normalize();
            drawHeading.Normalize();

            float dotProduct = Vector3.Dot(vToPlayer, drawHeading);
            Vector3 crossProduct = Vector3.Cross(vToPlayer, drawHeading);

            int spriteRow;
            if (dotProduct > 0.747f)
                spriteRow = 0;
            else if (dotProduct < -0.747f)
                spriteRow = 2;
            else if (crossProduct.Y < 0)
                spriteRow = 1;
            else
                spriteRow = 3;

            float texX = currentColumn * frameWidth - frameWidth / 24;
            float texY = spriteRow * frameHeight + frameHeight / 32;
            VertexPositionTexture v1 = new VertexPositionTexture(new Vector3(-0.375f * drawScale, 1 * drawScale, 0), new Vector2(texX, texY));
            VertexPositionTexture v2 = new VertexPositionTexture(new Vector3(0.375f * drawScale, 1 * drawScale, 0), new Vector2(texX + frameWidth, texY));
            VertexPositionTexture v3 = new VertexPositionTexture(new Vector3(-0.375f * drawScale, 0, 0), new Vector2(texX, texY + frameHeight));
            VertexPositionTexture v4 = new VertexPositionTexture(new Vector3(0.375f * drawScale, 0, 0), new Vector2(texX + frameWidth, texY + frameHeight));

            vertices[0] = v3;
            vertices[1] = v2;
            vertices[2] = v4;
            vertices[3] = v3;
            vertices[4] = v1;
            vertices[5] = v2;
            return vertices;
        }

        //public VertexPositionTexture[] GenerateVertices(Vector3 cameraPosition, Vector3 drawPosition, Vector3 drawHeading, float drawScale)
        //{
        //    VertexPositionTexture[] vertices = new VertexPositionTexture[6];

        //    Vector3 vToPlayer = cameraPosition - drawPosition;
        //    vToPlayer.Normalize();
        //    drawHeading.Normalize();
        //    Vector3 drawRight = Matrix.CreateBillboard(

        //    float dotProduct = Vector3.Dot(vToPlayer, drawHeading);
        //    Vector3 crossProduct = Vector3.Cross(vToPlayer, drawHeading);

        //    VertexPositionTexture v1, v2, v3, v4;

        //    Vector2 vTexStart = new Vector2(0, 0);
        //    float texOffsetX = 24 / texSprite.Width;
        //    float texOffsetY = 32 / texSprite.Height;

        //    if (Math.Abs(dotProduct) > 0.747f)
        //    {
        //        // We're looking at this from the front or the back.
        //        int spriteRow = (dotProduct > 0) ? 0 : 2;
        //        if (dotProduct > 0)
        //        {
        //            // Draw the front.
        //            v1 = new VertexPositionTexture(new Vector3(
        //        }
        //        else
        //        {
        //            // Draw the back.
        //        }
        //    }

        //    vertices[0] = v1;
        //    vertices[1] = v4;
        //    vertices[2] = v2;
        //    vertices[3] = v1;
        //    vertices[4] = v3;
        //    vertices[5] = v4;
        //    return vertices;
        //}

        // Advance the animation counter by the time-delta in gameTime.

        public void Update(GameTime gameTime)
        {
            List<AnimationFrame> currentAnimation = runningActive ? activeAnimation : passiveAnimation;

            timeCountdown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeCountdown <= 0)
            {
                animationStep += 1;
                if (animationStep >= currentAnimation.Count)
                {
                    // The animation has run over, so reset it.
                    if (runningActive)
                    {
                        runningActive = false;
                        currentAnimation = passiveAnimation;
                    }
                    animationStep = 0;
                }

                timeCountdown = currentAnimation[animationStep].length;
                currentColumn = currentAnimation[animationStep].spriteColumn;
            }
        }

        // Set a passive animation for this sprite that will be looped over when an active animation is not being
        // performed. When an active animation is finished, the passive animation will be restarted at the first frame.
        public void SetPassiveAnimation(string animationScript)
        {
            passiveAnimation = ParseAnimationScript(animationScript);
        }

        // Interrupt the currently playing animation to begin playing the provided animation script.
        // We resume the passive animation at its start.
        public void StartActiveAnimation(string animationScript)
        {
            activeAnimation = ParseAnimationScript(animationScript);
            runningActive = true;
            timeCountdown = activeAnimation[0].length;
            currentColumn = activeAnimation[0].spriteColumn;
        }

        public List<AnimationFrame> ParseAnimationScript(string animationScript)
        {
            List<AnimationFrame> animation = new List<AnimationFrame>();
            string[] cmdList = animationScript.Split(";".ToCharArray());
            foreach (string cmd in cmdList)
            {
                string[] argList = cmd.Split(",".ToCharArray());
                if (argList.Length == 2)
                {
                    AnimationFrame frame = new AnimationFrame();
                    frame.spriteColumn = int.Parse(argList[0], System.Globalization.CultureInfo.InvariantCulture);
                    frame.length = float.Parse(argList[1], System.Globalization.CultureInfo.InvariantCulture);
                    animation.Add(frame);
                }
            }
            return animation;
        }

        // Raised when the currently playing animation script encounters an animation callback command.
        public event EventHandler<AnimationCallbackEventArgs> RaiseAnimationCallbackEvent;
        protected void OnRaiseAnimationCallbackEvent(AnimationCallbackEventArgs e)
        {
            EventHandler<AnimationCallbackEventArgs> handler = RaiseAnimationCallbackEvent;
            if (handler != null)
                handler(this, e);
        }
    }
}
