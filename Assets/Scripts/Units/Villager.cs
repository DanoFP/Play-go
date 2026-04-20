using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Worker NPC.
/// • Auto-gathers from the nearest available ResourceNode and deposits at the nearest Town Center.
/// • Left-click selects; right-click when selected issues a move-to command.
/// • Continuously reveals fog of war around its position.
/// </summary>
public class Villager : MonoBehaviour
{
    // ── Static worker count (shown in HUD) ───────────────────────────────────
    public static int Count { get; private set; }

    // ── State machine ─────────────────────────────────────────────────────────
    enum State { Idle, MovingToNode, Gathering, ReturningToBase, CommandedMove }
    State _state = State.Idle;

    ResourceNode _targetNode;
    Vector3      _commandTarget;
    float        _gatherTimer;
    float        _idleTimer;
    int          _carriedAmount;
    ResourceType _carriedType;

    // ── Selection ─────────────────────────────────────────────────────────────
    public bool IsSelected { get; private set; }

    GameObject _selectionRing;
    Renderer   _bodyRenderer;

    // ── Constants ─────────────────────────────────────────────────────────────
    const float MoveSpeed      = 5f;
    const float GatherDuration = 3f;
    const int   GatherAmount   = 30;
    const float IdleRetryWait  = 1.5f;
    const float BaseVisionRadius = 12f;
    const float FogRevealRate  = 0.25f;

    float _fogTimer;

    // Pathfinding state
    readonly List<Vector3> _path = new List<Vector3>();
    int _pathIndex;
    Vector3 _pathDestination;
    bool _hasPath;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Count++;
        _idleTimer = Random.Range(0f, 1.5f); // stagger so workers don't all move at once
    }

    void OnDestroy() => Count--;

    void Start()
    {
        // _bodyRenderer set here after children are created by SpawnAt
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            if (r.gameObject.name == "Body") { _bodyRenderer = r; break; }
        if (_bodyRenderer == null && renderers.Length > 0) _bodyRenderer = renderers[0];
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        // Fog reveal
        _fogTimer -= Time.deltaTime;
        if (_fogTimer <= 0f)
        {
            float los = BaseVisionRadius + (ResearchManager.Instance?.GetVillagerLoSBonus() ?? 0f);
            FogOfWar.Instance?.Reveal(transform.position, los);
            _fogTimer = FogRevealRate;
        }

        switch (_state)
        {
            case State.Idle:            DoIdle();          break;
            case State.MovingToNode:    MoveToNode();      break;
            case State.Gathering:       DoGather();        break;
            case State.ReturningToBase: ReturnToBase();    break;
            case State.CommandedMove:   DoCommandedMove(); break;
        }

        // Stay on ground
        Vector3 p = transform.position; p.y = 0f; transform.position = p;

        UpdateBodyColor();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Select()
    {
        IsSelected = true;
        if (_selectionRing != null) _selectionRing.SetActive(true);
    }

    public void Deselect()
    {
        IsSelected = false;
        if (_selectionRing != null) _selectionRing.SetActive(false);
    }

    public void CommandMoveTo(Vector3 target)
    {
        _commandTarget = new Vector3(target.x, 0f, target.z);
        _targetNode?.FreeGatherer();
        _targetNode    = null;
        _carriedAmount = 0;
        BuildPathTo(_commandTarget);
        _state         = State.CommandedMove;
    }

    // ── State handlers ────────────────────────────────────────────────────────

    void DoIdle()
    {
        _idleTimer -= Time.deltaTime;
        if (_idleTimer > 0f) return;

        var nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        float    nearest = float.MaxValue;
        ResourceNode best = null;
        foreach (var n in nodes)
        {
            if (!n.IsAvailable) continue;
            float d = Vector3.Distance(transform.position, n.transform.position);
            if (d < nearest) { nearest = d; best = n; }
        }
        if (best != null)
        {
            _targetNode = best;
            _targetNode.AssignGatherer(this);
            _state = State.MovingToNode;
        }
        else _idleTimer = IdleRetryWait;
    }

    void MoveToNode()
    {
        if (_targetNode == null || !_targetNode.gameObject.activeSelf)
        { _targetNode = null; ClearPath(); _state = State.Idle; _idleTimer = 0.3f; return; }

        Vector3 targetPos = _targetNode.transform.position;
        Vector3 dir = targetPos - transform.position; dir.y = 0f;
        if (dir.magnitude < 1.6f) { ClearPath(); _gatherTimer = GatherDuration; _state = State.Gathering; }
        else if (!FollowPathTo(targetPos, 1.6f)) { _state = State.Idle; _idleTimer = 0.35f; }
    }

    void DoGather()
    {
        _gatherTimer -= Time.deltaTime;
        if (_gatherTimer > 0f) return;

        if (_targetNode == null || !_targetNode.gameObject.activeSelf || _targetNode.Amount <= 0)
        {
            _targetNode?.FreeGatherer();
            ClearPath();
            _targetNode = null; _state = State.Idle; _idleTimer = 0.2f;
            return;
        }

        int gatherPerTrip = Mathf.RoundToInt(GatherAmount * GetGatherMultiplier(_targetNode.Type));
        int gathered = _targetNode.Gather(Mathf.Max(1, gatherPerTrip));
        _carriedAmount = gathered;
        _carriedType   = _targetNode.Type == ResourceNode.NodeType.Wood  ? ResourceType.Wood  :
                         _targetNode.Type == ResourceNode.NodeType.Stone ? ResourceType.Stone :
                         ResourceType.Gold;
        _targetNode.FreeGatherer();
        ClearPath();
        _targetNode = null;
        _state = State.ReturningToBase;
    }

    void ReturnToBase()
    {
        Vector3 basePos = FindBasePosition();
        Vector3 dir = basePos - transform.position; dir.y = 0f;
        if (dir.magnitude < 2.5f)
        {
            ResourceManager.Instance?.AddResource(_carriedType, _carriedAmount);
            _carriedAmount = 0;
            ClearPath();
            _state = State.Idle; _idleTimer = 0.3f;
        }
        else if (!FollowPathTo(basePos, 2.5f)) { _state = State.Idle; _idleTimer = 0.35f; }
    }

    void DoCommandedMove()
    {
        Vector3 dir = _commandTarget - transform.position; dir.y = 0f;
        if (dir.magnitude < 0.6f) { ClearPath(); _state = State.Idle; _idleTimer = 0.5f; }
        else if (!FollowPathTo(_commandTarget, 0.6f)) { _state = State.Idle; _idleTimer = 0.35f; }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void MoveToward(Vector3 dir)
    {
        transform.position += new Vector3(dir.x, 0f, dir.z) * MoveSpeed * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
    }

    bool FollowPathTo(Vector3 target, float stopDistance)
    {
        if ((target - _pathDestination).sqrMagnitude > 4f || _path.Count == 0 || _pathIndex >= _path.Count)
            BuildPathTo(target);

        if (!_hasPath || _path.Count == 0 || _pathIndex >= _path.Count) return false;
        if (Vector3.Distance(transform.position, target) <= stopDistance)
        {
            ClearPath();
            return false;
        }

        Vector3 waypoint = _path[_pathIndex];
        Vector3 dir = waypoint - transform.position;
        dir.y = 0f;

        if (dir.magnitude < 0.2f)
        {
            _pathIndex++;
            if (_pathIndex >= _path.Count) return false;
            waypoint = _path[_pathIndex];
            dir = waypoint - transform.position;
            dir.y = 0f;
        }

        if (dir.sqrMagnitude > 0.001f)
        {
            MoveToward(dir.normalized);
            return true;
        }

        return false;
    }

    void BuildPathTo(Vector3 target)
    {
        _pathDestination = new Vector3(target.x, 0f, target.z);
        _path.Clear();
        _pathIndex = 0;
        _hasPath = false;

        var p = Pathfinder.Instance?.FindPath(transform.position, _pathDestination);
        if (p == null || p.Count == 0) return;
        _path.AddRange(p);
        _hasPath = true;

        if (_path.Count > 0 && Vector3.Distance(transform.position, _path[0]) < 0.35f)
            _pathIndex = 1;
    }

    void ClearPath()
    {
        _path.Clear();
        _pathIndex = 0;
        _hasPath = false;
    }

    Vector3 FindBasePosition()
    {
        // Preferred deposit building per resource type
        BuildingType preferred =
            _carriedType == ResourceType.Wood  ? BuildingType.LumberCamp :
            _carriedType == ResourceType.Stone ? BuildingType.MiningCamp :
            _carriedType == ResourceType.Gold  ? BuildingType.MiningCamp :
            BuildingType.Mill; // Food

        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        float   nearest     = float.MaxValue;
        Vector3 bestPref    = Vector3.zero;
        float   nearestTC   = float.MaxValue;
        Vector3 bestTC      = Vector3.zero;

        foreach (var b in buildings)
        {
            if (b == null || !b.IsBuilt || b.IsAI) continue;
            float d = Vector3.Distance(transform.position, b.transform.position);

            if (b.Data?.Type == preferred && d < nearest)
            { nearest = d; bestPref = b.transform.position; }

            if (b.Data?.Type == BuildingType.TownCenter && d < nearestTC)
            { nearestTC = d; bestTC = b.transform.position; }
        }

        // Prefer specialized depot; fall back to TownCenter
        if (nearest < float.MaxValue) return bestPref;
        if (nearestTC < float.MaxValue) return bestTC;
        return Vector3.zero;
    }

    void UpdateBodyColor()
    {
        if (_bodyRenderer == null) return;
        if (_carriedAmount > 0)
        {
            _bodyRenderer.material.color =
                _carriedType == ResourceType.Wood  ? new Color(0.45f, 0.28f, 0.10f) :
                _carriedType == ResourceType.Stone ? new Color(0.62f, 0.62f, 0.67f) :
                                                     new Color(1.0f,  0.85f, 0.1f);
        }
        else
        {
            _bodyRenderer.material.color = IsSelected
                ? new Color(0.35f, 0.88f, 1.0f)   // cyan tint when selected
                : new Color(0.85f, 0.72f, 0.55f);  // skin color
        }
    }

    float GetGatherMultiplier(ResourceNode.NodeType nodeType)
    {
        if (nodeType == ResourceNode.NodeType.Wood)
            return ResearchManager.Instance?.GetVillagerGatherMultiplier(ResourceType.Wood) ?? 1f;
        return 1f;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Villager SpawnAt(Vector3 pos)
    {
        var go = new GameObject("Villager");
        go.transform.position = new Vector3(pos.x, 0f, pos.z);

        // Pixel-art sprite
        var spriteGO = SpriteQuad.Create(PixelArtSprites.VillagerSprite(), 1.2f, 1.2f, 0.06f, go.transform);
        spriteGO.name = "Body";

        // Selection ring — flat quad glow
        var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ring.name = "SelectRing";
        ring.transform.SetParent(go.transform);
        ring.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale    = new Vector3(1.6f, 1.6f, 1f);
        ring.GetComponent<Renderer>().material.color = new Color(0.2f, 0.9f, 1f, 0.5f);
        Destroy(ring.GetComponent<Collider>());
        ring.SetActive(false);

        // Flat box collider for top-down raycast selection
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.1f, 0f);
        col.size   = new Vector3(1.2f, 0.2f, 1.2f);

        var v = go.AddComponent<Villager>();
        v._selectionRing = ring;
        return v;
    }
}
