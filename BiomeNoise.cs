using Engine;
using Game;

namespace BiomesExpansion {
    /// <summary>
    /// Mascaras de bioma suaves (0 a 1), deterministicas pela seed do mundo.
    /// Somente leitura depois de criada, portanto segura para varias threads.
    /// </summary>
    public class BiomeNoise {
        /// <summary>
        /// MODO DE TESTE: com true, a jungle aparece em todo lugar (so para ver o bioma rapido).
        /// Em false (normal), ela surge so em regioes quentes e umidas, em manchas grandes.
        /// </summary>
        public const bool ForceJungleEverywhere = false;

        readonly TerrainContentsGenerator24 m_generator;
        readonly float m_jungleOffsetX;
        readonly float m_jungleOffsetZ;

        public BiomeNoise(TerrainContentsGenerator24 generator, int seed) {
            m_generator = generator;
            Game.Random random = new Game.Random(seed ^ 0x5EED1A);
            m_jungleOffsetX = random.Float(-5000f, 5000f);
            m_jungleOffsetZ = random.Float(-5000f, 5000f);
        }

        public static float Squish(float v, float zero, float one) => MathUtils.Saturate((v - zero) / (one - zero));

        /// <summary>
        /// 0 = sem jungle, 1 = jungle plena. Condicoes (todas suaves, multiplicadas):
        ///  - terreno baixo e plano: afasta a jungle de colinas e montanhas (fator de cordilheira do jogo);
        ///  - clima quente e umido, calculado como o jogo faz mas com menos oitavas de ruido
        ///    (clima "suavizado"), o que forma manchas bem maiores e menos recortadas;
        ///  - uma mancha do ruido proprio da jungle.
        /// Abaixo de um valor minimo a mascara vale exatamente 0: nao sobram arvores soltas fora da jungle.
        /// Calibrado simulando o ruido do jogo (8 mundos): cerca de 11% do mapa tem alguma jungle,
        /// ~8% e nucleo, e metade da area esta em manchas de ~480 blocos de diametro ou mais.
        /// </summary>
        public float JungleWeight(int x, int z) {
            if (ForceJungleEverywhere) {
                return 1f;
            }
            float scaling = m_generator.TGBiomeScaling;
            // Longe de colinas e montanhas: so em terreno baixo.
            float flat = 1f - Squish(m_generator.CalculateMountainRangeFactor(x, z), 0.76f, 0.86f);
            if (flat <= 0f) {
                return 0f;
            }
            WorldSettings settings = m_generator.m_worldSettings;
            float temperature = 16f * MathUtils.Saturate(
                3f * SimplexNoise.OctavedNoise(
                    x + m_generator.m_temperatureOffset.X,
                    z + m_generator.m_temperatureOffset.Y,
                    0.0015f / scaling,
                    2,
                    2f,
                    0.6f
                ) - 1.1f + settings.TemperatureOffset / 16f
            );
            float warm = Squish(temperature, 6f, 9f);
            if (warm <= 0f) {
                return 0f;
            }
            float humidity = 16f * MathUtils.Saturate(
                3f * SimplexNoise.OctavedNoise(
                    x + m_generator.m_humidityOffset.X,
                    z + m_generator.m_humidityOffset.Y,
                    0.0012f / scaling,
                    2,
                    2f,
                    0.6f
                ) - 0.9f + settings.HumidityOffset / 16f
            );
            float wet = Squish(humidity, 5f, 8f);
            if (wet <= 0f) {
                return 0f;
            }
            float patch = SimplexNoise.OctavedNoise(
                x + m_jungleOffsetX,
                z + m_jungleOffsetZ,
                0.0007f / scaling,
                2,
                2f,
                0.55f
            );
            float raw = warm * wet * Squish(patch, 0.45f, 0.53f) * flat;
            return Squish(raw, 0.20f, 0.70f);
        }
    }
}
