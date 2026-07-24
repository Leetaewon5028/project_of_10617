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

    [Header("Gaster Blaster Skill (time based)")]
    [SerializeField] GasterBlaster blasterPrefab;
    [SerializeField] Transform blasterSpawnPoint;
    [SerializeField] float blasterCooldown = 7f;
    [SerializeField] float blasterSpawnDistance = 6f;
    float blasterTimer;

    [Header("Dodge (Miss)")]
    public int maxDodges = 23;
    int dodgeCount;
    [SerializeField] float dodgeBackDistance = 3f;
    [SerializeField] float dodgeDuration = 0.4f;
    [SerializeField] float arenaHalfWidth = 9.5f;

    [SerializeField] Slider bossbar;
    [SerializeField] Image bossbarFill;
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
        health.OnDeath(OnDeath);

        playerGravity = player.GetComponent<PlayerGravityStatus>();
        if (playerGravity == null)
            playerGravity = player.gameObject.AddComponent<PlayerGravityStatus>();

        gravityTimer = gravityCooldown;
    }

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

    void OnDeath(EntityHealth.Context ctx)
    {
        if (bossbarFill != null)
            bossbarFill.color = Color.red;
    }

    void Dodge()
    {
        animator.SetTrigger("Miss");

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
    void GravityManipulation()
    {
        Attack(0.5f, defaultAttack, transform.position);

        float facing = player.transform.position.x > transform.position.x ? 1 : -1;
        Vector2 launchDir = Random.value < 0.5f ? Vector2.up : new Vector2(facing, 0);
        playerGravity.ApplyGravityLaunch(launchDir, gravityLaunchSpeed);
    }

    void SummonGasterBlaster()
    {
        if (blasterPrefab == null)
            return;

        float facing = player.transform.position.x > transform.position.x ? 1 : -1;

        float spawnX = Mathf.Clamp(transform.position.x + facing * blasterSpawnDistance, -arenaHalfWidth, arenaHalfWidth);

        Vector2 spawnPos = blasterSpawnPoint != null
            ? (Vector2)blasterSpawnPoint.position
            : new Vector2(spawnX, transform.position.y);

        GasterBlaster blaster = Instantiate(blasterPrefab, spawnPos, Quaternion.identity);
        blaster.Init(player.transform, stat.GetResultValue("attackDamage"), health, enemyMask);
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
