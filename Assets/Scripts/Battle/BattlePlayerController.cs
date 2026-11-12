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

    [Tooltip("普攻和冲刺斩每打中一个敌人回复的 SP。法术打中不回。")]
    [Min(0)]
    [SerializeField] private int spGainOnHit = 3;

    private float nextAttackTime;
    private float attackPreviewEndTime;

    // 一次攻击中，记录已经打中过的敌人。
    private readonly HashSet<IBattleDamageable> hitEnemies =
        new HashSet<IBattleDamageable>();

    [Header("受伤")]
    [Min(0f)]
    [SerializeField] private float hurtInvincibleDuration = 0.8f;

    [Tooltip("被打中时的击退速度：x 向后，y 向上。")]
    [SerializeField] private Vector2 hurtKnockback = new Vector2(6f, 4f);

    [Min(0f)]
    [SerializeField] private float hurtKnockbackDuration = 0.2f;

    [Min(0.01f)]
    [SerializeField] private float hurtBlinkInterval = 0.08f;

    [SerializeField] private bool isDefeated;

    private float knockbackEndTime;
    private Color originalColor;

    [Header("技能：冲刺斩（K）")]
    [SerializeField] private float dashSpeed = 16f;
    [Min(0.01f)]
    [SerializeField] private float dashDuration = 0.18f;
    [Min(0f)]
    [SerializeField] private float dashCooldown = 0.8f;
    [Min(0f)]
    [SerializeField] private float dashDamageMultiplier = 1.5f;
    [SerializeField] private Vector2 dashHitSize = new Vector2(1.4f, 1f);

    private bool isDashing;
    private float dashEndTime;
    private float nextDashTime;
    private float dashDirection;
    private float savedGravityScale;
    private int dashDamage;

    private readonly HashSet<IBattleDamageable> dashHitEnemies =
        new HashSet<IBattleDamageable>();

    [Header("技能：法术（L，耗 SP）")]
    [Min(0)]
    [SerializeField] private int spellSPCost = 8;
    [Min(0f)]
    [SerializeField] private float spellDamageMultiplier = 2f;
    [SerializeField] private float spellSpeed = 12f;
    [SerializeField] private float spellMaxDistance = 8f;
    [SerializeField] private Vector2 spellHitSize = new Vector2(0.8f, 0.6f);
    [Min(0f)]
    [SerializeField] private float spellCooldown = 0.6f;

    [Tooltip("法术飞行时显示的物体（可选）。放在场景里、不要做主角的子物体，开始时会自动隐藏。")]
    [SerializeField] private Transform spellVisual;

    private bool spellActive;
    private Vector2 spellPosition;
    private float spellDirection;
    private float spellTravelled;
    private int spellDamage;
    private float nextSpellTime;

    private readonly HashSet<IBattleDamageable> spellHitEnemies =
        new HashSet<IBattleDamageable>();

    [Header("技能：回血（I，耗 SP）")]
    [Min(0)]
    [SerializeField] private int healSPCost = 10;
    [Min(1)]
    [SerializeField] private int healAmount = 25;
    [Min(0f)]
    [SerializeField] private float healCooldown = 1f;
    [SerializeField] private Color healFlashColor = new Color(0.4f, 1f, 0.4f);

    private float nextHealTime;

    [Header("动画（可选）")]
    [Tooltip("素材本身朝左时勾选。")]
    [SerializeField] private bool spriteFacesLeft;

    [Tooltip("参数：Speed、VelocityY（float），Grounded、Dead（bool），Attack、Dash、Cast、Heal、Hurt（trigger）。没有的参数会自动跳过。")]
    [SerializeField] private Animator animator;

    [Header("特效 Prefab（可选）")]
    [SerializeField] private GameObject attackEffect;
    [SerializeField] private GameObject dashEffect;
    [SerializeField] private GameObject spellCastEffect;
    [SerializeField] private GameObject healEffect;
    [SerializeField] private GameObject hurtEffect;
    [Tooltip("打中敌人时在敌人身上出现的特效。")]
    [SerializeField] private GameObject hitEffect;

    [Header("音效（可选）")]
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip attackSfx;
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private AudioClip dashSfx;
    [SerializeField] private AudioClip spellSfx;
    [SerializeField] private AudioClip healSfx;
    [SerializeField] private AudioClip hurtSfx;
    private float healFlashEndTime;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        originalColor = spriteRenderer.color;
        savedGravityScale = rb.gravityScale;

        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useTriggers = false;

        // 身体不带摩擦力：跳起来按着方向键贴墙或贴平台侧面时，不会被摩擦力挂住。
        if (bodyCollider.sharedMaterial == null)
        {
            bodyCollider.sharedMaterial = new PhysicsMaterial2D("PlayerNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        if (spellVisual != null)
            spellVisual.gameObject.SetActive(false);
    }

    private void Update()
    {
        ReadInput();
        UpdateFacing();

        Keyboard keyboard = Keyboard.current;

        if (Time.timeScale > 0f && keyboard != null)
        {
            if (keyboard.jKey.wasPressedThisFrame)
                TryAttack();

            if (keyboard.kKey.wasPressedThisFrame)
                TryDashSlash();

            if (keyboard.lKey.wasPressedThisFrame)
                TryCastSpell();

            if (keyboard.iKey.wasPressedThisFrame)
                TryHeal();
        }

        UpdateSpell();
        UpdateColor();
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        BattleSceneController.AnimFloat(animator, "Speed", Mathf.Abs(rb.linearVelocity.x));
        BattleSceneController.AnimFloat(animator, "VelocityY", rb.linearVelocity.y);
        BattleSceneController.AnimBool(animator, "Grounded", isGrounded);
        BattleSceneController.AnimBool(animator, "Dead", isDefeated);
    }

    private bool FacingLeft => !facingRight;

    private void FixedUpdate()
    {
        CheckGround();

        if (isDashing)
        {
            UpdateDash();
            jumpRequested = false;
            return;
        }

        // 被击退期间不接受左右输入，保留被打飞的速度。
        if (Time.time < knockbackEndTime)
        {
            jumpRequested = false;
            return;
        }

        // 只修改水平速度，保留重力造成的垂直速度。
        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;

        if (jumpRequested && isGrounded)
        {
            velocity.y = jumpSpeed;
            isGrounded = false;
            BattleSceneController.PlaySfx(jumpSfx);
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
        // 冲刺和击退时不改朝向。
        if (isDashing || Time.time < knockbackEndTime)
        {
            spriteRenderer.flipX = facingRight == spriteFacesLeft;
            return;
        }

        if (moveInput > 0f)
        {
            facingRight = true;
        }
        else if (moveInput < 0f)
        {
            facingRight = false;
        }

        // 默认角色素材朝右，只翻转图片，不翻转碰撞体。
        spriteRenderer.flipX = facingRight == spriteFacesLeft;
    }

    private void UpdateColor()
    {
        Color color = originalColor;

        if (Time.time < healFlashEndTime)
            color = Color.Lerp(originalColor, healFlashColor, 0.7f);

        // 受伤无敌期间闪烁。
        if (Time.time < nextDamageTime)
        {
            bool dim = Mathf.FloorToInt(Time.time / hurtBlinkInterval) % 2 == 0;
            color.a = dim ? 0.3f : originalColor.a;
        }

        spriteRenderer.color = color;
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

        EndDash();
        EndSpell();

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private Vector2 GetAttackCenter()
    {
        float direction = facingRight ? 1f : -1f;

        return (Vector2)transform.position + new Vector2(
            attackOffset.x * direction,
            attackOffset.y
        );
    }

    // 取主角状态；从正式入口进入且主角未倒下时才返回。
    private bool TryGetHero(out PartyManager party, out PartyMemberState hero)
    {
        party = PartyManager.Instance;
        hero = null;

        if (party == null)
        {
            Debug.LogWarning(
                "战斗需要队伍数据，请从正式入口进入神社。",
                this);
            return false;
        }

        hero = party.GetMemberState(party.MainCharacterId);

        return hero != null && hero.CurrentHP > 0;
    }

    private void TryAttack()
    {
        if (isDefeated || isDashing || Time.time < nextAttackTime)
            return;

        if (!TryGetHero(out _, out PartyMemberState hero))
            return;

        // 每次攻击读取当前属性，包含武器和防具提供的攻击加成。
        int damage = Mathf.Max(1, hero.Attack);

        nextAttackTime = Time.time + attackCooldown;
        attackPreviewEndTime = Time.time + 0.12f;

        hitEnemies.Clear();

        BattleSceneController.AnimTrigger(animator, "Attack");
        BattleSceneController.SpawnEffect(attackEffect, GetAttackCenter(), FacingLeft);
        BattleSceneController.PlaySfx(attackSfx);

        HitEnemiesInBox(GetAttackCenter(), attackSize, damage, hitEnemies, true);
    }

    // 对范围内的敌人造成伤害；同一个集合里的敌人只会被打一次。
    // gainSP：近战打中时回复 SP。
    private void HitEnemiesInBox(
        Vector2 center,
        Vector2 size,
        int damage,
        HashSet<IBattleDamageable> alreadyHit,
        bool gainSP)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            size,
            0f,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            IBattleDamageable enemy =
                hit.GetComponentInParent<IBattleDamageable>();

            if (enemy == null || enemy.IsDead)
                continue;

            if (alreadyHit.Add(enemy))
            {
                enemy.TakeDamage(damage, transform.position);

                BattleSceneController.SpawnEffect(hitEffect, hit.bounds.center, FacingLeft, 0.8f);
                BattleSceneController.PlaySfx(hitSfx);

                PartyManager party = PartyManager.Instance;

                if (gainSP && spGainOnHit > 0 && party != null)
                    party.RestoreSP(party.MainCharacterId, spGainOnHit);
            }
        }
    }

    // ---------- 冲刺斩 ----------

    private void TryDashSlash()
    {
        if (isDefeated || isDashing || Time.time < nextDashTime)
            return;

        if (!TryGetHero(out _, out PartyMemberState hero))
            return;

        dashDamage = Mathf.Max(
            1, Mathf.RoundToInt(hero.Attack * dashDamageMultiplier));

        dashDirection = facingRight ? 1f : -1f;
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        // 冲刺时不受重力影响，保持水平直线。
        savedGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;

        dashHitEnemies.Clear();

        BattleSceneController.AnimTrigger(animator, "Dash");
        BattleSceneController.SpawnEffect(dashEffect, transform.position, FacingLeft);
        BattleSceneController.PlaySfx(dashSfx);
    }

    private void UpdateDash()
    {
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

        Vector2 center = (Vector2)transform.position +
                         new Vector2(dashDirection * 0.5f, 0f);

        HitEnemiesInBox(center, dashHitSize, dashDamage, dashHitEnemies, true);

        if (Time.time >= dashEndTime)
        {
            EndDash();
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void EndDash()
    {
        if (!isDashing)
            return;

        isDashing = false;

        if (rb != null)
            rb.gravityScale = savedGravityScale;
    }

    // ---------- 法术 ----------

    private void TryCastSpell()
    {
        if (isDefeated || spellActive || Time.time < nextSpellTime)
            return;

        if (!TryGetHero(out PartyManager party, out PartyMemberState hero))
            return;

        if (!party.TrySpendSP(party.MainCharacterId, spellSPCost))
        {
            Debug.Log($"SP 不足，法术需要 {spellSPCost} SP。", this);
            return;
        }

        spellDamage = Mathf.Max(
            1, Mathf.RoundToInt(hero.Attack * spellDamageMultiplier));

        spellDirection = facingRight ? 1f : -1f;
        spellPosition = GetAttackCenter();
        spellTravelled = 0f;
        spellActive = true;
        nextSpellTime = Time.time + spellCooldown;

        spellHitEnemies.Clear();

        if (spellVisual != null)
        {
            spellVisual.position = new Vector3(
                spellPosition.x, spellPosition.y, spellVisual.position.z);

            Vector3 scale = spellVisual.localScale;
            scale.x = Mathf.Abs(scale.x) * spellDirection;
            spellVisual.localScale = scale;

            spellVisual.gameObject.SetActive(true);
        }

        BattleSceneController.AnimTrigger(animator, "Cast");
        BattleSceneController.SpawnEffect(spellCastEffect, spellPosition, FacingLeft);
        BattleSceneController.PlaySfx(spellSfx);
    }

    private void UpdateSpell()
    {
        if (!spellActive || Time.deltaTime <= 0f)
            return;

        float step = spellSpeed * Time.deltaTime;
        spellPosition.x += spellDirection * step;
        spellTravelled += step;

        if (spellVisual != null)
        {
            spellVisual.position = new Vector3(
                spellPosition.x, spellPosition.y, spellVisual.position.z);
        }

        // 穿透敌人，碰到地面、墙壁或飞到最远距离时消失。
        HitEnemiesInBox(spellPosition, spellHitSize, spellDamage, spellHitEnemies, false);

        bool hitWall = Physics2D.OverlapBox(
            spellPosition, spellHitSize, 0f, groundLayer) != null;

        if (hitWall || spellTravelled >= spellMaxDistance)
            EndSpell();
    }

    private void EndSpell()
    {
        spellActive = false;

        if (spellVisual != null)
            spellVisual.gameObject.SetActive(false);
    }

    // ---------- 回血 ----------

    private void TryHeal()
    {
        if (isDefeated || Time.time < nextHealTime)
            return;

        if (!TryGetHero(out PartyManager party, out PartyMemberState hero))
            return;

        // HP 已满时不扣 SP。
        if (hero.CurrentHP >= hero.MaxHP)
        {
            Debug.Log("HP 已满，不需要回血。", this);
            return;
        }

        if (!party.TrySpendSP(party.MainCharacterId, healSPCost))
        {
            Debug.Log($"SP 不足，回血需要 {healSPCost} SP。", this);
            return;
        }

        int healed = Mathf.Min(healAmount, hero.MaxHP - hero.CurrentHP);
        party.RestoreHP(party.MainCharacterId, healAmount);

        nextHealTime = Time.time + healCooldown;
        healFlashEndTime = Time.time + 0.25f;

        BattleSceneController.AnimTrigger(animator, "Heal");
        BattleSceneController.SpawnEffect(healEffect, transform.position, FacingLeft);
        BattleSceneController.PlaySfx(healSfx);

        if (BattleSceneController.Current != null)
            BattleSceneController.Current.OnPlayerHeal(transform.position, healed);
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

        if (spellActive)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                new Vector3(spellPosition.x, spellPosition.y, transform.position.z),
                new Vector3(spellHitSize.x, spellHitSize.y, 0.05f)
            );
        }
    }

    // sourcePosition：攻击来源的位置，用来决定击退方向。
    public bool ReceiveAttack(int enemyAttack, Vector2 sourcePosition)
    {
        // 冲刺斩期间无敌，可以用来穿过敌人的攻击。
        if (isDefeated ||
            isDashing ||
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

        BattleSceneController.SpawnEffect(hurtEffect, transform.position, FacingLeft);
        BattleSceneController.PlaySfx(hurtSfx);

        if (BattleSceneController.Current != null)
            BattleSceneController.Current.OnPlayerHurt(transform.position, damage);

        if (hero.CurrentHP <= 0)
        {
            isDefeated = true;
            UpdateAnimator();

            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;

            // 停止本脚本的移动、跳跃、攻击和技能，由场景控制器显示失败结算。
            enabled = false;

            Debug.Log("主角已倒下。", this);
            return true;
        }

        // 朝远离攻击来源的方向击退。
        float direction =
            transform.position.x >= sourcePosition.x ? 1f : -1f;

        rb.linearVelocity = new Vector2(
            direction * hurtKnockback.x,
            hurtKnockback.y
        );

        knockbackEndTime = Time.time + hurtKnockbackDuration;
        BattleSceneController.AnimTrigger(animator, "Hurt");

        return true;
    }
}
