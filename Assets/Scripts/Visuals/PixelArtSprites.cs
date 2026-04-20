using UnityEngine;

/// <summary>
/// Generates pixel-art Texture2D sprites for all game entities.
/// All sprites are top-down views rendered on flat Quads (y-up).
/// FilterMode.Point ensures crisp nearest-neighbor scaling.
/// </summary>
public static class PixelArtSprites
{
    // ── Palette (index → Color32) ────────────────────────────────────────────
    static readonly Color32[] Pal = {
        new Color32(  0,   0,   0,   0), //  0  transparent
        new Color32(106, 176,  76, 255), //  1  grass
        new Color32( 74, 128,  56, 255), //  2  dark grass
        new Color32(140, 140, 158, 255), //  3  stone
        new Color32( 72,  72,  92, 255), //  4  dark stone / shadow edge
        new Color32(196, 196, 218, 255), //  5  light stone / highlight
        new Color32(200, 168, 128, 255), //  6  wood floor / dirt
        new Color32(139,  94,  60, 255), //  7  wood brown
        new Color32( 88,  56,  24, 255), //  8  dark wood
        new Color32(255, 215,   0, 255), //  9  gold
        new Color32(184, 134,  11, 255), // 10  dark gold
        new Color32(204,  68,  68, 255), // 11  red / clay
        new Color32(130,  22,  22, 255), // 12  dark red
        new Color32( 64, 128, 220, 255), // 13  blue
        new Color32(140, 184, 255, 255), // 14  light blue
        new Color32( 34, 142,  34, 255), // 15  forest green
        new Color32( 18,  82,  18, 255), // 16  dark green
        new Color32(145,  72,  20, 255), // 17  trunk brown
        new Color32(232, 232, 232, 255), // 18  near-white
        new Color32( 18,  18,  28, 255), // 19  near-black (outline)
        new Color32(220,  28,  28, 255), // 20  bright red (flag)
        new Color32( 28,  60, 200, 255), // 21  bright blue (flag)
        new Color32(214, 182,  58, 255), // 22  straw / wheat
        new Color32( 96,  60,  22, 255), // 23  very dark wood
        new Color32(104, 104, 120, 255), // 24  iron / armor
        new Color32(255, 228,  56, 255), // 25  bright yellow
        new Color32(220,  96,  28, 255), // 26  orange / fire
        new Color32(232, 214, 164, 255), // 27  monk robe / sand
        new Color32( 48,  48,  68, 255), // 28  deep shadow
        new Color32(158, 158,  96, 255), // 29  moss / old stone
        new Color32(180, 130,  78, 255), // 30  sandstone
        new Color32( 86, 164,  86, 255), // 31  medium green
    };

    static Color32 C(int i) => (uint)i < (uint)Pal.Length ? Pal[i] : Pal[0];

    // ── PixelCanvas — tiny drawing helper ────────────────────────────────────
    class PixelCanvas
    {
        readonly int[] d;
        public readonly int W, H;

        public PixelCanvas(int w, int h) { W = w; H = h; d = new int[w * h]; }

        public PixelCanvas Fill(int c)
        {
            for (int i = 0; i < d.Length; i++) d[i] = c;
            return this;
        }

        public PixelCanvas Set(int x, int y, int c)
        {
            if ((uint)x < (uint)W && (uint)y < (uint)H) d[y * W + x] = c;
            return this;
        }

        public PixelCanvas Rect(int x, int y, int rw, int rh, int c)
        {
            for (int row = y; row < y + rh; row++)
                for (int col = x; col < x + rw; col++)
                    Set(col, row, c);
            return this;
        }

        // Filled rect with border
        public PixelCanvas Frame(int x, int y, int rw, int rh, int borderC, int fillC, int thick = 1)
        {
            Rect(x, y, rw, rh, fillC);
            for (int t = 0; t < thick; t++)
            {
                HLine(x + t, x + rw - 1 - t, y + t, borderC);
                HLine(x + t, x + rw - 1 - t, y + rh - 1 - t, borderC);
                VLine(y + t, y + rh - 1 - t, x + t, borderC);
                VLine(y + t, y + rh - 1 - t, x + rw - 1 - t, borderC);
            }
            return this;
        }

        public PixelCanvas HLine(int x0, int x1, int y, int c)
        {
            for (int x = x0; x <= x1; x++) Set(x, y, c);
            return this;
        }

        public PixelCanvas VLine(int y0, int y1, int x, int c)
        {
            for (int y = y0; y <= y1; y++) Set(x, y, c);
            return this;
        }

        public PixelCanvas Circle(int cx, int cy, int r, int c)
        {
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r * r)
                        Set(cx + dx, cy + dy, c);
            return this;
        }

        public PixelCanvas Checker(int x, int y, int rw, int rh, int c1, int c2)
        {
            for (int row = y; row < y + rh; row++)
                for (int col = x; col < x + rw; col++)
                    Set(col, row, ((row + col) & 1) == 0 ? c1 : c2);
            return this;
        }

        public Texture2D Build()
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode   = TextureWrapMode.Clamp;
            var pixels = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    // Unity Texture2D: y=0 is bottom; our canvas: y=0 is top → flip
                    pixels[y * W + x] = C(d[(H - 1 - y) * W + x]);
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public static Texture2D BuildingSprite(BuildingType t, bool isAI = false)
    {
        var tex = MakeBuildingTex(t);
        if (isAI) TintAI(tex);
        return tex;
    }

    public static Texture2D UnitSprite(UnitType t, bool isAI = false)
    {
        var tex = MakeUnitTex(t);
        if (isAI) TintAI(tex);
        return tex;
    }

    public static Texture2D VillagerSprite(bool isAI = false)
    {
        var tex = MakeVillagerTex();
        if (isAI) TintAI(tex);
        return tex;
    }

    public static Texture2D ResourceSprite(ResourceNode.NodeType t) => t switch
    {
        ResourceNode.NodeType.Wood  => ResWood(),
        ResourceNode.NodeType.Stone => ResStone(),
        ResourceNode.NodeType.Gold  => ResGold(),
        _                           => ResGeneric(),
    };

    public static Texture2D GrassTile()  => MakeGrassTile();
    public static Texture2D WaterTile()  => MakeWaterTile();
    public static Texture2D TreeTex()    => MakeTreeTex();
    public static Texture2D RockTex()    => MakeRockTex();
    public static Texture2D RelicTex()   => MakeRelicTex();

    // ── Buildings (32×32 top-down) ───────────────────────────────────────────

    static Texture2D MakeBuildingTex(BuildingType t) => t switch
    {
        BuildingType.TownCenter    => TownCenter(),
        BuildingType.House         => House(),
        BuildingType.Farm          => Farm(),
        BuildingType.LumberMill    => LumberMill(),
        BuildingType.LumberCamp    => LumberCamp(),
        BuildingType.Quarry        => Quarry(),
        BuildingType.MiningCamp    => MiningCamp(),
        BuildingType.Market        => Market(),
        BuildingType.Barracks      => Barracks(),
        BuildingType.ArcheryRange  => ArcheryRange(),
        BuildingType.Blacksmith    => Blacksmith(),
        BuildingType.University    => University(),
        BuildingType.Monastery     => Monastery(),
        BuildingType.SiegeWorkshop => SiegeWorkshop(),
        BuildingType.Castle        => Castle(),
        BuildingType.Stable        => Stable(),
        BuildingType.Tower         => Tower(),
        BuildingType.Wall          => Wall(),
        BuildingType.Temple        => Temple(),
        BuildingType.Mill          => Mill(),
        _                          => GenericBuilding(),
    };

    static Texture2D TownCenter()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 3, 2);     // outer walls
        c.Rect(6, 6, 20, 20, 6);             // courtyard floor
        c.Circle(3,  3,  3, 5);              // corner towers
        c.Circle(28, 3,  3, 5);
        c.Circle(3,  28, 3, 5);
        c.Circle(28, 28, 3, 5);
        c.Frame(10, 10, 12, 12, 4, 3, 1);   // keep walls
        c.Frame(12, 12, 8,  8,  4, 27, 1);  // keep interior
        c.VLine(12, 9, 15, 19);              // flagpole
        c.Rect(16, 9, 4, 3, 20);            // flag
        // Battlements
        for (int i = 3; i < 29; i += 4) { c.Set(i, 0, 5); c.Set(i, 31, 5); }
        for (int i = 3; i < 29; i += 4) { c.Set(0, i, 5); c.Set(31, i, 5); }
        return c.Build();
    }

    static Texture2D House()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 8, 7, 2);     // walls
        c.Rect(4, 4, 24, 24, 11);            // roof (clay red)
        c.HLine(4, 27, 15, 12);              // ridge line
        c.HLine(4, 27, 16, 12);
        c.Frame(6,  7,  6, 6, 19, 14, 1);   // window left
        c.Frame(20, 7,  6, 6, 19, 14, 1);   // window right
        c.Frame(13, 22, 6, 6, 19, 8,  1);   // door
        c.Set(15,24,6); c.Set(16,24,6);      // door handle
        return c.Build();
    }

    static Texture2D Farm()
    {
        var c = new PixelCanvas(32, 32);
        c.Fill(6);
        c.Checker(0, 0, 32, 32, 6, 23);
        for (int row = 1; row < 32; row += 4) {
            c.HLine(0, 31, row,   15);
            c.HLine(0, 31, row+1, 31);
        }
        c.Frame(0, 0, 32, 32, 23, 0, 1);
        c.Frame(13, 2, 6, 5, 8, 7, 1);      // small barn
        return c.Build();
    }

    static Texture2D LumberMill()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 8, 7, 2);
        c.Rect(4, 4, 24, 24, 6);
        c.Circle(16, 16, 9, 18);  // saw outer ring
        c.Circle(16, 16, 7, 24);  // saw blade
        c.Circle(16, 16, 4, 7);   // hub
        c.Circle(16, 16, 2, 8);   // hub center
        // teeth
        for (int i = 0; i < 8; i++) {
            float a = i * Mathf.PI / 4f;
            c.Set(16 + (int)(10f * Mathf.Cos(a)), 16 + (int)(10f * Mathf.Sin(a)), 18);
        }
        return c.Build();
    }

    static Texture2D LumberCamp()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 8, 7, 2);
        c.Rect(4, 4, 24, 24, 6);
        // Log pile
        for (int i = 0; i < 5; i++)
            c.Rect(5, 7 + i * 4, 22, 3, i % 2 == 0 ? 17 : 8);
        // Log end circles
        for (int i = 0; i < 5; i++) {
            c.Circle(5,  8 + i*4, 1, 23);
            c.Circle(26, 8 + i*4, 1, 23);
        }
        return c.Build();
    }

    static Texture2D Quarry()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 24, 2);
        c.Rect(4, 4, 24, 24, 3);
        c.Frame(5,  5,  10, 10, 4, 5, 1);
        c.Frame(17, 5,  10, 10, 4, 4, 1);
        c.Frame(5,  17, 10, 10, 4, 4, 1);
        c.Frame(17, 17, 10, 10, 4, 29, 1);
        c.Circle(16, 16, 2, 19);  // pick mark center
        return c.Build();
    }

    static Texture2D MiningCamp()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 8, 7, 2);
        c.Rect(4, 4, 24, 24, 3);
        c.Circle(10, 12, 5, 9);   // gold ore
        c.Circle(22, 12, 5, 3);   // stone
        c.Circle(16, 22, 5, 10);  // dark gold
        // Pickaxe
        c.HLine(10, 22, 16, 8); c.VLine(14, 22, 16, 8);
        c.Rect(14, 12, 4, 3, 24); // pick head
        return c.Build();
    }

    static Texture2D Market()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 10, 9, 2);
        c.Rect(4, 4, 24, 24, 22);
        // Awning stripes
        for (int x = 4; x < 28; x += 4)
            c.VLine(4, 14, x, (x / 4) % 2 == 0 ? 11 : 20);
        // Three coin stacks
        c.Circle(9,  22, 3, 9);  c.Set(9,  22, 10);
        c.Circle(16, 22, 3, 9);  c.Set(16, 22, 10);
        c.Circle(23, 22, 3, 9);  c.Set(23, 22, 10);
        c.Frame(14, 14, 4, 4, 10, 25, 1);  // scale
        return c.Build();
    }

    static Texture2D Barracks()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 3, 2);
        c.Rect(4, 4, 24, 24, 24);
        // Weapon rack horizontal bar
        c.HLine(5, 27, 13, 19);
        c.HLine(5, 27, 14, 18);
        // Swords on rack
        for (int x = 7; x < 28; x += 5)
            c.VLine(8, 18, x, x % 10 < 5 ? 18 : 24);
        // Door
        c.Frame(13, 24, 6, 4, 19, 28, 1);
        c.Set(15, 26, 6);
        return c.Build();
    }

    static Texture2D ArcheryRange()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 2, 2);
        c.Rect(4, 4, 24, 24, 31);
        // Target
        c.Circle(20, 12, 7, 11); // red outer
        c.Circle(20, 12, 5, 18); // white
        c.Circle(20, 12, 3, 20); // red inner
        c.Circle(20, 12, 1, 18); // bullseye
        // Arrow pointing right at target
        c.HLine(6, 14, 12, 19);
        c.Set(15, 11, 19); c.Set(16, 12, 19); c.Set(15, 13, 19); // arrowhead
        c.Frame(13, 24, 6, 4, 19, 28, 1);
        return c.Build();
    }

    static Texture2D Blacksmith()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 28, 2);
        c.Rect(4, 4, 24, 24, 24);
        // Anvil (top view)
        c.Rect(7,  13, 18, 5, 19);   // top face
        c.Rect(10, 18, 12, 4, 28);   // body
        c.Rect(8,  22, 16, 2, 19);   // base
        // Forge glow
        c.Circle(16, 7, 3, 26); c.Circle(16, 7, 2, 25); c.Set(16, 7, 18);
        // Hammer
        c.HLine(18, 26, 16, 7); c.Rect(22, 13, 4, 5, 24);
        return c.Build();
    }

    static Texture2D University()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 3, 2);
        c.Rect(4, 4, 24, 24, 13);
        // Three arched windows
        for (int x = 5; x <= 22; x += 9) {
            c.Frame(x, 7, 7, 11, 19, 14, 1);
            c.Circle(x + 3, 7, 3, 13);
        }
        // Open book (rectangle split)
        c.Rect(7, 21, 18, 7, 6);
        c.VLine(21, 27, 16, 8);
        c.HLine(7, 25, 21, 8);
        return c.Build();
    }

    static Texture2D Monastery()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 3, 2);
        c.Rect(4, 4, 24, 24, 27);
        // Cross
        c.VLine(4, 28, 16, 4);
        c.HLine(9, 23, 11, 4);
        // Arch doorway
        c.Frame(12, 22, 8, 6, 19, 8, 1);
        c.Circle(16, 22, 4, 27);
        // Candle glow dots
        c.Set(9, 20, 25); c.Set(23, 20, 25);
        return c.Build();
    }

    static Texture2D SiegeWorkshop()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 8, 7, 2);
        c.Rect(4, 4, 24, 24, 6);
        // Catapult silhouette (top-down)
        c.Frame(7, 18, 18, 7, 8, 23, 1);  // base frame
        c.VLine(8, 18, 16, 7);            // arm
        c.Circle(16, 8,  3, 3);           // sling cup
        c.Circle(9,  24, 3, 8); c.Circle(9,  24, 1, 23); // wheel L
        c.Circle(23, 24, 3, 8); c.Circle(23, 24, 1, 23); // wheel R
        return c.Build();
    }

    static Texture2D Castle()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 3, 2);
        // Curtain wall interior
        c.Rect(3, 3, 26, 26, 3);
        // 4 corner towers
        c.Frame(0, 0,  10, 10, 4, 5, 1); c.Circle(5,  5,  4, 3);
        c.Frame(22, 0, 10, 10, 4, 5, 1); c.Circle(27, 5,  4, 3);
        c.Frame(0, 22, 10, 10, 4, 5, 1); c.Circle(5,  27, 4, 3);
        c.Frame(22,22, 10, 10, 4, 5, 1); c.Circle(27, 27, 4, 3);
        // Courtyard + keep
        c.Frame(10, 10, 12, 12, 4, 6, 1);
        c.Frame(12, 12, 8,  8,  4, 5, 1);
        // Battlements along top
        for (int i = 3; i < 29; i += 4) c.Set(i, 1, 5);
        // Flag
        c.VLine(8, 5, 16, 19);
        c.Rect(17, 5, 4, 3, 20);
        return c.Build();
    }

    static Texture2D Stable()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 8, 7, 2);
        c.Rect(4, 4, 24, 24, 6);
        // Stall dividers
        c.HLine(4, 28, 12, 8); c.HLine(4, 28, 20, 8);
        // Horse silhouettes (head circles + body rects)
        c.Circle(12, 7,  2, 19); c.Rect(10, 9, 5, 2, 23);
        c.Circle(12, 16, 2, 19); c.Rect(10,18, 5, 2, 23);
        c.Circle(12, 24, 2, 19); c.Rect(10,26, 5, 2, 23);
        // Hay bales
        c.Frame(20, 5,  7, 5, 10, 22, 1);
        c.Frame(20, 14, 7, 5, 10, 22, 1);
        c.Frame(20, 22, 7, 5, 10, 22, 1);
        return c.Build();
    }

    static Texture2D Tower()
    {
        var c = new PixelCanvas(32, 32);
        c.Circle(16, 16, 15, 4);   // outer shadow ring
        c.Circle(16, 16, 13, 3);   // wall
        c.Circle(16, 16, 10, 5);   // walkway
        c.Circle(16, 16, 7,  3);   // inner wall
        c.Circle(16, 16, 4,  28);  // dark center
        // Battlements
        for (int i = 0; i < 12; i++) {
            float a = i * Mathf.PI * 2f / 12f;
            c.Set(16 + (int)(14f * Mathf.Cos(a)), 16 + (int)(14f * Mathf.Sin(a)), 5);
        }
        // Arrow slit
        c.VLine(10, 22, 16, 4); c.HLine(13, 19, 16, 4);
        return c.Build();
    }

    static Texture2D Wall()
    {
        var c = new PixelCanvas(32, 32);
        c.Fill(4);
        // Stone block pattern (offset rows)
        for (int row = 0; row < 4; row++) {
            int off = (row % 2) * 8;
            for (int col = 0; col < 5; col++) {
                int x = col * 8 - off;
                c.Frame(x, row * 8, 8, 8, 4, row % 2 == 0 ? 3 : 5, 1);
            }
        }
        return c.Build();
    }

    static Texture2D Temple()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 10, 9, 2);
        c.Rect(4, 4, 24, 24, 25);
        // Pillars
        for (int x = 4; x <= 26; x += 7) c.Rect(x, 4, 3, 24, 18);
        // Gold dome
        c.Circle(16, 14, 8, 9);
        c.Circle(16, 14, 5, 25);
        c.Circle(16, 14, 2, 10);
        c.Set(16, 14, 18);
        return c.Build();
    }

    static Texture2D Mill()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 10, 22, 2);
        c.Rect(4, 4, 24, 24, 6);
        // Windmill cross sails
        c.Rect(13, 4, 6, 24, 17);  // vertical sail
        c.Rect(4, 13, 24, 6, 17);  // horizontal sail
        // Sail blades (lighter tips)
        c.Rect(13, 4, 6, 6,  18);
        c.Rect(13, 22, 6, 6, 18);
        c.Rect(4,  13, 6, 6, 18);
        c.Rect(22, 13, 6, 6, 18);
        // Center hub
        c.Circle(16, 16, 3, 8);
        c.Circle(16, 16, 1, 19);
        return c.Build();
    }

    static Texture2D GenericBuilding()
    {
        var c = new PixelCanvas(32, 32);
        c.Frame(0, 0, 32, 32, 4, 3, 2);
        c.Frame(5, 5, 22, 22, 4, 5, 1);
        return c.Build();
    }

    // ── Units (16×16 top-down) ───────────────────────────────────────────────

    static Texture2D MakeUnitTex(UnitType t) => t switch
    {
        UnitType.Militia        => UnitMilitia(),
        UnitType.Spearman       => UnitSpearman(),
        UnitType.Archer         => UnitArcher(),
        UnitType.Skirmisher     => UnitSkirmisher(),
        UnitType.Scout          => UnitScout(),
        UnitType.Knight         => UnitKnight(),
        UnitType.Monk           => UnitMonk(),
        UnitType.BatteringRam   => UnitBatteringRam(),
        UnitType.Mangonel       => UnitMangonel(),
        UnitType.Trebuchet      => UnitTrebuchet(),
        UnitType.RoyalGuardsman => UnitRoyalGuard(),
        UnitType.ForestWarden   => UnitForestWarden(),
        UnitType.Ironbreaker    => UnitIronbreaker(),
        UnitType.Warchief       => UnitWarchief(),
        _                       => UnitGenericTex(new Color32(180, 180, 200, 255)),
    };

    // Helper: body circle + helmet circle + optional weapon arm
    static Texture2D MakeHumanUnit(int body, int helmet, int weapon, int weaponStyle)
    {
        var c = new PixelCanvas(16, 16);
        c.Circle(8, 9, 5, 4);          // shadow
        c.Circle(8, 8, 5, body);        // body
        // Outline
        for (int i = 0; i < 16; i++) {
            float a = i * Mathf.PI * 2f / 16f;
            c.Set(8 + (int)(5.6f * Mathf.Cos(a)), 8 + (int)(5.6f * Mathf.Sin(a)), 19);
        }
        c.Circle(8, 8, 3, helmet);      // head/helmet
        // Weapon
        switch (weaponStyle) {
            case 1: c.HLine(10, 14, 5, weapon); c.Set(14, 4, weapon); break; // sword
            case 2: c.VLine(2, 6, 8, weapon); c.HLine(6, 10, 4, weapon); break;   // bow
            case 3: c.HLine(2, 6, 7, weapon); c.VLine(4, 13, 2, weapon); break;   // spear left
            case 4: c.HLine(10, 14, 7, weapon); c.Set(14, 6, weapon); c.Set(14, 8, weapon); break; // lance
            case 5: c.VLine(3, 7, 9, weapon); c.Circle(9, 3, 1, 27); break;       // staff
        }
        return c.Build();
    }

    static Texture2D UnitMilitia()     => MakeHumanUnit(24, 4,  18, 1);
    static Texture2D UnitSpearman()    => MakeHumanUnit(13, 21, 18, 3);
    static Texture2D UnitArcher()      => MakeHumanUnit(15, 16, 22, 2);
    static Texture2D UnitSkirmisher()  => MakeHumanUnit(31, 16, 18, 2);
    static Texture2D UnitScout()       => MakeHumanUnit(14, 13, 18, 4);
    static Texture2D UnitKnight()      => MakeHumanUnit(5,  18, 9,  4);
    static Texture2D UnitMonk()        => MakeHumanUnit(27, 17, 4,  5);
    static Texture2D UnitRoyalGuard()  => MakeHumanUnit(18, 9,  9,  1);
    static Texture2D UnitForestWarden()=> MakeHumanUnit(16, 15, 15, 2);
    static Texture2D UnitIronbreaker() => MakeHumanUnit(28, 24, 24, 1);
    static Texture2D UnitWarchief()    => MakeHumanUnit(15, 11, 20, 1);

    static Texture2D UnitBatteringRam()
    {
        var c = new PixelCanvas(16, 16);
        c.Frame(2, 5, 12, 7, 8, 7, 1);      // body
        c.Rect(4, 8, 8, 2, 23);              // ram beam dark
        c.Circle(3,  12, 2, 23); c.Circle(12, 12, 2, 23); // wheels
        c.Circle(3,  12, 1, 8);  c.Circle(12, 12, 1, 8);
        return c.Build();
    }

    static Texture2D UnitMangonel()
    {
        var c = new PixelCanvas(16, 16);
        c.Frame(3, 9, 10, 5, 8, 7, 1);      // base
        c.VLine(3, 9, 8, 7);                 // arm
        c.Circle(8, 3, 2, 3);                // sling cup
        c.Circle(3,  13, 2, 23); c.Circle(12, 13, 2, 23);
        c.Circle(3,  13, 1, 8);  c.Circle(12, 13, 1, 8);
        return c.Build();
    }

    static Texture2D UnitTrebuchet()
    {
        var c = new PixelCanvas(16, 16);
        c.Frame(2, 9, 12, 5, 8, 7, 1);
        c.VLine(2, 10, 8, 7);                // tall arm
        c.Circle(8, 2, 2, 4);                // counterweight
        c.Circle(8, 10, 1, 3);               // pivot
        c.Circle(2,  13, 2, 23); c.Circle(13, 13, 2, 23);
        return c.Build();
    }

    static Texture2D UnitGenericTex(Color32 col)
    {
        var c = new PixelCanvas(16, 16);
        c.Circle(8, 9, 5, 4);
        c.Circle(8, 8, 5, 3);
        return c.Build();
    }

    // ── Villager ─────────────────────────────────────────────────────────────

    static Texture2D MakeVillagerTex()
    {
        var c = new PixelCanvas(16, 16);
        c.Circle(8, 9, 5, 4);              // shadow
        c.Circle(8, 8, 5, 6);              // body (earthy)
        for (int i = 0; i < 16; i++) {
            float a = i * Mathf.PI * 2f / 16f;
            c.Set(8 + (int)(5.6f * Mathf.Cos(a)), 8 + (int)(5.6f * Mathf.Sin(a)), 19);
        }
        c.Circle(8, 8, 3, 30);             // head
        c.HLine(10, 14, 8, 22);            // tool handle
        c.Set(14, 7, 24); c.Set(14, 9, 24); // tool head
        return c.Build();
    }

    // ── Resources ────────────────────────────────────────────────────────────

    static Texture2D ResWood()
    {
        var c = new PixelCanvas(16, 16);
        c.Circle(8, 9, 6, 16);
        c.Circle(8, 8, 6, 15);
        c.Circle(7, 7, 3, 31);
        c.Set(8, 8, 16);
        return c.Build();
    }

    static Texture2D ResStone()
    {
        var c = new PixelCanvas(16, 16);
        c.Circle(8, 9, 6, 4);
        c.Circle(8, 8, 6, 3);
        c.Circle(7, 7, 3, 5);
        c.Set(10, 10, 4);
        return c.Build();
    }

    static Texture2D ResGold()
    {
        var c = new PixelCanvas(16, 16);
        c.Circle(8, 9, 6, 10);
        c.Circle(8, 8, 6, 9);
        c.Circle(7, 7, 3, 25);
        c.Set(10, 10, 10);
        return c.Build();
    }

    static Texture2D ResGeneric()
    {
        var c = new PixelCanvas(16, 16);
        c.Circle(8, 8, 6, 3);
        return c.Build();
    }

    // ── Terrain ──────────────────────────────────────────────────────────────

    static Texture2D MakeGrassTile()
    {
        var c = new PixelCanvas(16, 16).Fill(1);
        // Scattered dark dots for texture
        int[] xs = { 2, 7, 12, 4, 9, 14, 1, 6, 11, 3, 8, 13, 0, 5, 10, 15 };
        int[] ys = { 1, 3, 1, 6, 5, 7, 10, 9, 11, 14, 13, 15, 4, 12, 2,  8 };
        for (int i = 0; i < xs.Length; i++) c.Set(xs[i], ys[i], 2);
        return c.Build();
    }

    static Texture2D MakeWaterTile()
    {
        var c = new PixelCanvas(16, 16).Fill(13);
        c.HLine(1, 3, 2, 14); c.HLine(6, 8, 5, 14);
        c.HLine(11, 13, 8, 14); c.HLine(2, 4, 11, 14);
        c.HLine(8, 10, 14, 14);
        return c.Build();
    }

    // ── Decoration ───────────────────────────────────────────────────────────

    static Texture2D MakeTreeTex()
    {
        var c = new PixelCanvas(16, 16);
        c.Rect(7, 10, 2, 6, 17);    // trunk
        c.Circle(8, 7, 6, 16);
        c.Circle(8, 6, 5, 15);
        c.Circle(7, 5, 3, 31);
        return c.Build();
    }

    static Texture2D MakeRockTex()
    {
        var c = new PixelCanvas(12, 12);
        c.Circle(6, 7, 5, 4);
        c.Circle(6, 6, 5, 3);
        c.Circle(5, 5, 2, 5);
        return c.Build();
    }

    static Texture2D MakeRelicTex()
    {
        var c = new PixelCanvas(12, 12);
        c.Circle(6, 6, 5, 9);
        c.Circle(6, 6, 3, 25);
        c.Circle(6, 6, 1, 18);
        return c.Build();
    }

    // ── AI tint ───────────────────────────────────────────────────────────────

    static void TintAI(Texture2D tex)
    {
        var pixels = tex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++) {
            var p = pixels[i];
            if (p.a < 10) continue;
            pixels[i] = new Color32(
                (byte)Mathf.Min(255, p.r + 50),
                (byte)(p.g * 0.65f),
                (byte)(p.b * 0.65f),
                p.a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
    }
}
