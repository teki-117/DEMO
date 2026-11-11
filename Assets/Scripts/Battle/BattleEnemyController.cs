using UnityEngine;

// 能被主角攻击的对象：小怪和 Boss 都实现它。
public interface IBattleDamageable
{
    bool IsDead { get; }

    // sourcePosition：攻击来源的位置，用来决定击退方向。
    void TakeDamage(int damage, Vector2 sourcePosition);
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BattleEnemyController : MonoBehaviour, IBattleDamageable
{
    private enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Windup,
        Attack,
        Dead
    }

    [Header("场景引用")]
    [SerializeField] private BattlePlayerController player;
    [SerializeField] private LayerMask groundLayer;

    [Header("生命")]
    [Min(1)]
    [SerializeField] private int maxHP = 30;

    [Header("巡逻")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float patrolHalfWidth = 2f;

    [Header("发现和追击")]
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float detectionHeight = 1.5f;
    [SerializeField] private float chaseSpeed = 2.5f;

    [Header("攻击")]
    [SerializeField] private int attackPower = 12;
    [SerializeField] private float attackRange = 1.15f;
    [SerializeField] private float attackHeight = 0.9f;
    [SerializeField] private float attackInterval = 1.2f;

    [Header("攻击前摇")]
    [Min(0f)]
    [SerializeField] private float windupDuration = 0.45f;
    [SerializeField] private Color windupColor = new Color(1f, 0.45f, 0.1f);

    [Header("受击表现")]
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private float knockbackSpeed = 5f;
    [Min(0f)]
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("奖励")]
    [Min(0)]
    [SerializeField] private int moneyReward = 10;
    [Min(0)]
    [SerializeField] private int expReward = 5;
    [Tooltip("勾选后，这个敌人被击败时掉出 Boss 房钥匙。")]
    [SerializeField] private bool dropsKey;

    [Header("动画（可选）")]
    [Tooltip("素材本身朝左时勾选。")]
    [SerializeField] private bool spriteFacesLeft;

    [Tooltip("参数：Speed（float），Windup、Dead（bool），Attack、Hurt（trigger）。没有 Dead 参数时死亡直接消失。")]
    [SerializeField] private Animator animator;
    [Tooltip("有 Animator 时，死亡后等多久再消失，用来播死亡动画。")]
    [Min(0f)]
    [SerializeField] private float deathAnimDuration = 0.6f;

    [Header("特效 Prefab（可选）")]
    [SerializeField] private GameObject attackEffect;
    [SerializeField] private GameObject deathEffect;

    [Header("音效（可选）")]
    [SerializeField] private AudioClip attackSfx;
    [SerializeField] private AudioClip deathSfx;

    [Header("运行状态（观察用）")]
    [SerializeField] private int currentHP;
    [SerializeField] private bool isDead;
#pragma warning disable CS0414 // 只在 Inspector 里观察，代码不读取
    [SerializeField] private EnemyState currentState;
#pragma warning restore CS0414

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private BoxCollider2D bodyCollider;

    private Color originalColor;
    private float flashEndTime;
    private float nextAttackTime;

    private bool isWindingUp;
    private float windupEndTime;
    private float knockbackEndTime;
    private bool facingLeft;

    private float patrolCenterX;
    private int patrolDirection = 1;

    public bool IsDead => isDead;
    public int MoneyReward => moneyReward;
    public int ExpReward => expReward;
    public bool DropsKey => dropsKey;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();

        // 没拖主角引用时自动找场景里的主角，加敌人不用逐个拖。
        if (player == null)
            player = FindAnyObjectByType<BattlePlayerController>();

        originalColor = spriteRenderer.color;
        currentHP = maxHP;
        isDead = false;

        // 以敌人在场景里的初始位置作为巡逻中心。
        patrolCenterX = transform.position.x;
        currentState = EnemyState.Patrol;
    }

    private void Update()
    {
        if (isDead)
            return;

        // 受击白闪优先，其次是前摇变色。
        if (Time.time < flashEndTime)
            spriteRenderer.color = Color.white;
        else if (isWindingUp)
            spriteRenderer.color = windupColor;
        else
            spriteRenderer.color = originalColor;

        if (animator != null)
        {
            BattleSceneController.AnimFloat(animator, "Speed", Mathf.Abs(rb.linearVelocity.x));
            BattleSceneController.AnimBool(animator, "Windup", isWindingUp);
        }
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (player != null && player.IsDefeated)
        {
            isWindingUp = false;
            currentState = EnemyState.Idle;
            SetHorizontalSpeed(0f);
            return;
        }

        // 击退期间保留被打飞的速度，不执行 AI。
        if (Time.time < knockbackEndTime)
            return;

        if (isWindingUp)
        {
            UpdateWindup();
            return;
        }

        if (CanSeePlayer())
        {
            ChaseOrAttack();
        }
        else
        {
            Patrol();
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector2 difference =
            player.transform.position - transform.position;

        if (Mathf.Abs(difference.x) > detectionRange ||
            Mathf.Abs(difference.y) > detectionHeight)
        {
            return false;
        }

        // 地面或墙壁挡住视线时，不隔着障碍追击、攻击。
        RaycastHit2D obstacle = Physics2D.Linecast(
            bodyCollider.bounds.center,
            player.transform.position,
            groundLayer
        );

        return obstacle.collider == null;
    }

    private bool IsPlayerInAttackRange()
    {
        if (player == null)
            return false;

        Vector2 difference =
            player.transform.position - transform.position;

        return Mathf.Abs(difference.x) <= attackRange &&
               Mathf.Abs(difference.y) <= attackHeight;
    }

    private void Patrol()
    {
        currentState = EnemyState.Patrol;

        float leftLimit = patrolCenterX - patrolHalfWidth;
        float rightLimit = patrolCenterX + patrolHalfWidth;

        if (transform.position.x <= leftLimit)
            patrolDirection = 1;
        else if (transform.position.x >= rightLimit)
            patrolDirection = -1;

        // 遇到墙壁或地面边缘就转向。
        if (!CanMove(patrolDirection))
            patrolDirection *= -1;

        FaceDirection(patrolDirection);

        float speed = CanMove(patrolDirection)
            ? patrolDirection * patrolSpeed
            : 0f;

        SetHorizontalSpeed(speed);
    }

    private void ChaseOrAttack()
    {
        Vector2 difference =
            player.transform.position - transform.position;

        int direction = difference.x >= 0f ? 1 : -1;
        FaceDirection(direction);

        if (Mathf.Abs(difference.x) <= attackRange)
        {
            // 已接近主角，停下，避免一直顶着主角走。
            SetHorizontalSpeed(0f);

            if (Mathf.Abs(difference.y) <= attackHeight)
            {
                currentState = EnemyState.Attack;

                if (Time.time >= nextAttackTime)
                {
                    // 先进入前摇，前摇结束才真正出手，给玩家躲开的时间。
                    isWindingUp = true;
                    windupEndTime = Time.time + windupDuration;
                    currentState = EnemyState.Windup;
                }
            }
            else
            {
                // 主角跳到头顶时等待，不隔空向上攻击。
                currentState = EnemyState.Chase;
            }

            return;
        }

        currentState = EnemyState.Chase;

        float speed = CanMove(direction)
            ? direction * chaseSpeed
            : 0f;

        SetHorizontalSpeed(speed);
    }

    private void UpdateWindup()
    {
        currentState = EnemyState.Windup;
        SetHorizontalSpeed(0f);

        if (Time.time < windupEndTime)
            return;

        isWindingUp = false;
        nextAttackTime = Time.time + attackInterval;
        currentState = EnemyState.Attack;

        BattleSceneController.AnimTrigger(animator, "Attack");
        BattleSceneController.SpawnEffect(
            attackEffect,
            bodyCollider.bounds.center + Vector3.right * (facingLeft ? -attackRange * 0.5f : attackRange * 0.5f),
            facingLeft);
        BattleSceneController.PlaySfx(attackSfx);

        // 出手瞬间再判一次距离：玩家在前摇期间躲开就打空。
        if (IsPlayerInAttackRange())
        {
            player.ReceiveAttack(attackPower, transform.position);
        }
    }

    private bool CanMove(int direction)
    {
        Bounds bounds = bodyCollider.bounds;

        // 水平探测前方是否有墙。
        RaycastHit2D wall = Physics2D.Raycast(
            bounds.center,
            Vector2.right * direction,
            bounds.extents.x + 0.15f,
            groundLayer
        );

        // 在身体前方、靠近脚底的位置向下探测地面。
        Vector2 footAhead = new Vector2(
            bounds.center.x + direction * (bounds.extents.x + 0.15f),
            bounds.min.y + 0.15f
        );

        RaycastHit2D floor = Physics2D.Raycast(
            footAhead,
            Vector2.down,
            0.4f,
            groundLayer
        );

        return wall.collider == null && floor.collider != null;
    }

    private void SetHorizontalSpeed(float speed)
    {
        // 保留重力与落地所需的垂直速度。
        rb.linearVelocity = new Vector2(
            speed,
            rb.linearVelocity.y
        );
    }

    private void FaceDirection(int direction)
    {
        facingLeft = direction < 0;
        spriteRenderer.flipX = facingLeft != spriteFacesLeft;
    }

    // sourcePosition：攻击来源的位置，用来决定击退方向。
    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (isDead || damage <= 0)
            return;

        currentHP = Mathf.Max(0, currentHP - damage);

        if (BattleSceneController.Current != null)
            BattleSceneController.Current.OnEnemyHit(bodyCollider.bounds.center, damage, false);

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        flashEndTime = Time.time + hitFlashDuration;
        spriteRenderer.color = Color.white;
        BattleSceneController.AnimTrigger(animator, "Hurt");

        // 朝远离攻击来源的方向击退。
        float direction =
            transform.position.x >= sourcePosition.x ? 1f : -1f;

        rb.linearVelocity = new Vector2(
            direction * knockbackSpeed,
            rb.linearVelocity.y
        );

        knockbackEndTime = Time.time + knockbackDuration;
    }

    private void Die()
    {
        isDead = true;
        isWindingUp = false;
        currentState = EnemyState.Dead;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        Debug.Log($"{name} 已被击败。", this);

        // 奖励只交给场景累计，真正发放在战斗结算时。
        if (BattleSceneController.Current != null)
        {
            BattleSceneController.Current.ReportEnemyDefeated(this);
        }

        BattleSceneController.SpawnEffect(deathEffect, bodyCollider.bounds.center, facingLeft);
        BattleSceneController.PlaySfx(deathSfx);

        // 有死亡动画时先播完再消失；身体碰撞先关掉，不再挡路。
        if (deathAnimDuration > 0f && BattleSceneController.HasParam(animator, "Dead"))
        {
            spriteRenderer.color = originalColor;
            bodyCollider.enabled = false;
            BattleSceneController.AnimBool(animator, "Dead", true);
            Destroy(gameObject, deathAnimDuration);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
