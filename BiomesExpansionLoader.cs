using Engine;
using Game;

namespace BiomesExpansion {
    public class BiomesExpansionLoader : ModLoader {
        // Passo 4 (vegetacao). Ordem 310: logo depois de GenerateTrees (300).
        const int JungleTreesOrder = 310;

        // Obrigatorio: sem registrar o gancho aqui, o jogo nunca chama TerrainContentsGenerator24Initialize.
        public override void __ModInitialize() {
            ModsManager.RegisterHook("TerrainContentsGenerator24Initialize", this);
        }

        public override void TerrainContentsGenerator24Initialize(
            ITerrainContentsGenerator terrainContentsGenerator,
            SubsystemTerrain subsystemTerrain) {
            if (terrainContentsGenerator is not TerrainContentsGenerator24 generator) {
                Log.Warning("[BiomesExpansion] Gerador de terreno inesperado: "
                    + terrainContentsGenerator?.GetType().Name);
                return;
            }
            try {
                BiomeNoise noise = new(generator, generator.m_seed);
                JungleBiome jungle = new(noise, subsystemTerrain, generator.m_seed);
                generator.ChunkGenerationStep4.Add(
                    new ChunkGenerationStep(JungleTreesOrder, jungle.GenerateJungle, "BiomesExpansion.Jungle")
                );
                // A lista e ordenada pelo jogo depois de todos os hooks.
                Log.Information("[BiomesExpansion] Jungle v0.2 registrada.");
            }
            catch (Exception e) {
                Log.Error("[BiomesExpansion] Falha ao registrar a jungle: " + e);
            }
        }
    }
}
