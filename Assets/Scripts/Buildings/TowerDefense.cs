using UnityEngine;

/// <summary>
/// Attached to Tower buildings on build-complete.
/// Automatically finds the nearest AI unit in range and fires projectiles at it.
/// </summary>
public class TowerDefense : MonoBehaviour
{
    const float BaseRange  = 10f;
    const float BaseAttack = 10f;
    const float AttackSpeed  = 0.6f; // attacks per second

    float AttackRange  => BaseRange  + (ResearchManager.Instance?.GetTowerRangeBonus()  ?? 0f);
    float AttackDamage => BaseAttack + (ResearchManager.Instance?.GetTowerAttackBonus() ?? 0f);

    MilitaryUnit _target;
    float        _cooldown;
    float        _searchTimer;
    const float  SearchInterval = 0.5f;

    // Offset so projectiles appear to fire from the top of the tower
    static readonly Vector3 FireOffset = new Vector3(0f, 3f, 0f);

    void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        // Cooldown ticks down every frame
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;

        // Periodically search for a target
        _searchTimer -= Time.deltaTime;
        if (_searchTimer <= 0f)
        {
            _searchTimer = SearchInterval;
            _target = FindTarget();
        }

        // Fire if ready and target is valid
        if (_cooldown <= 0f && _target != null && _target.IsAlive)
        {
            float dist = Vector3.Distance(transform.position, _target.transform.position);
            if (dist <= AttackRange)
            {
                FireAt(_target);
                _cooldown = 1f / AttackSpeed;
            }
            else
            {
                _target = null; // walked out of range
            }
        }
    }

    MilitaryUnit FindTarget()
    {
        MilitaryUnit closest  = null;
        float        bestDist = AttackRange + 1f;

        foreach (var u in MilitaryUnit.AllUnits)
        {
            if (u == null || !u.IsAlive || !u.IsAI) continue;
            float d = Vector3.Distance(transform.position, u.transform.position);
            if (d < bestDist) { bestDist = d; closest = u; }
        }

        return closest;
    }

    void FireAt(MilitaryUnit target)
    {
        Vector3 origin = transform.position + FireOffset;
        Projectile.SpawnAt(origin, target, AttackDamage, DamageType.Pierce);
    }
}
