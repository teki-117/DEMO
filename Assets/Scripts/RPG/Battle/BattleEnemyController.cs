using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BattleEnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
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

    [Header("受击表现")]
    [SerializeField] private float hitFlashDuration = 0.12f;

    [Header("运行状态（观察用）")]
    [SerializeField] private int currentHP;
    [SerializeField] private bool isDead;
    [SerializeField] private EnemyState currentState;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private BoxCollider2D bodyCollider;

    private Color originalColor;
    private float flashEndTime;
    private float nextAttackTime;

    private float patrolCenterX;
    private int patrolDirection = 1;

    public bool IsDead => isDead;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();

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

        spriteRenderer.color =
            Time.time < flashEndTime ? Color.white : originalColor;
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (player != null && player.IsDefeated)
        {
            currentState = EnemyState.Idle;
            SetHorizontalSpeed(0f);
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
                    nextAttackTime = Time.time + attackInterval;
                    player.ReceiveAttack(attackPower);
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
        spriteRenderer.flipX = direction < 0;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHP = Mathf.Max(0, currentHP - damage);

        Debug.Log(
            $"{name} 受到 {damage} 点伤害，HP：{currentHP}/{maxHP}",
            this);

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        flashEndTime = Time.time + hitFlashDuration;
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        Debug.Log($"{name} 已被击败。", this);
        Destroy(gameObject);
    }
}