using UnityEngine;

public class PartyDebugTools : MonoBehaviour
{
    [SerializeField] private CharacterData testMember;

    private PartyManager GetParty()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("队伍测试需要在运行中执行。", this);
            return null;
        }

        PartyManager party = PartyManager.Instance;

        if (party == null || !party.EnsureInitialized())
        {
            Debug.LogWarning("队伍测试：找不到已初始化的队伍。", this);
            return null;
        }

        return party;
    }

    [ContextMenu("队伍测试/1 加入测试角色")]
    private void JoinTestMember()
    {
        PartyManager party = GetParty();

        if (party == null || testMember == null)
            return;

        bool result = party.JoinMember(testMember.characterID);
        Debug.Log($"测试角色入队：{result}", this);
    }

    [ContextMenu("队伍测试/2 测试角色受伤25")]
    private void DamageTestMember()
    {
        PartyManager party = GetParty();

        if (party == null || testMember == null)
            return;

        bool result = party.TakeDamage(testMember.characterID, 25);
        Debug.Log($"测试角色受伤：{result}", this);
    }

    [ContextMenu("队伍测试/3 测试角色离队")]
    private void LeaveTestMember()
    {
        PartyManager party = GetParty();

        if (party == null || testMember == null)
            return;

        bool result = party.LeaveMember(testMember.characterID);
        Debug.Log($"测试角色离队：{result}", this);
    }

    [ContextMenu("队伍测试/4 打印队伍")]
    private void PrintParty()
    {
        PartyManager party = GetParty();

        if (party == null)
            return;

        Debug.Log($"团队资金：{party.Money}", this);

        foreach (string id in party.MemberIds)
        {
            PartyMemberState state = party.GetMemberState(id);
            CharacterData data = party.GetCharacterData(id);

            Debug.Log(
                $"{CharacterDisplayUtility.GetName(data)} " +
                $"HP {state.CurrentHP}/{state.MaxHP}，" +
                $"SP {state.CurrentSP}/{state.MaxSP}，" +
                $"攻击 {state.Attack}，防御 {state.Defense}",
                this);
        }
    }
}