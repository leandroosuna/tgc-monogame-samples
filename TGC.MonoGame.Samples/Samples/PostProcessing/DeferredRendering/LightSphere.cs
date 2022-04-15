using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.Samples.Samples.PostProcessing
{
    public class LightSphere
    {
        public static Model Model;
        public static List<LightSphere> Lights;
        public static List<Vector3> Positions = new List<Vector3>();
        public static List<Vector3> Colors = new List<Vector3>();

        static float Scale = 0.03f;

        public Vector3 Color;

        public Vector3 Position;

        public Matrix World;
        Matrix ScaleMx;
        Matrix RotationMx;

        LightType Type;
        float CurrentAngle;
        float rotationSpeed = 2.5f;
        float CurrentDistance = 0;
        public LightSphere(Vector3 position, Vector3 color, LightType type, float startingAngle)
        {
            Position = position;
            Color = color;
            Type = type;
            CurrentAngle = startingAngle;
            ScaleMx = Matrix.CreateScale(Scale);
            RotationMx = Matrix.Identity;

            Colors.Add(color);
            Update(0f,Vector3.Zero);
            //World = ScaleMx * Matrix.CreateTranslation(Position) * RotationMx; 
        }


        public void Update(float delta, Vector3 robotPos)
        {
            CurrentAngle += rotationSpeed * delta;
            CurrentDistance += rotationSpeed * delta;
            CurrentDistance %= 50f;

            Position.X = robotPos.X+ MathF.Cos(CurrentAngle) * CurrentDistance;
            Position.Y = robotPos.Y;
            Position.Z = robotPos.Z+ MathF.Sin(CurrentAngle) * CurrentDistance;

            World = ScaleMx * Matrix.CreateTranslation(Position) * RotationMx;
        }



        public static void UpdateAll(GameTime gameTime, Vector3 robotPos)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Positions.Clear();
            Lights.ForEach(light => {
                light.Update(deltaTime, robotPos);
                Positions.Add(light.Position);
            });


        }

    }
    public enum LightType
    {
        Directional,
        OmniDirectional
    }
}
