using UnityEngine;

public enum TimePeriod
{
    Morning,
    AfterSchool,
    Evening,
    Night
}

public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance { get; private set; }

    [SerializeField] private int currentDay = 1;
    [SerializeField] private TimePeriod currentPeriod = TimePeriod.Morning;

    public int CurrentDay => currentDay;
    public TimePeriod CurrentPeriod => currentPeriod;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetTime()
    {
        currentDay = 1;
        currentPeriod = TimePeriod.Morning;
    }

    public void AdvanceTime()
    {
        if (currentPeriod == TimePeriod.Night)
        {
            currentDay++;
            currentPeriod = TimePeriod.Morning;
        }
        else
        {
            currentPeriod = (TimePeriod)((int)currentPeriod + 1);
        }

        Debug.Log($"时间推进：{GetDisplayText()}");
    }

    public string GetDisplayText()
    {
        string periodName;

        switch (currentPeriod)
        {
            case TimePeriod.Morning:
                periodName = "早晨";
                break;
            case TimePeriod.AfterSchool:
                periodName = "放学后";
                break;
            case TimePeriod.Evening:
                periodName = "傍晚";
                break;
            default:
                periodName = "夜晚";
                break;
        }

        return $"第 {currentDay} 天 · {periodName}";
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}