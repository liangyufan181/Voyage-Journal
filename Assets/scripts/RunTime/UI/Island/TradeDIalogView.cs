using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TradeDialogView : MonoBehaviour
{
    public static TradeDialogView Instance { get; private set; }

    [Header("面板与大背景")]
    [SerializeField] private GameObject islandStagePanel; 
    private GameObject activeTradePanel;                   

    [SerializeField] private Image bgImage; 


    [Header("专属货架引用")]
    [SerializeField] private GameObject benTradePanel;    // 浅滩岛货架
    [SerializeField] private GameObject elderTradePanel;  // 星落屿货架
    [SerializeField] private GameObject yunTradePanel;    // 绸市岛货架

    // 按剧情类型返回对应的专属货架面板
    public GameObject GetTradePanelFor(IslandStoryType type)
    {
        switch (type)
        {
            case IslandStoryType.Chapter2_Ben: return benTradePanel;
            case IslandStoryType.Chapter3_Elder: return elderTradePanel;
            case IslandStoryType.Chapter4_Yun: return yunTradePanel;
            default: return null;
        }
    }

    private int currentSpecialtyPrice;
    private IslandStoryType currentIslandType;

    [Header("悬浮提示框 UI")]
    [SerializeField] private GameObject tooltipPanel; // 提示框大面板
    [SerializeField] private TMP_Text tooltipText;     // 提示框文字组件

    // 显示提示框
    public void ShowTooltip(string description, Vector3 position)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipPanel.SetActive(true);
        tooltipText.text = description;

        // 将提示框坐标定位在鼠标右下方
        tooltipPanel.transform.position = position + new Vector3(200, -160, 0);
    }

    // 隐藏提示框
    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        islandStagePanel.SetActive(false);
    }


    public void SetupShopData(Sprite bg, GameObject localPanel, int specPrice, IslandStoryType islandType)
    {
        if (bgImage != null && bg != null)
        {
            bgImage.sprite = bg;
        }

        if (activeTradePanel != null) activeTradePanel.SetActive(false);
        activeTradePanel = localPanel;
        if (activeTradePanel != null) activeTradePanel.SetActive(false);

        currentSpecialtyPrice = specPrice;
        currentIslandType = islandType;
    }
    // 弹出商店大面板与当前岛的专属货架弹窗
    public void Open()
    {
        if (islandStagePanel != null)
        {
            islandStagePanel.SetActive(true); //先点亮商店总舞台
        }

        if (activeTradePanel != null)
        {
            activeTradePanel.SetActive(true);
        }
    }

    public void Close()
    {
        if (activeTradePanel != null) activeTradePanel.SetActive(false); // 关闭专属货架
        if (islandStagePanel != null) islandStagePanel.SetActive(false);
    }


    public void BuyFood()
    {
        var data = GameManager.Instance.CurrentPlayerData;
        if (data.Gold >= 50) data.AddResources(10, 0, 0, -50);
    }

    public void BuyWater()
    {
        var data = GameManager.Instance.CurrentPlayerData;
        if (data.Gold >= 40) data.AddResources(0, 10, 0, -40);
    }

    public void RepairHull()
    {
        var data = GameManager.Instance.CurrentPlayerData;
        if (data.Gold >= 110) data.AddResources(0, 0, 25, -110);
    }


    public void BuySpecialty()
    {
        // 自动根据当前踩中的岛屿，去执行对应的特色升级
        if (currentIslandType == IslandStoryType.Chapter2_Ben)
        {
            BuyHerbalUpgrade();
        }
        else if (currentIslandType == IslandStoryType.Chapter3_Elder)
        {
            BuyAstrolabeUpgrade();
        }
        else if (currentIslandType == IslandStoryType.Chapter4_Yun)
        {
            BuySailUpgrade();
        }
    }

    public void SellSurplusRations()
    {
        var data = GameManager.Instance.CurrentPlayerData;

        // 必须确保船上的食物和水都大于 5，才允许折价变卖
        if (data.Food >= 5 && data.Water >= 5)
        {
            data.AddResources(-5, -5, 0, 38);
        }
    }

    public void BuyHerbalUpgrade()
    {
        var data = GameManager.Instance.CurrentPlayerData;
        if (data.Gold >= 15 && data.Food >= 10)
        {
            data.AddResources(10, 0, 0, -15);
            GameManager.Instance.isHerbalCrafted = true;
        }
    }

    public void BuyAstrolabeUpgrade()
    {
        var data = GameManager.Instance.CurrentPlayerData;
        if (data.Gold >= 180)
        {
            data.AddResources(0, 0, 0, -180);
            GameManager.Instance.isAstrolabeCalibrated = true;
        }
    }

    public void BuySailUpgrade()
    {
        var data = GameManager.Instance.CurrentPlayerData;
        if (data.Gold >= 250)
        {
            data.AddResources(0, 0, 0, -250);
            GameManager.Instance.isSailReinforced = true;
        }
    }

    public void BuyInfo()
    {
        var playerData = GameManager.Instance.CurrentPlayerData;

        if (playerData.Gold >= 60)
        {
            playerData.AddResources(0, 0, 0, -60); 

            //  随机挑一个尚未到访过的岛，播放它的情报图
            int infoCGIndex = GameManager.Instance.PickRandomUnvisitedInfoIndex();

            if (StoryCGPlayer.Instance != null)
            {
                StoryCGPlayer.Instance.PlayStoryCG(infoCGIndex);
            }
        }
    }
}