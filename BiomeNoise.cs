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
        /// Depois de testar, volte para false para a jungle surgir so em regioes quentes e umidas.
        /// </summary>
        public const bool ForceJungleEverywhere = true;

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
        /// 0 = sem jungle, 1 = jungle plena. Exige clima quente e umido (usa o clima original
        /// do jogo) e um ruido proprio de manchas grandes, para a jungle aparecer em regioes
        /// coerentes e com transicao gradual nas bordas.
        /// </summary>
        public float JungleWeight(int x, int z) {
            if (ForceJungleEverywhere) {
                return 1f;
            }
            float warm = Squish(m_generator.CalculateTemperature(x, z), 6f, 9f);
            if (warm <= 0f) {
                return 0f;
            }
            float wet = Squish(m_generator.CalculateHumidity(x, z), 4f, 7f);
            if (wet <= 0f) {
                return 0f;
            }
            float patch = SimplexNoise.OctavedNoise(
                x + m_jungleOffsetX,
                z + m_jungleOffsetZ,
                0.0010f / m_generator.TGBiomeScaling,
                4,
                2f,
                0.55f
            );
            return warm * wet * Squish(patch, 0.55f, 0.65f);
        }
    }
}
