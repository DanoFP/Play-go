using UnityEngine;

/// <summary>
/// A gatherable resource node in the world. Villagers seek these out, gather from them,
/// then carry the resources back to the nearest Town Center.
/// </summary>
public class ResourceNode : MonoBehaviour
{
    public enum NodeType { Wood, Stone, Gold }

    public NodeType Type;
    public int Amount = 100;

    private Villager _assignedGatherer;
    private Renderer[] _renderers;

    public bool IsAvailable => Amount > 0 && _assignedGatherer == null;

    void Awake() => _renderers = GetComponentsInChildren<Renderer>();

    public void AssignGatherer(Villager v) => _assignedGatherer = v;

    public void FreeGatherer()
    {
        if (_assignedGatherer != null) _assignedGatherer = null;
    }

    /// <summary>
    /// Gather up to <paramref name="amount"/> from this node. Returns how much was actually taken.
    /// </summary>
    public int Gather(int amount)
    {
        int taken = Mathf.Min(amount, Amount);
        Amount -= taken;
        if (Amount <= 0)
        {
            // Visually mark as depleted, then destroy
            foreach (var r in _renderers)
                if (r != null) r.material.color = new Color(0.25f, 0.25f, 0.25f);
            Destroy(gameObject, 4f);
        }
        return taken;
    }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static ResourceNode SpawnAt(Vector3 pos, NodeType type, int amount)
    {
        var go = new GameObject(type + "Node");
        go.transform.position = pos;

        var mesh = GameObject.CreatePrimitive(
            type == NodeType.Wood  ? PrimitiveType.Cylinder :
            type == NodeType.Gold  ? PrimitiveType.Sphere   : PrimitiveType.Cube);
        mesh.transform.SetParent(go.transform);
        Destroy(mesh.GetComponent<Collider>());

        switch (type)
        {
            case NodeType.Wood:
                mesh.transform.localScale    = new Vector3(0.4f, 1.0f, 0.4f);
                mesh.transform.localPosition = new Vector3(0, 0.5f, 0);
                mesh.GetComponent<Renderer>().material.color = new Color(0.45f, 0.28f, 0.10f);
                break;
            case NodeType.Stone:
                mesh.transform.localScale    = new Vector3(1.1f, 0.8f, 1.1f);
                mesh.transform.localPosition = new Vector3(0, 0.4f, 0);
                mesh.GetComponent<Renderer>().material.color = new Color(0.62f, 0.62f, 0.67f);
                break;
            case NodeType.Gold:
                mesh.transform.localScale    = new Vector3(0.7f, 0.5f, 0.7f);
                mesh.transform.localPosition = new Vector3(0, 0.25f, 0);
                mesh.GetComponent<Renderer>().material.color = new Color(1.0f, 0.85f, 0.1f);
                break;
        }

        // Trigger collider so villagers can detect proximity
        var col = go.AddComponent<SphereCollider>();
        col.radius = 1.2f;
        col.center = new Vector3(0, 0.5f, 0);
        col.isTrigger = true;

        var node = go.AddComponent<ResourceNode>();
        node.Type   = type;
        node.Amount = amount;
        return node;
    }
}
