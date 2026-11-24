using Engine;
using Game;

namespace BiomesExpansion {
    public class BiomesExpansionLoader : ModLoader {
        // Passo 4 (vegetacao). Ordem 310: logo depois de GenerateTrees (300).
        const int JungleOrder = 310;
        // Clima da jungle (cor da grama): depois da vegetacao, antes de hera (1000) e neve (1300).
        const int JungleClimateOrder = 320;

        // Obrigatorio: sem registrar o gancho aqui, o jogo nunca chama TerrainContentsGenerator24Initialize.
        public override void __ModInitialize() {
            ModsManager.RegisterHook("TerrainContentsGenerator24Initialize", this);
            ModsManager.RegisterHook("OnLoadingFinished", this);
        }

        // Carrega a textura dos blocos da jungle na thread principal, no fim do carregamento.
        public override void OnLoadingFinished(List<Action> actions) {
            actions.Add(JungleTextures.Load);
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
                JungleBiome jungle = new(generator, noise, subsystemTerrain, generator.m_seed);

                // Troca o passo original de arvores por uma versao que respeita a jungle.
                ChunkGenerationStep vanillaTrees = generator.ChunkGenerationStep4.Find(step => step.Name == "GenerateTrees");
                if (vanillaTrees != null) {
                    vanillaTrees.GenerateAction = jungle.GenerateVanillaTrees;
                }
                else {
                    Log.Warning("[BiomesExpansion] Passo GenerateTrees nao encontrado; arvores originais ficam na jungle.");
                }

                generator.ChunkGenerationStep4.Add(
                    new ChunkGenerationStep(JungleOrder, jungle.GenerateJungle, "BiomesExpansion.Jungle")
                );
                generator.ChunkGenerationStep4.Add(
                    new ChunkGenerationStep(JungleClimateOrder, jungle.GenerateClimate, "BiomesExpansion.JungleClimate")
                );
                // A lista e ordenada pelo jogo depois de todos os hooks.
                Log.Information("[BiomesExpansion] Jungle v0.4 registrada.");
            }
            catch (Exception e) {
                Log.Error("[BiomesExpansion] Falha ao registrar a jungle: " + e);
            }
        }
    }
}
