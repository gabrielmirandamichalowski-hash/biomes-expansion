using Engine;
using Game;

namespace BiomesExpansion {
    /// <summary>
    /// Jungle v0.1: arvores gigantes e densas dentro da mascara de jungle.
    /// Acrescenta arvores por cima das originais (nao remove nada do jogo base).
    /// </summary>
    public class JungleBiome {
        readonly BiomeNoise m_noise;
        readonly SubsystemTerrain m_subsystemTerrain;
        readonly int m_seed;
        readonly TerrainBrush[] m_trees;
        int m_loggedTrees;

        public JungleBiome(BiomeNoise noise, SubsystemTerrain subsystemTerrain, int seed) {
            m_noise = noise;
            m_subsystemTerrain = subsystemTerrain;
            m_seed = seed;
            m_trees = CreateTreeBrushes(seed);
        }

        static TerrainBrush[] CreateTreeBrushes(int seed) {
            // Alturas variadas: das arvores medias aos gigantes do dossel.
            int[] heights = [12, 14, 15, 16, 18, 19, 20, 22, 24, 26, 28, 30];
            int trunk = PlantsManager.GetTreeTrunkValue(TreeType.Oak);
            int leaves = PlantsManager.GetTreeLeavesValue(TreeType.Oak);
            Game.Random random = new Game.Random(seed ^ 0x6A09);
            TerrainBrush[] brushes = new TerrainBrush[heights.Length];
            for (int i = 0; i < heights.Length; i++) {
                int height = heights[i];
                int branches = 14 + i;
                brushes[i] = PlantsManager.CreateTreeBrush(
                    random,
                    trunk,
                    leaves,
                    height,
                    branches,
                    3,
                    (y, round) => y < 0.45f * height ? 0.02f : 0.38f,
                    y => y < 0.45f * height || y > 0.95f * height ? 0f : random.Float(0.4f, 1f) * 0.3f * height
                );
            }
            return brushes;
        }

        /// <summary>Passo de geracao (pass 4, depois das arvores originais).</summary>
        public void GenerateJungleTrees(TerrainChunk chunk) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            Game.Random random = new Game.Random(m_seed + chunk.Coords.X * 7919 + chunk.Coords.Y * 104729);
            int placed = 0;
            for (int attempt = 0; attempt < 48 && placed < 9; attempt++) {
                int x = chunk.Origin.X + random.Int(2, 13);
                int z = chunk.Origin.Y + random.Int(2, 13);
                float weight = m_noise.JungleWeight(x, z);
                if (weight <= 0.05f || !random.Bool(weight)) {
                    continue;
                }
                int y = terrain.CalculateTopmostCellHeight(x, z);
                if (y < 66) {
                    continue;
                }
                int ground = terrain.GetCellContentsFast(x, y, z);
                if (ground != 2 && ground != 8) {
                    continue;
                }
                y++;
                TerrainBrush brush = m_trees[random.Int(m_trees.Length)];
                brush.PaintFast(chunk, x, y, z);
                chunk.AddBrushPaint(x, y, z, brush);
                placed++;
                if (Interlocked.Increment(ref m_loggedTrees) == 1) {
                    Log.Information("[BiomesExpansion] Primeira arvore de jungle em (" + x + ", " + y + ", " + z + ")");
                }
            }
        }
    }
}
