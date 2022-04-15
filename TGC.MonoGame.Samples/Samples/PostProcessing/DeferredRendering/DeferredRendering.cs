using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TGC.MonoGame.Samples.Cameras;
using TGC.MonoGame.Samples.Geometries;
using TGC.MonoGame.Samples.Viewer;
using TGC.MonoGame.Samples.Viewer.GUI;
using TGC.MonoGame.Samples.Viewer.GUI.Modifiers;

namespace TGC.MonoGame.Samples.Samples.PostProcessing
{
    public class DeferredRendering : TGCSample
    {
        Model Robot;
        Texture2D RobotTex;
        public Vector3 RobotPos;
        Effect Effect;
        SpriteFont sp;
        public DeferredRendering(TGCViewer game) : base(game)
        {
            Category = TGCSampleCategory.PostProcessing;
            Name = "Deferred Rendering";
            Description = "Rendering a scene on multiple passes with multiple render targets";
        }
        private FreeCamera Camera { get; set; }

        public override void Initialize()
        {
            var screenSize = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            Camera = new FreeCamera(GraphicsDevice.Viewport.AspectRatio, new Vector3(0, 0, 40), screenSize);
            //We dont need as big of a frustum, this should improve depth calculation
            //Camera.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            //Camera.NearPlane = 10;
            //Camera.FarPlane = 200;
            //Camera.FieldOfView = MathHelper.PiOver4;
            //Camera.BuildProjection(Camera.AspectRatio, Camera.NearPlane, Camera.FarPlane, Camera.FieldOfView);

            LightSphere.Lights = new List<LightSphere>();

            LightSphere.Lights.Add(new LightSphere(new Vector3(20, 0, 0), new Vector3(1f, 0f, 1f), LightType.OmniDirectional, 0f));
            //LightSphere.Lights.Add(new LightSphere(new Vector3(-20, 0, 0), new Vector3(0f, 1f, 0f), LightType.OmniDirectional, MathHelper.Pi));

            InitRenderTargets();

            base.Initialize();
        }
        protected override void LoadContent()
        {
            Robot = Game.Content.Load<Model>(ContentFolder3D + "tgcito-classic/tgcito-classic");
            RobotTex = ((BasicEffect)Robot.Meshes[0].MeshParts[0].Effect).Texture;

            LightSphere.Model = Game.Content.Load<Model>(ContentFolder3D + "geometries/sphere");
            sp = Game.Content.Load<SpriteFont>(ContentFolderSpriteFonts + "CascadiaCode/CascadiaMonoPL");
            Effect = Game.Content.Load<Effect>(ContentFolderEffects + "DeferredRendering");

            InitEffectParameters();

            foreach (var mesh in Robot.Meshes)
                foreach (var part in mesh.MeshParts)
                    part.Effect = Effect;

            foreach (var mesh in LightSphere.Model.Meshes)
                foreach (var part in mesh.MeshParts)
                    part.Effect = Effect;

            ModifierController.AddTexture("color", ColorTarget);
            ModifierController.AddTexture("x", PosXTarget);
            ModifierController.AddTexture("y", PosYTarget);
            ModifierController.AddTexture("z", PosZTarget);


            base.LoadContent();
        }
        float inputDelta = 1000;
        public override void Update(GameTime gameTime)
        {
            // Update the state of the camera
            Camera.Update(gameTime);

            LightSphere.UpdateAll(gameTime, RobotPos);
            Game.Gizmos.UpdateViewProjection(Camera.View, Camera.Projection);

            base.Update(gameTime);

            var deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
            var kState = Keyboard.GetState();
            if(kState.IsKeyDown(Keys.L))
            {
                RobotPos.X += inputDelta * deltaTime; 
            }
            else if (kState.IsKeyDown(Keys.J))
            {
                RobotPos.X -= inputDelta * deltaTime;
            }
            else if (kState.IsKeyDown(Keys.I))
            {
                RobotPos.Y += inputDelta * deltaTime;
            }
            else if (kState.IsKeyDown(Keys.K))
            {
                RobotPos.Y -= inputDelta * deltaTime;
            }
            else if (kState.IsKeyDown(Keys.O))
            {
                RobotPos.Z += inputDelta * deltaTime;
            }
            else if (kState.IsKeyDown(Keys.P))
            {
                RobotPos.Z -= inputDelta * deltaTime;
            }
        }
        public override void Draw(GameTime gameTime)
        {
            //Step 1 Draw model Color, Normal, Bloom
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SetRenderTargets(ColorTarget, NormalTarget, BloomTarget, DepthTarget);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            GraphicsDevice.BlendState = BlendState.Opaque;

            DrawRobot(DrawType.ColorNormalBloomLight);
            DrawLights();

            //Step 2 Encode the world position of omnilight affected models
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SetRenderTargets(PosXTarget, PosYTarget, PosZTarget);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
            GraphicsDevice.BlendState = BlendState.Opaque;

            DrawRobot(DrawType.Position);

            //Step 3 Reconstruct position, apply light
            EffectColorMap.SetValue(ColorTarget);
            EffectNormalMap.SetValue(NormalTarget);
            Effect.Parameters["PosXMap"].SetValue(PosXTarget);
            Effect.Parameters["PosYMap"].SetValue(PosYTarget);
            Effect.Parameters["PosZMap"].SetValue(PosZTarget);

            Effect.Parameters["Light1Pos"].SetValue(LightSphere.Positions[0]);
            //Effect.Parameters["LightsColor"].SetValue(LightSphere.Colors.ToArray());

            Effect.CurrentTechnique = Effect.Techniques["PosReconstruct"];

            GraphicsDevice.SetRenderTargets(SceneTarget);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            FullScreenQuad.Draw(Effect);

            //EffectInverseView.SetValue(Matrix.Invert(Camera.View));
            //EffectInverseProjection.SetValue(Matrix.Invert(Camera.Projection));

            //GraphicsDevice.BlendState = BlendState.Opaque;

            GraphicsDevice.SetRenderTargets(null);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);


            SpriteBatch.Begin();
            GraphicsDevice.BlendState = BlendState.Opaque;
            SpriteBatch.Draw(SceneTarget, Vector2.Zero, Color.White);
            SpriteBatch.DrawString(sp, "X " + RobotPos.X + " Y " + RobotPos.Y + " Z " + RobotPos.Z, new Vector2(600,1), Color.White);
            SpriteBatch.End();

            base.Draw(gameTime);
        }
        void DrawRobot(DrawType draw)
        {
            if (draw.Equals(DrawType.ColorNormalBloomLight))
                Effect.CurrentTechnique = TextureTech;
            else if (draw.Equals(DrawType.Position))
                Effect.CurrentTechnique = Effect.Techniques["PosEncode"];
            else
                return;

            EffectModelTexture.SetValue(RobotTex);
            foreach (var mesh in Robot.Meshes)
            {
                var world = mesh.ParentBone.Transform * Matrix.CreateScale(0.1f) * Matrix.CreateTranslation(RobotPos);
                var wv = world * Camera.View;
                var wvp = wv * Camera.Projection;


                EffectWorld.SetValue(world);
                EffectWorldViewProjection.SetValue(wvp);
                if (draw.Equals(DrawType.ColorNormalBloomLight))
                {
                    EffectInverseTransposeWorld.SetValue(Matrix.Transpose(Matrix.Invert(world)));
                    //EffectWorldView.SetValue(wv);
                }

                mesh.Draw();
            }
        }
        void DrawLights()
        {
            Effect.CurrentTechnique = LightSphereTech;

            foreach (var light in LightSphere.Lights)
            {
                foreach (var mesh in LightSphere.Model.Meshes)
                {
                    var world = mesh.ParentBone.Transform * light.World;
                    var wv = world * Camera.View;
                    var wvp = wv * Camera.Projection;

                    EffectWorld.SetValue(world);
                    EffectInverseTransposeWorld.SetValue(Matrix.Transpose(Matrix.Invert(world)));
                    //EffectWorldView.SetValue(wv);
                    EffectWorldViewProjection.SetValue(wvp);
                    EffectColor.SetValue(light.Color);

                    mesh.Draw();
                }
            }
        }

        #region Render targets
        private RenderTarget2D ColorTarget;
        private RenderTarget2D NormalTarget;
        private RenderTarget2D BloomTarget;
        private RenderTarget2D DepthTarget;
        private RenderTarget2D PosXTarget;
        private RenderTarget2D PosYTarget;
        private RenderTarget2D PosZTarget;


        private RenderTarget2D SceneTarget;
        private RenderTarget2D BlurH;
        private RenderTarget2D BlurV;
        private RenderTarget2D FinalTarget;

        private FullScreenQuad FullScreenQuad;
        private SpriteBatch SpriteBatch;
        void InitRenderTargets()
        {
            var width = GraphicsDevice.Viewport.Width;
            var height = GraphicsDevice.Viewport.Height;
            // Create the targets we are going to use
            BlurH = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            BlurV = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            ColorTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            NormalTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            BloomTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            SceneTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

            PosXTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8);
            PosYTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8);
            PosZTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8);

            DepthTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8);

            FinalTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            // To easily draw render targets
            FullScreenQuad = new FullScreenQuad(GraphicsDevice);
            SpriteBatch = new SpriteBatch(GraphicsDevice);
        }
        #endregion

        #region Effect
        private EffectParameter EffectWorld;
        private EffectParameter EffectWorldView;
        private EffectParameter EffectWorldViewProjection;
        private EffectParameter EffectInverseView;
        private EffectParameter EffectInverseProjection;
        private EffectParameter EffectInverseTransposeWorld;
        private EffectParameter EffectColor;
        private EffectParameter EffectModelTexture;

        private EffectParameter EffectKD;
        private EffectParameter EffectKS;
        private EffectParameter EffectKA;
        private EffectParameter EffectColorMap;
        private EffectParameter EffectNormalMap;
        private EffectParameter EffectBloomFilter;
        private EffectParameter EffectDepthMap;

        private EffectParameter EffectAmbientColor;
        private EffectParameter EffectScreenSize;
        private EffectParameter EffectDLightDirection;
        private EffectParameter EffectDLightColor;
        private EffectParameter EffectSpecularColor;
        private EffectParameter EffectCameraPosition;
        private EffectParameter EffectBlurHTexture;
        private EffectParameter EffectBlurVTexture;
        private EffectParameter EffectSceneTexture;

        private EffectTechnique TextureTech;
        private EffectTechnique ColorTech;
        private EffectTechnique LightSphereTech;
        private EffectTechnique IntermediateTech;

        private EffectTechnique PostProcessTech;
        private EffectTechnique FinalIntegrateTech;

        void InitEffectParameters()
        {
            EffectWorld = Effect.Parameters["World"];
            EffectWorldView = Effect.Parameters["WorldView"];
            EffectWorldViewProjection = Effect.Parameters["WorldViewProjection"];
            EffectInverseTransposeWorld = Effect.Parameters["InverseTransposeWorld"];
            EffectInverseView = Effect.Parameters["InvView"];
            EffectInverseProjection = Effect.Parameters["InvProjection"];

            EffectColor = Effect.Parameters["Color"];
            EffectModelTexture = Effect.Parameters["ModelTexture"];

            EffectKA = Effect.Parameters["KA"];
            EffectKD = Effect.Parameters["KD"];
            EffectKS = Effect.Parameters["KS"];
            EffectColorMap = Effect.Parameters["ColorMap"];
            EffectNormalMap = Effect.Parameters["NormalMap"];
            EffectDepthMap = Effect.Parameters["DepthMap"];
            EffectBloomFilter = Effect.Parameters["BloomFilter"];

            EffectAmbientColor = Effect.Parameters["AmbientColor"];
            EffectScreenSize = Effect.Parameters["ScreenSize"];
            EffectDLightDirection = Effect.Parameters["DLightDirection"];
            EffectDLightColor = Effect.Parameters["DLightColor"];
            EffectSpecularColor = Effect.Parameters["SpecularColor"];
            EffectCameraPosition = Effect.Parameters["CameraPosition"];
            EffectBlurHTexture = Effect.Parameters["BlurHTexture"];
            EffectBlurVTexture = Effect.Parameters["BlurVTexture"];
            EffectSceneTexture = Effect.Parameters["SceneTexture"];

            TextureTech = Effect.Techniques["Textured"];
            ColorTech = Effect.Techniques["FlatColor"];
            LightSphereTech = Effect.Techniques["LightSphere"];
            PostProcessTech = Effect.Techniques["PostProcess"];
            FinalIntegrateTech = Effect.Techniques["FinalIntegrate"];
            IntermediateTech = Effect.Techniques["Intermediate"];

            EffectKA.SetValue(0.3f);


            EffectAmbientColor.SetValue(Vector3.One);
            EffectDLightColor.SetValue(Vector3.One);
            EffectSpecularColor.SetValue(Vector3.One);
            Effect.Parameters["FarPlaneDistance"]?.SetValue(Camera.FarPlane);

        }
        #endregion

        enum DrawType
        {
            ColorNormalBloomLight,
            Position
        };
    }
}
