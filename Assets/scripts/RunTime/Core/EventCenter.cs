using System;

public static class EventCenter
{
    public static event Action<int, int, int, int> OnResourceChanged;
    public static void BroadcastResourceChanged(int food, int water, int hull, int gold)
    {
        OnResourceChanged?.Invoke(food, water, hull, gold);
    }

    public static event Action<QuestState> OnQuestChanged;
    public static void BroadcastQuestChanged(QuestState newState)
    {
        OnQuestChanged?.Invoke(newState);
    }

    public static event Action<int> OnCountdownChanged;
    public static void BroadcastCountdownChanged(int daysLeft)
    {
        OnCountdownChanged?.Invoke(daysLeft);
    }
}

public enum QuestState
{
    ExploreIslands, // 探索另外三个岛屿
    ReturnHome,     // 重返故乡
    Victory,        // 胜利通关
    Defeat          // 游戏失败
}