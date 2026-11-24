using Engine;
using Engine.Graphics;
using Game;

namespace BiomesExpansion {
    /// <summary>
    /// Textura propria dos blocos da jungle (fora do atlas original do jogo).
    /// Slots (grade 16x16): 0 = casca do tronco, 1 = corte do tronco, 2 = folhas.
    /// Carregada na thread principal, no fim do carregamento do jogo.
    /// </summary>
    public static class JungleTextures {
        public static Texture2D Blocks { get; private set; }

        public static void Load() {
            try {
                Blocks = ContentManager.Get<Texture2D>("Textures/JungleBlocks");
            }
            catch (Exception e) {
                Log.Error("[BiomesExpansion] Nao foi possivel carregar Textures/JungleBlocks: " + e);
            }
        }
    }

    /// <summary>
    /// Tronco da jungle. So e usado pelas arvores da jungle (e pode ser coletado e usado como madeira).
    /// Todas as propriedades sao definidas aqui no codigo (copiadas do tronco de carvalho do jogo),
    /// sem depender de arquivo csv.
    /// </summary>
    public class JungleWoodBlock : WoodBlock {
        // O jogo preenche este campo com o ID real do bloco depois de alocar o bloco.
        public static int Index = -1;

        // slot 1 = corte (topo e base), slot 0 = casca (laterais), na textura JungleBlocks.
        public JungleWoodBlock() : base(1, 0) {
            DefaultDisplayName = "Jungle Wood";
            DefaultCategory = "Plants";
            Behaviors = "WoodBlockBehavior";
            DisplayOrder = 41;
            CraftingId = "wood";
            FirstPersonScale = 0.4f;
            FirstPersonOffset = new Vector3(0.5f, -0.5f, -0.6f);
            FirstPersonRotation = new Vector3(0f, 40f, 0f);
            InHandScale = 0.3f;
            InHandOffset = new Vector3(0f, 0.12f, 0f);
            InHandRotation = new Vector3(0f, 0f, 45f);
            IsTransparent = false;
            IsFluidBlocker = true;
            DefaultShadowStrength = 15;
            LightAttenuation = 0;
            ObjectShadowStrength = 1f;
            DefaultDropCount = 1f;
            DefaultExperienceCount = 0.01f;
            Density = 0.5f;
            FuelHeatLevel = 1f;
            FuelFireDuration = 20f;
            DefaultSoundMaterialName = "Wood";
            ToolLevel = 1;
            DefaultProjectilePower = 2f;
            FireDuration = 20f;
            ExplosionResilience = 20f;
            DigMethod = BlockDigMethod.Hack;
            DigResilience = 5f;
            ProjectileResilience = 1f;
        }

        // Depois do carregamento o jogo ja definiu o ID do bloco: o tronco dropa a si mesmo.
        public override void Initialize() {
            base.Initialize();
            DefaultDropContent = BlockIndex;
        }

        public override Texture2D GetDefaultTexture(int value) => JungleTextures.Blocks;

        // Os fragmentos ao quebrar usam o atlas original: usa o tile da casca do carvalho (slot 20).
        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) =>
            new(subsystemTerrain, position, strength, DestructionDebrisScale, new Color(150, 120, 80), 20);
    }

    /// <summary>
    /// Folhas da jungle. Cinza na textura; o jogo pinta com a MESMA paleta de cor das folhas de carvalho.
    /// Propriedades definidas no codigo (copiadas das folhas do jogo), sem depender de csv.
    /// </summary>
    public class JungleLeavesBlock : LeavesBlock {
        public static int Index = -1;

        public JungleLeavesBlock() {
            DefaultDisplayName = "Jungle Leaves";
            DefaultCategory = "Plants";
            DisplayOrder = 49;
            CraftingId = "jungleleaves";
            FirstPersonScale = 0.4f;
            FirstPersonOffset = new Vector3(0.5f, -0.5f, -0.6f);
            FirstPersonRotation = new Vector3(0f, 40f, 0f);
            InHandScale = 0.3f;
            InHandOffset = new Vector3(0f, 0.12f, 0f);
            InHandRotation = new Vector3(0f, 0f, 45f);
            // Folhas sao transparentes: sem isso as faces entre folhas e blocos vizinhos somem
            // (efeito raio-x) e a luz nao passa.
            IsTransparent = true;
            IsFluidBlocker = false;
            DefaultShadowStrength = 12;
            LightAttenuation = 3;
            ObjectShadowStrength = 0f;
            DefaultDropCount = 1f;
            RequiredToolLevel = 1;
            Density = 0.5f;
            FuelHeatLevel = 1f;
            FuelFireDuration = 1f;
            DefaultSoundMaterialName = "Leaves";
            ToolLevel = 1;
            FireDuration = 8f;
            ExplosionResilience = 2f;
            DigMethod = BlockDigMethod.Hack;
            DigResilience = 0f;
            ProjectileResilience = 0.5f;
            // Slot 2 da textura JungleBlocks = folhas.
            DefaultTextureSlot = 2;
        }

        public override void Initialize() {
            base.Initialize();
            DefaultDropContent = BlockIndex;
        }

        public override int GetFaceTextureSlot(int face, int value) => 2;

        public override Color GetLeavesBlockColor(int value, Terrain terrain, int x, int y, int z) =>
            BlockColorsMap.OakLeaves.Lookup(terrain, x, y, z);

        public override Color GetLeavesItemColor(int value, DrawBlockEnvironmentData environmentData) =>
            BlockColorsMap.OakLeaves.Lookup(environmentData);

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(BlockIndex);
        }

        public override Texture2D GetDefaultTexture(int value) => JungleTextures.Blocks;

        // Os fragmentos ao quebrar usam o atlas original: usa o tile das folhas do carvalho (slot 52).
        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) =>
            new(
                subsystemTerrain,
                position,
                strength,
                DestructionDebrisScale,
                GetLeavesBlockColor(
                    value,
                    subsystemTerrain.Terrain,
                    Terrain.ToCell(position.X),
                    Terrain.ToCell(position.Y),
                    Terrain.ToCell(position.Z)
                ),
                52
            );
    }
}
