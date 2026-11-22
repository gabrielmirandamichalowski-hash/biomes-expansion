using Engine;
using Game;

namespace BiomesExpansion {
    /// <summary>
    /// Jungle v0.3 (estilo Minecraft): arvores gigantes de tronco largo, medias, pequenas, arbustos
    /// e cipos; remove as arvores originais dentro da jungle e deixa a grama e as folhas mais vivas.
    /// </summary>
    public class JungleBiome {
        readonly TerrainContentsGenerator24 m_generator;
        readonly BiomeNoise m_noise;
        readonly SubsystemTerrain m_subsystemTerrain;
        readonly int m_seed;
        readonly TerrainBrush[] m_giantTrees;
        readonly TerrainBrush[] m_midTrees;
        readonly TerrainBrush[] m_smallTrees;
        readonly TerrainBrush[] m_bushes;
        int m_loggedTrees;

        public JungleBiome(TerrainContentsGenerator24 generator, BiomeNoise noise, SubsystemTerrain subsystemTerrain, int seed) {
            m_generator = generator;
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
            m_smallTrees = new TerrainBrush[10];
            for (int i = 0; i < m_smallTrees.Length; i++) {
                m_smallTrees[i] = JungleTreeBuilder.CreateSmallTree(random, trunk, leaves);
            }
            m_bushes = new TerrainBrush[6];
            for (int i = 0; i < m_bushes.Length; i++) {
                m_bushes[i] = JungleTreeBuilder.CreateBush(random, trunk, leaves);
            }
        }

        /// <summary>
        /// Substitui o passo original "GenerateTrees". E a mesma logica do jogo, mas cada candidato a
        /// arvore e descartado com probabilidade proporcional a mascara da jungle: no centro da jungle
        /// nao nasce arvore original, e nas bordas elas voltam aos poucos.
        /// </summary>
        public void GenerateVanillaTrees(TerrainChunk chunk) {
            if (!m_generator.TGExtras) {
                return;
            }
            Terrain terrain = m_subsystemTerrain.Terrain;
            int cx = chunk.Coords.X;
            int cz = chunk.Coords.Y;
            Game.Random random = new(m_seed + cx + 3943 * cz);
            int humidity = m_generator.CalculateHumidity(cx * 16, cz * 16);
            int temperature = m_generator.CalculateTemperature(cx * 16, cz * 16);
            float forestDensity = m_generator.CalculateForestDensity(cx * 16, cz * 16);
            int treeCount = (int)(6f * forestDensity);
            int placed = 0;
            for (int k = 0; k < 36 && placed < treeCount; k++) {
                int x = cx * 16 + random.Int(2, 13);
                int z = cz * 16 + random.Int(2, 13);
                float jungle = m_noise.JungleWeight(x, z);
                if (jungle > 0f && random.Bool(MathUtils.Saturate(jungle * 1.3f))) {
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
                if (!BlocksManager.Blocks[terrain.GetCellContentsFast(x + 1, y, z)].IsCollidable
                    && !BlocksManager.Blocks[terrain.GetCellContentsFast(x - 1, y, z)].IsCollidable
                    && !BlocksManager.Blocks[terrain.GetCellContentsFast(x, y, z + 1)].IsCollidable
                    && !BlocksManager.Blocks[terrain.GetCellContentsFast(x, y, z - 1)].IsCollidable) {
                    TreeType? treeType = PlantsManager.GenerateRandomTreeType(
                        random,
                        temperature + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y),
                        humidity,
                        y
                    );
                    if (treeType.HasValue) {
                        var brushes = PlantsManager.GetTreeBrushes(treeType.Value);
                        TerrainBrush brush = brushes[random.Int(brushes.Count)];
                        brush.PaintFast(chunk, x, y, z);
                        chunk.AddBrushPaint(x, y, z, brush);
                    }
                    placed++;
                }
            }
        }

        /// <summary>Vegetacao da jungle (pass 4, depois das arvores originais).</summary>
        public void GenerateJungle(TerrainChunk chunk) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            Game.Random random = new(m_seed + chunk.Coords.X * 7919 + chunk.Coords.Y * 104729);
            List<Point3> placed = [];
            //            pinceis        tent. max  chance  desvio  dist  plano  sob copa
            Place(chunk, terrain, random, m_giantTrees, 12, 2, 0.55f, 4, 11, 6, true, false, placed);
            Place(chunk, terrain, random, m_midTrees, 40, 8, 1.00f, 3, 12, 3, false, false, placed);
            Place(chunk, terrain, random, m_smallTrees, 60, 14, 1.00f, 2, 13, 3, false, false, placed);
            // Arbustos por ultimo e em lista propria: procuram o chao POR BAIXO da copa das arvores.
            List<Point3> bushPlaced = [];
            Place(chunk, terrain, random, m_bushes, 80, 24, 0.95f, 2, 13, 2, false, true, bushPlaced);
        }

        /// <summary>
        /// Deixa o clima da jungle mais quente e umido, coluna por coluna, proporcional a mascara.
        /// O jogo colore grama, folhas e hera pelo clima: com umidade alta e clima quente a grama
        /// fica no verde mais vivo da paleta. Efeito colateral: a jungle fica mais quente e umida.
        /// </summary>
        public void GenerateClimate(TerrainChunk chunk) {
            for (int lx = 0; lx < 16; lx++) {
                for (int lz = 0; lz < 16; lz++) {
                    int worldX = chunk.Origin.X + lx;
                    int worldZ = chunk.Origin.Y + lz;
                    float weight = m_noise.JungleWeight(worldX, worldZ);
                    if (weight <= 0.01f) {
                        continue;
                    }
                    int temperature = chunk.GetTemperatureFast(lx, lz);
                    // Altura do terreno SEM vegetacao (as copas das arvores nao contam).
                    float groundHeight = m_generator.CalculateHeight(worldX, worldZ);
                    // Preserva neve e gelo: o passo de neve do jogo usa esta mesma temperatura.
                    // Colunas congeladas (ou quase) na altura do terreno ficam como o jogo gerou.
                    if (temperature + SubsystemWeather.GetTemperatureAdjustmentAtHeight((int)groundHeight) <= 3) {
                        continue;
                    }
                    // O efeito some gradualmente entre 90 e 120 blocos de altura (montanhas).
                    weight *= 1f - MathUtils.Saturate((groundHeight - 90f) / 30f);
                    if (weight <= 0.01f) {
                        continue;
                    }
                    int humidity = chunk.GetHumidityFast(lx, lz);
                    int newTemperature = Math.Max(temperature, (int)MathF.Round(MathUtils.Lerp(temperature, 12f, weight)));
                    int newHumidity = Math.Max(humidity, (int)MathF.Round(MathUtils.Lerp(humidity, 15f, weight)));
                    if (newTemperature != temperature) {
                        chunk.SetTemperatureFast(lx, lz, newTemperature);
                    }
                    if (newHumidity != humidity) {
                        chunk.SetHumidityFast(lx, lz, newHumidity);
                    }
                }
            }
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
