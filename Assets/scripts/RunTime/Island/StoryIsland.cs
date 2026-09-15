using UnityEngine;

public enum IslandStoryType
{
    Chapter1_Departure, 
    Chapter2_Ben,       
    Chapter3_Elder,     
    Chapter4_Yun,       
    Chapter5_Return    
}


public class StoryIsland : MonoBehaviour
{
    [Header("选择这个物体对应的剧情")]
    public IslandStoryType storyType;

    [Header("该岛商店货架背景")]
    public Sprite islandBackground;

    [Header("该岛商店货架面板")]
    public GameObject localTradePanel;

    [Header("该岛特产设置")]
    public int specialtyPrice;

    public void OnLanded()
    {
        if (storyType == IslandStoryType.Chapter5_Return)
        {
            if (GameManager.Instance.CurrentQuest == QuestState.ReturnHome)
            {
                if (StoryCGPlayer.Instance != null)
                {
                    StoryCGPlayer.Instance.PlayStoryCG(5);
                }
            }
            return;
        }

        if (storyType == IslandStoryType.Chapter1_Departure) return;

        //  检查这个岛我以前来过吗
        bool alreadyVisited = GameManager.Instance.IsIslandVisited(transform.position);

        if (!alreadyVisited)
        {
            // 首次靠岸：先向大管家登记增加一次主线进度
            GameManager.Instance.RegisterIslandVisit(transform.position);
            ConfigureShopDataOnly();


            int cgIndex = 2;
            if (storyType == IslandStoryType.Chapter2_Ben) cgIndex = 2;
            else if (storyType == IslandStoryType.Chapter3_Elder) cgIndex = 3;
            else if (storyType == IslandStoryType.Chapter4_Yun) cgIndex = 4;

            if (StoryCGPlayer.Instance != null)
            {
                StoryCGPlayer.Instance.PlayStoryCG(cgIndex);
            }
        }
        else
        {
            // 老岛重游：直接配置并弹出商店
            ConfigureShopDataOnly();
            if (TradeDialogView.Instance != null)
            {
                TradeDialogView.Instance.Open();
            }
        }
    }

    private void ConfigureShopDataOnly()
    {
        if (TradeDialogView.Instance != null)
        {
            // 因为岛屿是预制体，无法直接引用"场景中的商店面板"。
            // 所以 localTradePanel 通常是 null，需回退由 TradeDialogView 按剧情类型自动匹配专属货架。
            GameObject panel = localTradePanel;
            if (panel == null)
            {
                panel = TradeDialogView.Instance.GetTradePanelFor(storyType);
            }

            TradeDialogView.Instance.SetupShopData(
                islandBackground,
                panel,
                specialtyPrice,
                storyType
            );
        }
    }
}
