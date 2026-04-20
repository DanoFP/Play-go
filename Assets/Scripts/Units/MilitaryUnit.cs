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

    // ── Monk healing & faith ─────────────────────────────────────────────────
    float _healTimer;
    const float HealRate     = 2f;   // seconds between heals
    const float HealAmount   = 5f;   // HP per tick
    const float HealRadius   = 6f;   // world-units

    // Monk: relic carrying
    bool  _isCarryingRelic;

    // Monk: faith & conversion
    float _faith = 50f;
    const float MaxFaith            = 100f;
    const float FaithRegenRate      = 2f;    // per second
    const float ConversionFaithCost = 30f;
    const float ConversionTime      = 18f;   // seconds
    const float ConversionRange     = 10f;
    MilitaryUnit _conversionTarget;
    float        _conversionTimer;
    bool         _isConverting;

    // ── Trebuchet deploy ──────────────────────────────────────────────────────
    bool  _trebuchetDeployed;
    bool  _trebuchetAnimating;
    float _trebuchetAnimTimer;
    const float TrebuchetDeployTime = 5f;

    public bool TrebuchetDeployed  => _trebuchetDeployed;
    public bool TrebuchetAnimating => _trebuchetAnimating;

    // ── Warchief aura ─────────────────────────────────────────────────────────
    const float WarchiefAuraRadius = 5f;
    const float WarchiefAuraBonus  = 0.05f; // +5% attack

    // ── Fog ───────────────────────────────────────────────────────────────────
    float _fogTimer;
    const float FogRevealRate = 0.5f;

    // ── Properties ────────────────────────────────────────────────────────────
    public bool  IsAlive       => _hp > 0f && gameObject.activeSelf;
    public float HP            => _hp;
    public float MaxHP         => Data != null ? Data.MaxHP : 1f;
    public float Faith         => _faith;
    public bool  IsConverting  => _isConverting;
    public bool  IsCarryingRelic => _isCarryingRelic;

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

        // Trebuchet: deploy / undeploy animation (blocks all other actions)
        if (Data?.Type == UnitType.Trebuchet && _trebuchetAnimating)
        {
            _trebuchetAnimTimer -= Time.deltaTime;
            if (_trebuchetAnimTimer <= 0f)
            {
                _trebuchetAnimating = false;
                _trebuchetDeployed  = !_trebuchetDeployed;
                UIManager.Instance?.ShowMessage(_trebuchetDeployed ? "Trebuchet deployed!" : "Trebuchet packed!");
            }
            var p0 = transform.position; p0.y = 0f; transform.position = p0;
            if (_healthBarGO != null && Camera.main != null)
                _healthBarGO.transform.rotation = Camera.main.transform.rotation;
            return;
        }

        // Monk: faith, heal, relic, conversion — skip main combat FSM
        if (Data != null && Data.Type == UnitType.Monk)
        {
            // Faith regeneration
            if (!IsAI)
            {
                _faith = Mathf.Min(MaxFaith, _faith + FaithRegenRate * Time.deltaTime);
            }

            // Heal nearby friendlies
            _healTimer -= Time.deltaTime;
            if (_healTimer <= 0f)
            {
                _healTimer = HealRate;
                if (!_isConverting) HealNearbyFriendlies();
            }

            // Relic pickup (player monks only)
            if (!IsAI && !_isCarryingRelic)
            {
                foreach (var relic in Relic.All)
                {
                    if (relic == null || relic.IsCollected) continue;
                    if (Vector3.Distance(transform.position, relic.transform.position) < 1.8f)
                    {
                        relic.Collect();
                        _isCarryingRelic = true;
                        UIManager.Instance?.ShowMessage("Relic collected! Bring it to the Monastery.");
                        break;
                    }
                }
            }
            else if (!IsAI && _isCarryingRelic)
            {
                // Deposit relic at Monastery
                var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
                foreach (var b in buildings)
                {
                    if (b == null || b.IsAI || !b.IsBuilt || b.Data?.Type != BuildingType.Monastery) continue;
                    if (Vector3.Distance(transform.position, b.transform.position) < 3.5f)
                    {
                        _isCarryingRelic = false;
                        // Find and deposit the carried relic
                        foreach (var r in Relic.All)
                        {
                            if (r != null && r.IsCollected) { r.Deposit(); break; }
                        }
                        break;
                    }
                }
            }

            // Conversion logic
            if (_isConverting)
            {
                if (_conversionTarget == null || !_conversionTarget.IsAlive)
                {
                    _isConverting = false;
                    _conversionTarget = null;
                }
                else
                {
                    float dist = Vector3.Distance(transform.position, _conversionTarget.transform.position);
                    if (dist > ConversionRange + 2f)
                    {
                        _isConverting = false;
                        UIManager.Instance?.ShowMessage("Conversion failed — target out of range.");
                        _conversionTarget = null;
                    }
                    else
                    {
                        // Face the target
                        Vector3 dir = (_conversionTarget.transform.position - transform.position); dir.y = 0f;
                        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);

                        _conversionTimer -= Time.deltaTime;
                        if (_conversionTimer <= 0f)
                            CompleteConversion();
                    }
                }
            }

            if (_state == State.CommandedMove)
                DoCommandedMove();
            else if (!_isConverting)
                _state = State.Idle;

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

        // Mangonel / Trebuchet: area-of-effect splash at target location
        if (Data != null && (Data.Type == UnitType.Mangonel || Data.Type == UnitType.Trebuchet))
        {
            Vector3 splashPos = targetUnit != null
                ? targetUnit.transform.position
                : targetBuilding != null ? targetBuilding.transform.position : transform.position;
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            float radius = Data.Type == UnitType.Trebuchet ? 4.5f : 3.5f;
            Projectile.SpawnAt(origin, splashPos, dmg, DamageType.Siege, splashRadius: radius, isAI: IsAI);
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

    public void TrebuchetToggleDeploy()
    {
        if (Data?.Type != UnitType.Trebuchet) return;
        if (_trebuchetAnimating) { UIManager.Instance?.ShowMessage("Trebuchet is already animating..."); return; }
        _trebuchetAnimating = true;
        _trebuchetAnimTimer = TrebuchetDeployTime;
        // Stop any movement or attack
        ClearPath();
        _state          = State.Idle;
        _targetUnit     = null;
        _targetBuilding = null;
    }

    public void CommandMoveTo(Vector3 pos)
    {
        // Deployed trebuchet cannot move — must pack first
        if (Data?.Type == UnitType.Trebuchet && _trebuchetDeployed)
        {
            UIManager.Instance?.ShowMessage("Pack the Trebuchet before moving!");
            return;
        }
        _moveTarget   = new Vector3(pos.x, 0f, pos.z);
        _targetUnit   = null;
        _targetBuilding = null;
        BuildPathTo(_moveTarget);
        _state        = State.CommandedMove;
    }

    public void CommandAttack(MilitaryUnit target)
    {
        // Monks convert instead of attacking
        if (Data?.Type == UnitType.Monk && !IsAI && target != null && target.IsAI)
        {
            StartConversion(target);
            return;
        }
        _targetUnit     = target;
        _targetBuilding = null;
        ClearPath();
        _state          = State.MovingToTarget;
    }

    public void StartConversion(MilitaryUnit target)
    {
        if (target == null || !target.IsAlive) return;
        if (_faith < ConversionFaithCost)
        {
            UIManager.Instance?.ShowMessage($"Not enough faith! ({(int)_faith}/{(int)ConversionFaithCost})");
            return;
        }
        _faith           -= ConversionFaithCost;
        _conversionTarget = target;
        _conversionTimer  = ConversionTime;
        _isConverting     = true;
        // Move close to target
        CommandMoveTo(target.transform.position);
    }

    void CompleteConversion()
    {
        if (_conversionTarget == null) return;
        var t = _conversionTarget;
        _isConverting     = false;
        _conversionTarget = null;

        // Switch sides: remove from AI, give to player
        AIController.Instance?.UnregisterUnit(t);
        t.IsAI = false;
        ResourceManager.Instance?.ReservePopulation(t.Data?.PopulationCost ?? 1);

        // Swap body color to player color
        var renderers = t.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.name == "Body" || r.gameObject.name == "SelectRing") continue;
            if (t.Data != null && r.gameObject.name == "Body") r.material.color = t.Data.UnitColor;
        }

        UIManager.Instance?.ShowMessage("Unit converted!");
        GameManager.Instance?.AddScore(50);
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
        // Flat sprite health bar: scale X and shift left so it shrinks from right
        float fullW = _healthBarFill.parent != null
            ? (_healthBarFill.parent.GetComponent<MeshRenderer>() == null ? 1.15f : 1.15f)
            : 1.15f;
        _healthBarFill.localScale = new Vector3(fullW * pct, _healthBarFill.localScale.y, 1f);
        _healthBarFill.localPosition = new Vector3(-fullW * (1f - pct) * 0.5f,
                                                    _healthBarFill.localPosition.y,
                                                    _healthBarFill.localPosition.z);
        var rend = _healthBarFill.GetComponent<Renderer>();
        if (rend) rend.material.color = Color.Lerp(Color.red, Color.green, pct);
    }

    float GetEffectiveAttack()
    {
        float baseAttack = Data?.Attack ?? 4f;
        if (!IsAI)
            baseAttack += ResearchManager.Instance?.GetAttackBonus(Data) ?? 0f;
        // Warchief aura: +5% attack for all friendly units within range
        baseAttack *= (1f + GetWarchiefAuraBonus(transform.position, IsAI));
        return baseAttack;
    }

    // ── Warchief aura (static helper) ─────────────────────────────────────────

    static float GetWarchiefAuraBonus(Vector3 pos, bool isAI)
    {
        foreach (var u in _all)
        {
            if (u == null || !u.IsAlive) continue;
            if (u.IsAI != isAI) continue;
            if (u.Data?.Type != UnitType.Warchief) continue;
            if (Vector3.Distance(pos, u.transform.position) <= WarchiefAuraRadius)
                return WarchiefAuraBonus;
        }
        return 0f;
    }

    float GetEffectiveAttackRange()
    {
        // Trebuchet can only attack when deployed
        if (Data?.Type == UnitType.Trebuchet)
            return _trebuchetDeployed ? Data.AttackRange : 0.2f;

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
        // Notify stats before destruction
        if (!IsAI)
            GameManager.Instance?.NotifyUnitLost();
        else
            GameManager.Instance?.NotifyEnemyKilled();

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

        // ── Pixel-art sprite quad ─────────────────────────────────────────────
        float spriteSize = (data.Type == UnitType.BatteringRam || data.Type == UnitType.Mangonel ||
                            data.Type == UnitType.Trebuchet) ? 2.2f : 1.4f;
        var tex = PixelArtSprites.UnitSprite(data.Type, isAI);
        var spriteGO = SpriteQuad.Create(tex, spriteSize, spriteSize, 0.06f, go.transform);
        spriteGO.name = "Body";
        var bodyRenderer = spriteGO.GetComponent<Renderer>();

        // Selection ring (player only) — flat quad glow
        GameObject selRing = null;
        if (!isAI)
        {
            selRing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            selRing.name = "SelectRing";
            selRing.transform.SetParent(go.transform);
            selRing.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            selRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            selRing.transform.localScale    = new Vector3(spriteSize * 1.3f, spriteSize * 1.3f, 1f);
            selRing.GetComponent<Renderer>().material.color = new Color(0.2f, 0.9f, 1f, 0.5f);
            Object.Destroy(selRing.GetComponent<Collider>());
            selRing.SetActive(false);
        }

        // Root collider (flat box for top-down raycasting)
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.1f, 0f);
        col.size   = new Vector3(spriteSize, 0.2f, spriteSize);

        // Health bar — flat sprite quads
        var hbGO = new GameObject("HealthBar");
        hbGO.transform.SetParent(go.transform);
        hbGO.transform.localPosition = Vector3.zero;

        float barW = spriteSize * 0.85f;
        float barH = 0.18f;
        float barY = 0.12f;

        var bgTex = new Texture2D(1, 1); bgTex.SetPixel(0,0,new Color(0.1f,0.1f,0.1f,0.9f)); bgTex.Apply();
        var fillTex = new Texture2D(1, 1); fillTex.SetPixel(0,0,Color.green); fillTex.Apply();

        var hbBg   = SpriteQuad.Create(bgTex,   barW,       barH,            barY,          hbGO.transform);
        var hbFill = SpriteQuad.Create(fillTex, barW-0.04f, barH * 0.6f, barY + 0.001f, hbGO.transform);
        hbBg.name   = "HPBarBG";
        hbFill.name = "HPBarFill";

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

    // ── Ironbreaker geometry ──────────────────────────────────────────────────

    static void BuildIronbreakerMesh(GameObject go, UnitData data, bool isAI)
    {
        Color armor = isAI ? new Color(0.55f, 0.12f, 0.12f) : data.UnitColor;
        Color iron  = new Color(0.42f, 0.42f, 0.50f);

        // Heavy oversized body
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(go.transform);
        body.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        body.transform.localScale    = new Vector3(0.52f, 0.52f, 0.52f);
        body.GetComponent<Renderer>().material.color = armor;
        Object.Destroy(body.GetComponent<Collider>());

        // Broad shoulders (pauldrons)
        for (int side = -1; side <= 1; side += 2)
        {
            var shoulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shoulder.transform.SetParent(go.transform);
            shoulder.transform.localPosition = new Vector3(side * 0.42f, 0.95f, 0f);
            shoulder.transform.localScale    = new Vector3(0.28f, 0.22f, 0.28f);
            shoulder.GetComponent<Renderer>().material.color = iron;
            Object.Destroy(shoulder.GetComponent<Collider>());
        }

        // Head — war helm
        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.transform.SetParent(go.transform);
        head.transform.localPosition = new Vector3(0f, 1.32f, 0f);
        head.transform.localScale    = new Vector3(0.30f, 0.26f, 0.30f);
        head.GetComponent<Renderer>().material.color = iron;
        Object.Destroy(head.GetComponent<Collider>());

        // Warhammer
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.transform.SetParent(go.transform);
        handle.transform.localPosition = new Vector3(0.38f, 0.7f, 0.1f);
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
        handle.transform.localScale    = new Vector3(0.07f, 0.45f, 0.07f);
        handle.GetComponent<Renderer>().material.color = new Color(0.38f, 0.22f, 0.08f);
        Object.Destroy(handle.GetComponent<Collider>());

        var hammerHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hammerHead.transform.SetParent(go.transform);
        hammerHead.transform.localPosition = new Vector3(0.38f, 1.18f, 0.1f);
        hammerHead.transform.localScale    = new Vector3(0.20f, 0.14f, 0.16f);
        hammerHead.GetComponent<Renderer>().material.color = iron;
        Object.Destroy(hammerHead.GetComponent<Collider>());
    }

    // ── Warchief geometry ─────────────────────────────────────────────────────

    static void BuildWarchiefMesh(GameObject go, UnitData data, bool isAI)
    {
        Color skin  = isAI ? new Color(0.55f, 0.12f, 0.12f) : data.UnitColor;
        Color bone  = new Color(0.88f, 0.86f, 0.72f);
        Color dark  = new Color(0.25f, 0.18f, 0.10f);

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(go.transform);
        body.transform.localPosition = new Vector3(0f, 0.60f, 0f);
        body.transform.localScale    = new Vector3(0.44f, 0.46f, 0.44f);
        body.GetComponent<Renderer>().material.color = skin;
        Object.Destroy(body.GetComponent<Collider>());

        // Head with horned helmet
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(go.transform);
        head.transform.localPosition = new Vector3(0f, 1.22f, 0f);
        head.transform.localScale    = new Vector3(0.26f, 0.26f, 0.26f);
        head.GetComponent<Renderer>().material.color = isAI ? new Color(0.55f, 0.12f, 0.12f) : new Color(0.42f, 0.58f, 0.22f);
        Object.Destroy(head.GetComponent<Collider>());

        // Horns
        for (int side = -1; side <= 1; side += 2)
        {
            var horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.transform.SetParent(go.transform);
            horn.transform.localPosition = new Vector3(side * 0.16f, 1.40f, 0f);
            horn.transform.localRotation = Quaternion.Euler(0f, 0f, side * 30f);
            horn.transform.localScale    = new Vector3(0.05f, 0.22f, 0.05f);
            horn.GetComponent<Renderer>().material.color = bone;
            Object.Destroy(horn.GetComponent<Collider>());
        }

        // War axe
        var haft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        haft.transform.SetParent(go.transform);
        haft.transform.localPosition = new Vector3(0.32f, 0.65f, 0.1f);
        haft.transform.localRotation = Quaternion.Euler(0f, 0f, 5f);
        haft.transform.localScale    = new Vector3(0.06f, 0.52f, 0.06f);
        haft.GetComponent<Renderer>().material.color = dark;
        Object.Destroy(haft.GetComponent<Collider>());

        var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.transform.SetParent(go.transform);
        blade.transform.localPosition = new Vector3(0.32f, 1.20f, 0.08f);
        blade.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
        blade.transform.localScale    = new Vector3(0.24f, 0.12f, 0.06f);
        blade.GetComponent<Renderer>().material.color = new Color(0.62f, 0.62f, 0.68f);
        Object.Destroy(blade.GetComponent<Collider>());
    }

    // ── Siege engine geometry ─────────────────────────────────────────────────

    static void BuildSiegeMesh(GameObject go, UnitData data, bool isAI)
    {
        Color wood  = new Color(0.45f, 0.30f, 0.14f);
        Color iron  = new Color(0.42f, 0.42f, 0.46f);

        if (data.Type == UnitType.Trebuchet)
        {
            // Heavy wheeled trebuchet frame
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.transform.SetParent(go.transform);
            frame.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            frame.transform.localScale    = new Vector3(1.1f, 0.55f, 1.6f);
            frame.GetComponent<Renderer>().material.color = wood;
            Object.Destroy(frame.GetComponent<Collider>());

            // Support legs (A-frame sides)
            for (int side = -1; side <= 1; side += 2)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.transform.SetParent(go.transform);
                leg.transform.localPosition = new Vector3(side * 0.42f, 0.60f, 0f);
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, side * 15f);
                leg.transform.localScale    = new Vector3(0.10f, 0.85f, 0.20f);
                leg.GetComponent<Renderer>().material.color = wood * 0.9f;
                Object.Destroy(leg.GetComponent<Collider>());
            }

            // Long throwing arm (raised, ready to fire)
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.transform.SetParent(go.transform);
            arm.transform.localPosition = new Vector3(0f, 1.05f, -0.20f);
            arm.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            arm.transform.localScale    = new Vector3(0.10f, 0.80f, 0.10f);
            arm.GetComponent<Renderer>().material.color = isAI ? new Color(0.6f, 0.2f, 0.2f) : wood * 1.1f;
            Object.Destroy(arm.GetComponent<Collider>());

            // Counterweight (heavy iron box at base of arm)
            var cw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cw.transform.SetParent(go.transform);
            cw.transform.localPosition = new Vector3(0f, 0.80f, 0.42f);
            cw.transform.localScale    = new Vector3(0.38f, 0.38f, 0.38f);
            cw.GetComponent<Renderer>().material.color = iron;
            Object.Destroy(cw.GetComponent<Collider>());

            // Sling cup (tip of arm)
            var cup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cup.transform.SetParent(go.transform);
            cup.transform.localPosition = new Vector3(0f, 1.85f, -0.65f);
            cup.transform.localScale    = new Vector3(0.22f, 0.22f, 0.22f);
            cup.GetComponent<Renderer>().material.color = iron;
            Object.Destroy(cup.GetComponent<Collider>());

            // Wheels (4, larger than mangonel)
            float[] wx3 = { -0.48f, 0.48f };
            float[] wz3 = { -0.65f, 0.65f };
            foreach (float x in wx3)
                foreach (float z in wz3)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.transform.SetParent(go.transform);
                    wheel.transform.localPosition = new Vector3(x, 0.18f, z);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    wheel.transform.localScale    = new Vector3(0.26f, 0.09f, 0.26f);
                    wheel.GetComponent<Renderer>().material.color = wood * 0.8f;
                    Object.Destroy(wheel.GetComponent<Collider>());
                }
            return; // skip Mangonel/Ram block
        }

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
