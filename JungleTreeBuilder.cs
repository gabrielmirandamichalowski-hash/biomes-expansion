using Engine;
using Game;

namespace BiomesExpansion {
    /// <summary>
    /// Monta pinceis (TerrainBrush) de vegetacao de jungle: arvores gigantes de tronco 2x2,
    /// arvores medias com copa fechada e arbustos. Cipos (hera) sobem pelos troncos.
    /// Tudo e criado uma unica vez, na inicializacao, e depois so lido.
    /// </summary>
    public class JungleTreeBuilder {
        readonly TerrainBrush m_brush = new();
        readonly HashSet<int> m_used = [];
        readonly HashSet<int> m_walls = [];
        readonly List<Point3> m_trunkCells = [];
        readonly List<Point3> m_leafCells = [];
        readonly Game.Random m_random;
        readonly int m_trunk;
        readonly int m_leaves;

        JungleTreeBuilder(Game.Random random, int trunk, int leaves) {
            m_random = random;
            m_trunk = trunk;
            m_leaves = leaves;
        }

        void SetCell(int x, int y, int z, int value) {
            m_brush.AddCell(x, y, z, value);
            m_used.Add(TerrainBrush.Key(x, y, z));
        }

        bool IsFree(int x, int y, int z) => !m_used.Contains(TerrainBrush.Key(x, y, z));

        void AddTrunk(int x, int y, int z) {
            SetCell(x, y, z, m_trunk);
            m_walls.Add(TerrainBrush.Key(x, y, z));
            m_trunkCells.Add(new Point3(x, y, z));
        }

        void AddLeaf(int x, int y, int z) {
            if (!IsFree(x, y, z)) {
                return;
            }
            SetCell(x, y, z, m_leaves);
            m_walls.Add(TerrainBrush.Key(x, y, z));
            m_leafCells.Add(new Point3(x, y, z));
        }

        /// <summary>Elipsoide de folhas com a borda irregular.</summary>
        void AddBlob(int cx, int cy, int cz, float rx, float ry, float rz) {
            int ix = (int)MathF.Ceiling(rx);
            int iy = (int)MathF.Ceiling(ry);
            int iz = (int)MathF.Ceiling(rz);
            for (int dx = -ix; dx <= ix; dx++) {
                for (int dy = -iy; dy <= iy; dy++) {
                    for (int dz = -iz; dz <= iz; dz++) {
                        float d = dx * dx / (rx * rx) + dy * dy / (ry * ry) + dz * dz / (rz * rz);
                        if (d > 1f) {
                            continue;
                        }
                        if (d > 0.72f && m_random.Bool(0.22f)) {
                            continue;
                        }
                        AddLeaf(cx + dx, cy + dy, cz + dz);
                    }
                }
            }
        }

        /// <summary>Galho de tronco (1 bloco de espessura) entre dois pontos.</summary>
        void AddBranch(float x0, float y0, float z0, float x1, float y1, float z1) {
            float length = MathF.Max(MathF.Abs(x1 - x0), MathF.Max(MathF.Abs(y1 - y0), MathF.Abs(z1 - z0)));
            int steps = Math.Max(1, (int)MathF.Ceiling(length) * 2);
            for (int i = 0; i <= steps; i++) {
                float t = i / (float)steps;
                AddTrunk(
                    (int)MathF.Round(x0 + (x1 - x0) * t),
                    (int)MathF.Round(y0 + (y1 - y0) * t),
                    (int)MathF.Round(z0 + (z1 - z0) * t)
                );
            }
        }

        /// <summary>
        /// Cipos de hera encostados nos troncos (e um pouco nas folhas), do mesmo jeito que o
        /// jogo original cria hera: cada celula so recebe hera se o bloco-parede existir na mesma altura.
        /// </summary>
        void AddVines(float trunkChance, float leafChance, int maxLength) {
            foreach (Point3 wall in m_trunkCells.ToArray()) {
                if (m_random.Bool(trunkChance)) {
                    AddVine(wall, maxLength);
                }
            }
            foreach (Point3 wall in m_leafCells.ToArray()) {
                if (m_random.Bool(leafChance)) {
                    AddVine(wall, maxLength);
                }
            }
        }

        void AddVine(Point3 wall, int maxLength) {
            int face = m_random.Int(0, 3);
            Point3 dir = CellFace.FaceToPoint3(face);
            int value = Terrain.MakeBlockValue(197, 0, IvyBlock.SetFace(0, CellFace.OppositeFace(face)));
            for (int k = 0; k < maxLength; k++) {
                int wy = wall.Y - k;
                int ix = wall.X + dir.X;
                int iz = wall.Z + dir.Z;
                if (!m_walls.Contains(TerrainBrush.Key(wall.X, wy, wall.Z)) || !IsFree(ix, wy, iz)) {
                    break;
                }
                SetCell(ix, wy, iz, value);
                if (k > 0 && m_random.Bool(0.12f)) {
                    break;
                }
            }
        }

        TerrainBrush Finish() {
            m_brush.Compile();
            return m_brush;
        }

        /// <summary>Arvore gigante: tronco 2x2, raizes, copa larga e copas em galhos laterais.</summary>
        public static TerrainBrush CreateGiantTree(Game.Random random, int trunk, int leaves) {
            JungleTreeBuilder b = new(random, trunk, leaves);
            int h = random.Int(26, 40);
            b.AddBlob(1, h - 1, 1, random.Float(6.5f, 8f), random.Float(3f, 4f), random.Float(6.5f, 8f));
            b.AddBlob(1, h + 2, 1, 4f, 2.2f, 4f);
            int branches = random.Int(4, 6);
            for (int i = 0; i < branches; i++) {
                float angle = random.Float(0f, 6.2832f);
                float length = random.Float(5f, 8f);
                float y0 = h * random.Float(0.5f, 0.82f);
                int bx = 1 + (int)MathF.Round(length * MathF.Cos(angle));
                int bz = 1 + (int)MathF.Round(length * MathF.Sin(angle));
                int by = (int)y0 + random.Int(1, 4);
                b.AddBlob(bx, by, bz, random.Float(3.4f, 4.6f), random.Float(1.8f, 2.6f), random.Float(3.4f, 4.6f));
                b.AddBranch(1, y0, 1, bx, by, bz);
            }
            // Tronco 2x2, comecando abaixo do chao para nao ficar flutuando em encostas.
            for (int y = -2; y <= h; y++) {
                b.AddTrunk(0, y, 0);
                b.AddTrunk(0, y, 1);
                b.AddTrunk(1, y, 0);
                b.AddTrunk(1, y, 1);
            }
            // Raizes nos quatro lados.
            int r1 = random.Int(0, 1);
            int r2 = random.Int(0, 1);
            int r3 = random.Int(0, 1);
            int r4 = random.Int(0, 1);
            b.AddTrunk(-1, 0, r1);
            b.AddTrunk(2, 0, r2);
            b.AddTrunk(r3, 0, -1);
            b.AddTrunk(r4, 0, 2);
            if (random.Bool(0.6f)) {
                b.AddTrunk(-1, 1, r1);
            }
            if (random.Bool(0.6f)) {
                b.AddTrunk(2, 1, r2);
            }
            b.AddVines(0.30f, 0.02f, 14);
            return b.Finish();
        }

        /// <summary>Arvore media: tronco fino, copa fechada (sem galhos expostos) e algumas copas laterais.</summary>
        public static TerrainBrush CreateMidTree(Game.Random random, int trunk, int leaves) {
            JungleTreeBuilder b = new(random, trunk, leaves);
            int h = random.Int(13, 22);
            b.AddBlob(0, h, 0, random.Float(3.4f, 4.6f), random.Float(2.2f, 3f), random.Float(3.4f, 4.6f));
            int sides = random.Int(2, 3);
            for (int i = 0; i < sides; i++) {
                float angle = random.Float(0f, 6.2832f);
                float length = random.Float(3f, 4.5f);
                float y0 = h * random.Float(0.55f, 0.8f);
                int bx = (int)MathF.Round(length * MathF.Cos(angle));
                int bz = (int)MathF.Round(length * MathF.Sin(angle));
                int by = (int)y0 + random.Int(1, 2);
                b.AddBlob(bx, by, bz, random.Float(2.2f, 2.8f), random.Float(1.5f, 2f), random.Float(2.2f, 2.8f));
                b.AddBranch(0, y0, 0, bx, by, bz);
            }
            for (int y = -2; y <= h - 1; y++) {
                b.AddTrunk(0, y, 0);
            }
            b.AddVines(0.18f, 0.01f, 10);
            return b.Finish();
        }

        /// <summary>Arvore pequena de jungle: tronco curto e copa compacta.</summary>
        public static TerrainBrush CreateSmallTree(Game.Random random, int trunk, int leaves) {
            JungleTreeBuilder b = new(random, trunk, leaves);
            int h = random.Int(5, 9);
            b.AddBlob(0, h, 0, random.Float(2.3f, 3.1f), random.Float(1.5f, 2.1f), random.Float(2.3f, 3.1f));
            if (random.Bool(0.5f)) {
                b.AddBlob(random.Int(-2, 2), h - 2, random.Int(-2, 2), 1.8f, 1.3f, 1.8f);
            }
            for (int y = -2; y <= h - 1; y++) {
                b.AddTrunk(0, y, 0);
            }
            b.AddVines(0.12f, 0.02f, 6);
            return b.Finish();
        }

        /// <summary>Arbusto: um toco de tronco coberto por uma bola de folhas.</summary>
        public static TerrainBrush CreateBush(Game.Random random, int trunk, int leaves) {
            JungleTreeBuilder b = new(random, trunk, leaves);
            b.AddBlob(0, 1, 0, random.Float(1.6f, 2.3f), random.Float(1.2f, 1.8f), random.Float(1.6f, 2.3f));
            b.AddTrunk(0, -1, 0);
            b.AddTrunk(0, 0, 0);
            return b.Finish();
        }
    }
}
