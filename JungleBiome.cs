using Engine;
using Game;

namespace BiomesExpansion {
    /// <summary>
    /// Jungle v0.2 (estilo Minecraft): arvores gigantes de tronco largo, arvores medias de copa
    /// fechada, arbustos e cipos. Acrescenta vegetacao por cima da original; nada do jogo base e removido.
    /// </summary>
    public class JungleBiome {
        readonly BiomeNoise m_noise;
        readonly SubsystemTerrain m_subsystemTerrain;
        readonly int m_seed;
        readonly TerrainBrush[] m_giantTrees;
        readonly TerrainBrush[] m_midTrees;
        readonly TerrainBrush[] m_bushes;
        int m_loggedTrees;

        public JungleBiome(BiomeNoise noise, SubsystemTerrain subsystemTerrain, int seed) {
            m_noise = noise;
            m_subsystemTerrain = subsystemTerrain;
            m_seed = seed;
            int trunk = PlantsManager.GetTreeTrunkValue(TreeType.Oak);
            int leaves = PlantsManager.GetTreeLeavesValue(TreeType.Oak);
            Game.Random random = new(seed ^ 0x6A09);
            m_giantTrees = new TerrainBrush[6];
            for (int i = 0; i < m_giantTrees.Length; i++) {
                m_giantTrees[i] = JungleTreeBuilder.CreateGiantTree(random, trunk, leaves);
            }
            m_midTrees = new TerrainBrush[10];
            for (int i = 0; i < m_midTrees.Length; i++) {
                m_midTrees[i] = JungleTreeBuilder.CreateMidTree(random, trunk, leaves);
            }
            m_bushes = new TerrainBrush[6];
            for (int i = 0; i < m_bushes.Length; i++) {
                m_bushes[i] = JungleTreeBuilder.CreateBush(random, trunk, leaves);
            }
        }

        /// <summary>Passo de geracao (pass 4, depois das arvores originais).</summary>
        public void GenerateJungle(TerrainChunk chunk) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            Game.Random random = new(m_seed + chunk.Coords.X * 7919 + chunk.Coords.Y * 104729);
            List<Point3> placed = [];
            //            pinceis        tent. max  chance  desvio  dist  plano  sob copa
            Place(chunk, terrain, random, m_giantTrees, 12, 2, 0.55f, 4, 11, 6, true, false, placed);
            Place(chunk, terrain, random, m_midTrees, 40, 8, 1.00f, 3, 12, 3, false, false, placed);
            // Arbustos por ultimo e em lista propria: procuram o chao POR BAIXO da copa das arvores.
            List<Point3> bushPlaced = [];
            Place(chunk, terrain, random, m_bushes, 80, 24, 0.95f, 2, 13, 2, false, true, bushPlaced);
        }

        void Place(TerrainChunk chunk,
            Terrain terrain,
            Game.Random random,
            TerrainBrush[] brushes,
            int attempts,
            int maxCount,
            float chance,
            int minOffset,
            int maxOffset,
            int minDistance,
            bool needsFlat,
            bool belowCanopy,
            List<Point3> placed) {
            int count = 0;
            for (int attempt = 0; attempt < attempts && count < maxCount; attempt++) {
                int x = chunk.Origin.X + random.Int(minOffset, maxOffset);
                int z = chunk.Origin.Y + random.Int(minOffset, maxOffset);
                float weight = m_noise.JungleWeight(x, z);
                if (weight <= 0.05f || !random.Bool(weight * chance)) {
                    continue;
                }
                int y = belowCanopy ? FindGroundBelowCanopy(terrain, x, z) : terrain.CalculateTopmostCellHeight(x, z);
                if (y < 66) {
                    continue;
                }
                int ground = terrain.GetCellContentsFast(x, y, z);
                if (ground != 2 && ground != 8) {
                    continue;
                }
                if (needsFlat && !IsFlat(terrain, x, y, z)) {
                    continue;
                }
                if (TooClose(placed, x, z, minDistance)) {
                    continue;
                }
                TerrainBrush brush = brushes[random.Int(0, brushes.Length - 1)];
                brush.PaintFast(chunk, x, y + 1, z);
                chunk.AddBrushPaint(x, y + 1, z, brush);
                placed.Add(new Point3(x, 0, z));
                count++;
                if (Interlocked.Increment(ref m_loggedTrees) == 1) {
                    Log.Information("[BiomesExpansion] Primeira vegetacao de jungle em (" + x + ", " + y + ", " + z + ")");
                }
            }
        }

        /// <summary>
        /// Desce a partir do topo da coluna atravessando so ar, folhas e hera ate achar terra ou grama.
        /// Devolve -1 se encontrar tronco, pedra, agua etc. (coluna ocupada).
        /// </summary>
        static int FindGroundBelowCanopy(Terrain terrain, int x, int z) {
            int y = terrain.CalculateTopmostCellHeight(x, z);
            for (int i = 0; i < 64 && y >= 1; i++, y--) {
                int contents = terrain.GetCellContentsFast(x, y, z);
                if (contents == 2 || contents == 8) {
                    return y;
                }
                if (contents != 0 && contents != 12 && contents != 197) {
                    return -1;
                }
            }
            return -1;
        }

        static bool TooClose(List<Point3> placed, int x, int z, int minDistance) {
            foreach (Point3 p in placed) {
                int dx = p.X - x;
                int dz = p.Z - z;
                if (dx * dx + dz * dz < minDistance * minDistance) {
                    return true;
                }
            }
            return false;
        }

        static bool IsFlat(Terrain terrain, int x, int y, int z) {
            int[] offsets = [-3, 3];
            foreach (int dx in offsets) {
                foreach (int dz in offsets) {
                    if (Math.Abs(terrain.CalculateTopmostCellHeight(x + dx, z + dz) - y) > 3) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
