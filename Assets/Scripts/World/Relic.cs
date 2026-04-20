using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Golden relic scattered on the map.
/// Monks walk over to collect; deposit at a Monastery for +1 Gold/s passive income.
/// </summary>
public class Relic : MonoBehaviour
{
    static readonly List<Relic> _all = new List<Relic>();
    public static IReadOnlyList<Relic> All => _all;

    public bool IsCollected { get; private set; }

    // Slow spin for visibility
    void Awake() => _all.Add(this);
    void OnDestroy() => _all.Remove(this);

    void Update()
    {
        transform.Rotate(Vector3.up, 60f * Time.deltaTime);
    }

    public void Collect()
    {
        if (IsCollected) return;
        IsCollected = true;
        gameObject.SetActive(false);
    }

    /// <summary>Called when a Monk deposits this relic at a Monastery.</summary>
    public void Deposit()
    {
        ResourceManager.Instance?.AddProduction(ResourceType.Gold, 1);
        GameManager.Instance?.NotifyRelicDeposited();
        UIManager.Instance?.ShowMessage("Relic deposited! +1 Gold/s", 3f);
        Destroy(gameObject);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Relic SpawnAt(Vector3 pos)
    {
        var go = new GameObject("Relic");
        go.transform.position = new Vector3(pos.x, 0f, pos.z);

        // Pixel-art relic sprite
        SpriteQuad.Create(PixelArtSprites.RelicTex(), 1.0f, 1.0f, 0.08f, go.transform);

        // Trigger collider on root
        var col = go.AddComponent<SphereCollider>();
        col.radius    = 0.6f;
        col.isTrigger = true;

        return go.AddComponent<Relic>();
    }
}
