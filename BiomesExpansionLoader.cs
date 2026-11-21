using Engine;
using Game;

namespace BiomesExpansion {
    /// <summary>
    /// Etapa 0: apenas prova que o mod carrega e que conseguimos registrar um passo
    /// de geracao de terreno. Este passo NAO altera nenhum bloco.
    /// </summary>
    public class BiomesExpansionLoader : ModLoader {
        // Passo 3 = depois dos passos 2 e antes do passo 4 (vegetacao).
        // Ordem 450: logo depois de GenerateSurface (400) e antes de PropagateFluidsDownwards (500).
        const int ProbeOrder = 450;

        static int s_chunkCount;

        public override void TerrainContentsGenerator24Initialize(
            ITerrainContentsGenerator terrainContentsGenerator,
            SubsystemTerrain subsystemTerrain) {
            if (terrainContentsGenerator is not TerrainContentsGenerator24 generator) {
                Log.Warning("[BiomesExpansion] Gerador de terreno inesperado: "
                    + terrainContentsGenerator?.GetType().Name);
                return;
            }
            generator.ChunkGenerationStep3.Add(
                new ChunkGenerationStep(ProbeOrder, ProbeStep, "BiomesExpansion.Probe")
            );
            // A lista so e ordenada pelo jogo depois de todos os hooks; nao precisamos ordenar.
            Log.Information("[BiomesExpansion] Passo de teste registrado (ordem " + ProbeOrder + ").");
        }

        static void ProbeStep(TerrainChunk chunk) {
            int n = Interlocked.Increment(ref s_chunkCount);
            if (n == 1 || n % 500 == 0) {
                Log.Information("[BiomesExpansion] Chunk #" + n
                    + " em (" + chunk.Coords.X + ", " + chunk.Coords.Y + ")");
            }
        }
    }
}
