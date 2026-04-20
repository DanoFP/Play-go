using UnityEngine;

/// <summary>
/// A simple homing projectile spawned by ranged MilitaryUnits.
/// Flies toward its target and applies damage on arrival.
/// Mangonel variant flies to a ground position and splashes area damage.
/// </summary>
public class Projectile : MonoBehaviour
{
    const float Speed    = 16f;
    const float HitDist  = 0.6f;
    const float Lifetime = 8f;

    float       _damage;
    DamageType  _dmgType;
    float       _life;

    MilitaryUnit _targetUnit;
    Building     _targetBuilding;

    // ── Area-of-effect (Mangonel) ─────────────────────────────────────────────
    bool    _isAoe;
    Vector3 _aoeTarget;
    float   _splashRadius;
    bool    _isAI;

    void Update()
    {
        _life += Time.deltaTime;
        if (_life > Lifetime) { Destroy(gameObject); return; }

        Vector3 targetPos;

        if (_isAoe)
        {
            targetPos = _aoeTarget;
        }
        else if (_targetUnit != null && _targetUnit.IsAlive)
            targetPos = _targetUnit.transform.position + Vector3.up * 0.6f;
        else if (_targetBuilding != null && _targetBuilding.gameObject.activeSelf)
            targetPos = _targetBuilding.transform.position + Vector3.up * 1f;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Arc motion for Mangonel (parabolic Y offset)
        Vector3 dir = (targetPos - transform.position).normalized;
        float dist  = Vector3.Distance(transform.position, targetPos);
        if (_isAoe && dist > HitDist)
        {
            float arc = Mathf.Sin(_life / Lifetime * Mathf.PI) * 4f;
            transform.position += (dir + Vector3.up * arc) * Speed * Time.deltaTime;
        }
        else
        {
            transform.position += dir * Speed * Time.deltaTime;
        }

        if (Vector3.Distance(transform.position, targetPos) < (_isAoe ? 1.2f : HitDist))
            Impact();
    }

    void Impact()
    {
        if (_isAoe)
        {
            // Splash all enemy units and buildings within radius
            foreach (var u in MilitaryUnit.AllUnits)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.IsAI == _isAI) continue; // only damage enemies
                if (Vector3.Distance(_aoeTarget, u.transform.position) <= _splashRadius)
                    u.TakeDamage(_damage, DamageType.Siege);
            }

            var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in buildings)
            {
                if (b == null || !b.IsBuilt) continue;
                if (b.IsAI == _isAI) continue;
                if (Vector3.Distance(_aoeTarget, b.transform.position) <= _splashRadius)
                    b.TakeDamage(_damage * 0.5f);
            }
        }
        else if (_targetUnit != null && _targetUnit.IsAlive)
            _targetUnit.TakeDamage(_damage, _dmgType);
        else if (_targetBuilding != null && _targetBuilding.gameObject.activeSelf)
            _targetBuilding.TakeDamage(_damage);

        Destroy(gameObject);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    static GameObject Build(Vector3 pos, bool isAoe = false)
    {
        var go = new GameObject("Projectile");
        go.transform.position = pos;

        var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mesh.transform.SetParent(go.transform);
        mesh.transform.localPosition = Vector3.zero;
        // Mangonel rocks are bigger and darker
        float scale = isAoe ? 0.28f : 0.14f;
        mesh.transform.localScale = new Vector3(scale, scale, scale);
        mesh.GetComponent<Renderer>().material.color = isAoe
            ? new Color(0.30f, 0.28f, 0.25f)
            : new Color(0.2f, 0.15f, 0.05f);
        Destroy(mesh.GetComponent<Collider>());
        return go;
    }

    public static void SpawnAt(Vector3 pos, MilitaryUnit target, float damage, DamageType type)
    {
        var go = Build(pos);
        var p = go.AddComponent<Projectile>();
        p._targetUnit = target;
        p._damage = damage;
        p._dmgType = type;
    }

    public static void SpawnAt(Vector3 pos, Building target, float damage, DamageType type)
    {
        var go = Build(pos);
        var p = go.AddComponent<Projectile>();
        p._targetBuilding = target;
        p._damage = damage;
        p._dmgType = type;
    }

    /// <summary>Area-of-effect shot for Mangonel. Damages all enemies within splashRadius at target point.</summary>
    public static void SpawnAt(Vector3 pos, Vector3 groundTarget, float damage, DamageType type,
                                float splashRadius, bool isAI)
    {
        var go = Build(pos, isAoe: true);
        var p  = go.AddComponent<Projectile>();
        p._isAoe       = true;
        p._aoeTarget   = new Vector3(groundTarget.x, 0f, groundTarget.z);
        p._splashRadius = splashRadius;
        p._damage      = damage;
        p._dmgType     = type;
        p._isAI        = isAI;
    }
}
