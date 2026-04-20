using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Military unit with a 3-state combat FSM.
/// Player units (IsAI=false) auto-aggro AI units; AI units auto-aggro player units/buildings.
/// </summary>
public class MilitaryUnit : MonoBehaviour
{
    // ── Static registry ───────────────────────────────────────────────────────
    static readonly List<MilitaryUnit> _all = new List<MilitaryUnit>();
    public static IReadOnlyList<MilitaryUnit> AllUnits => _all;

    // ── Config ────────────────────────────────────────────────────────────────
    public UnitData Data;
    public bool IsAI;
    public bool IsSelected { get; private set; }

    // ── State machine ─────────────────────────────────────────────────────────
    enum State { Idle, MovingToTarget, AttackingUnit, AttackingBuilding, CommandedMove }
    State _state = State.Idle;

    MilitaryUnit _targetUnit;
    Building     _targetBuilding;
    Vector3      _moveTarget;

    // Pathfinding state
    readonly List<Vector3> _path = new List<Vector3>();
    int _pathIndex;
    Vector3 _pathDestination;
    float _targetPathRefresh;
    bool _hasPath;

    // ── Combat ────────────────────────────────────────────────────────────────
    float _hp;
    float _attackCooldown;

    // Cache building search so we don't FindObjectsByType every frame
    Building _cachedBuilding;
    float    _buildingSearchCooldown;

    // ── Visuals ───────────────────────────────────────────────────────────────
    Renderer   _bodyRenderer;
    GameObject _selectionRing;
    GameObject _healthBarGO;
    Transform  _healthBarFill;

    // ── Monk healing ─────────────────────────────────────────────────────────
    float _healTimer;
    const float HealRate     = 2f;   // seconds between heals
    const float HealAmount   = 5f;   // HP per tick
    const float HealRadius   = 6f;   // world-units

    // ── Fog ───────────────────────────────────────────────────────────────────
    float _fogTimer;
    const float FogRevealRate = 0.5f;

    // ── Properties ────────────────────────────────────────────────────────────
    public bool IsAlive => _hp > 0f && gameObject.activeSelf;
    public float HP     => _hp;
    public float MaxHP  => Data != null ? Data.MaxHP : 1f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _all.Add(this);
    }

    void OnDestroy()
    {
        _all.Remove(this);
        if (!IsAI && Data != null)
            ResourceManager.Instance?.FreePopulation(Data.PopulationCost);
    }

    void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        // Fog reveal
        _fogTimer -= Time.deltaTime;
        if (_fogTimer <= 0f)
        {
            FogOfWar.Instance?.Reveal(transform.position, Data?.LineOfSight ?? 8f);
            _fogTimer = FogRevealRate;
        }

        _attackCooldown -= Time.deltaTime;
        _buildingSearchCooldown -= Time.deltaTime;

        // Monk: heal nearby friendly units instead of attacking
        if (Data != null && Data.Type == UnitType.Monk)
        {
            _healTimer -= Time.deltaTime;
            if (_healTimer <= 0f)
            {
                _healTimer = HealRate;
                HealNearbyFriendlies();
            }
            // Monks still move on command; skip combat FSM
            if (_state == State.CommandedMove)
                DoCommandedMove();
            else
                _state = State.Idle;

            // Stay on ground + health bar rotation handled below
            var p2 = transform.position; p2.y = 0f; transform.position = p2;
            if (_healthBarGO != null && Camera.main != null)
                _healthBarGO.transform.rotation = Camera.main.transform.rotation;
            return;
        }

        switch (_state)
        {
            case State.Idle:              DoIdle();              break;
            case State.MovingToTarget:    DoMoveToTarget();      break;
            case State.AttackingUnit:     DoAttackUnit();        break;
            case State.AttackingBuilding: DoAttackBuilding();    break;
            case State.CommandedMove:     DoCommandedMove();     break;
        }

        // Stay on ground
        var p = transform.position; p.y = 0f; transform.position = p;

        // Health bar face camera
        if (_healthBarGO != null && Camera.main != null)
            _healthBarGO.transform.rotation = Camera.main.transform.rotation;
    }

    // ── FSM states ────────────────────────────────────────────────────────────

    void DoIdle()
    {
        // Find nearest enemy unit first
        var enemy = FindNearestEnemy(Data?.LineOfSight ?? 8f);
        if (enemy != null) { _targetUnit = enemy; _state = State.MovingToTarget; return; }

        // Find nearest enemy building (cached every 2s)
        if (_buildingSearchCooldown <= 0f)
        {
            _buildingSearchCooldown = 2f;
            _cachedBuilding = FindNearestEnemyBuilding(Data?.LineOfSight ?? 8f);
        }
        if (_cachedBuilding != null && _cachedBuilding.gameObject.activeSelf)
        {
            _targetBuilding = _cachedBuilding;
            _state = State.MovingToTarget;
        }
    }

    void DoMoveToTarget()
    {
        // Validate targets
        if (_targetUnit != null && !_targetUnit.IsAlive) { _targetUnit = null; _state = State.Idle; return; }
        if (_targetBuilding != null && !_targetBuilding.gameObject.activeSelf) { _targetBuilding = null; _state = State.Idle; return; }
        if (_targetUnit == null && _targetBuilding == null) { _state = State.Idle; return; }

        Vector3 targetPos = _targetUnit != null
            ? _targetUnit.transform.position
            : _targetBuilding.transform.position;

        float attackRange = GetEffectiveAttackRange() + 0.5f;
        float dist = Vector3.Distance(transform.position, targetPos);

        if (dist <= attackRange)
        {
            ClearPath();
            _state = _targetUnit != null ? State.AttackingUnit : State.AttackingBuilding;
        }
        else
        {
            // Rebuild path periodically because moving targets shift constantly.
            _targetPathRefresh -= Time.deltaTime;
            bool targetChanged = (targetPos - _pathDestination).sqrMagnitude > 4f;
            if (_targetPathRefresh <= 0f || targetChanged || _pathIndex >= _path.Count)
            {
                BuildPathTo(targetPos);
                _targetPathRefresh = 0.4f;
            }

            if (!FollowPath(attackRange))
            {
                // If we cannot path to the objective, attack a nearby blocking wall.
                var wall = FindNearestEnemyWall(Data?.LineOfSight ?? 8f);
                if (wall != null)
                {
                    _targetBuilding = wall;
                    _targetUnit = null;
                    _state = State.AttackingBuilding;
                }
            }
        }
    }

    void DoAttackUnit()
    {
        if (_targetUnit == null || !_targetUnit.IsAlive) { _targetUnit = null; _state = State.Idle; return; }

        Vector3 dir = (_targetUnit.transform.position - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);

        float dist = Vector3.Distance(transform.position, _targetUnit.transform.position);
        if (dist > GetEffectiveAttackRange() + 2f)
        {
            _state = State.MovingToTarget; return;
        }

        if (_attackCooldown <= 0f)
        {
            _attackCooldown = Data != null ? 1f / Data.AttackSpeed : 1f;
            PerformAttack(_targetUnit, null);
        }
    }

    void DoAttackBuilding()
    {
        if (_targetBuilding == null || !_targetBuilding.gameObject.activeSelf)
        { _targetBuilding = null; _state = State.Idle; return; }

        // Enemy unit interrupted attack?
        var closer = FindNearestEnemy(Data?.AttackRange ?? 1.5f + 2f);
        if (closer != null) { _targetUnit = closer; _targetBuilding = null; _state = State.MovingToTarget; return; }

        Vector3 dir = (_targetBuilding.transform.position - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);

        float dist = Vector3.Distance(transform.position, _targetBuilding.transform.position);
        if (dist > GetEffectiveAttackRange() + 2f)
        {
            _state = State.MovingToTarget; return;
        }

        if (_attackCooldown <= 0f)
        {
            _attackCooldown = Data != null ? 1f / Data.AttackSpeed : 1f;
            PerformAttack(null, _targetBuilding);
        }
    }

    void DoCommandedMove()
    {
        Vector3 dir = _moveTarget - transform.position; dir.y = 0f;
        if (dir.magnitude < 0.8f) { _state = State.Idle; return; }

        // Interrupt for close enemy units
        var enemy = FindNearestEnemy(Mathf.Min(Data?.LineOfSight * 0.4f ?? 3f, 5f));
        if (enemy != null) { _targetUnit = enemy; ClearPath(); _state = State.MovingToTarget; return; }

        if (!FollowPath(0.8f))
        {
            // When commanded path is blocked, switch to idle so auto-aggro can react (e.g., attack walls).
            _state = State.Idle;
        }
    }

    // ── Attack execution ──────────────────────────────────────────────────────

    void PerformAttack(MilitaryUnit targetUnit, Building targetBuilding)
    {
        float dmg = GetEffectiveAttack();
        bool isRanged = GetEffectiveAttackRange() > 2f;

        // Mangonel: area-of-effect splash at target location
        if (Data != null && Data.Type == UnitType.Mangonel)
        {
            Vector3 splashPos = targetUnit != null
                ? targetUnit.transform.position
                : targetBuilding != null ? targetBuilding.transform.position : transform.position;
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Projectile.SpawnAt(origin, splashPos, dmg, DamageType.Siege, splashRadius: 3.5f, isAI: IsAI);
            return;
        }

        if (isRanged)
        {
            Vector3 origin = transform.position + Vector3.up * 0.7f;
            if (targetUnit != null)
                Projectile.SpawnAt(origin, targetUnit, dmg, Data?.DamageType ?? DamageType.Pierce);
            else if (targetBuilding != null)
                Projectile.SpawnAt(origin, targetBuilding, dmg, Data?.DamageType ?? DamageType.Pierce);
        }
        else
        {
            if (targetUnit != null)
                targetUnit.TakeDamage(dmg, Data?.DamageType ?? DamageType.Melee);
            else if (targetBuilding != null)
                targetBuilding.TakeDamage(dmg);
        }
    }

    // ── Public commands ───────────────────────────────────────────────────────

    public void CommandMoveTo(Vector3 pos)
    {
        _moveTarget   = new Vector3(pos.x, 0f, pos.z);
        _targetUnit   = null;
        _targetBuilding = null;
        BuildPathTo(_moveTarget);
        _state        = State.CommandedMove;
    }

    public void CommandAttack(MilitaryUnit target)
    {
        _targetUnit     = target;
        _targetBuilding = null;
        ClearPath();
        _state          = State.MovingToTarget;
    }

    public void CommandAttackBuilding(Building target)
    {
        _targetBuilding = target;
        _targetUnit     = null;
        ClearPath();
        _state          = State.MovingToTarget;
    }

    public void TakeDamage(float amount, DamageType type)
    {
        if (!IsAlive) return;
        float armor = type == DamageType.Melee   ? GetEffectiveMeleeArmor() :
                      type == DamageType.Pierce   ? GetEffectivePierceArmor() : 0f;
        _hp -= Mathf.Max(1f, amount - armor);
        UpdateHealthBar();
        if (_hp <= 0f) StartCoroutine(DieRoutine());
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    public void Select()
    {
        IsSelected = true;
        _selectionRing?.SetActive(true);
    }

    public void Deselect()
    {
        IsSelected = false;
        _selectionRing?.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    MilitaryUnit FindNearestEnemy(float range)
    {
        MilitaryUnit best = null;
        float bestDist = range;
        foreach (var u in _all)
        {
            if (u == null || u == this || u.IsAI == IsAI || !u.IsAlive) continue;
            float d = Vector3.Distance(transform.position, u.transform.position);
            if (d < bestDist) { bestDist = d; best = u; }
        }
        return best;
    }

    Building FindNearestEnemyBuilding(float range)
    {
        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        Building best = null;
        float bestDist = range;
        foreach (var b in buildings)
        {
            if (b == null || !b.IsBuilt) continue;
            // Player units attack AI buildings, AI units attack player buildings
            if (b.IsAI == IsAI) continue;
            float d = Vector3.Distance(transform.position, b.transform.position);
            if (d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }

    void MoveToward(Vector3 dir)
    {
        float speed = Data?.MoveSpeed ?? 4f;
        transform.position += new Vector3(dir.x, 0f, dir.z) * speed * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
    }

    void BuildPathTo(Vector3 destination)
    {
        _pathDestination = new Vector3(destination.x, 0f, destination.z);
        _path.Clear();
        _pathIndex = 0;
        _hasPath = false;

        var p = Pathfinder.Instance?.FindPath(transform.position, _pathDestination);
        if (p == null || p.Count == 0) return;

        _path.AddRange(p);
        _hasPath = true;
        if (_path.Count > 0)
        {
            // First waypoint often matches current cell.
            if (Vector3.Distance(transform.position, _path[0]) < 0.35f)
                _pathIndex = 1;
        }
    }

    bool FollowPath(float stopDistance)
    {
        if (!_hasPath || _path.Count == 0 || _pathIndex >= _path.Count) return false;

        if (Vector3.Distance(transform.position, _pathDestination) <= stopDistance)
        {
            ClearPath();
            return false;
        }

        var waypoint = _path[_pathIndex];
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

    void ClearPath()
    {
        _path.Clear();
        _pathIndex = 0;
        _hasPath = false;
    }

    Building FindNearestEnemyWall(float range)
    {
        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        Building best = null;
        float bestDist = range;
        foreach (var b in buildings)
        {
            if (b == null || !b.IsBuilt || b.IsAI == IsAI) continue;
            if (b.Data == null || b.Data.Type != BuildingType.Wall) continue;
            float d = Vector3.Distance(transform.position, b.transform.position);
            if (d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }

    void HealNearbyFriendlies()
    {
        foreach (var u in _all)
        {
            if (u == null || u == this || u.IsAI != IsAI || !u.IsAlive) continue;
            if (Vector3.Distance(transform.position, u.transform.position) > HealRadius) continue;
            if (u._hp >= u.MaxHP) continue;
            u._hp = Mathf.Min(u.MaxHP, u._hp + HealAmount);
            u.UpdateHealthBar();
        }
    }

    void UpdateHealthBar()
    {
        if (_healthBarFill == null || Data == null) return;
        float pct = Mathf.Clamp01(_hp / Data.MaxHP);
        _healthBarFill.localScale = new Vector3(1.0f * pct, 0.08f, 0.04f);
        var rend = _healthBarFill.GetComponent<Renderer>();
        if (rend) rend.material.color = Color.Lerp(Color.red, Color.green, pct);
    }

    float GetEffectiveAttack()
    {
        float baseAttack = Data?.Attack ?? 4f;
        if (!IsAI)
            baseAttack += ResearchManager.Instance?.GetAttackBonus(Data) ?? 0f;
        return baseAttack;
    }

    float GetEffectiveAttackRange()
    {
        float baseRange = Data?.AttackRange ?? 1.5f;
        if (!IsAI)
            baseRange += ResearchManager.Instance?.GetRangeBonus(Data) ?? 0f;
        return baseRange;
    }

    float GetEffectiveMeleeArmor()
    {
        float baseArmor = Data?.MeleeArmor ?? 0f;
        if (!IsAI)
            baseArmor += ResearchManager.Instance?.GetMeleeArmorBonus(Data) ?? 0f;
        return baseArmor;
    }

    float GetEffectivePierceArmor()
    {
        return Data?.PierceArmor ?? 0f;
    }

    IEnumerator DieRoutine()
    {
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 0.3f);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MilitaryUnit SpawnAt(Vector3 pos, UnitData data, bool isAI = false)
    {
        if (!isAI)
        {
            if (ResourceManager.Instance != null && !ResourceManager.Instance.CanTrainUnit(data.PopulationCost))
            {
                UIManager.Instance?.ShowMessage("Population limit reached!");
                return null;
            }
        }

        var go = new GameObject(data.UnitName + (isAI ? "_AI" : ""));
        go.transform.position = new Vector3(pos.x, 0f, pos.z);

        Color bodyColor = isAI ? new Color(0.7f, 0.15f, 0.15f) : data.UnitColor;
        Renderer bodyRenderer = null; // captured from humanoid block; null for siege units

        // ── Siege engine: entirely different mesh ────────────────────────────
        if (data.Type == UnitType.BatteringRam || data.Type == UnitType.Mangonel)
        {
            BuildSiegeMesh(go, data, isAI);
            goto FinishUnit;
        }

        {
        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(go.transform);
        float bodyScale = data.Type == UnitType.Knight ? 0.40f : 0.30f;
        body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        body.transform.localScale    = new Vector3(bodyScale, 0.40f, bodyScale);
        body.GetComponent<Renderer>().material.color = bodyColor;
        Object.Destroy(body.GetComponent<Collider>());
        bodyRenderer = body.GetComponent<Renderer>();

        // Weapon
        if (data.Type == UnitType.Monk)
        {
            // Staff
            var staff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            staff.transform.SetParent(go.transform);
            staff.transform.localPosition = new Vector3(0.22f, 0.72f, 0.1f);
            staff.transform.localRotation = Quaternion.Euler(0f, 0f, 5f);
            staff.transform.localScale    = new Vector3(0.05f, 0.45f, 0.05f);
            staff.GetComponent<Renderer>().material.color = new Color(0.42f, 0.28f, 0.10f);
            Object.Destroy(staff.GetComponent<Collider>());
        }
        else if (data.Type == UnitType.Knight)
        {
            // Lance
            var lance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lance.transform.SetParent(go.transform);
            lance.transform.localPosition = new Vector3(0.25f, 0.65f, 0.22f);
            lance.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            lance.transform.localScale    = new Vector3(0.05f, 0.55f, 0.05f);
            lance.GetComponent<Renderer>().material.color = new Color(0.65f, 0.50f, 0.30f);
            Object.Destroy(lance.GetComponent<Collider>());
        }
        else if (data.AttackRange > 2f)
        {
            // Bow (ranged)
            var bow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bow.transform.SetParent(go.transform);
            bow.transform.localPosition = new Vector3(0.2f, 0.6f, 0.1f);
            bow.transform.localRotation = Quaternion.Euler(0f, 0f, 75f);
            bow.transform.localScale    = new Vector3(0.04f, 0.22f, 0.04f);
            bow.GetComponent<Renderer>().material.color = new Color(0.35f, 0.22f, 0.08f);
            Object.Destroy(bow.GetComponent<Collider>());
        }
        else
        {
            // Sword (melee)
            var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.transform.SetParent(go.transform);
            sword.transform.localPosition = new Vector3(0.22f, 0.58f, 0.1f);
            sword.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            sword.transform.localScale    = new Vector3(0.06f, 0.35f, 0.05f);
            sword.GetComponent<Renderer>().material.color = new Color(0.75f, 0.75f, 0.8f);
            Object.Destroy(sword.GetComponent<Collider>());
        }

        // Head
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(go.transform);
        head.transform.localPosition = new Vector3(0f, 1.08f, 0f);
        head.transform.localScale    = new Vector3(0.22f, 0.22f, 0.22f);
        // Monk: hooded darker skin tone
        head.GetComponent<Renderer>().material.color = isAI
            ? new Color(0.55f, 0.12f, 0.12f)
            : (data.Type == UnitType.Monk ? new Color(0.60f, 0.50f, 0.38f) : new Color(0.85f, 0.72f, 0.55f));
        Object.Destroy(head.GetComponent<Collider>());
        } // end humanoid block

        FinishUnit:
        // Selection ring (player only)
        GameObject selRing = null;
        if (!isAI)
        {
            selRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            selRing.name = "SelectRing";
            selRing.transform.SetParent(go.transform);
            selRing.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            selRing.transform.localScale    = new Vector3(0.9f, 0.03f, 0.9f);
            selRing.GetComponent<Renderer>().material.color = new Color(0.2f, 0.9f, 1f);
            Object.Destroy(selRing.GetComponent<Collider>());
            selRing.SetActive(false);
        }

        // Root collider (for raycasting)
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.55f, 0f);
        col.height = 1.1f;
        col.radius = 0.3f;

        // Health bar
        var hbGO = new GameObject("HealthBar");
        hbGO.transform.SetParent(go.transform);
        hbGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        var hbBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hbBg.transform.SetParent(hbGO.transform);
        hbBg.transform.localPosition = Vector3.zero;
        hbBg.transform.localScale    = new Vector3(1.05f, 0.08f, 0.04f);
        hbBg.GetComponent<Renderer>().material.color = Color.black;
        Object.Destroy(hbBg.GetComponent<Collider>());

        var hbFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hbFill.transform.SetParent(hbGO.transform);
        hbFill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        hbFill.transform.localScale    = new Vector3(1.0f, 0.08f, 0.04f);
        hbFill.GetComponent<Renderer>().material.color = Color.green;
        Object.Destroy(hbFill.GetComponent<Collider>());

        // Assemble component
        var unit = go.AddComponent<MilitaryUnit>();
        unit.Data            = data;
        unit.IsAI            = isAI;
        unit._hp             = data.MaxHP;
        unit._selectionRing  = selRing;
        unit._healthBarGO    = hbGO;
        unit._healthBarFill  = hbFill.transform;
        unit._bodyRenderer   = bodyRenderer;

        if (!isAI)
            ResourceManager.Instance?.ReservePopulation(data.PopulationCost);

        return unit;
    }

    // ── Siege engine geometry ─────────────────────────────────────────────────

    static void BuildSiegeMesh(GameObject go, UnitData data, bool isAI)
    {
        Color wood  = new Color(0.45f, 0.30f, 0.14f);
        Color iron  = new Color(0.42f, 0.42f, 0.46f);

        if (data.Type == UnitType.BatteringRam)
        {
            // Wheeled log frame
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.transform.SetParent(go.transform);
            frame.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            frame.transform.localScale    = new Vector3(0.7f, 0.35f, 1.4f);
            frame.GetComponent<Renderer>().material.color = wood;
            Object.Destroy(frame.GetComponent<Collider>());

            // Ram log
            var ram = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ram.transform.SetParent(go.transform);
            ram.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            ram.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ram.transform.localScale    = new Vector3(0.16f, 0.7f, 0.16f);
            ram.GetComponent<Renderer>().material.color = isAI ? new Color(0.6f, 0.2f, 0.2f) : new Color(0.35f, 0.22f, 0.10f);
            Object.Destroy(ram.GetComponent<Collider>());

            // Iron head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(go.transform);
            head.transform.localPosition = new Vector3(0f, 0.48f, 0.72f);
            head.transform.localScale    = new Vector3(0.22f, 0.22f, 0.28f);
            head.GetComponent<Renderer>().material.color = iron;
            Object.Destroy(head.GetComponent<Collider>());

            // Wheels (4 cylinders)
            float[] wx = { -0.38f, 0.38f };
            float[] wz = { -0.5f, 0.5f };
            foreach (float x in wx)
                foreach (float z in wz)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.transform.SetParent(go.transform);
                    wheel.transform.localPosition = new Vector3(x, 0.20f, z);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    wheel.transform.localScale    = new Vector3(0.20f, 0.09f, 0.20f);
                    wheel.GetComponent<Renderer>().material.color = wood * 0.85f;
                    Object.Destroy(wheel.GetComponent<Collider>());
                }
        }
        else // Mangonel
        {
            // Base platform
            var base_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            base_.transform.SetParent(go.transform);
            base_.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            base_.transform.localScale    = new Vector3(1.0f, 0.44f, 1.2f);
            base_.GetComponent<Renderer>().material.color = wood;
            Object.Destroy(base_.GetComponent<Collider>());

            // Arm (trebuchet-style beam)
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.transform.SetParent(go.transform);
            arm.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            arm.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            arm.transform.localScale    = new Vector3(0.09f, 0.55f, 0.09f);
            arm.GetComponent<Renderer>().material.color = isAI ? new Color(0.6f, 0.2f, 0.2f) : wood * 1.1f;
            Object.Destroy(arm.GetComponent<Collider>());

            // Sling cup (small sphere at arm tip)
            var cup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cup.transform.SetParent(go.transform);
            cup.transform.localPosition = new Vector3(0f, 1.28f, 0.38f);
            cup.transform.localScale    = new Vector3(0.18f, 0.18f, 0.18f);
            cup.GetComponent<Renderer>().material.color = iron;
            Object.Destroy(cup.GetComponent<Collider>());

            // Wheels
            float[] wx2 = { -0.45f, 0.45f };
            float[] wz2 = { -0.5f, 0.5f };
            foreach (float x in wx2)
                foreach (float z in wz2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.transform.SetParent(go.transform);
                    wheel.transform.localPosition = new Vector3(x, 0.18f, z);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    wheel.transform.localScale    = new Vector3(0.22f, 0.09f, 0.22f);
                    wheel.GetComponent<Renderer>().material.color = wood * 0.85f;
                    Object.Destroy(wheel.GetComponent<Collider>());
                }
        }
    }
}
