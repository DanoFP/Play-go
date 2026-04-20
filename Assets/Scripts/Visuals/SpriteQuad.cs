using UnityEngine;

/// <summary>
/// Creates flat sprite GameObjects for the pixel-art top-down view.
/// All sprites are Quad primitives lying flat on the XZ plane (rotated 90° on X).
/// Uses Unlit/Transparent shader so they render without lighting influence.
/// </summary>
public static class SpriteQuad
{
    static Shader _shader;

    static Shader GetShader()
    {
        if (_shader != null) return _shader;
        _shader = Shader.Find("Unlit/Transparent");
        if (_shader == null) _shader = Shader.Find("Sprites/Default");
        if (_shader == null) _shader = Shader.Find("Standard");
        return _shader;
    }

    /// <summary>
    /// Create a flat sprite quad.
    /// </summary>
    /// <param name="tex">The pixel-art texture (FilterMode.Point already set).</param>
    /// <param name="worldW">Width in world units (X axis).</param>
    /// <param name="worldH">Height in world units (Z axis).</param>
    /// <param name="yOffset">Y position (default 0.05 — just above ground).</param>
    /// <param name="parent">Optional parent transform.</param>
    public static GameObject Create(Texture2D tex, float worldW, float worldH,
                                    float yOffset = 0.05f, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Sprite";

        // Remove the quad's MeshCollider — we handle colliders separately
        Object.Destroy(go.GetComponent<Collider>());

        var mr = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows     = false;

        var mat = new Material(GetShader());
        mat.mainTexture = tex;
        mr.sharedMaterial = mat;

        // Quad is 1×1 in XY by default; rotate to lie flat on XZ plane
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale    = new Vector3(worldW, worldH, 1f);
        go.transform.localPosition = new Vector3(0f, yOffset, 0f);

        if (parent != null) go.transform.SetParent(parent, false);

        return go;
    }

    /// <summary>
    /// Add an invisible BoxCollider sized to the sprite's footprint on the parent.
    /// Used so raycasts can select buildings/units.
    /// </summary>
    public static BoxCollider AddFlatCollider(GameObject root, float worldW, float worldH, float height = 1f)
    {
        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, height * 0.5f, 0f);
        col.size   = new Vector3(worldW, height, worldH);
        return col;
    }

    /// <summary>
    /// Replace the Renderer's texture at runtime (e.g., damage state, selection tint).
    /// </summary>
    public static void SetTexture(GameObject spriteGO, Texture2D tex)
    {
        var mr = spriteGO?.GetComponent<MeshRenderer>();
        if (mr != null) mr.material.mainTexture = tex;
    }
}
