using UnityEngine;

/// <summary>
/// Manages fog of war:
///   • A world-space Plane at y=0.5 covered by a transparent RGBA Texture2D (starts opaque/black).
///   • Reveal() punches transparent holes where units have explored.
///   • MinimapTexture is a separate small texture (dark → terrain-green) shown in the UI minimap.
/// </summary>
public class FogOfWar : MonoBehaviour
{
    public static FogOfWar Instance { get; private set; }

    // World bounds covered by the fog plane
    public const float WorldMin  = -64f;
    public const float WorldMax  =  64f;
    public const float WorldSize = WorldMax - WorldMin; // 128

    const int Res = 64; // grid resolution

    bool[,]   _revealed;
    Texture2D _fogTex;
    Texture2D _minimapTex;
    bool      _dirty;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _revealed   = new bool[Res, Res];
        _fogTex     = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        _minimapTex = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point,    wrapMode = TextureWrapMode.Clamp };

        Color fog  = new Color(0f, 0f, 0f, 0.92f);
        Color dark = new Color(0.03f, 0.03f, 0.05f);
        Color[] fp = new Color[Res * Res];
        Color[] mp = new Color[Res * Res];
        for (int i = 0; i < fp.Length; i++) { fp[i] = fog; mp[i] = dark; }
        _fogTex.SetPixels(fp);     _fogTex.Apply();
        _minimapTex.SetPixels(mp); _minimapTex.Apply();

        // World-space fog plane: Unity Plane faces up by default, no rotation needed.
        // Scale = WorldSize / 10 because the default Plane primitive is 10×10 units.
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "FogPlane";
        plane.transform.SetParent(transform);
        plane.transform.position   = new Vector3(0f, 0.5f, 0f);
        plane.transform.localScale = new Vector3(WorldSize / 10f, 1f, WorldSize / 10f);
        Destroy(plane.GetComponent<Collider>());

        var mat = plane.GetComponent<Renderer>().material; // instanced Default-Material
        mat.mainTexture = _fogTex;
        mat.color       = Color.white;
        mat.SetFloat("_Mode",    3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite",   0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3100; // above water (3000)
    }

    void LateUpdate()
    {
        if (!_dirty) return;
        _fogTex.Apply();
        _minimapTex.Apply();
        _dirty = false;
    }

    /// <summary>Reveal a circular area of radius <paramref name="radius"/> around <paramref name="worldPos"/>.</summary>
    public void Reveal(Vector3 worldPos, float radius)
    {
        int cx = ToCell(worldPos.x);
        int cz = ToCell(worldPos.z);
        float cellSize = WorldSize / Res;
        int   cr       = Mathf.CeilToInt(radius / cellSize) + 1;

        for (int x = Mathf.Max(0, cx - cr); x <= Mathf.Min(Res - 1, cx + cr); x++)
        for (int z = Mathf.Max(0, cz - cr); z <= Mathf.Min(Res - 1, cz + cr); z++)
        {
            if (_revealed[x, z]) continue;
            float dx = (x - cx) * cellSize, dz = (z - cz) * cellSize;
            if (dx * dx + dz * dz > radius * radius) continue;

            _revealed[x, z] = true;
            _fogTex.SetPixel(x, z, Color.clear);
            _minimapTex.SetPixel(x, z, new Color(0.24f, 0.43f, 0.17f)); // terrain green
            _dirty = true;
        }
    }

    public bool IsExplored(float worldX, float worldZ)
    {
        int x = ToCell(worldX), z = ToCell(worldZ);
        if ((uint)x >= Res || (uint)z >= Res) return false;
        return _revealed[x, z];
    }

    public Texture2D MinimapTexture => _minimapTex;

    int ToCell(float w) =>
        Mathf.Clamp(Mathf.FloorToInt((w - WorldMin) / WorldSize * Res), 0, Res - 1);
}
