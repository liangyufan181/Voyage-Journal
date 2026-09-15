using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SailingCycleManager : MonoBehaviour
{
    public static SailingCycleManager Instance { get; private set; }

    private ShipEntity ship;
    private ShipMapDetector detector;

    private int daysLeftInCycle = 3;
    private int accumulatedFood = 0;
    private int accumulatedWater = 0;
    private int accumulatedHull = 0;
    private List<string> nightlyStories = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ship = GetComponent<ShipEntity>();
        detector = GetComponent<ShipMapDetector>();
        EventCenter.BroadcastCountdownChanged(daysLeftInCycle);
    }

    public void ProcessDayTurn(Vector2Int direction)
    {
        var playerData = GameManager.Instance.CurrentPlayerData;

        // 步骤 1：检测洋流...
        string moveStory = "Normal";
        Collider2D[] startColliders = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        Vector2Int currentDir = Vector2Int.zero; // 记录当前格子洋流方向（无洋流则为 zero）
        foreach (var col in startColliders)
        {
            if (col.CompareTag("Current"))
            {
                // 自动点亮洋流图片
                SpriteRenderer[] srs = col.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in srs) sr.enabled = true;

                CurrentEffect ce = col.GetComponent<CurrentEffect>();
                if (ce != null) currentDir = ce.driftDirection;
                // 洋流不改变移动（moveStory 保持 Normal），只影响物资消耗（见下方洋流物资影响段）
                break;
            }
        }

        // 升级效果一：风帆加固计算
        int foodCost = 1;
        int waterCost = 1;
        int moveDistance = 1;
        string weatherStory = "Normal";

        // 洋流物资影响：不改变移动格数，只影响物资消耗 
        if (currentDir != Vector2Int.zero)
        {
            if (direction == currentDir)
            {
                // 顺洋流：今日不消耗任何物资
                foodCost = 0;
                waterCost = 0;
                weatherStory = "CurrentFavored";
                Debug.Log("[洋流] 顺流而下，滴水未进今日物资零消耗！");
            }
            else if (direction == -currentDir)
            {
                // 逆洋流：顶浪行舟，物资消耗翻倍
                foodCost *= 2;
                waterCost *= 2;
                weatherStory = "CurrentAgainst";
                Debug.Log("[洋流] 逆流而上，顶浪行舟物资消耗翻倍！");
            }
            // 侧过（垂直方向）不受影响，维持正常消耗
        }

        Collider2D[] windColliders = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        foreach (var col in windColliders)
        {
            if (col.CompareTag("Wind"))
            {
                WindArea wind = col.GetComponent<WindArea>();
                if (wind != null)
                {
                    // 自动点亮风向图片（无论顺风/逆风/侧风都点亮，便于看清图标方向）
                    SpriteRenderer[] srs = col.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var sr in srs) sr.enabled = true;

                    if (direction == wind.windDirection)
                    {
                        // 顺风（与图标同向）：顺风助力，前进 2 格（加固风帆则 3 格）
                        moveDistance = GameManager.Instance.isSailReinforced ? 3 : 2;
                        weatherStory = "Tailwind";
                        Debug.Log($"<color=cyan>[风向{DirectionName(wind.windDirection)}] 顺风借力！乘风破浪冲 {moveDistance} 格！</color>");
                    }
                    else if (direction == -wind.windDirection)
                    {
                        // 逆风（与图标反向）：迎面狂风把船顶死，今日无法移动
                        moveDistance = 0;
                        weatherStory = "Deadwind";
                        Debug.Log($"<color=orange>[风向{DirectionName(wind.windDirection)}] 逆风！巨浪顶住船头，今日寸步难行！</color>");
                    }
                    // 侧风（垂直）：不受影响，保持默认移动 1 格
                    break;
                }
            }
        }

        bool wasFullSpeed = false;
        if (ship.isFullSpeedActive)
        {
            moveDistance *= 2;
            wasFullSpeed = true;
            ship.isFullSpeedActive = false;
            moveStory = "FullSpeed";
        }

        //升级效果二：草药干粮不消耗水计算
        if (GameManager.Instance.isHerbalCrafted)
        {
            waterCost = 0; // 如果配制了草药，今日移动不消耗任何淡水
        }

        // 步骤 3：扣除资源
        if (playerData.ConsumeResources(foodCost, waterCost))
        {
            // 消耗成功后，将草药状态重置
            if (GameManager.Instance.isHerbalCrafted)
            {
                GameManager.Instance.isHerbalCrafted = false; // 复位，下次需要重新去配制
                Debug.Log("<color=lime>[草药配制] 消耗了草药干粮，免去今日淡水消耗！</color>");
            }

            int todayFoodDelta = -foodCost;
            int todayWaterDelta = -waterCost;
            int todayHullDelta = 0;

            if (CheckDeathCondition(playerData)) return;

            // 逆风(/风等导致 moveDistance<=0)：船被顶在原位，只耗资源与天数，不结算当前格避免重复触发
            if (moveDistance <= 0)
            {
                int dayIndex0 = 4 - daysLeftInCycle;
                string deadStory = CompileSingleDayStory(weatherStory, moveStory, "None", "", dayIndex0);
                nightlyStories.Add(deadStory);

                daysLeftInCycle--;
                EventCenter.BroadcastCountdownChanged(daysLeftInCycle);

                if (daysLeftInCycle <= 0)
                {
                    Trigger3DaySettlement();
                }
                return;
            }

            //顺风/全速多格推进：逐格移动（每步都带切屏与边界检查），不会飘到禁地(X18/19、Y9/10)或越界
            for (int i = 0; i < moveDistance; i++)
            {
                if (!ship.StepOnce(direction)) break; // 前方被世界边界挡住：剩余步数作废
            }

            // 步骤 4：结算新格子

            string hazardStory = "None";
            string islandStory = "";
            Vector3Int postDeltas = detector.DetectAndResolveTile(wasFullSpeed, ref hazardStory, ref islandStory);

            todayFoodDelta += postDeltas.x;
            todayWaterDelta += postDeltas.y;
            todayHullDelta += postDeltas.z;

            if (CheckDeathCondition(playerData)) return;

            // 累加
            accumulatedFood += todayFoodDelta;
            accumulatedWater += todayWaterDelta;
            accumulatedHull += todayHullDelta;

            int dayIndex = 4 - daysLeftInCycle;
            string todayStoryText = CompileSingleDayStory(weatherStory, moveStory, hazardStory, islandStory, dayIndex);
            nightlyStories.Add(todayStoryText);

            daysLeftInCycle--;
            EventCenter.BroadcastCountdownChanged(daysLeftInCycle);

            if (daysLeftInCycle <= 0)
            {
                Trigger3DaySettlement();
            }
        }
        else
        {
            GameManager.Instance.UpdateQuest(QuestState.Defeat);
            Debug.LogError("<color=red>[死亡] 物资耗尽，无法航行！</color>");
        }
    }

    public void ProcessDriftSettle(Vector2Int direction)
    {
        string hStory = "None";
        string iStory = "";
        Vector3Int postDeltas = detector.DetectAndResolveTile(false, ref hStory, ref iStory);

        accumulatedFood += postDeltas.x;
        accumulatedWater += postDeltas.y;
        accumulatedHull += postDeltas.z;

        int dayIndex = 4 - daysLeftInCycle;
        string todayStoryText = CompileSingleDayStory("Normal", "Drift", hStory, iStory, dayIndex);
        nightlyStories.Add(todayStoryText);

        daysLeftInCycle--;
        EventCenter.BroadcastCountdownChanged(daysLeftInCycle);

        if (daysLeftInCycle <= 0)
        {
            Trigger3DaySettlement();
        }
    }

    private void Trigger3DaySettlement()
    {
        string finalCombinedStory = string.Join("\n\n", nightlyStories);
        Vector2Int currentGrid = new Vector2Int(Mathf.RoundToInt(ship.transform.position.x), Mathf.RoundToInt(ship.transform.position.y));

        // 设置开关：若玩家在设置里勾选了“关闭三步航海总结”，则跳过弹出手记
        if (SettingsView.ShowNightSummary && CaptainsJournalView.Instance != null)
        {
            CaptainsJournalView.Instance.ShowNightJournal(
                finalCombinedStory, accumulatedFood, accumulatedWater, accumulatedHull, currentGrid
            );
        }

        daysLeftInCycle = 3;
        accumulatedFood = 0;
        accumulatedWater = 0;
        accumulatedHull = 0;
        nightlyStories.Clear();
        EventCenter.BroadcastCountdownChanged(daysLeftInCycle);
    }

    private string CompileSingleDayStory(string weather, string move, string hazard, string island, int day)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"<b>【第 {day} 天·夜】</b> ");

        if (weather == "Tailwind") sb.Append("海上大风刮起。狂风在身后咆哮，风帆扯满，在黑夜中风驰电掣；");
        else if (weather == "Deadwind") sb.Append("迎面刮来顶头狂风，巨浪死死压住船头，水手们奋力划桨却也一整天没能推动一寸；");
        else if (weather == "CurrentFavored") sb.Append("小船顺流而下，借洋流势能毫无消耗地漂过一整夜，省下了宝贵的口粮与水；");
        else if (weather == "CurrentAgainst") sb.Append("我们奋力逆流而上，浪花拍碎在船头，水手们耗尽双倍体力口粮才顶住急流；");
        else sb.Append("夜里的海风很温和，海面平静得像镜子，船只平稳渡过了一夜；");

        if (move == "Current") sb.Append("沉睡中，静默洋流无声将我们向前推送了一格；");
        else if (move == "FullSpeed") sb.Append("船长下达冲刺死命令！小船拉满帆极速狂飙；");
        else if (move == "Drift") sb.Append("我们收起帆随波飘荡，海浪将船送至完全随机方位；");

        if (hazard == "Reef") sb.Append("惊雷震动！我们在黑夜中撞上暗礁受损！");
        else if (hazard == "ReefFullSpeed") sb.Append("灾难！极速冲刺下的船只猛烈撞碎在暗礁上！");
        else if (hazard == "Storm") sb.Append("突入暴风雨，补充淡水，但舵盘被锁死，船体剧烈打转！");

        if (!string.IsNullOrEmpty(island))
        {
            if (island == "Decoy") sb.Append("拂晓，前方出现荒滩，采得一些零星野果。");
            else sb.Append($"破晓，前方浮现岛屿，靠岸休整（进度：{island}）。");
        }
        else sb.Append("海平线依然无垠，我们迎来新一天的航程。");

        return sb.ToString();
    }

    private bool CheckDeathCondition(PlayerData data)
    {
        if (data.Food <= 0 || data.Water <= 0 || data.Hull <= 0)
        {
            GameManager.Instance.UpdateQuest(QuestState.Defeat);
            return true;
        }
        return false;
    }

    // 把方向向量转成简明方向标记，用于日志提示
    private string DirectionName(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return "↑";
        if (dir == Vector2Int.down) return "↓";
        if (dir == Vector2Int.left) return "←";
        if (dir == Vector2Int.right) return "→";
        return "?";
    }
}