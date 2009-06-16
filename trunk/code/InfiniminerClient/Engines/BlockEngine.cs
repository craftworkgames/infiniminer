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
    [Serializable]
    public struct VertexPositionTextureShade
    {
        Vector3 pos;
        Vector2 tex;
        float shade;

        public static readonly VertexElement[] VertexElements = new VertexElement[]
        { 
            new VertexElement(0,0,VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
            new VertexElement(0,sizeof(float)*3,VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(0,sizeof(float)*5,VertexElementFormat.Single, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1)               
        };

        public VertexPositionTextureShade(Vector3 position, Vector2 uv, double shade)
        {
            pos = position;
            tex = uv;
            this.shade = (float)shade;
        }

        public Vector3 Position { get { return pos; } set { pos = value; } }
        public Vector2 Tex { get { return tex; } set { tex = value; } }
        public float Shade { get { return shade; } set { shade = value; } }
        public static int SizeInBytes { get { return sizeof(float) * 6; } }
    }

    public class IMTexture
    {
        public Texture2D Texture = null;
        public Color LODColor = Color.Black;

        public IMTexture(Texture2D texture)
        {
            Texture = texture;
            LODColor = Color.Black;

            // If this is a null texture, use a black LOD color.
            if (Texture == null)
                return;

            // Calculate the load color dynamically.
            float r = 0, g = 0, b = 0;
            Color[] pixelData = new Color[texture.Width * texture.Height];
            texture.GetData<Color>(pixelData);
            for (int i = 0; i < texture.Width; i++)
                for (int j = 0; j < texture.Height; j++)
                {
                    r += pixelData[i + j * texture.Width].R;
                    g += pixelData[i + j * texture.Width].G;
                    b += pixelData[i + j * texture.Width].B;
                }
            r /= texture.Width * texture.Height;
            g /= texture.Width * texture.Height;
            b /= texture.Width * texture.Height;
            LODColor = new Color(r / 256, g / 256, b / 256);
        }
    }

    public class BlockEngine
    {
        public BlockType[,,] blockList = null;
        public BlockType[, ,] downloadList = null;
        Dictionary<uint,bool>[,] faceMap = null;
        BlockTexture[,] blockTextureMap = null;
        IMTexture[] blockTextures = null;
        Effect basicEffect;
        InfiniminerGame gameInstance;
        DynamicVertexBuffer[,] vertexBuffers = null;
        bool[,] vertexListDirty = null;
        VertexDeclaration vertexDeclaration;
        BloomComponent bloomPosteffect;

        public void MakeRegionDirty(int texture, int region)
        {
            vertexListDirty[texture, region] = true;
        }

        public const int MAPSIZE = 64;
        const int REGIONSIZE = 16;
        const int REGIONRATIO = MAPSIZE / REGIONSIZE;
        const int NUMREGIONS = REGIONRATIO * REGIONRATIO * REGIONRATIO;

        public void DownloadComplete()
        {
            for (ushort i = 0; i < MAPSIZE; i++)
                for (ushort j = 0; j < MAPSIZE; j++)
                    for (ushort k = 0; k < MAPSIZE; k++)
                        if (downloadList[i, j, k] != BlockType.None)
                            AddBlock(i, j, k, downloadList[i, j, k]);
        }

        public BlockEngine(InfiniminerGame gameInstance)
        {
            this.gameInstance = gameInstance;

            // Initialize the block list.
            downloadList = new BlockType[MAPSIZE, MAPSIZE, MAPSIZE];
            blockList = new BlockType[MAPSIZE, MAPSIZE, MAPSIZE];
            for (ushort i = 0; i < MAPSIZE; i++)
                for (ushort j = 0; j < MAPSIZE; j++)
                    for (ushort k = 0; k < MAPSIZE; k++)
                    {
                        downloadList[i, j, k] = BlockType.None;
                        blockList[i, j, k] = BlockType.None;
                    }

            // Initialize the face lists.
            faceMap = new Dictionary<uint,bool>[(byte)BlockTexture.MAXIMUM, NUMREGIONS];
            for (BlockTexture blockTexture = BlockTexture.None; blockTexture < BlockTexture.MAXIMUM; blockTexture++)
                for (int r=0; r<NUMREGIONS; r++)
                    faceMap[(byte)blockTexture, r] = new Dictionary<uint, bool>();

            // Initialize the texture map.
            blockTextureMap = new BlockTexture[(byte)BlockType.MAXIMUM, 6];
            for (BlockType blockType = BlockType.None; blockType < BlockType.MAXIMUM; blockType++)
                for (BlockFaceDirection faceDir = BlockFaceDirection.XIncreasing; faceDir < BlockFaceDirection.MAXIMUM; faceDir++)
                    blockTextureMap[(byte)blockType,(byte)faceDir] = BlockInformation.GetTexture(blockType, faceDir);

            // Load the textures we'll use.
            blockTextures = new IMTexture[(byte)BlockTexture.MAXIMUM];
            blockTextures[(byte)BlockTexture.None] = new IMTexture(null);
            blockTextures[(byte)BlockTexture.Dirt] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_dirt"));
            blockTextures[(byte)BlockTexture.Rock] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_rock"));
            blockTextures[(byte)BlockTexture.Ore] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_ore"));
            blockTextures[(byte)BlockTexture.Gold] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_silver"));
            blockTextures[(byte)BlockTexture.Diamond] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_diamond"));
            blockTextures[(byte)BlockTexture.HomeRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_home_red"));
            blockTextures[(byte)BlockTexture.HomeBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_home_blue"));
            blockTextures[(byte)BlockTexture.SolidRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_red"));
            blockTextures[(byte)BlockTexture.SolidBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_blue"));
            blockTextures[(byte)BlockTexture.Ladder] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_ladder"));
            blockTextures[(byte)BlockTexture.LadderTop] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_ladder_top"));
            blockTextures[(byte)BlockTexture.Spikes] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_spikes"));
            blockTextures[(byte)BlockTexture.Jump] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_jump"));
            blockTextures[(byte)BlockTexture.JumpTop] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_jump_top"));
            blockTextures[(byte)BlockTexture.Explosive] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_explosive"));
            blockTextures[(byte)BlockTexture.Metal] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_metal"));
            blockTextures[(byte)BlockTexture.DirtSign] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_dirt_sign"));
            blockTextures[(byte)BlockTexture.BankTopRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_top_red"));
            blockTextures[(byte)BlockTexture.BankLeftRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_left_red"));
            blockTextures[(byte)BlockTexture.BankFrontRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_front_red"));
            blockTextures[(byte)BlockTexture.BankRightRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_right_red"));
            blockTextures[(byte)BlockTexture.BankBackRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_back_red"));
            blockTextures[(byte)BlockTexture.BankTopBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_top_blue"));
            blockTextures[(byte)BlockTexture.BankLeftBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_left_blue"));
            blockTextures[(byte)BlockTexture.BankFrontBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_front_blue"));
            blockTextures[(byte)BlockTexture.BankRightBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_right_blue"));
            blockTextures[(byte)BlockTexture.BankBackBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_bank_back_blue"));
            blockTextures[(byte)BlockTexture.TeleSideA] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_teleporter_a"));
            blockTextures[(byte)BlockTexture.TeleSideB] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_teleporter_b"));
            blockTextures[(byte)BlockTexture.TeleTop] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_teleporter_top"));
            blockTextures[(byte)BlockTexture.TeleBottom] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_teleporter_bottom"));
            blockTextures[(byte)BlockTexture.Lava] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_lava"));
            blockTextures[(byte)BlockTexture.Road] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_road"));
            blockTextures[(byte)BlockTexture.RoadTop] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_road_top"));
            blockTextures[(byte)BlockTexture.RoadBottom] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_road_bottom"));
            blockTextures[(byte)BlockTexture.BeaconRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_beacon_top_red"));
            blockTextures[(byte)BlockTexture.BeaconBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_beacon_top_blue"));
            blockTextures[(byte)BlockTexture.TransRed] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_trans_red"));
            blockTextures[(byte)BlockTexture.TransBlue] = new IMTexture(gameInstance.Content.Load<Texture2D>("blocks/tex_block_trans_blue"));

            // Load our effects.
            basicEffect = gameInstance.Content.Load<Effect>("effect_basic");

            // Build vertex lists.
            vertexBuffers = new DynamicVertexBuffer[(byte)BlockTexture.MAXIMUM, NUMREGIONS];
            vertexListDirty = new bool[(byte)BlockTexture.MAXIMUM, NUMREGIONS];
            for (int i = 0; i < (byte)BlockTexture.MAXIMUM; i++)
                for (int j = 0; j < NUMREGIONS; j++)
                    vertexListDirty[i, j] = true;

            // Initialize any graphics stuff.
            vertexDeclaration = new VertexDeclaration(gameInstance.GraphicsDevice, VertexPositionTextureShade.VertexElements);

            // Initialize the bloom engine.
            if (gameInstance.RenderPretty)
            {
                bloomPosteffect = new BloomComponent();
                bloomPosteffect.Load(gameInstance.GraphicsDevice, gameInstance.Content);
            }
            else
                bloomPosteffect = null;
        }

        // Returns true if we are solid at this point.
        public bool SolidAtPoint(Vector3 point)
        {
            return BlockAtPoint(point) != BlockType.None; 
        }

        public bool SolidAtPointForPlayer(Vector3 point)
        {
            return !BlockPassibleForPlayer(BlockAtPoint(point));
        }

        private bool BlockPassibleForPlayer(BlockType blockType)
        {
            if (blockType == BlockType.None)
                return true;
            if (gameInstance.propertyBag.playerTeam == PlayerTeam.Red && blockType == BlockType.TransRed)
                return true;
            if (gameInstance.propertyBag.playerTeam == PlayerTeam.Blue && blockType == BlockType.TransBlue)
                return true;
            return false;
        }

        public BlockType BlockAtPoint(Vector3 point)
        {
            ushort x = (ushort)point.X;
            ushort y = (ushort)point.Y;
            ushort z = (ushort)point.Z;
            if (x < 0 || y < 0 || z < 0 || x >= MAPSIZE || y >= MAPSIZE || z >= MAPSIZE)
                return BlockType.None;
            return blockList[x, y, z]; 
        }

        public bool RayCollision(Vector3 startPosition, Vector3 rayDirection, float distance, int searchGranularity, ref Vector3 hitPoint, ref Vector3 buildPoint)
        {
            Vector3 testPos = startPosition;
            Vector3 buildPos = startPosition;
            for (int i=0; i<searchGranularity; i++)
            {
                testPos += rayDirection * distance / searchGranularity;
                BlockType testBlock = BlockAtPoint(testPos);
                if (testBlock != BlockType.None)
                {
                    hitPoint = testPos;
                    buildPoint = buildPos;
                    return true;
                }
                buildPos = testPos;
            }
            return false;
        }

        public void Render(GraphicsDevice graphicsDevice, GameTime gameTime)
        {
            RegenerateDirtyVertexLists();

            for (BlockTexture blockTexture = BlockTexture.None+1; blockTexture < BlockTexture.MAXIMUM; blockTexture++)
                for (uint r = 0; r < NUMREGIONS; r++)
                {
                    // Figure out if we should be rendering translucently.
                    bool renderTranslucent = false;
                    if (blockTexture == BlockTexture.TransRed || blockTexture == BlockTexture.TransBlue)
                        renderTranslucent = true;

                    // If this is empty, don't render it.
                    DynamicVertexBuffer regionBuffer = vertexBuffers[(byte)blockTexture, r];
                    if (regionBuffer == null)
                        continue;

                    // If this isn't in our view frustum, don't render it.
                    BoundingSphere regionBounds = new BoundingSphere(GetRegionCenter(r), REGIONSIZE);
                    BoundingFrustum boundingFrustum = new BoundingFrustum(gameInstance.propertyBag.playerCamera.ViewMatrix * gameInstance.propertyBag.playerCamera.ProjectionMatrix);
                    if (boundingFrustum.Contains(regionBounds) == ContainmentType.Disjoint)
                        continue;

                    // Make sure our vertex buffer is clean.
                    if (vertexListDirty[(byte)blockTexture, r])
                        continue;

                    // Actually render.
                    RenderVertexList(graphicsDevice, regionBuffer, blockTextures[(byte)blockTexture].Texture, blockTextures[(byte)blockTexture].LODColor, renderTranslucent, blockTexture == BlockTexture.Lava, (float)gameTime.TotalRealTime.TotalSeconds);
                }

            // Apply posteffects.
            if (bloomPosteffect != null)
                bloomPosteffect.Draw(graphicsDevice);
        }

        private void RenderVertexList(GraphicsDevice graphicsDevice, DynamicVertexBuffer vertexBuffer, Texture2D blockTexture, Color lodColor, bool renderTranslucent, bool renderLava, float elapsedTime)
        {
            if (vertexBuffer == null)
                return;

            if (renderLava)
            {
                basicEffect.CurrentTechnique = basicEffect.Techniques["LavaBlock"];
                basicEffect.Parameters["xTime"].SetValue(elapsedTime%5);
            }
            else
                basicEffect.CurrentTechnique = basicEffect.Techniques["Block"];
            
            basicEffect.Parameters["xWorld"].SetValue(Matrix.Identity);
            basicEffect.Parameters["xView"].SetValue(gameInstance.propertyBag.playerCamera.ViewMatrix);
            basicEffect.Parameters["xProjection"].SetValue(gameInstance.propertyBag.playerCamera.ProjectionMatrix);
            basicEffect.Parameters["xTexture"].SetValue(blockTexture);
            basicEffect.Parameters["xLODColor"].SetValue(lodColor.ToVector3());
            basicEffect.Begin();
            
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                if (renderTranslucent)
                {
                    // TODO: Make translucent blocks look like we actually want them to look!
                    // We probably also want to pull this out to be rendered AFTER EVERYTHING ELSE IN THE GAME.
                    graphicsDevice.RenderState.DepthBufferWriteEnable = false;
                    graphicsDevice.RenderState.AlphaBlendEnable = true;
                    graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
                    graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
                }

                graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                graphicsDevice.SamplerStates[0].MagFilter = TextureFilter.Point;
                graphicsDevice.VertexDeclaration = vertexDeclaration;
                graphicsDevice.Vertices[0].SetSource(vertexBuffer, 0, VertexPositionTextureShade.SizeInBytes);
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.SizeInBytes / VertexPositionTextureShade.SizeInBytes / 3);
                graphicsDevice.RenderState.CullMode = CullMode.None;

                if (renderTranslucent)
                {
                    graphicsDevice.RenderState.DepthBufferWriteEnable = true;
                    graphicsDevice.RenderState.AlphaBlendEnable = false;
                }

                pass.End();
            }
            
            basicEffect.End();
        }

        private void RegenerateDirtyVertexLists()
        {
            for (BlockTexture blockTexture = BlockTexture.None+1; blockTexture < BlockTexture.MAXIMUM; blockTexture++)
                for (int r = 0; r < NUMREGIONS; r++)
                    if (vertexListDirty[(byte)blockTexture, r])
                    {
                        vertexListDirty[(byte)blockTexture, r] = false;
                        Dictionary<uint, bool> faceList = faceMap[(byte)blockTexture, r];
                        vertexBuffers[(byte)blockTexture, r] = CreateVertexBufferFromFaceList(faceList, (byte)blockTexture, r);
                    }
        }

        public struct DynamicVertexBufferTag
        {
            public BlockEngine blockEngine;
            public int texture, region;
            public DynamicVertexBufferTag(BlockEngine blockEngine, int texture, int region)
            {
                this.blockEngine = blockEngine;
                this.texture = texture;
                this.region = region;
            }
        }

        // Create a dynamic vertex buffer. The arguments texture and region are used to flag a content reload if the device is lost.
        private DynamicVertexBuffer CreateVertexBufferFromFaceList(Dictionary<uint, bool> faceList, int texture, int region)
        {
            if (faceList.Count == 0)
                return null;

            VertexPositionTextureShade[] vertexList = new VertexPositionTextureShade[faceList.Count * 6];
            ulong vertexPointer = 0;
            foreach (uint faceInfo in faceList.Keys)
            {
                BuildFaceVertices(ref vertexList, vertexPointer, faceInfo, texture == (int)BlockTexture.Spikes);
                vertexPointer += 6;            
            }
            DynamicVertexBuffer vertexBuffer = new DynamicVertexBuffer(gameInstance.GraphicsDevice, vertexList.Length * VertexPositionTextureShade.SizeInBytes, BufferUsage.WriteOnly);
            vertexBuffer.ContentLost += new EventHandler(vertexBuffer_ContentLost);
            vertexBuffer.Tag = new DynamicVertexBufferTag(this, texture, region);
            vertexBuffer.SetData(vertexList);
            return vertexBuffer;
        }

        void vertexBuffer_ContentLost(object sender, EventArgs e)
        {
            DynamicVertexBuffer dvb = sender as DynamicVertexBuffer;
            if (dvb != null)
            {
                DynamicVertexBufferTag tag = (DynamicVertexBufferTag)dvb.Tag;
                tag.blockEngine.MakeRegionDirty(tag.texture, tag.region);
            }
        }

        private void BuildFaceVertices(ref VertexPositionTextureShade[] vertexList, ulong vertexPointer, uint faceInfo, bool isShockBlock)
        {
            // Decode the face information.
            ushort x = 0, y = 0, z = 0;
            BlockFaceDirection faceDir = BlockFaceDirection.MAXIMUM;
            DecodeBlockFace(faceInfo, ref x, ref y, ref z, ref faceDir);

            // Insert the vertices.
            switch (faceDir)
            {
                case BlockFaceDirection.XIncreasing:
                    {
                        vertexList[vertexPointer + 0] = new VertexPositionTextureShade(new Vector3(x + 1, y + 1, z + 1), new Vector2(0, 0), 0.6);
                        vertexList[vertexPointer + 1] = new VertexPositionTextureShade(new Vector3(x + 1, y + 1, z), new Vector2(1, 0), 0.6);
                        vertexList[vertexPointer + 2] = new VertexPositionTextureShade(new Vector3(x + 1, y, z + 1), new Vector2(0, 1), 0.6);
                        vertexList[vertexPointer + 3] = new VertexPositionTextureShade(new Vector3(x + 1, y, z + 1), new Vector2(0, 1), 0.6);
                        vertexList[vertexPointer + 4] = new VertexPositionTextureShade(new Vector3(x + 1, y + 1, z), new Vector2(1, 0), 0.6);
                        vertexList[vertexPointer + 5] = new VertexPositionTextureShade(new Vector3(x + 1, y, z), new Vector2(1, 1), 0.6);
                    }
                    break;


                case BlockFaceDirection.XDecreasing:
                    {
                        vertexList[vertexPointer + 0] = new VertexPositionTextureShade(new Vector3(x, y + 1, z), new Vector2(0, 0), 0.6);
                        vertexList[vertexPointer + 1] = new VertexPositionTextureShade(new Vector3(x, y + 1, z + 1), new Vector2(1, 0), 0.6);
                        vertexList[vertexPointer + 2] = new VertexPositionTextureShade(new Vector3(x, y, z + 1), new Vector2(1, 1), 0.6);
                        vertexList[vertexPointer + 3] = new VertexPositionTextureShade(new Vector3(x, y + 1, z), new Vector2(0, 0), 0.6);
                        vertexList[vertexPointer + 4] = new VertexPositionTextureShade(new Vector3(x, y, z + 1), new Vector2(1, 1), 0.6);
                        vertexList[vertexPointer + 5] = new VertexPositionTextureShade(new Vector3(x, y, z), new Vector2(0, 1), 0.6);
                    }
                    break;

                case BlockFaceDirection.YIncreasing:
                    {
                        vertexList[vertexPointer + 0] = new VertexPositionTextureShade(new Vector3(x, y + 1, z), new Vector2(0, 1), 0.8);
                        vertexList[vertexPointer + 1] = new VertexPositionTextureShade(new Vector3(x + 1, y + 1, z), new Vector2(0, 0), 0.8);
                        vertexList[vertexPointer + 2] = new VertexPositionTextureShade(new Vector3(x + 1, y + 1, z + 1), new Vector2(1, 0), 0.8);
                        vertexList[vertexPointer + 3] = new VertexPositionTextureShade(new Vector3(x, y + 1, z), new Vector2(0, 1), 0.8);
                        vertexList[vertexPointer + 4] = new VertexPositionTextureShade(new Vector3(x + 1, y + 1, z + 1), new Vector2(1, 0), 0.8);
                        vertexList[vertexPointer + 5] = new VertexPositionTextureShade(new Vector3(x, y + 1, z + 1), new Vector2(1, 1), 0.8);
                    }
                    break;

                case BlockFaceDirection.YDecreasing:
                    {
                        vertexList[vertexPointer + 0] = new VertexPositionTextureShade(new Vector3(x + 1, y, z + 1), new Vector2(0, 0), isShockBlock ? 1.5 : 0.2);
                        vertexList[vertexPointer + 1] = new VertexPositionTextureShade(new Vector3(x + 1, y, z), new Vector2(1, 0), isShockBlock ? 1.5 : 0.2);
                        vertexList[vertexPointer + 2] = new VertexPositionTextureShade(new Vector3(x, y, z + 1), new Vector2(0, 1), isShockBlock ? 1.5 : 0.2);
                        vertexList[vertexPointer + 3] = new VertexPositionTextureShade(new Vector3(x, y, z + 1), new Vector2(0, 1), isShockBlock ? 1.5 : 0.2);
                        vertexList[vertexPointer + 4] = new VertexPositionTextureShade(new Vector3(x + 1, y, z), new Vector2(1, 0), isShockBlock ? 1.5 : 0.2);
                        vertexList[vertexPointer + 5] = new VertexPositionTextureShade(new Vector3(x, y, z), new Vector2(1, 1), isShockBlock ? 1.5 : 0.2);
                    }
                    break;

                case BlockFaceDirection.ZIncreasing:
                    {
                        vertexList[vertexPointer + 0] = new VertexPositionTextureShade(new Vector3(x, y + 1, z + 1), new Vector2(0, 0), 0.4);
                        vertexList[vertexPointer + 1] = new VertexPositionTextureShade(new Vector3(x + 1, y + 1, z + 1), new Vector2(1, 0), 0.4);
                        vertexList[vertexPointer + 2] = new VertexPositionTextureShade(new Vector3(x + 1, y, z + 1), new Vector2(1, 1), 0.4);
                        vertexList[vertexPointer + 3] = new VertexPositionTextureShade(new Vector3(x, y + 1, z + 1), new Vector2(0, 0), 0.4);
                        vertexList[vertexPointer + 4] = new VertexPositionTextureShade(new Vector3(x + 1, y, z + 1), new Vector2(1, 1), 0.4);
                        vertexList[vertexPointer + 5] = new VertexPositionTextureShade(new Vector3(x, y, z + 1), new Vector2(0, 1), 0.4);
                    }
                    break;

                case BlockFaceDirection.ZDecreasing:
                    {
                        vertexList[vertexPointer + 0] = new VertexPositionTextureShade(new Vector3(x + 1, y + 1, z), new Vector2(0, 0), 0.4);
                        vertexList[vertexPointer + 1] = new VertexPositionTextureShade(new Vector3(x, y + 1, z), new Vector2(1, 0), 0.4);
                        vertexList[vertexPointer + 2] = new VertexPositionTextureShade(new Vector3(x + 1, y, z), new Vector2(0, 1), 0.4);
                        vertexList[vertexPointer + 3] = new VertexPositionTextureShade(new Vector3(x + 1, y, z), new Vector2(0, 1), 0.4);
                        vertexList[vertexPointer + 4] = new VertexPositionTextureShade(new Vector3(x, y + 1, z), new Vector2(1, 0), 0.4);
                        vertexList[vertexPointer + 5] = new VertexPositionTextureShade(new Vector3(x, y, z), new Vector2(1, 1), 0.4);
                    }
                    break;
            }
        }

        private void _AddBlock(ushort x, ushort y, ushort z, BlockFaceDirection dir, BlockType type, int x2, int y2, int z2, BlockFaceDirection dir2)
        {
            BlockType type2 = blockList[x2, y2, z2];
            if (type2 != BlockType.None && type != BlockType.TransRed && type != BlockType.TransBlue && type2 != BlockType.TransRed && type2 != BlockType.TransBlue)
                HideQuad((ushort)x2, (ushort)y2, (ushort)z2, dir2, type2);
            else
                ShowQuad(x, y, z, dir, type);
        }

        public void AddBlock(ushort x, ushort y, ushort z, BlockType blockType)
        {
            if (x <= 0 || y <= 0 || z <= 0 || x >= MAPSIZE - 1 || y >= MAPSIZE - 1 || z >= MAPSIZE - 1)
                return;

            blockList[x, y, z] = blockType;

            _AddBlock(x, y, z, BlockFaceDirection.XIncreasing, blockType, x + 1, y, z, BlockFaceDirection.XDecreasing);
            _AddBlock(x, y, z, BlockFaceDirection.XDecreasing, blockType, x - 1, y, z, BlockFaceDirection.XIncreasing);
            _AddBlock(x, y, z, BlockFaceDirection.YIncreasing, blockType, x, y + 1, z, BlockFaceDirection.YDecreasing);
            _AddBlock(x, y, z, BlockFaceDirection.YDecreasing, blockType, x, y - 1, z, BlockFaceDirection.YIncreasing);
            _AddBlock(x, y, z, BlockFaceDirection.ZIncreasing, blockType, x, y, z + 1, BlockFaceDirection.ZDecreasing);
            _AddBlock(x, y, z, BlockFaceDirection.ZDecreasing, blockType, x, y, z - 1, BlockFaceDirection.ZIncreasing);
        }

        private void _RemoveBlock(ushort x, ushort y, ushort z, BlockFaceDirection dir, int x2, int y2, int z2, BlockFaceDirection dir2)
        {
            BlockType type = blockList[x, y, z];
            BlockType type2 = blockList[x2, y2, z2];
            if (type2 != BlockType.None && type != BlockType.TransRed && type != BlockType.TransBlue && type2 != BlockType.TransRed && type2 != BlockType.TransBlue)
                ShowQuad((ushort)x2, (ushort)y2, (ushort)z2, dir2, type2);
            else
                HideQuad(x, y, z, dir, type);
        }

        public void RemoveBlock(ushort x, ushort y, ushort z)
        {
            if (x <= 0 || y <= 0 || z <= 0 || x >= MAPSIZE - 1 || y >= MAPSIZE - 1 || z >= MAPSIZE - 1)
                return;

            _RemoveBlock(x, y, z, BlockFaceDirection.XIncreasing, x + 1, y, z, BlockFaceDirection.XDecreasing);
            _RemoveBlock(x, y, z, BlockFaceDirection.XDecreasing, x - 1, y, z, BlockFaceDirection.XIncreasing);
            _RemoveBlock(x, y, z, BlockFaceDirection.YIncreasing, x, y + 1, z, BlockFaceDirection.YDecreasing);
            _RemoveBlock(x, y, z, BlockFaceDirection.YDecreasing, x, y - 1, z, BlockFaceDirection.YIncreasing);
            _RemoveBlock(x, y, z, BlockFaceDirection.ZIncreasing, x, y, z + 1, BlockFaceDirection.ZDecreasing);
            _RemoveBlock(x, y, z, BlockFaceDirection.ZDecreasing, x, y, z - 1, BlockFaceDirection.ZIncreasing);

            blockList[x, y, z] = BlockType.None;
        }

        private uint EncodeBlockFace(ushort x, ushort y, ushort z, BlockFaceDirection faceDir)
        {
            //TODO: OPTIMIZE BY HARD CODING VALUES IN
            return (uint)(x + y * MAPSIZE + z * MAPSIZE * MAPSIZE + (byte)faceDir * MAPSIZE * MAPSIZE * MAPSIZE);
        }

        private void DecodeBlockFace(uint faceCode, ref ushort x, ref ushort y, ref ushort z, ref BlockFaceDirection faceDir)
        {
            x = (ushort)(faceCode % MAPSIZE);
            faceCode = (faceCode - x) / MAPSIZE;
            y = (ushort)(faceCode % MAPSIZE);
            faceCode = (faceCode - y) / MAPSIZE;
            z = (ushort)(faceCode % MAPSIZE);
            faceCode = (faceCode - z) / MAPSIZE;
            faceDir = (BlockFaceDirection)faceCode;
        }

        // Returns the region that a block at (x,y,z) should belong in.
        private uint GetRegion(ushort x, ushort y, ushort z)
        {
            return (uint)(x / REGIONSIZE + (y / REGIONSIZE) * REGIONRATIO + (z / REGIONSIZE) * REGIONRATIO * REGIONRATIO);
        }

        private Vector3 GetRegionCenter(uint regionNumber)
        {
            uint x, y, z;
            x = regionNumber % REGIONRATIO;
            regionNumber = (regionNumber - x) / REGIONRATIO;
            y = regionNumber % REGIONRATIO;
            regionNumber = (regionNumber - y) / REGIONRATIO;
            z = regionNumber;
            return new Vector3(x * REGIONSIZE + REGIONSIZE / 2, y * REGIONSIZE + REGIONSIZE / 2, z * REGIONSIZE + REGIONSIZE / 2);            
        }

        private void ShowQuad(ushort x, ushort y, ushort z, BlockFaceDirection faceDir, BlockType blockType)
        {
            BlockTexture blockTexture = blockTextureMap[(byte)blockType, (byte)faceDir];
            uint blockFace = EncodeBlockFace(x, y, z, faceDir);
            uint region = GetRegion(x, y, z);
            if (!faceMap[(byte)blockTexture, region].ContainsKey(blockFace))
                faceMap[(byte)blockTexture, region].Add(blockFace, true);
            vertexListDirty[(byte)blockTexture, region] = true;
        }

        private void HideQuad(ushort x, ushort y, ushort z, BlockFaceDirection faceDir, BlockType blockType)
        {
            BlockTexture blockTexture = blockTextureMap[(byte)blockType, (byte)faceDir];
            uint blockFace = EncodeBlockFace(x, y, z, faceDir);
            uint region = GetRegion(x, y, z);
            if (faceMap[(byte)blockTexture, region].ContainsKey(blockFace))
                faceMap[(byte)blockTexture, region].Remove(blockFace);
            vertexListDirty[(byte)blockTexture, region] = true;
        }
    }
}
