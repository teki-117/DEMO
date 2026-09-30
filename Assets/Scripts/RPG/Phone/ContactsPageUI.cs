using System.Collections.Generic;
using UnityEngine;

// 联系人资料由 Inspector 配置，好感度来自现有角色状态系统。
public class ContactsPageUI : MonoBehaviour
{
    [Header("列表引用")]
    [SerializeField] private Transform content;
    [SerializeField] private ContactItemUI itemPrefab;

    [Header("本页显示的角色，先配置 sakura")]
    [SerializeField] private CharacterData[] contacts = new CharacterData[0];

    private readonly List<ContactItemUI> items = new List<ContactItemUI>();
    private bool hasStarted;
    private bool hasBuilt;

    private void Start()
    {
        // Start 时场景中所有对象的 Awake 已执行，适合读取 Manager。
        hasStarted = true;
        RefreshContacts();
    }

    private void OnEnable()
    {
        // 首次由 Start 刷新，以后每次打开联系人页面都刷新。
        if (hasStarted)
        {
            RefreshContacts();
        }
    }

    public void RefreshContacts()
    {
        var manager = CharacterStateManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("联系人页：找不到 CharacterStateManager，请从游戏原来的入口启动。", this);
            return;
        }

        if (content == null || itemPrefab == null)
        {
            Debug.LogError("联系人页：请绑定 Content 和 ContactItem 预制体。", this);
            return;
        }

        if (!hasBuilt)
        {
            if (contacts == null || contacts.Length == 0)
            {
                Debug.LogWarning("联系人页：请在 Contacts 数组中添加角色数据。", this);
                return;
            }

            var addedIds = new HashSet<string>();

            foreach (var data in contacts)
            {
                if (data == null || string.IsNullOrEmpty(data.characterID)) continue;
                if (!addedIds.Add(data.characterID)) continue;

                if (!IsRegistered(manager, data.characterID))
                {
                    Debug.LogWarning($"联系人页：角色 {data.characterID} 尚未加入 CharacterStateManager 的 All Character Datas。", this);
                    continue;
                }

                var item = Instantiate(itemPrefab, content, false);
                item.Initialize(data);
                item.gameObject.SetActive(true);
                items.Add(item);
            }

            // 没有生成任何条目时，允许后续打开页面再次尝试。
            hasBuilt = items.Count > 0;
        }

        foreach (var item in items)
        {
            if (item != null && item.Data != null)
            {
                item.SetAffection(manager.GetAffection(item.Data.characterID));
            }
        }
    }

    private bool IsRegistered(CharacterStateManager manager, string characterId)
    {
        if (manager.allCharacterDatas == null) return false;

        foreach (var data in manager.allCharacterDatas)
        {
            if (data != null && data.characterID == characterId)
            {
                return true;
            }
        }

        return false;
    }
}
