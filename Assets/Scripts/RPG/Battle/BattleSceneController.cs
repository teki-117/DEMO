using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class BattleSceneController : MonoBehaviour
{
    // 当前战斗场景的控制器，敌人被击败时向它报告奖励。
    public static BattleSceneController Current { get; private set; }

    [Header("场景引用")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera battleCamera;

    [Header("相机水平跟随")]
    [Min(0.01f)]
    [SerializeField] private float followSmoothTime = 0.15f;

    [SerializeField] private float horizontalOffset = 0f;

    [Header("开发测试")]
    [SerializeField] private bool enableTestReturn = true;

    private float cameraVelocityX;
    private float fixedCameraY;
    private float fixedCameraZ;

    private bool isReturning;

    [Header("战斗 HUD")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private TMP_Text statusText;

    private PartyManager subscribedParty;

    [Header("关卡区域")]
    [SerializeField] private BoxCollider2D bossRoomArea;
    [SerializeField] private BoxCollider2D exitArea;
    [SerializeField] private TMP_Text objectiveText;

    [Header("掉落失败")]
    [SerializeField] private float fallFailY = -8f;

    [Header("Boss 房钥匙与门")]
    [Tooltip("钥匙物体：场景里先放好，开始时自动隐藏，掉钥匙的敌人被击败时出现在它的位置。")]
    [SerializeField] private GameObject keyPickup;

    [Tooltip("主角离钥匙多近算捡到。")]
    [Min(0.1f)]
    [SerializeField] private float keyPickupRadius = 0.9f;

    [Tooltip("Boss 房入口的门：带着钥匙在门前按 E 打开。留空表示不需要钥匙。")]
    [SerializeField] private GameObject bossDoor;

    [Tooltip("离门多近可以按 E（入口门和出口门共用）。")]
    [Min(0.1f)]
    [SerializeField] private float doorInteractDistance = 0.6f;

    private bool entranceOpened;

    [Header("结算画面")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    [Header("Boss 战")]
    [Tooltip("留空表示这一关没有 Boss，Boss 房只是占位。")]
    [SerializeField] private BattleBossController boss;

    [Tooltip("出口的门：击败 Boss 后，站到门前按 E 离开（完成关卡）。")]
    [SerializeField] private GameObject exitDoor;

    [Tooltip("主角离入口门多远（进到房里）才锁门开战。")]
    [Min(0.5f)]
    [SerializeField] private float bossLockDistance = 2f;

    [Tooltip("Boss 血条的整体（包含血条和名字），平时隐藏。")]
    [SerializeField] private GameObject bossHudRoot;
    [SerializeField] private Slider bossHpBar;
    [SerializeField] private TMP_Text bossNameText;

    private bool bossFightStarted;
    private bool bossDefeated;

    private BattlePlayerController playerController;

    // 本次战斗累计的奖励，结算时交给 WorldManager 发放。
    private int earnedMoney;
    private int earnedExp;
    private int defeatedCount;
    private bool hasKey;

    private bool isShowingResult;
    private BattleResult pendingResult;
    private int resultShownFrame;

    [Header("打击感")]
    [Tooltip("打中小怪时画面停顿的秒数（真实时间）。")]
    [Min(0f)]
    [SerializeField] private float hitStopEnemy = 0.05f;
    [Min(0f)]
    [SerializeField] private float hitStopBoss = 0.08f;
    [Tooltip("停顿期间的时间流速，0.05 = 几乎停住。")]
    [Range(0f, 1f)]
    [SerializeField] private float hitStopTimeScale = 0.05f;

    [SerializeField] private float shakeOnPlayerHurt = 0.25f;
    [SerializeField] private float shakeOnBossHit = 0.08f;
    [SerializeField] private float shakeOnBossSlam = 0.4f;
    [Min(0.01f)]
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("伤害数字")]
    [SerializeField] private bool showDamageNumbers = true;
    [Tooltip("留空用 TextMeshPro 默认字体。")]
    [SerializeField] private TMP_FontAsset damageFont;
    [SerializeField] private float damageFontSize = 5f;
    [SerializeField] private float damageRiseDistance = 1f;
    [SerializeField] private float damageDuration = 0.7f;
    [SerializeField] private Color enemyDamageColor = Color.white;
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color healColor = new Color(0.4f, 1f, 0.4f);

    [Header("场景音效（可选）")]
    [SerializeField] private AudioClip keyPickupSfx;
    [SerializeField] private AudioClip doorOpenSfx;
    [SerializeField] private AudioClip resultSfx;

    private float hitStopEndTime;
    private bool hitStopActive;

    private float shakeEndTime;
    private float shakeStrength;
    private float cameraFollowX;

    private class FloatingNumber
    {
        public TextMeshPro text;
        public Vector3 start;
        public float startTime;
        public Color color;
    }

    private readonly System.Collections.Generic.List<FloatingNumber> floatingNumbers =
        new System.Collections.Generic.List<FloatingNumber>();

    // 动画参数缓存：只设置 Animator 里真的有的参数，避免刷警告。
    private static readonly System.Collections.Generic.Dictionary<Animator, System.Collections.Generic.HashSet<int>> animatorParams =
        new System.Collections.Generic.Dictionary<Animator, System.Collections.Generic.HashSet<int>>();

    private void Awake()
    {
        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;

        // 防止结算画面或命中停顿中途离开场景时游戏一直暂停。
        if (isShowingResult || hitStopActive)
            Time.timeScale = 1f;

        animatorParams.Clear();
    }

    // ---------- 打击感：给各控制器调用 ----------

    // 敌人或 Boss 被打中：伤害数字 + 命中停顿（Boss 再加轻微震动）。
    public void OnEnemyHit(Vector3 position, int damage, bool isBoss)
    {
        ShowNumber(position, damage.ToString(), enemyDamageColor);
        HitStop(isBoss ? hitStopBoss : hitStopEnemy);

        if (isBoss)
            Shake(shakeOnBossHit);
    }

    public void OnPlayerHurt(Vector3 position, int damage)
    {
        ShowNumber(position, damage.ToString(), playerDamageColor);
        Shake(shakeOnPlayerHurt);
    }

    public void OnPlayerHeal(Vector3 position, int amount)
    {
        ShowNumber(position, "+" + amount, healColor);
    }

    public void OnBossSlam()
    {
        Shake(shakeOnBossSlam);
    }

    public void HitStop(float duration)
    {
        if (duration <= 0f || isShowingResult || isReturning)
            return;

        hitStopEndTime = Mathf.Max(hitStopEndTime, Time.unscaledTime + duration);

        if (!hitStopActive)
        {
            hitStopActive = true;
            Time.timeScale = hitStopTimeScale;
        }
    }

    public void Shake(float strength)
    {
        if (strength <= 0f)
            return;

        shakeStrength = Mathf.Max(strength,
            Time.unscaledTime < shakeEndTime ? shakeStrength : 0f);
        shakeEndTime = Time.unscaledTime + shakeDuration;
    }

    private void ShowNumber(Vector3 position, string content, Color color)
    {
        if (!showDamageNumbers)
            return;

        var go = new GameObject("DamageNumber");
        go.transform.position = position + new Vector3(Random.Range(-0.2f, 0.2f), 0.6f, 0f);

        var text = go.AddComponent<TextMeshPro>();
        if (damageFont != null)
            text.font = damageFont;

        text.text = content;
        text.fontSize = damageFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.sortingOrder = 100;

        floatingNumbers.Add(new FloatingNumber
        {
            text = text,
            start = go.transform.position,
            startTime = Time.unscaledTime,
            color = color
        });
    }

    private void UpdateFeedback()
    {
        // 命中停顿用真实时间计时，结束后恢复正常速度。
        if (hitStopActive && Time.unscaledTime >= hitStopEndTime)
        {
            hitStopActive = false;

            if (!isShowingResult)
                Time.timeScale = 1f;
        }

        for (int i = floatingNumbers.Count - 1; i >= 0; i--)
        {
            FloatingNumber n = floatingNumbers[i];

            if (n.text == null)
            {
                floatingNumbers.RemoveAt(i);
                continue;
            }

            float t = (Time.unscaledTime - n.startTime) / damageDuration;

            if (t >= 1f)
            {
                Destroy(n.text.gameObject);
                floatingNumbers.RemoveAt(i);
                continue;
            }

            n.text.transform.position = n.start + Vector3.up * (damageRiseDistance * t);

            Color c = n.color;
            c.a = 1f - t * t;
            n.text.color = c;
        }
    }

    // ---------- 特效、音效、动画：通用工具 ----------

    // 生成特效 Prefab，facingLeft 时左右翻转，lifetime 秒后自动销毁。
    public static void SpawnEffect(GameObject prefab, Vector3 position, bool facingLeft, float lifetime = 1.5f)
    {
        if (prefab == null)
            return;

        GameObject fx = Instantiate(prefab, position, Quaternion.identity);

        if (facingLeft)
        {
            Vector3 scale = fx.transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            fx.transform.localScale = scale;
        }

        Destroy(fx, lifetime);
    }

    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip, volume);
    }

    private static bool HasParam(Animator animator, string name)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        if (!animatorParams.TryGetValue(animator, out var set))
        {
            set = new System.Collections.Generic.HashSet<int>();
            foreach (var p in animator.parameters)
                set.Add(p.nameHash);
            animatorParams[animator] = set;
        }

        return set.Contains(Animator.StringToHash(name));
    }

    public static void AnimTrigger(Animator animator, string name)
    {
        if (HasParam(animator, name))
            animator.SetTrigger(name);
    }

    public static void AnimBool(Animator animator, string name, bool value)
    {
        if (HasParam(animator, name))
            animator.SetBool(name, value);
    }

    public static void AnimFloat(Animator animator, string name, float value)
    {
        if (HasParam(animator, name))
            animator.SetFloat(name, value);
    }

    private void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (keyPickup != null)
            keyPickup.SetActive(false);

        // 没配门就当作 Boss 房不需要钥匙。
        hasKey = bossDoor == null;
        entranceOpened = bossDoor == null;

        // 没配 Boss 就当作已经击败，保持以前“走到出口就能完成”的流程。
        bossDefeated = boss == null;

        // 出口门一直都在，击败 Boss 后对着它按 E 离开。
        if (exitDoor != null)
            exitDoor.SetActive(true);

        if (bossHudRoot != null)
            bossHudRoot.SetActive(false);

        if (player == null || battleCamera == null)
        {
            Debug.LogError(
                "请给 BattleSceneController 配置 Player 和 Battle Camera。",
                this);

            enabled = false;
            return;
        }

        // 保留相机原来的高度与深度，只跟随水平方向。
        Vector3 position = battleCamera.transform.position;
        fixedCameraY = position.y;
        fixedCameraZ = position.z;

        // 开场直接对准主角，避免从远处滑过来。
        cameraFollowX = player.position.x + horizontalOffset;

        battleCamera.transform.position = new Vector3(
            cameraFollowX,
            fixedCameraY,
            fixedCameraZ
        );

        playerController = player.GetComponent<BattlePlayerController>();

        if (playerController == null ||
            bossRoomArea == null ||
            exitArea == null)
        {
            Debug.LogError(
                "请检查主角控制脚本、Boss Room Area 和 Exit Area。",
                this);

            enabled = false;
            return;
        }

        // 提示文字只是显示用，缺了不影响结算。
        if (objectiveText == null)
        {
            Debug.LogWarning(
                "BattleSceneController 没有配置 Objective Text，目标提示不会显示。",
                this);
        }

        if (resultPanel == null || resultText == null)
        {
            Debug.LogWarning(
                "BattleSceneController 没有配置结算画面，结束时会直接返回地图。",
                this);
        }

        if (bossDoor != null && keyPickup == null)
        {
            Debug.LogWarning(
                "配置了 Boss Door 但没有配置 Key Pickup，门将无法打开。",
                this);
        }
    }

    private void SetObjective(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;
    }

    private void Update()
    {
        UpdateFeedback();

        // 结算画面显示时游戏暂停，只等待确认键。
        if (isShowingResult)
        {
            UpdateResultScreen();
            return;
        }

        if (isReturning ||
            player == null ||
            playerController == null ||
            Time.timeScale <= 0f)
        {
            return;
        }

        // 先判失败，避免零血时按撤退绕过失败结算。
        if (playerController.IsDefeated ||
            player.position.y < fallFailY)
        {
            EndBattle(BattleResult.Defeated);
            return;
        }

        UpdateKeyPickup();

        Vector2 playerPosition = player.position;

        bool insideBossRoom =
            bossRoomArea.OverlapPoint(playerPosition);

        bool insideExit =
            exitArea.OverlapPoint(playerPosition);

        UpdateBossFight(insideBossRoom);

        // 入口门：带着钥匙站到门前按 E 才打开。
        bool canOpenEntrance =
            !entranceOpened &&
            hasKey &&
            IsPlayerNearDoor(bossDoor);

        // 出口：有出口门时对着门按 E 离开；没配门时沿用站进 Exit Area 的做法。
        bool canLeave =
            bossDefeated &&
            (exitDoor != null
                ? IsPlayerNearDoor(exitDoor)
                : insideBossRoom && insideExit);

        if (canLeave)
        {
            SetObjective("E：开门离开（完成关卡）    Esc：撤退");
        }
        else if (canOpenEntrance)
        {
            SetObjective("E：用钥匙开门    Esc：撤退");
        }
        else if (boss != null &&
                 !bossDefeated &&
                 (bossFightStarted || insideBossRoom))
        {
            SetObjective("击败 Boss    Esc：撤退");
        }
        else if (insideBossRoom && boss != null)
        {
            SetObjective("Boss 已击败，前往出口的门");
        }
        else if (insideBossRoom)
        {
            SetObjective("Boss 房（占位）——前往出口");
        }
        else if (!hasKey)
        {
            SetObjective("击败敌人，找到 Boss 房钥匙    Esc：撤退");
        }
        else if (!entranceOpened)
        {
            SetObjective("带着钥匙前往 Boss 房的门    Esc：撤退");
        }
        else
        {
            SetObjective("前往右边的 Boss 房    Esc：撤退");
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            EndBattle(BattleResult.Retreated);
            return;
        }

        // 保留旧测试快捷键，统一走撤退规则。
        if (enableTestReturn && keyboard.f1Key.wasPressedThisFrame)
        {
            ReturnToMapForTesting();
            return;
        }

        if (!keyboard.eKey.wasPressedThisFrame)
            return;

        if (canLeave)
        {
            EndBattle(BattleResult.Completed);
        }
        else if (canOpenEntrance)
        {
            entranceOpened = true;
            bossDoor.SetActive(false);
            PlaySfx(doorOpenSfx);
            Debug.Log("用钥匙打开了 Boss 房的门。", this);
        }
    }

    // 主角是否站在门前（贴着门或离门很近）。
    private bool IsPlayerNearDoor(GameObject door)
    {
        if (door == null || !door.activeInHierarchy)
            return false;

        Collider2D doorCollider = door.GetComponent<Collider2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        if (doorCollider != null && playerCollider != null)
        {
            Bounds reach = playerCollider.bounds;
            reach.Expand(new Vector3(doorInteractDistance * 2f, 0f, 10f));
            return reach.Intersects(doorCollider.bounds);
        }

        return Vector2.Distance(door.transform.position, player.position)
               <= doorInteractDistance + 0.5f;
    }

    private void LateUpdate()
    {
        if (isReturning || player == null || battleCamera == null)
            return;

        // 暂停时不更新平滑计算。
        if (Time.deltaTime <= 0f)
            return;

        float targetX = player.position.x + horizontalOffset;

        // 跟随位置单独记录，震动只加在最终画面上，不影响平滑跟随。
        cameraFollowX = Mathf.SmoothDamp(
            cameraFollowX,
            targetX,
            ref cameraVelocityX,
            followSmoothTime
        );

        Vector2 shake = Vector2.zero;

        if (Time.unscaledTime < shakeEndTime)
        {
            float remaining = (shakeEndTime - Time.unscaledTime) / shakeDuration;
            shake = Random.insideUnitCircle * (shakeStrength * remaining);
        }

        battleCamera.transform.position = new Vector3(
            cameraFollowX + shake.x,
            fixedCameraY + shake.y,
            fixedCameraZ
        );
    }

    public void ReturnToMapForTesting()
    {
        EndBattle(BattleResult.Retreated);
    }

    // ---------- 奖励与钥匙 ----------

    public void ReportEnemyDefeated(BattleEnemyController enemy)
    {
        if (enemy == null || isShowingResult || isReturning)
            return;

        defeatedCount++;
        earnedMoney += enemy.MoneyReward;
        earnedExp += enemy.ExpReward;

        if (enemy.DropsKey && !hasKey && keyPickup != null)
        {
            Vector3 dropPosition = enemy.transform.position;
            dropPosition.z = keyPickup.transform.position.z;

            keyPickup.transform.position = dropPosition;
            keyPickup.SetActive(true);
        }
    }

    private void UpdateKeyPickup()
    {
        if (hasKey || keyPickup == null || !keyPickup.activeSelf)
            return;

        float distance = Vector2.Distance(
            keyPickup.transform.position, player.position);

        if (distance > keyPickupRadius)
            return;

        hasKey = true;
        keyPickup.SetActive(false);
        PlaySfx(keyPickupSfx);

        Debug.Log("捡到 Boss 房钥匙，到门前按 E 开门。", this);
    }

    // ---------- Boss 战 ----------

    private void UpdateBossFight(bool insideBossRoom)
    {
        if (boss == null || bossDefeated)
            return;

        // 主角走进房间一段距离后锁门开战；Boss 被提前打到也算开战。
        if (!bossFightStarted)
        {
            bool farEnoughInside =
                insideBossRoom &&
                (bossDoor == null ||
                 Mathf.Abs(player.position.x - bossDoor.transform.position.x)
                     >= bossLockDistance);

            if (farEnoughInside || boss.IsActive)
                StartBossFight();
        }

        if (bossFightStarted && bossHpBar != null)
        {
            bossHpBar.SetValueWithoutNotify(
                boss.MaxHP > 0 ? (float)boss.CurrentHP / boss.MaxHP : 0f);
        }
    }

    private void StartBossFight()
    {
        bossFightStarted = true;

        // 入口门重新出现，把主角关在房里。
        if (bossDoor != null)
            bossDoor.SetActive(true);

        boss.Activate();

        if (bossHudRoot != null)
            bossHudRoot.SetActive(true);

        if (bossNameText != null)
            bossNameText.text = boss.BossName;

        Debug.Log("进入 Boss 房，门已锁上。", this);
    }

    public void ReportBossDefeated(BattleBossController defeatedBoss)
    {
        if (defeatedBoss == null || bossDefeated || isShowingResult || isReturning)
            return;

        bossDefeated = true;
        defeatedCount++;
        earnedMoney += defeatedBoss.MoneyReward;
        earnedExp += defeatedBoss.ExpReward;

        if (bossHudRoot != null)
            bossHudRoot.SetActive(false);

        Debug.Log("Boss 已击败，到出口的门前按 E 离开。", this);
    }

    // ---------- HUD ----------

    private void OnEnable()
    {
        subscribedParty = PartyManager.Instance;

        if (subscribedParty != null)
        {
            subscribedParty.OnStateChanged += RefreshHUD;
        }

        RefreshHUD();
    }

    private void OnDisable()
    {
        if (subscribedParty != null)
        {
            subscribedParty.OnStateChanged -= RefreshHUD;
        }

        subscribedParty = null;
    }

    private void RefreshHUD()
    {
        PartyMemberState hero = subscribedParty != null
            ? subscribedParty.GetMemberState(subscribedParty.MainCharacterId)
            : null;

        if (hero == null)
        {
            if (hpBar != null)
                hpBar.SetValueWithoutNotify(0f);

            if (statusText != null)
                statusText.text = "HP -- / --    SP -- / --    ATK --    DEF --";

            return;
        }

        float hpRatio = hero.MaxHP > 0
            ? (float)hero.CurrentHP / hero.MaxHP
            : 0f;

        if (hpBar != null)
        {
            hpBar.SetValueWithoutNotify(Mathf.Clamp01(hpRatio));
        }

        if (statusText != null)
        {
            statusText.text =
                $"HP {hero.CurrentHP} / {hero.MaxHP}    " +
                $"SP {hero.CurrentSP} / {hero.MaxSP}    " +
                $"ATK {hero.Attack}    DEF {hero.Defense}";

            if (hero.CurrentHP <= 0)
            {
                statusText.text += "    DOWN";
            }
        }
    }

    // ---------- 结束与结算画面 ----------

    private void EndBattle(BattleResult result)
    {
        if (isReturning || isShowingResult)
            return;

        if (WorldManager.Instance == null)
        {
            Debug.LogError(
                "缺少 WorldManager，请停止运行，"
                + "从正式入口进入地图，再通过神社测试。",
                this);

            // 避免独立场景测试时每帧重复报错。
            enabled = false;
            return;
        }

        // 和 WorldManager 的规则一致：零血时一律按失败显示。
        PartyManager party = PartyManager.Instance;
        PartyMemberState hero = party != null
            ? party.GetMemberState(party.MainCharacterId)
            : null;

        if (hero != null && hero.CurrentHP <= 0)
            result = BattleResult.Defeated;

        pendingResult = result;

        if (resultPanel == null || resultText == null)
        {
            ConfirmResult();
            return;
        }

        resultText.text = BuildResultText(result);
        resultPanel.SetActive(true);

        isShowingResult = true;
        resultShownFrame = Time.frameCount;
        PlaySfx(resultSfx);

        // 暂停敌人和主角，等玩家看完再返回；命中停顿也一并结束。
        hitStopActive = false;
        Time.timeScale = 0f;
    }

    private string BuildResultText(BattleResult result)
    {
        string rewards =
            $"击败敌人：{defeatedCount}\n" +
            $"金钱：{earnedMoney}\n" +
            $"EXP：{earnedExp}";

        switch (result)
        {
            case BattleResult.Completed:
                return "关卡完成\n\n" +
                       $"击败敌人：{defeatedCount}\n" +
                       $"金钱 +{earnedMoney}\n" +
                       $"EXP +{earnedExp}\n" +
                       BuildLevelUpLine() + "\n" +
                       "SP 已回满，时段 +1\n\n" +
                       "按 E 返回地图";

            case BattleResult.Retreated:
                return "已撤退\n\n" +
                       rewards + "\n" +
                       "奖励未获得。SP 已回满，时段不变\n\n" +
                       "按 E 返回地图";

            default:
                return "战斗失败\n\n" +
                       rewards + "\n" +
                       "奖励未获得。HP 降为 25%，SP 已回满，时段 +1\n\n" +
                       "按 E 返回地图";
        }
    }

    // EXP 在按 E 之后才发放，这里先预览会不会升级。
    private string BuildLevelUpLine()
    {
        PartyManager party = PartyManager.Instance;

        PartyMemberState hero = party != null
            ? party.GetMemberState(party.MainCharacterId)
            : null;

        if (hero == null || earnedExp <= 0)
            return "";

        int newLevel = party.PreviewLevelAfterEXP(
            party.MainCharacterId, earnedExp);

        return newLevel > hero.Level
            ? $"升级！Lv {hero.Level} → {newLevel}\n"
            : "";
    }

    private void UpdateResultScreen()
    {
        Keyboard keyboard = Keyboard.current;

        // 跳过打开画面的那一帧，避免出口的 E 直接把画面关掉。
        if (keyboard == null || Time.frameCount <= resultShownFrame)
            return;

        if (keyboard.eKey.wasPressedThisFrame)
            ConfirmResult();
    }

    private void ConfirmResult()
    {
        if (isReturning)
            return;

        isShowingResult = false;
        Time.timeScale = 1f;

        WorldManager world = WorldManager.Instance;

        if (world == null)
        {
            Debug.LogError("缺少 WorldManager，无法结算战斗。", this);
            enabled = false;
            return;
        }

        isReturning = world.FinishBattle(
            pendingResult, earnedMoney, earnedExp);

        if (!isReturning)
        {
            Debug.LogError(
                "战斗结算未执行，请检查前面的 Console 提示。",
                this);

            if (resultPanel != null)
                resultPanel.SetActive(false);

            enabled = false;
        }
    }
}
