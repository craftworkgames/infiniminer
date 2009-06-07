using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Infiniminer
{
    public class Camera
    {
        public float Pitch, Yaw;
        public Vector3 Position;
        public Matrix ViewMatrix = Matrix.Identity;
        public Matrix ProjectionMatrix = Matrix.Identity;

        public Camera(GraphicsDevice device)
        {
            Pitch = 0;
            Yaw = 0;
            Position = Vector3.Zero;

            float aspectRatio = device.Viewport.AspectRatio;
            this.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(70), aspectRatio, 0.01f, 1000.0f);
        }

        // Returns a unit vector pointing in the direction that we're looking.
        public Vector3 GetLookVector()
        {
            Matrix rotation = Matrix.CreateRotationX(Pitch) * Matrix.CreateRotationY(Yaw);
            return Vector3.Transform(Vector3.Forward, rotation);
        }

        public Vector3 GetRightVector()
        {
            Matrix rotation = Matrix.CreateRotationX(Pitch) * Matrix.CreateRotationY(Yaw);
            return Vector3.Transform(Vector3.Right, rotation);
        }

        public void Update()
        {
            Vector3 target = Position + GetLookVector();
            this.ViewMatrix = Matrix.CreateLookAt(Position, target, Vector3.Up);
        }
    }
}
