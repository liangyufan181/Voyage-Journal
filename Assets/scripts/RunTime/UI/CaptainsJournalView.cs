using System.Text;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class CaptainsJournalView : MonoBehaviour
{
    public static CaptainsJournalView Instance { get; private set; }

    [Header("手记UI组件")]
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private TMP_Text ledgerText;
    [SerializeField] private Button nextDayButton;

    private int currentDay = 1;

    // 当手记面板显示时，IsOpen 为 true，用来锁死小船和技能
    public bool IsOpen => journalPanel != null && journalPanel.activeSelf;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        journalPanel.SetActive(false);
        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(CloseJournal);
        }
    }

    public void ShowDecoyIslandUI()
    {
        journalPanel.SetActive(true);
        dateText.text = $"第 {currentDay} 天 · 孤礁荒滩";
        contentText.text = "这里是一片荒芜的礁石滩。如爷爷所说，往北多是孤礁荒滩，一步两步仅千洼水泽。你在浅水洼中仅寻得零星野果，勉强果腹。此地不宜久留，切莫停靠。";

        StringBuilder ledger = new StringBuilder();
        ledger.Append("<b>【 锚 泊 坐 标 】</b>\n");
        ledger.Append("<color=#FF5555><b>[ 荒 滩 孤 礁 ]</b></color>\n\n");
        ledger.Append("食物：<color=#33FF33>+5</color> | ");
        ledger.Append("淡水：<color=#33FF33>+5</color> | ");
        ledger.Append("船体：0");
        ledgerText.text = ledger.ToString();
    }

    public void ShowNightJournal(string combinedStories, int foodDelta, int waterDelta, int hullDelta, Vector2Int coords)
    {
        journalPanel.SetActive(true);

        dateText.text = $"航海手札 · 航程周期总结";
        contentText.text = combinedStories;

        StringBuilder ledger = new StringBuilder();
        ledger.Append("<b>【 终 点 锚 泊 】</b>\n");
        ledger.Append($"<color=#FF5555><b>[ 经纬度：({coords.x}, {coords.y}) ]</b></color>\n\n");
        ledger.Append("----------------------\n");
        ledger.Append("<b>【 航程物资总账 】</b>\n\n");

        if (foodDelta < 0) ledger.Append($"食物总变化：<color=#FF3333>{foodDelta}</color>\n");
        else if (foodDelta > 0) ledger.Append($"食物总变化：<color=#33FF33>+{foodDelta}</color>\n");
        else ledger.Append("食物总变化：0\n");

        if (waterDelta < 0) ledger.Append($"淡水总变化：<color=#FF3333>{waterDelta}</color>\n");
        else if (waterDelta > 0) ledger.Append($"淡水总变化：<color=#33FF33>+{waterDelta}</color>\n");
        else ledger.Append("淡水总变化：0\n");

        if (hullDelta < 0) ledger.Append($"船体总变化：<color=#FF3333>{hullDelta}</color>\n");
        else if (hullDelta > 0) ledger.Append($"船体总变化：<color=#33FF33>+{hullDelta}</color>\n");
        else ledger.Append("船体总变化：0\n");

        ledgerText.text = ledger.ToString();
    }

    public void PlayChapterStory(GameChapter chapter)
    {
        journalPanel.SetActive(true);

        switch (chapter)
        {
            case GameChapter.Chapter1_Departure:
                dateText.text = "第一章：启程";
                contentText.text = "落日熔金，暮云四合。与漫天光辉相映衬的，是一望无际的蔚蓝大海……少年离开孤岛的那天清晨微风轻拂。爷爷告诉少年【年轻的时候曾去过一座漂亮的岛屿，上面有着浅白色的沙滩，一望无际的树林，依稀记得是顺流而行，具体的位置记不清了，不过你记着“往北多是孤礁荒滩，一步两步仅千洼水泽”，切莫停靠】。少年摊开爷爷给的地图，只身远航。";
                break;

            case GameChapter.Chapter2_FirstMeet:
                dateText.text = "第二章：初遇";
                contentText.text = "少年的前方出现了一片海滩。“陆地！”少年雀跃地叫喊。他意外地发现了一个受伤的黑人男孩本，用草药救下了他。本赠与少年一块打磨光滑的贝壳，贝壳内壁刻着细碎星纹诗句：【六星垂落断崖边，七重海浪绕山岩。四九远隔星云外，六三偏航失星轨。】少年再次起航。";
                break;

            case GameChapter.Chapter3_Watch:
                dateText.text = "第三章：守望";
                contentText.text = "无边暗蓝色深海环抱着这座孤立无援的小岛——星落屿。岛上唯有一位守星老者独居在此。老者将一块黄铜星盘递给少年，上面刻着【九街绸市临海生，三道清溪绕城门。八五雾锁无人市，十一荒滩无织纹。】“这里或许有你想要的答案。”少年在断崖刻下星星，与老者告别。他没注意到石碑角落刻着：“所有航线兜兜转转，最终都会回到最初的起点。”";
                break;

            case GameChapter.Chapter4_Encounter:
                dateText.text = "第四章：邂逅";
                contentText.text = "少年来到了一片瓷器的国度——绸市岛。他结识了卖纱的鲛人女孩芸。离别时，芸的海豚朋友告诉少年：“有一座在大海深处的岛屿，或许那就是你的梦想之岛，去吧，往西南方向”。";
                break;

            case GameChapter.Chapter5_Return:
                dateText.text = "第五章：归根";
                contentText.text = "少年划着疲惫的船只，来到了一片温暖熟悉的金色海域。熟悉的木屋，熟悉的港湾，满头白发的爷爷正轻轻拍打他的后背：“回来了就好，回来了就好”。\n\n<b>【尾声】</b>\n远方，是海图上无数未知坐标；行路却总藏满迷雾风浪。当你细数船身刻下的星辰，才会看清：你苦苦追寻的理想远方，早已藏在一一路途经的每一座岛屿坐标里。";
                break;
        }

        ledgerText.text = "<b>【 航 海 日 志 】</b>\n\n<color=#FF5555>[ 主 线 剧 情 解 锁 ]</color>\n\n新航道与壁垒已开通。\n请利用线索，寻找下一个岛屿坐标！";
    }

    public void CloseJournal()
    {
        currentDay++;
        journalPanel.SetActive(false);
        Debug.Log($"[拔锚启航] 迎来了第 {currentDay} 天的晨曦。小船重新获得控制。");
    }
}