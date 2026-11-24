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
        /// 0 = sem jungle, 1 = jungle plena. Exige clima realmente quente e umido (clima original do
        /// jogo) E uma mancha do ruido proprio. Abaixo de um valor minimo a mascara vale exatamente 0,
        /// entao nao sobram arvores soltas fora da jungle: ela comeca e termina em bordas definidas
        /// (com transicao gradual so dentro da borda).
        /// Calibrado simulando o ruido do jogo: ~7,7% do mapa tem alguma jungle e ~5,3% e nucleo.
        /// </summary>
        public float JungleWeight(int x, int z) {
            if (ForceJungleEverywhere) {
                return 1f;
            }
            float warm = Squish(m_generator.CalculateTemperature(x, z), 7f, 10f);
            if (warm <= 0f) {
                return 0f;
            }
            float wet = Squish(m_generator.CalculateHumidity(x, z), 7f, 10f);
            if (wet <= 0f) {
                return 0f;
            }
            float patch = SimplexNoise.OctavedNoise(
                x + m_jungleOffsetX,
                z + m_jungleOffsetZ,
                0.0012f / m_generator.TGBiomeScaling,
                4,
                2f,
                0.55f
            );
            float raw = warm * wet * Squish(patch, 0.52f, 0.61f);
            return Squish(raw, 0.30f, 0.80f);
        }
    }
}
