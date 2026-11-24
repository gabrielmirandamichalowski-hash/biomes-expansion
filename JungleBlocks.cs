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

    /// <summary>Tronco da jungle. So e usado pelas arvores da jungle (e pode ser coletado e usado como madeira).</summary>
    public class JungleWoodBlock : WoodBlock {
        // O jogo preenche este campo com o ID real do bloco depois de alocar o bloco.
        public static int Index = -1;

        // slot 1 = corte (topo e base), slot 0 = casca (laterais), na textura JungleBlocks.
        public JungleWoodBlock() : base(1, 0) { }

        public override Texture2D GetDefaultTexture(int value) => JungleTextures.Blocks;

        // Os fragmentos ao quebrar usam o atlas original: usa o tile da casca do carvalho (slot 20).
        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) =>
            new(subsystemTerrain, position, strength, DestructionDebrisScale, new Color(150, 120, 80), 20);
    }

    /// <summary>
    /// Folhas da jungle. Cinza na textura; o jogo pinta com a cor da grama do clima do lugar,
    /// entao ficam no verde mais vivo dentro da jungle.
    /// </summary>
    public class JungleLeavesBlock : LeavesBlock {
        public static int Index = -1;

        public override Color GetLeavesBlockColor(int value, Terrain terrain, int x, int y, int z) =>
            BlockColorsMap.Grass.Lookup(terrain, x, y, z);

        public override Color GetLeavesItemColor(int value, DrawBlockEnvironmentData environmentData) =>
            BlockColorsMap.Grass.Lookup(environmentData);

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
