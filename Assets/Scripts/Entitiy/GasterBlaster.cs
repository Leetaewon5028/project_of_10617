using System.Collections;
using UnityEngine;

public class GasterBlaster : MonoBehaviour
{
    [SerializeField] float chargeTime = 0.6666667f;
    [SerializeField] float beamDuration = 1.5f;
    [SerializeField] float damageTickInterval = 1f;
    [SerializeField] Vector2 beamSize = new Vector2(22f, 1.2f);
    [SerializeField] SpriteRenderer beamVisual;
    [SerializeField] Transform muzzlePoint;
    [SerializeField] AudioClip fireSound;

    Transform target;
    Vector2 fireDir = Vector2.right;
    float damage;
    EntityHealth attacker;
    LayerMask targetMask;
    SpriteRenderer sprite;
    AudioSource audioSource;
    bool fired;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    public void Init(Transform playerTarget, float dmg, EntityHealth attackerHealth, LayerMask mask)
    {
        target = playerTarget;
        damage = dmg;
        attacker = attackerHealth;
        targetMask = mask;

        UpdateFacing();
        StartCoroutine(FireRoutine());
    }

    void Update()
    {
        if (!fired)
            UpdateFacing();
    }

    void UpdateFacing()
    {
        fireDir = target.position.x > transform.position.x ? Vector2.right : Vector2.left;

        sprite.flipX = fireDir.x < 0;
    }

    Vector2 MuzzlePosition()
    {
        if (muzzlePoint == null)
            return transform.position;

        Vector2 localOffset = muzzlePoint.localPosition;
        Vector2 facingOffset = new Vector2(sprite.flipX ? -localOffset.x : localOffset.x, localOffset.y);
        return (Vector2)transform.position + facingOffset;
    }

    IEnumerator FireRoutine()
    {
        yield return new WaitForSeconds(chargeTime);
        fired = true;

        if (audioSource != null && fireSound != null)
            audioSource.PlayOneShot(fireSound);

        float angle = Mathf.Atan2(fireDir.y, fireDir.x) * Mathf.Rad2Deg;
        Vector2 muzzlePos = MuzzlePosition();

        if (beamVisual != null)
        {
            Vector2 nativeSize = beamVisual.sprite.bounds.size;
            beamVisual.transform.position = muzzlePos;
            beamVisual.transform.localScale = new Vector3(beamSize.x / nativeSize.x, beamSize.y / nativeSize.y, 1);
            beamVisual.transform.rotation = Quaternion.Euler(0, 0, angle);
            beamVisual.enabled = true;
        }

        Vector2 center = muzzlePos;

        debugActive = true;
        debugCenter = center;
        debugAngle = angle;

        float elapsed = 0f;
        while (elapsed < beamDuration)
        {
            var hits = Physics2D.OverlapBoxAll(center, beamSize, angle, targetMask);
            Debug.Log($"[GasterBlaster] tick: hits={hits.Length} center={center} size={beamSize} angle={angle} mask={targetMask.value}");
            foreach (var hit in hits)
            {
                Debug.Log($"[GasterBlaster]   hit: {hit.gameObject.name} layer={hit.gameObject.layer} hasEntityHealth={hit.GetComponent<EntityHealth>() != null}");
                EntityHealth hp = hit.GetComponent<EntityHealth>();
                if (hp != null)
                    hp.GetDamage(damage, attacker);
            }

            yield return new WaitForSeconds(damageTickInterval);
            elapsed += damageTickInterval;
        }

        debugActive = false;
        Destroy(gameObject);
    }

    bool debugActive;
    Vector2 debugCenter;
    float debugAngle;

    void OnDrawGizmos()
    {
        if (!debugActive)
            return;

        Gizmos.color = Color.red;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(debugCenter, Quaternion.Euler(0, 0, debugAngle), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(beamSize.x, beamSize.y, 0.1f));
        Gizmos.matrix = oldMatrix;
    }

    void OnDrawGizmosSelected()
    {
        if (muzzlePoint == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(muzzlePoint.position, 0.1f);
    }
}
