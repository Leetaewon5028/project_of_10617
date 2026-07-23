using UnityEngine;
using UnityEngine.UI;

public class Boss : Enemy
{
    [SerializeField]
    PlayerController player;
    public float attackDist = 1.5f;
    [SerializeField] AttackRange defaultAttack;

    public float retreatTime = 0.6f;
    float retreatTimer;

    [Header("Gravity Manipulation (close range)")]
    [SerializeField] float gravityLaunchSpeed = 12f;
    [SerializeField] float gravityCooldown = 5f;
    float gravityTimer;

    [Header("Bone Skill (time based)")]
    [SerializeField] Bone bonePrefab;
    [SerializeField] Transform boneSpawnPoint;
    [SerializeField] float boneCooldown = 5f;
    float boneTimer;

    [Header("Gaster Blaster Skill (time based)")]
    [SerializeField] GasterBlaster blasterPrefab;
    [SerializeField] Transform blasterSpawnPoint;
    [SerializeField] float blasterCooldown = 7f;
    float blasterTimer;

    [Header("Dodge (Miss)")]
    public int maxDodges = 23;
    int dodgeCount;
    [SerializeField] float dodgeBackDistance = 3f;
    [SerializeField] float dodgeDuration = 0.4f;
    [SerializeField] float arenaHalfWidth = 9.5f;

    [SerializeField] Slider bossbar;
    Animator animator;
    PlayerGravityStatus playerGravity;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    protected override void Start()
    {
        base.Start();

        health.AddDamageInterceptor(OnIncomingHit);

        playerGravity = player.GetComponent<PlayerGravityStatus>();
        if (playerGravity == null)
            playerGravity = player.gameObject.AddComponent<PlayerGravityStatus>();

        gravityTimer = gravityCooldown;
    }

    // Sans-style dodge: the first `maxDodges` hits are evaded outright, the
    // one after that is guaranteed lethal regardless of remaining health.
    bool OnIncomingHit(float incomingDamage, EntityHealth attacker)
    {
        if (dodgeCount < maxDodges)
        {
            dodgeCount++;
            Dodge();
            return true;
        }

        health.ReduceHealth(health.health);
        return true;
    }

    void Dodge()
    {
        animator.SetTrigger("Miss");

        // Clamp instead of just teleporting: an unclamped jump can shove the
        // boss past the arena's boundary walls, leaving it stuck outside
        // since it moves purely via Translate (walls don't block that).
        float away = player.transform.position.x > transform.position.x ? -1 : 1;
        float targetX = Mathf.Clamp(transform.position.x + away * dodgeBackDistance, -arenaHalfWidth, arenaHalfWidth);
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        retreatTimer = dodgeDuration;
    }

    protected override void MobUpdate()
    {
        bossbar.value = health.health / health.maxHealth;

        if (retreatTimer > 0)
            retreatTimer -= Time.deltaTime;

        boneTimer += Time.deltaTime;
        blasterTimer += Time.deltaTime;
        gravityTimer += Time.deltaTime;

        float dist = Vector2.Distance(player.transform.position, transform.position);
        bool moving = false;

        if (retreatTimer > 0)
        {
            float away = player.transform.position.x > transform.position.x ? -1 : 1;
            Move(Vector2.right * away);
            moving = true;
        }
        else if (dist <= attackDist && atkCool <= 0 && gravityTimer >= gravityCooldown)
        {
            gravityTimer = 0;
            retreatTimer = retreatTime;
            animator.SetTrigger("Attack");
            GravityManipulation();
        }
        else if (boneTimer >= boneCooldown)
        {
            boneTimer = 0;
            animator.SetTrigger("Attack");
            SummonBone();
        }
        else if (blasterTimer >= blasterCooldown)
        {
            blasterTimer = 0;
            animator.SetTrigger("Attack");
            SummonGasterBlaster();
        }
        else if (dist > attackDist)
        {
            Chase(player.transform);
            moving = true;
        }

        SetFacing(player.transform.position.x > transform.position.x ? 1 : -1);
        animator.SetBool("isMoving", moving);
    }

    // Replaces the old punch: still lands the same melee hitbox, but instead
    // of a plain damage hit it flips gravity on the player, launching them
    // upward or off in whichever direction Sans is currently facing.
    void GravityManipulation()
    {
        Attack(0.5f, defaultAttack, transform.position);

        float facing = player.transform.position.x > transform.position.x ? 1 : -1;
        Vector2 launchDir = Random.value < 0.5f ? Vector2.up : new Vector2(facing, 0);
        playerGravity.ApplyGravityLaunch(launchDir, gravityLaunchSpeed);
    }

    void SummonBone()
    {
        if (bonePrefab == null)
            return;

        Vector2 spawnPos = boneSpawnPoint != null
            ? (Vector2)boneSpawnPoint.position
            : (Vector2)player.transform.position + Vector2.down * 3f;

        Bone bone = Instantiate(bonePrefab, spawnPos, Quaternion.identity);
        bone.Init(Vector2.up, stat.GetResultValue("attackDamage"), health, enemyMask);
    }

    void SummonGasterBlaster()
    {
        if (blasterPrefab == null)
            return;

        Vector2 spawnPos = blasterSpawnPoint != null
            ? (Vector2)blasterSpawnPoint.position
            : (Vector2)transform.position + Vector2.right * direction * 3f;

        GasterBlaster blaster = Instantiate(blasterPrefab, spawnPos, Quaternion.identity);
        blaster.Init(new Vector2(direction, 0), stat.GetResultValue("attackDamage"), health, enemyMask);
    }

    void SetFacing(float dir)
    {
        direction = dir;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;
    }

    protected override void DrawGizmos()
    {
        Draw(defaultAttack);
    }
}
