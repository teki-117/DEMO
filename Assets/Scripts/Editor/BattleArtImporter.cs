using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// 战斗美术一键工具（只在编辑器里用，不会打进游戏）。
// 菜单：Tools / 战斗美术 / 1. 一键生成：导入设置、切帧、动画、Animator、特效 Prefab
//       Tools / 战斗美术 / 2. 选中角色：把缩放合并进碰撞体
// 要改帧对应关系，只改下面“配置”部分的表，再点一次菜单 1 即可（已拖进场景的引用不会丢）。
public static class BattleArtImporter
{
    private const string OutDir = "Assets/Art/Generated";

    private const string HeroSheet = "Assets/Art/Samurai/Characters Pack/Without Outline/WarriorMan-Sheet.png";
    private const string OgreDir = "Assets/Art/Gothicvania/Legacy Collection/Legacy Collection/Assets/Gothicvania/Characters/Ogre/Spritesheets/";
    private const string DemonDir = "Assets/Art/Gothicvania/Legacy Collection/Legacy Collection/Assets/Gothicvania/Characters/demon-Files/Spritesheets/";
    private const string FxDir = "Assets/Art/Effects/Free Pixel Effects Pack/";

    // ---------- 配置 ----------

    // 一组 = 一个角色。用参考图第一帧的身高算出 PPU，让角色身高等于 targetHeight（单位）。
    private class Group
    {
        public string name;
        public float targetHeight;   // 角色身高（世界单位），和碰撞体高度一致
        public string refSheet;      // 用哪张图的第一帧量身高
        public int fixedPPU;         // >0 时不量身高，直接用这个 PPU（特效用）
    }

    private class Sheet
    {
        public string group;
        public string path;
        public int cellW, cellH;
        public bool centerPivot;     // 特效：中心对齐；角色：脚底对齐、身体居中
    }

    // row = 第几行（从 0 开始，-1 = 整张图按行依次取）；from/count = 这一行取第几帧开始、取几帧（count -1 = 到行尾）
    private class Clip
    {
        public string group, name, sheet;
        public int row = -1, from, count = -1;
        public float fps = 12f;
        public bool loop;
    }

    private static readonly Group[] Groups =
    {
        new Group { name = "Hero",  targetHeight = 1.4f, refSheet = HeroSheet },
        new Group { name = "Ogre",  targetHeight = 1.4f, refSheet = OgreDir + "ogre-idle.png" },
        new Group { name = "Demon", targetHeight = 2.5f, refSheet = DemonDir + "demon-idle.png" },
        new Group { name = "FX",    fixedPPU = 64 },
    };

    private static readonly Sheet[] Sheets =
    {
        new Sheet { group = "Hero",  path = HeroSheet, cellW = 80, cellH = 64 },

        new Sheet { group = "Ogre",  path = OgreDir + "ogre-idle.png",   cellW = 144, cellH = 80 },
        new Sheet { group = "Ogre",  path = OgreDir + "ogre-walk.png",   cellW = 144, cellH = 80 },
        new Sheet { group = "Ogre",  path = OgreDir + "ogre-attack.png", cellW = 144, cellH = 80 },

        new Sheet { group = "Demon", path = DemonDir + "demon-idle.png",             cellW = 160, cellH = 144 },
        new Sheet { group = "Demon", path = DemonDir + "demon-attack.png",           cellW = 240, cellH = 192 },
        new Sheet { group = "Demon", path = DemonDir + "demon-attack-no-breath.png", cellW = 192, cellH = 176 },
        new Sheet { group = "Demon", path = DemonDir + "breath-fire.png",            cellW = 160, cellH = 96, centerPivot = true },

        new Sheet { group = "FX", path = FxDir + "10_weaponhit_spritesheet.png",      cellW = 100, cellH = 100, centerPivot = true },
        new Sheet { group = "FX", path = FxDir + "1_magicspell_spritesheet.png",      cellW = 100, cellH = 100, centerPivot = true },
        new Sheet { group = "FX", path = FxDir + "4_casting_spritesheet.png",         cellW = 100, cellH = 100, centerPivot = true },
        new Sheet { group = "FX", path = FxDir + "8_protectioncircle_spritesheet.png", cellW = 100, cellH = 100, centerPivot = true },
        new Sheet { group = "FX", path = FxDir + "11_fire_spritesheet.png",           cellW = 100, cellH = 100, centerPivot = true },
        new Sheet { group = "FX", path = FxDir + "6_flamelash_spritesheet.png",       cellW = 100, cellH = 100, centerPivot = true },
        new Sheet { group = "FX", path = FxDir + "9_brightfire_spritesheet.png",      cellW = 100, cellH = 100, centerPivot = true },
    };

    private static readonly Clip[] Clips =
    {
        // 主角：WarriorMan，每格 80×64，行号从 0 开始数
        new Clip { group = "Hero", name = "Idle",   sheet = HeroSheet, row = 0,  fps = 10, loop = true },
        new Clip { group = "Hero", name = "Run",    sheet = HeroSheet, row = 3,  fps = 12, loop = true },
        new Clip { group = "Hero", name = "Jump",   sheet = HeroSheet, row = 19, fps = 14 },
        new Clip { group = "Hero", name = "Fall",   sheet = HeroSheet, row = 20, fps = 10, loop = true },
        new Clip { group = "Hero", name = "Attack", sheet = HeroSheet, row = 5,  fps = 18 },
        new Clip { group = "Hero", name = "Dash",   sheet = HeroSheet, row = 16, fps = 30 },
        new Clip { group = "Hero", name = "Cast",   sheet = HeroSheet, row = 13, fps = 20 },
        new Clip { group = "Hero", name = "Heal",   sheet = HeroSheet, row = 21, fps = 14 },
        new Clip { group = "Hero", name = "Hurt",   sheet = HeroSheet, row = 22, fps = 10 },
        new Clip { group = "Hero", name = "Dead",   sheet = HeroSheet, row = 23, fps = 10 },

        // 小怪：Ogre。攻击前 4 帧举棒当前摇，后 3 帧砸下当出手
        new Clip { group = "Ogre", name = "Idle",   sheet = OgreDir + "ogre-idle.png",   fps = 8,  loop = true },
        new Clip { group = "Ogre", name = "Walk",   sheet = OgreDir + "ogre-walk.png",   fps = 10, loop = true },
        new Clip { group = "Ogre", name = "Windup", sheet = OgreDir + "ogre-attack.png", from = 0, count = 4, fps = 8 },
        new Clip { group = "Ogre", name = "Attack", sheet = OgreDir + "ogre-attack.png", from = 4, count = 3, fps = 12 },

        // Boss：Demon（素材朝左）。喷火攻击前 6 帧当前摇，后 5 帧循环当喷射
        new Clip { group = "Demon", name = "Idle",   sheet = DemonDir + "demon-idle.png",   fps = 8, loop = true },
        new Clip { group = "Demon", name = "Walk",   sheet = DemonDir + "demon-idle.png",   fps = 10, loop = true },
        new Clip { group = "Demon", name = "Windup", sheet = DemonDir + "demon-attack.png", from = 0, count = 6, fps = 8 },
        new Clip { group = "Demon", name = "Beam",   sheet = DemonDir + "demon-attack.png", from = 6, count = 5, fps = 10, loop = true },
        new Clip { group = "Demon", name = "Charge", sheet = DemonDir + "demon-attack-no-breath.png", from = 6, count = 2, fps = 8, loop = true },
        new Clip { group = "Demon", name = "Jump",   sheet = DemonDir + "demon-attack-no-breath.png", from = 0, count = 8, fps = 10 },

        // 特效（播完自动隐藏）
        new Clip { group = "FX", name = "Hit",       sheet = FxDir + "10_weaponhit_spritesheet.png",      fps = 30 },
        new Clip { group = "FX", name = "Spell",     sheet = FxDir + "1_magicspell_spritesheet.png",      fps = 30 },
        new Clip { group = "FX", name = "Cast",      sheet = FxDir + "4_casting_spritesheet.png",         fps = 30 },
        new Clip { group = "FX", name = "Heal",      sheet = FxDir + "8_protectioncircle_spritesheet.png", fps = 30 },
        new Clip { group = "FX", name = "Slam",      sheet = FxDir + "11_fire_spritesheet.png",           fps = 30 },
        new Clip { group = "FX", name = "Dash",      sheet = FxDir + "6_flamelash_spritesheet.png",       fps = 30 },
        new Clip { group = "FX", name = "BeamStart", sheet = DemonDir + "breath-fire.png",               fps = 12 },
        new Clip { group = "FX", name = "Explode",   sheet = FxDir + "9_brightfire_spritesheet.png",      fps = 30 },
    };

    // 特效 Prefab：名字 → 用哪个特效动画
    private static readonly string[] EffectPrefabs = { "Hit", "Spell", "Cast", "Heal", "Slam", "Dash", "BeamStart", "Explode" };

    // 菜单 3 的默认填法：脚本字段名 → 特效 Prefab 名（FX_xxx）或音效文件名（不带 .wav）
    private static readonly (string field, string asset)[] BossDefaults =
    {
        ("chargeEffect", "FX_Dash"), ("slamEffect", "FX_Slam"), ("beamStartEffect", "FX_BeamStart"), ("deathEffect", "FX_Explode"),
        ("roarSfx", "sfx_deathscream_alien1"), ("windupSfx", "sfx_sounds_powerup1"), ("chargeSfx", "sfx_sounds_impact5"),
        ("slamSfx", "sfx_exp_short_hard1"), ("beamSfx", "sfx_exp_long1"), ("deathSfx", "sfx_deathscream_alien3"),
    };

    private static readonly (string field, string asset)[] PlayerDefaults =
    {
        ("dashEffect", "FX_Dash"), ("spellCastEffect", "FX_Cast"), ("healEffect", "FX_Heal"),
        ("hurtEffect", "FX_Hit"), ("hitEffect", "FX_Hit"),
        ("jumpSfx", "sfx_movement_jump1"), ("attackSfx", "sfx_wpn_sword1"), ("hitSfx", "sfx_damage_hit1"),
        ("dashSfx", "sfx_wpn_sword2"), ("spellSfx", "sfx_wpn_laser1"), ("healSfx", "sfx_sounds_powerup2"),
        ("hurtSfx", "sfx_damage_hit5"),
    };

    private static readonly (string field, string asset)[] EnemyDefaults =
    {
        ("deathEffect", "FX_Explode"), ("attackSfx", "sfx_wpn_punch1"), ("deathSfx", "sfx_deathscream_alien2"),
    };

    private static readonly (string field, string asset)[] SceneDefaults =
    {
        ("keyPickupSfx", "sfx_coin_single1"), ("doorOpenSfx", "sfx_movement_dooropen1"), ("resultSfx", "sfx_sounds_fanfare1"),
    };

    // ---------- 菜单 1：一键生成 ----------

    [MenuItem("Tools/战斗美术/1. 一键生成：导入设置、切帧、动画、Animator、特效 Prefab")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(OutDir);

        var missing = Sheets.Where(s => !File.Exists(s.path)).Select(s => s.path).ToList();
        if (missing.Count > 0)
        {
            Debug.LogError("战斗美术：找不到这些图，请检查路径：\n" + string.Join("\n", missing));
            return;
        }

        // 1) 每组算 PPU 和身高（像素）
        var groupPPU = new Dictionary<string, int>();
        var groupHeightPx = new Dictionary<string, int>();

        foreach (var g in Groups)
        {
            if (g.fixedPPU > 0)
            {
                groupPPU[g.name] = g.fixedPPU;
                continue;
            }

            var refSheet = Sheets.First(s => s.path == g.refSheet);
            var pixels = LoadPixels(refSheet.path, out int w, out int h);
            RectInt? b = OpaqueBounds(pixels, w, h, refSheet, 0, 0);

            int heightPx = b.HasValue ? b.Value.height : refSheet.cellH;
            groupHeightPx[g.name] = heightPx;
            groupPPU[g.name] = Mathf.Max(1, Mathf.RoundToInt(heightPx / g.targetHeight));
        }

        // 2) 导入设置 + 切帧
        foreach (var s in Sheets)
            SliceSheet(s, groupPPU[s.group], groupHeightPx.TryGetValue(s.group, out int hp) ? hp : 0);

        AssetDatabase.Refresh();

        // 3) 动画片段
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var c in Clips)
        {
            var clip = BuildClip(c);
            if (clip != null)
                clips[c.group + "/" + c.name] = clip;
        }

        // 4) Animator Controller
        BuildHeroController(clips);
        BuildOgreController(clips);
        BuildDemonController(clips);

        // 5) 特效 Prefab
        foreach (var name in EffectPrefabs)
        {
            if (clips.TryGetValue("FX/" + name, out var clip))
                BuildEffectPrefab(name, clip);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"战斗美术：生成完成，输出在 {OutDir}。PPU：" +
                  string.Join("，", groupPPU.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    // ---------- 菜单 2：缩放合并进碰撞体 ----------

    [MenuItem("Tools/战斗美术/2. 选中角色：把缩放合并进碰撞体")]
    public static void BakeScaleIntoCollider()
    {
        foreach (var go in Selection.gameObjects)
        {
            var col = go.GetComponent<BoxCollider2D>();
            Vector3 s = go.transform.localScale;

            if (col == null || (Mathf.Approximately(s.x, 1f) && Mathf.Approximately(s.y, 1f)))
            {
                Debug.Log($"战斗美术：{go.name} 没有 BoxCollider2D 或缩放已经是 1，跳过。", go);
                continue;
            }

            Undo.RecordObject(col, "Bake scale");
            Undo.RecordObject(go.transform, "Bake scale");

            col.size = new Vector2(col.size.x * Mathf.Abs(s.x), col.size.y * Mathf.Abs(s.y));
            col.offset = new Vector2(col.offset.x * s.x, col.offset.y * s.y);

            // 子物体保持原来的世界大小和位置。
            foreach (Transform child in go.transform)
            {
                Undo.RecordObject(child, "Bake scale");
                child.localPosition = Vector3.Scale(child.localPosition, s);
                child.localScale = Vector3.Scale(child.localScale, s);
            }

            go.transform.localScale = Vector3.one;
            Debug.Log($"战斗美术：{go.name} 缩放已合并，碰撞体大小 {col.size}。", go);
        }
    }

    // ---------- 菜单 3：填默认特效和音效 ----------

    [MenuItem("Tools/战斗美术/3. 当前场景：Boss、主角、小怪、场景控制器的空槽位填上默认特效和音效")]
    public static void FillDefaults()
    {
        int filled = 0;

        foreach (var c in Object.FindObjectsByType<BattleBossController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            filled += Fill(c, BossDefaults);

        foreach (var c in Object.FindObjectsByType<BattlePlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            filled += Fill(c, PlayerDefaults);

        foreach (var c in Object.FindObjectsByType<BattleEnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            filled += Fill(c, EnemyDefaults);

        foreach (var c in Object.FindObjectsByType<BattleSceneController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            filled += Fill(c, SceneDefaults);

        Debug.Log($"战斗美术：填了 {filled} 个空槽位。已有内容的槽位没有改动。记得保存场景（Ctrl+S）。");
    }

    // 只填空着的槽位，已经拖了东西的不覆盖。
    private static int Fill(Object target, (string field, string asset)[] defaults)
    {
        var so = new SerializedObject(target);
        int count = 0;

        foreach (var (field, assetName) in defaults)
        {
            var prop = so.FindProperty(field);
            if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogWarning($"战斗美术：{target.name} 没有字段 {field}。", target);
                continue;
            }

            if (prop.objectReferenceValue != null)
                continue;

            Object asset = assetName.StartsWith("FX_")
                ? AssetDatabase.LoadAssetAtPath<GameObject>($"{OutDir}/FX/{assetName}.prefab")
                : FindAudioClip(assetName);

            if (asset == null)
            {
                Debug.LogWarning($"战斗美术：找不到 {assetName}（{target.name}.{field}）。特效请先运行菜单 1。", target);
                continue;
            }

            prop.objectReferenceValue = asset;
            count++;
        }

        if (count > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(((Component)target).gameObject.scene);
        }

        return count;
    }

    private static AudioClip FindAudioClip(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{fileName} t:AudioClip", new[] { "Assets/Art/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == fileName)
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        return null;
    }

    // ---------- 切帧 ----------

    private static void SliceSheet(Sheet s, int ppu, int groupHeightPx)
    {
        var pixels = LoadPixels(s.path, out int texW, out int texH);
        int cols = texW / s.cellW;
        int rows = texH / s.cellH;

        // 角色：用这张图第一帧量出脚底和身体中线，所有帧共用，动画不会左右抖。
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        if (!s.centerPivot)
        {
            RectInt? b = OpaqueBounds(pixels, texW, texH, s, 0, 0);
            if (b.HasValue)
            {
                float cx = b.Value.x + b.Value.width * 0.5f;
                float cy = b.Value.y + groupHeightPx * 0.5f;
                pivot = new Vector2(cx / s.cellW, cy / s.cellH);
            }
        }

        string baseName = Path.GetFileNameWithoutExtension(s.path);
        var rects = new List<SpriteRect>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!OpaqueBounds(pixels, texW, texH, s, r, c).HasValue)
                    continue;

                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{r}_{c}",
                    rect = new Rect(c * s.cellW, texH - (r + 1) * s.cellH, s.cellW, s.cellH),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = GUID.Generate()
                });
            }
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(s.path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // 同名的帧保留原来的 ID，重新生成时动画引用不会断。
        var oldIds = provider.GetSpriteRects().ToDictionary(x => x.name, x => x.spriteID);
        foreach (var rect in rects)
        {
            if (oldIds.TryGetValue(rect.name, out var id))
                rect.spriteID = id;
        }

        provider.SetSpriteRects(rects.ToArray());

        var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider != null)
            nameProvider.SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));

        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Color32[] LoadPixels(string path, out int w, out int h)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        w = tex.width;
        h = tex.height;
        var pixels = tex.GetPixels32();
        Object.DestroyImmediate(tex);
        return pixels;
    }

    // 返回某一格里不透明像素的范围（坐标相对这一格的左下角）；全透明返回 null。
    private static RectInt? OpaqueBounds(Color32[] pixels, int texW, int texH, Sheet s, int row, int col)
    {
        int x0 = col * s.cellW;
        int y0 = texH - (row + 1) * s.cellH;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

        for (int y = 0; y < s.cellH; y++)
        {
            int py = y0 + y;
            if (py < 0 || py >= texH) continue;

            for (int x = 0; x < s.cellW; x++)
            {
                int px = x0 + x;
                if (px >= texW) break;

                if (pixels[py * texW + px].a > 10)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < 0)
            return null;

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    // ---------- 动画片段 ----------

    private static AnimationClip BuildClip(Clip c)
    {
        string baseName = Path.GetFileNameWithoutExtension(c.sheet);

        var all = AssetDatabase.LoadAllAssetsAtPath(c.sheet).OfType<Sprite>()
            .Select(sp => new { sp, parts = sp.name.Substring(baseName.Length + 1).Split('_') })
            .Select(x => new { x.sp, r = int.Parse(x.parts[0]), c = int.Parse(x.parts[1]) })
            .OrderBy(x => x.r).ThenBy(x => x.c)
            .ToList();

        var frames = all.Where(x => c.row < 0 || x.r == c.row).Select(x => x.sp).ToList();
        frames = frames.Skip(c.from).ToList();
        if (c.count > 0)
            frames = frames.Take(c.count).ToList();

        if (frames.Count == 0)
        {
            Debug.LogError($"战斗美术：动画 {c.group}/{c.name} 没有取到任何帧，请检查行号或帧范围。");
            return null;
        }

        bool isEffect = c.group == "FX";
        var keys = new List<ObjectReferenceKeyframe>();

        for (int i = 0; i < frames.Count; i++)
            keys.Add(new ObjectReferenceKeyframe { time = i / c.fps, value = frames[i] });

        // 不循环的动画补一帧，让最后一帧也停留一帧的时间；特效最后变空，播完就看不见。
        if (!c.loop)
            keys.Add(new ObjectReferenceKeyframe { time = frames.Count / c.fps, value = isEffect ? null : frames[frames.Count - 1] });

        var clip = new AnimationClip { frameRate = c.fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = c.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string dir = $"{OutDir}/{c.group}";
        Directory.CreateDirectory(dir);
        string path = $"{dir}/{c.group}_{c.name}.anim";

        // 已存在就覆盖内容、保留 GUID。
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ---------- Animator Controller ----------

    private static AnimatorController PrepareController(string group)
    {
        string path = $"{OutDir}/{group}/{group}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        if (controller == null)
            return AnimatorController.CreateAnimatorControllerAtPath(path);

        // 已存在就清空重建，保留 GUID，场景里的引用不会断。
        var sm = controller.layers[0].stateMachine;
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        controller.parameters = new AnimatorControllerParameter[0];
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, Dictionary<string, AnimationClip> clips, string key, string name, Vector3 pos)
    {
        var state = sm.AddState(name, pos);
        clips.TryGetValue(key, out var clip);
        state.motion = clip;
        return state;
    }

    private static AnimatorStateTransition Go(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        return t;
    }

    private static AnimatorStateTransition GoAfterFinish(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0f;
        return t;
    }

    private static AnimatorStateTransition FromAny(AnimatorStateMachine sm, AnimatorState to, bool canRestart)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.canTransitionToSelf = canRestart;
        return t;
    }

    private static void BuildHeroController(Dictionary<string, AnimationClip> clips)
    {
        var ctrl = PrepareController("Hero");
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("VelocityY", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        foreach (var trig in new[] { "Attack", "Dash", "Cast", "Heal", "Hurt" })
            ctrl.AddParameter(trig, AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var idle = AddState(sm, clips, "Hero/Idle", "Idle", new Vector3(300, 0));
        var run = AddState(sm, clips, "Hero/Run", "Run", new Vector3(550, 0));
        var jump = AddState(sm, clips, "Hero/Jump", "Jump", new Vector3(300, 120));
        var fall = AddState(sm, clips, "Hero/Fall", "Fall", new Vector3(550, 120));
        sm.defaultState = idle;

        Go(idle, run).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        Go(run, idle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        foreach (var ground in new[] { idle, run })
        {
            var up = Go(ground, jump);
            up.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            up.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");

            var down = Go(ground, fall);
            down.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            down.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocityY");
        }

        Go(jump, fall).AddCondition(AnimatorConditionMode.Less, 0f, "VelocityY");
        Go(jump, idle).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        Go(fall, idle).AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        // 招式：任何状态都能打断进入，播完回到待机。
        string[] actions = { "Attack", "Dash", "Cast", "Heal", "Hurt" };
        for (int i = 0; i < actions.Length; i++)
        {
            var st = AddState(sm, clips, "Hero/" + actions[i], actions[i], new Vector3(50 + i * 180, 280));
            FromAny(sm, st, actions[i] == "Attack").AddCondition(AnimatorConditionMode.If, 0, actions[i]);
            GoAfterFinish(st, idle);
        }

        var dead = AddState(sm, clips, "Hero/Dead", "Dead", new Vector3(50, 120));
        FromAny(sm, dead, false).AddCondition(AnimatorConditionMode.If, 0, "Dead");

        EditorUtility.SetDirty(ctrl);
    }

    private static void BuildOgreController(Dictionary<string, AnimationClip> clips)
    {
        var ctrl = PrepareController("Ogre");
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Windup", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        // 没有受伤和死亡动画：不加 Hurt、Dead 参数，代码会自动跳过，死亡时直接消失。

        var sm = ctrl.layers[0].stateMachine;
        var idle = AddState(sm, clips, "Ogre/Idle", "Idle", new Vector3(300, 0));
        var walk = AddState(sm, clips, "Ogre/Walk", "Walk", new Vector3(550, 0));
        var windup = AddState(sm, clips, "Ogre/Windup", "Windup", new Vector3(300, 150));
        var attack = AddState(sm, clips, "Ogre/Attack", "Attack", new Vector3(550, 150));
        sm.defaultState = idle;

        Go(idle, walk).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        Go(walk, idle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        FromAny(sm, windup, false).AddCondition(AnimatorConditionMode.If, 0, "Windup");
        Go(windup, attack).AddCondition(AnimatorConditionMode.If, 0, "Attack");
        Go(windup, idle).AddCondition(AnimatorConditionMode.IfNot, 0, "Windup");
        GoAfterFinish(attack, idle);

        EditorUtility.SetDirty(ctrl);
    }

    private static void BuildDemonController(Dictionary<string, AnimationClip> clips)
    {
        var ctrl = PrepareController("Demon");
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Windup", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Attacking", AnimatorControllerParameterType.Bool);
        foreach (var trig in new[] { "Charge", "Jump", "Beam" })
            ctrl.AddParameter(trig, AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var idle = AddState(sm, clips, "Demon/Idle", "Idle", new Vector3(300, 0));
        var walk = AddState(sm, clips, "Demon/Walk", "Walk", new Vector3(550, 0));
        var windup = AddState(sm, clips, "Demon/Windup", "Windup", new Vector3(300, 150));
        sm.defaultState = idle;

        Go(idle, walk).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        Go(walk, idle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        FromAny(sm, windup, false).AddCondition(AnimatorConditionMode.If, 0, "Windup");

        string[] attacks = { "Charge", "Jump", "Beam" };
        for (int i = 0; i < attacks.Length; i++)
        {
            var st = AddState(sm, clips, "Demon/" + attacks[i], attacks[i], new Vector3(100 + i * 220, 300));
            Go(windup, st).AddCondition(AnimatorConditionMode.If, 0, attacks[i]);
            Go(st, idle).AddCondition(AnimatorConditionMode.IfNot, 0, "Attacking");
        }

        Go(windup, idle).AddCondition(AnimatorConditionMode.IfNot, 0, "Windup");

        EditorUtility.SetDirty(ctrl);
    }

    // ---------- 特效 Prefab ----------

    private static void BuildEffectPrefab(string name, AnimationClip clip)
    {
        string dir = $"{OutDir}/FX";
        string ctrlPath = $"{dir}/FX_{name}.controller";

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

        var sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        var state = sm.AddState("Play");
        state.motion = clip;
        sm.defaultState = state;
        EditorUtility.SetDirty(ctrl);

        var go = new GameObject("FX_" + name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 50;

        var firstFrame = AnimationUtility.GetObjectReferenceCurve(
            clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
        if (firstFrame != null && firstFrame.Length > 0)
            sr.sprite = firstFrame[0].value as Sprite;

        go.AddComponent<Animator>().runtimeAnimatorController = ctrl;

        // 同路径保存会覆盖并保留 GUID。
        PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/FX_{name}.prefab");
        Object.DestroyImmediate(go);
    }
}
