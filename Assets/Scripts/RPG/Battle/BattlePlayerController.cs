using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;



[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BattlePlayerController : MonoBehaviour
{
    private float nextDamageTime;
    public bool IsDefeated => isDefeated;
    [Header("移动")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("跳跃")]
    [SerializeField] private float jumpSpeed = 12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.06f;

    [Header("运行状态（观察用）")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool facingRight = true;

    private Rigidbody2D rb;
    private BoxCollider2D bodyCollider;
    private SpriteRenderer spriteRenderer;

    private float moveInput;
    private bool jumpRequested;

    // 重复使用检测结果数组，避免每次检测都创建新数组。
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private ContactFilter2D groundFilter;

    public bool IsGrounded => isGrounded;
    public bool FacingRight => facingRight;


    [Header("普通攻击")]

    [Min(0.01f)]
    [SerializeField] private float attackCooldown = 0.35f;

    [SerializeField] private Vector2 attackSize = new Vector2(1.2f, 1f);
    [SerializeField] private Vector2 attackOffset = new Vector2(0.9f, 0f);
    [SerializeField] private LayerMask enemyLayer;

    private float nextAttackTime;
    private float attackPreviewEndTime;

    // 一次攻击中，记录已经打中过的敌人。
    private readonly HashSet<BattleEnemyController> hitEnemies =
        new HashSet<BattleEnemyController>();

    [Header("受伤")]
    [Min(0f)]
    [SerializeField] private float hurtInvincibleDuration = 0.8f;

    [SerializeField] private bool isDefeated;

   
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useTriggers = false;
    }

    private void Update()
    {
        ReadInput();
        UpdateFacing();

        Keyboard keyboard = Keyboard.current;

        if (Time.timeScale > 0f &&
            keyboard != null &&
            keyboard.jKey.wasPressedThisFrame)
        {
            TryAttack();
        }
    }

    private void FixedUpdate()
    {
        CheckGround();

        // 只修改水平速度，保留重力造成的垂直速度。
        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;

        if (jumpRequested && isGrounded)
        {
            velocity.y = jumpSpeed;
            isGrounded = false;
        }

        rb.linearVelocity = velocity;

        // 这次按键只处理一次，不能留到下一次落地。
        jumpRequested = false;
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            moveInput = 0f;
            jumpRequested = false;
            return;
        }

        bool leftPressed =
            keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed;

        bool rightPressed =
            keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed;

        moveInput =
            (rightPressed ? 1f : 0f) -
            (leftPressed ? 1f : 0f);

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }
    }

    private void UpdateFacing()
    {
        if (moveInput > 0f)
        {
            facingRight = true;
        }
        else if (moveInput < 0f)
        {
            facingRight = false;
        }

        // 默认角色素材朝右，只翻转图片，不翻转碰撞体。
        spriteRenderer.flipX = !facingRight;
    }

    private void CheckGround()
    {
        isGrounded = false;

        // 正在向上跳时，不把脚边尚未离开的地面当作落地。
        if (rb.linearVelocity.y > 0.1f)
        {
            return;
        }

        int hitCount = bodyCollider.Cast(
            Vector2.down,
            groundFilter,
            groundHits,
            groundCheckDistance
        );

        for (int i = 0; i < hitCount; i++)
        {
            // 表面朝上才算地面，侧面的墙壁不算。
            if (groundHits[i].normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnDisable()
    {
        moveInput = 0f;
        jumpRequested = false;
    }

    private Vector2 GetAttackCenter()
    {
        float direction = facingRight ? 1f : -1f;

        return (Vector2)transform.position + new Vector2(
            attackOffset.x * direction,
            attackOffset.y
        );
    }

    private void TryAttack()
    {
        if (isDefeated || Time.time < nextAttackTime)
            return;

        PartyManager party = PartyManager.Instance;

        if (party == null)
        {
            Debug.LogWarning(
                "攻击需要队伍数据，请从正式入口进入神社。",
                this);
            return;
        }

        PartyMemberState hero =
            party.GetMemberState(party.MainCharacterId);

        if (hero == null || hero.CurrentHP <= 0)
            return;

        // 每次攻击读取当前属性，包含武器和防具提供的攻击加成。
        int damage = Mathf.Max(1, hero.Attack);

        nextAttackTime = Time.time + attackCooldown;
        attackPreviewEndTime = Time.time + 0.12f;

        hitEnemies.Clear();

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            GetAttackCenter(),
            attackSize,
            0f,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            BattleEnemyController enemy =
                hit.GetComponentInParent<BattleEnemyController>();

            if (enemy == null || enemy.IsDead)
                continue;

            if (hitEnemies.Add(enemy))
            {
                enemy.TakeDamage(damage);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Application.isPlaying && Time.time < attackPreviewEndTime
                ? Color.red
                : Color.yellow;

        Vector2 center = GetAttackCenter();

        Gizmos.DrawWireCube(
            new Vector3(center.x, center.y, transform.position.z),
            new Vector3(attackSize.x, attackSize.y, 0.05f)
        );
    }
    public bool ReceiveAttack(int enemyAttack)
    {
        if (isDefeated ||
            enemyAttack <= 0 ||
            Time.time < nextDamageTime)
        {
            return false;
        }

        PartyManager party = PartyManager.Instance;

        if (party == null)
        {
            Debug.LogWarning(
                "受伤需要 PartyManager，请从正式入口进入神社。",
                this);
            return false;
        }

        PartyMemberState hero =
            party.GetMemberState(party.MainCharacterId);

        if (hero == null)
            return false;

        // 防御值包含当前装备提供的加成。
        int damage = Mathf.Max(1, enemyAttack - hero.Defense);

        // 真正修改的是队伍中的主角 HP，不另外保存一份血量。
        if (!party.TakeDamage(party.MainCharacterId, damage))
            return false;

        nextDamageTime = Time.time + hurtInvincibleDuration;

        Debug.Log(
            $"主角受到 {damage} 点伤害，HP：{hero.CurrentHP}/{hero.MaxHP}",
            this);

        if (hero.CurrentHP <= 0)
        {
            isDefeated = true;

            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;

            // 停止本脚本的移动、跳跃和攻击。
            // 场景控制器仍工作，所以 F1 仍能返回地图。
            enabled = false;

            Debug.Log(
                "主角已倒下。当前测试阶段请按 F1 返回，"
                + "在背包中恢复主角 HP 后重新进入。",
                this);
        }

        return true;
    }
}