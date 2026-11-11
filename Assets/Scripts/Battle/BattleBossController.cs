using UnityEngine;

// Boss：平时朝主角走，隔一段时间从“冲撞、跳砸、能量喷射”里选一种攻击。
// 每种攻击都先变色前摇；HP 低于一半进入二阶段，节奏变快。
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BattleBossController : MonoBehaviour, IBattleDamageable
{
    private enum BossState
    {
        Inactive,
        Idle,
        Windup,
        Charging,
        Jumping,
        Beaming,
        Recover,
        Dead
    }

    private enum BossAttack
    {
        Charge,
        JumpSlam,
        Beam
    }

    [Header("场景引用")]
    [SerializeField] private BattlePlayerController player;
    [SerializeField] private LayerMask groundLayer;

    [Header("基础")]
    [SerializeField] private string bossName = "神社守护者";
    [Min(1)]
    [SerializeField] private int maxHP = 250;
    [Tooltip("所有攻击和身体接触共用的攻击力。")]
    [Min(1)]
    [SerializeField] private int attackPower = 18;
    [SerializeField] private float walkSpeed = 1.5f;
    [Tooltip("离主角多近就停下不再走。")]
    [SerializeField] private float stopDistance = 2f;
    [Tooltip("两次攻击之间的间隔（秒）。")]
    [SerializeField] private float attackInterval = 1.6f;
    [Tooltip("每次攻击结束后原地硬直的时间，是主角反击的机会。")]
    [SerializeField] private float recoverDuration = 0.6f;

    [Header("二阶段")]
    [Range(0f, 1f)]
    [SerializeField] private float phase2HPRatio = 0.5f;
    [Tooltip("二阶段时前摇、间隔、硬直乘以这个数（越小越快）。")]
    [Range(0.2f, 1f)]
    [SerializeField] private float phase2TimeScale = 0.65f;
    [SerializeField] private Color phase2Color = new Color(1f, 0.55f, 0.55f);

    [Header("前摇与受击")]
    [SerializeField] private Color windupColor = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private float hitFlashDuration = 0.1f;

    [Header("攻击 1：冲撞")]
    [SerializeField] private float chargeWindup = 0.6f;
    [SerializeField] private float chargeSpeed = 14f;
    [SerializeField] private float chargeMaxDuration = 1.5f;

    [Header("攻击 2：跳砸")]
    [SerializeField] private float jumpWindup = 0.5f;
    [Tooltip("从起跳到落地大约多少秒。")]
    [SerializeField] private float jumpAirTime = 0.9f;
    [Tooltip("落地冲击波向左右各延伸多远。")]
    [SerializeField] private float shockwaveRange = 3f;
    [SerializeField] private float shockwaveHeight = 0.8f;
    [Tooltip("冲击波外观（可选）：1×1 的方块，不要做 Boss 的子物体。")]
    [SerializeField] private Transform shockwaveVisual;

    [Header("攻击 3：能量喷射")]
    [SerializeField] private float beamWindup = 0.9f;
    [SerializeField] private float beamDuration = 1.2f;
    [SerializeField] private float beamThickness = 1.2f;
    [SerializeField] private float beamMaxLength = 20f;
    [Tooltip("喷射口相对 Boss 中心的高度。")]
    [SerializeField] private float beamHeightOffset = 0.3f;
    [Tooltip("喷射外观（可选）：1×1 的方块，不要做 Boss 的子物体。")]
    [SerializeField] private Transform beamVisual;

    [Header("奖励")]
    [Min(0)]
    [SerializeField] private int moneyReward = 100;
    [Min(0)]
    [SerializeField] private int expReward = 30;

    [Header("动画（可选）")]
    [Tooltip("素材本身朝左时勾选（Demon 要勾）。")]
    [SerializeField] private bool spriteFacesLeft;

    [Tooltip("参数：Speed（float），Windup、Attacking、Dead（bool），Charge、Jump、Beam、Hurt（trigger）。没有 Dead 参数时死亡直接隐藏。")]
    [SerializeField] private Animator animator;
    [Min(0f)]
    [SerializeField] private float deathAnimDuration = 1f;

    [Header("特效 Prefab（可选）")]
    [SerializeField] private GameObject chargeEffect;
    [SerializeField] private GameObject slamEffect;
    [SerializeField] private GameObject beamStartEffect;
    [SerializeField] private GameObject deathEffect;

    [Header("音效（可选）")]
    [Tooltip("开战时的吼叫。")]
    [SerializeField] private AudioClip roarSfx;
    [SerializeField] private AudioClip windupSfx;
    [SerializeField] private AudioClip chargeSfx;
    [SerializeField] private AudioClip slamSfx;
    [SerializeField] private AudioClip beamSfx;
    [SerializeField] private AudioClip deathSfx;

    [Header("运行状态（观察用）")]
    [SerializeField] private int currentHP;
    [SerializeField] private BossState state = BossState.Inactive;
    [SerializeField] private bool isPhase2;

    private Rigidbody2D rb;
    private BoxCollider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private ContactFilter2D groundFilter;

    private Color baseColor;
    private float flashEndTime;

    private BossAttack currentAttack;
    private BossAttack lastAttack = BossAttack.Beam;
    private float stateEndTime;
    private float stateStartTime;
    private float nextAttackTime;
    private int facing = -1;

    private float shockwaveHideTime;

    public bool IsDead => state == BossState.Dead;
    public bool IsActive => state != BossState.Inactive && state != BossState.Dead;
    public string BossName => bossName;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int MoneyReward => moneyReward;
    public int ExpReward => expReward;

    private float TimeFactor => isPhase2 ? phase2TimeScale : 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        baseColor = spriteRenderer.color;
        currentHP = maxHP;

        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useTriggers = false;

        // 没拖主角引用时自动找场景里的主角。
        if (player == null)
            player = FindAnyObjectByType<BattlePlayerController>();

        if (player != null)
            playerCollider = player.GetComponent<Collider2D>();
        else
            Debug.LogError("请给 BattleBossController 配置 Player。", this);

        HideVisual(beamVisual);
        HideVisual(shockwaveVisual);
    }

    // 主角进入 Boss 房、门锁上时由场景控制器调用。
    public void Activate()
    {
        if (state != BossState.Inactive)
            return;

        state = BossState.Idle;
        nextAttackTime = Time.time + attackInterval;
        BattleSceneController.PlaySfx(roarSfx);
    }

    private void Update()
    {
        if (state == BossState.Dead)
            return;

        if (Time.time < flashEndTime)
            spriteRenderer.color = Color.white;
        else if (state == BossState.Windup)
            spriteRenderer.color = windupColor;
        else
            spriteRenderer.color = baseColor;

        spriteRenderer.flipX = (facing < 0) != spriteFacesLeft;

        if (animator != null)
        {
            BattleSceneController.AnimFloat(animator, "Speed", Mathf.Abs(rb.linearVelocity.x));
            BattleSceneController.AnimBool(animator, "Windup", state == BossState.Windup);
            BattleSceneController.AnimBool(animator, "Attacking",
                state == BossState.Charging || state == BossState.Jumping || state == BossState.Beaming);
        }

        if (shockwaveVisual != null &&
            shockwaveVisual.gameObject.activeSelf &&
            Time.time >= shockwaveHideTime)
        {
            HideVisual(shockwaveVisual);
        }
    }

    private void FixedUpdate()
    {
        if (!IsActive || player == null)
            return;

        // 主角倒下后停手。
        if (player.IsDefeated)
        {
            StopBeam();
            SetHorizontalSpeed(0f);
            state = BossState.Idle;
            return;
        }

        CheckContactDamage();

        switch (state)
        {
            case BossState.Idle:
                UpdateIdle();
                break;

            case BossState.Windup:
                SetHorizontalSpeed(0f);
                if (Time.time >= stateEndTime)
                    StartAttack();
                break;

            case BossState.Charging:
                UpdateCharge();
                break;

            case BossState.Jumping:
                UpdateJump();
                break;

            case BossState.Beaming:
                UpdateBeam();
                break;

            case BossState.Recover:
                SetHorizontalSpeed(0f);
                if (Time.time >= stateEndTime)
                {
                    state = BossState.Idle;
                    nextAttackTime = Time.time + attackInterval * TimeFactor;
                }
                break;
        }
    }

    // ---------- 平时 ----------

    private void UpdateIdle()
    {
        float dx = player.transform.position.x - transform.position.x;
        facing = dx >= 0f ? 1 : -1;

        SetHorizontalSpeed(Mathf.Abs(dx) > stopDistance ? facing * walkSpeed : 0f);

        if (Time.time >= nextAttackTime && IsGrounded())
            BeginWindup();
    }

    private void BeginWindup()
    {
        // 随机选攻击，尽量不和上一次重复。
        currentAttack = (BossAttack)Random.Range(0, 3);

        if (currentAttack == lastAttack)
            currentAttack = (BossAttack)Random.Range(0, 3);

        lastAttack = currentAttack;

        // 前摇开始时锁定朝向，之后不再转身。
        float dx = player.transform.position.x - transform.position.x;
        facing = dx >= 0f ? 1 : -1;

        float windup = currentAttack switch
        {
            BossAttack.Charge => chargeWindup,
            BossAttack.JumpSlam => jumpWindup,
            _ => beamWindup
        };

        state = BossState.Windup;
        stateEndTime = Time.time + windup * TimeFactor;
        SetHorizontalSpeed(0f);
        BattleSceneController.PlaySfx(windupSfx);
    }

    private void StartAttack()
    {
        stateStartTime = Time.time;

        switch (currentAttack)
        {
            case BossAttack.Charge:
                state = BossState.Charging;
                stateEndTime = Time.time + chargeMaxDuration;
                BattleSceneController.AnimTrigger(animator, "Charge");
                BattleSceneController.SpawnEffect(chargeEffect, transform.position, facing < 0);
                BattleSceneController.PlaySfx(chargeSfx);
                break;

            case BossAttack.JumpSlam:
                BattleSceneController.AnimTrigger(animator, "Jump");
                StartJump();
                break;

            case BossAttack.Beam:
                state = BossState.Beaming;
                stateEndTime = Time.time + beamDuration;
                BattleSceneController.AnimTrigger(animator, "Beam");
                BattleSceneController.SpawnEffect(
                    beamStartEffect,
                    new Vector3(
                        bodyCollider.bounds.center.x + facing * bodyCollider.bounds.extents.x,
                        bodyCollider.bounds.center.y + beamHeightOffset,
                        0f),
                    facing < 0);
                BattleSceneController.PlaySfx(beamSfx);
                break;
        }
    }

    private void EnterRecover()
    {
        state = BossState.Recover;
        stateEndTime = Time.time + recoverDuration * TimeFactor;
        SetHorizontalSpeed(0f);
    }

    // ---------- 攻击 1：冲撞 ----------

    private void UpdateCharge()
    {
        SetHorizontalSpeed(facing * chargeSpeed);

        // 撞到墙或门、或者冲够时间就停下。
        Bounds bounds = bodyCollider.bounds;

        RaycastHit2D wall = Physics2D.Raycast(
            bounds.center,
            Vector2.right * facing,
            bounds.extents.x + 0.2f,
            groundLayer);

        if (wall.collider != null || Time.time >= stateEndTime)
            EnterRecover();
    }

    // ---------- 攻击 2：跳砸 ----------

    private void StartJump()
    {
        state = BossState.Jumping;
        stateEndTime = Time.time + jumpAirTime + 2f; // 超时保护

        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        float dx = player.transform.position.x - transform.position.x;

        // 算出刚好落在主角当前位置的起跳速度。
        rb.linearVelocity = new Vector2(
            dx / jumpAirTime,
            gravity * jumpAirTime * 0.5f);
    }

    private void UpdateJump()
    {
        bool landed =
            Time.time > stateStartTime + 0.15f &&
            rb.linearVelocity.y <= 0.1f &&
            IsGrounded();

        if (landed)
        {
            Shockwave();
            EnterRecover();
        }
        else if (Time.time >= stateEndTime)
        {
            EnterRecover();
        }
    }

    private void Shockwave()
    {
        Bounds bounds = bodyCollider.bounds;

        // 贴着地面、向左右延伸的一条区域；落地时人在空中就能躲开。
        Vector2 center = new Vector2(
            bounds.center.x,
            bounds.min.y + shockwaveHeight * 0.5f);

        Vector2 size = new Vector2(
            bounds.size.x + shockwaveRange * 2f,
            shockwaveHeight);

        if (OverlapsPlayer(center, size))
            player.ReceiveAttack(attackPower, transform.position);

        BattleSceneController.SpawnEffect(slamEffect, new Vector3(center.x, bounds.min.y, 0f), facing < 0);
        BattleSceneController.PlaySfx(slamSfx);

        if (BattleSceneController.Current != null)
            BattleSceneController.Current.OnBossSlam();

        if (shockwaveVisual != null)
        {
            ShowVisual(shockwaveVisual, center, size);
            shockwaveHideTime = Time.time + 0.25f;
        }
    }

    // ---------- 攻击 3：能量喷射 ----------

    private void UpdateBeam()
    {
        SetHorizontalSpeed(0f);

        Bounds bounds = bodyCollider.bounds;

        Vector2 start = new Vector2(
            bounds.center.x + facing * bounds.extents.x,
            bounds.center.y + beamHeightOffset);

        // 向前一直喷到墙或门为止。
        RaycastHit2D wall = Physics2D.Raycast(
            start, Vector2.right * facing, beamMaxLength, groundLayer);

        float length = wall.collider != null ? wall.distance : beamMaxLength;

        Vector2 center = start + Vector2.right * (facing * length * 0.5f);
        Vector2 size = new Vector2(length, beamThickness);

        // 持续伤害；主角受伤后的无敌时间决定了多久扣一次血。
        if (OverlapsPlayer(center, size))
            player.ReceiveAttack(attackPower, transform.position);

        ShowVisual(beamVisual, center, size);

        if (Time.time >= stateEndTime)
        {
            StopBeam();
            EnterRecover();
        }
    }

    private void StopBeam()
    {
        HideVisual(beamVisual);
    }

    // ---------- 伤害 ----------

    private void CheckContactDamage()
    {
        if (playerCollider == null)
            return;

        // 稍微放大一点，物理上被挡住、刚好贴着也算碰到。
        Bounds bossBounds = bodyCollider.bounds;
        bossBounds.Expand(0.1f);

        if (bossBounds.Intersects(playerCollider.bounds))
            player.ReceiveAttack(attackPower, transform.position);
    }

    private bool OverlapsPlayer(Vector2 center, Vector2 size)
    {
        if (playerCollider == null)
            return false;

        Bounds area = new Bounds(center, new Vector3(size.x, size.y, 10f));
        Bounds target = playerCollider.bounds;
        target.center = new Vector3(target.center.x, target.center.y, 0f);

        return area.Intersects(target);
    }

    // 霸体：只闪白，不被击退。
    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (state == BossState.Dead || damage <= 0)
            return;

        currentHP = Mathf.Max(0, currentHP - damage);
        flashEndTime = Time.time + hitFlashDuration;

        if (BattleSceneController.Current != null)
            BattleSceneController.Current.OnEnemyHit(bodyCollider.bounds.center, damage, true);

        if (currentHP > 0)
            BattleSceneController.AnimTrigger(animator, "Hurt");

        // 没进房就被打到（比如法术飞进去）也直接开战。
        if (state == BossState.Inactive)
            Activate();

        if (!isPhase2 && currentHP <= maxHP * phase2HPRatio)
        {
            isPhase2 = true;
            baseColor = phase2Color;
            Debug.Log($"{bossName} 进入二阶段。", this);
        }

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        state = BossState.Dead;

        StopBeam();
        HideVisual(shockwaveVisual);

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        Debug.Log($"{bossName} 已被击败。", this);

        if (BattleSceneController.Current != null)
            BattleSceneController.Current.ReportBossDefeated(this);

        BattleSceneController.SpawnEffect(deathEffect, bodyCollider.bounds.center, facing < 0, 3f);
        BattleSceneController.PlaySfx(deathSfx);

        // 有死亡动画时先播完再隐藏；只隐藏不销毁，场景控制器还引用着它。
        if (deathAnimDuration > 0f && BattleSceneController.HasParam(animator, "Dead"))
        {
            spriteRenderer.color = baseColor;
            bodyCollider.enabled = false;
            BattleSceneController.AnimBool(animator, "Dead", true);
            Invoke(nameof(HideAfterDeath), deathAnimDuration);
        }
        else
        {
            HideAfterDeath();
        }
    }

    private void HideAfterDeath()
    {
        gameObject.SetActive(false);
    }

    // ---------- 工具 ----------

    private bool IsGrounded()
    {
        int hitCount = bodyCollider.Cast(
            Vector2.down, groundFilter, groundHits, 0.08f);

        for (int i = 0; i < hitCount; i++)
        {
            if (groundHits[i].normal.y > 0.5f)
                return true;
        }

        return false;
    }

    private void SetHorizontalSpeed(float speed)
    {
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    private static void ShowVisual(Transform visual, Vector2 center, Vector2 size)
    {
        if (visual == null)
            return;

        visual.position = new Vector3(center.x, center.y, visual.position.z);
        visual.localScale = new Vector3(size.x, size.y, 1f);

        if (!visual.gameObject.activeSelf)
            visual.gameObject.SetActive(true);
    }

    private static void HideVisual(Transform visual)
    {
        if (visual != null && visual.gameObject.activeSelf)
            visual.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        HideVisual(beamVisual);
    }

    private void OnDrawGizmosSelected()
    {
        if (bodyCollider == null)
            bodyCollider = GetComponent<BoxCollider2D>();

        Bounds bounds = bodyCollider.bounds;

        // 冲击波范围
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(
            new Vector3(bounds.center.x, bounds.min.y + shockwaveHeight * 0.5f, 0f),
            new Vector3(bounds.size.x + shockwaveRange * 2f, shockwaveHeight, 0.05f));

        // 喷射口高度和粗细（只画朝左的一段示意）
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            new Vector3(bounds.min.x - 2f, bounds.center.y + beamHeightOffset, 0f),
            new Vector3(4f, beamThickness, 0.05f));
    }
}
