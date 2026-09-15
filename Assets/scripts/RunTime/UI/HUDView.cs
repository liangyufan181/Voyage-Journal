using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class HUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text foodText;
    [SerializeField] private TMP_Text waterText;
    [SerializeField] private TMP_Text hullText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text questText;
    [SerializeField] private TMP_Text countdownText;

    [Header(" 结局视频/图片组件")]
    [SerializeField] private GameObject videoPanel;       
    [SerializeField] private VideoPlayer videoPlayer; 
    [SerializeField] private VideoClip victoryVideo;      // 通关胜利的 .mp4 视频
    [SerializeField] private VideoClip defeatVideo;       // 游戏失败的 .mp4 视频
    [SerializeField] private GameObject endingImagePanel; 
    [SerializeField] private Image endingImage;           
    [SerializeField] private Sprite victoryEndImage;      // WebGL 通关胜利静态图
    [SerializeField] private Sprite defeatEndImage;       // WebGL 游戏失败静态图
    private bool canReturnToMenu = false; 

    private void Start()
    {
        // 开局主动同步一次当前资源
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayerData != null)
        {
            var p = GameManager.Instance.CurrentPlayerData;
            UpdateResourceUI(p.Food, p.Water, p.Hull, p.Gold);
        }
    }

    private void OnEnable()
    {
        EventCenter.OnResourceChanged += UpdateResourceUI;

        EventCenter.OnQuestChanged += UpdateQuestUI;
        EventCenter.OnCountdownChanged += UpdateCountdownUI;
        EventCenter.OnCountdownChanged -= UpdateCountdownUI;
    }

    private void OnDisable()
    {
        EventCenter.OnResourceChanged -= UpdateResourceUI;
        EventCenter.OnQuestChanged -= UpdateQuestUI;

    }

    // 收到广播后，自动执行这个方法更新界面
    private void UpdateResourceUI(int food, int water, int hull, int gold)
    {
        foodText.text = food.ToString();
        waterText.text = water.ToString();
        hullText.text = hull.ToString();
        goldText.text = gold.ToString();
    }
    private void UpdateQuestUI(QuestState state)
    {
        switch (state)
        {
            case QuestState.ExploreIslands:
                // 动态获取大管家里记录的已探索数量
                int count = GameManager.Instance.VisitedIslandCount;
                questText.text = $"Goal: Explore all islands! ({count}/3)";
                break;
            case QuestState.ReturnHome:
                questText.text = "Go find the golden island!";
                break;
            case QuestState.Victory:
                questText.text = "VICTORY! The journey is the reward.";
                PlayEndingVideo(true);
                break;
            case QuestState.Defeat:
                questText.text = "GAME OVER! Your resources are exhausted in the deep ocean.";
                PlayEndingVideo(false);
                break;
        }
    }

    private void UpdateCountdownUI(int daysLeft)
    {
        countdownText.text = $"距下一次落笔：<size=120%>{daysLeft}</size> 天";
        // 只剩 1 天时，显示为红色，警告玩家
        countdownText.color = (daysLeft == 1) ? Color.red : Color.yellow;
    }

private void PlayEndingVideo(bool isVictory)
    {
#if UNITY_WEBGL
        // WebGL 不支持本地 mp4，降级为静态结局图，避免 loopPointReached 永不触发导致无法返回
        if (videoPanel != null) videoPanel.SetActive(false);
        if (endingImagePanel == null || endingImage == null) return;
        endingImage.sprite = isVictory ? victoryEndImage : defeatEndImage;
        endingImagePanel.SetActive(true);
        canReturnToMenu = true; // 静态图无需等待，点一下即可返回
        return;
#else
        if (videoPlayer == null || videoPanel == null) return;

        videoPanel.SetActive(true);
        canReturnToMenu = false;

        videoPlayer.clip = isVictory ? victoryVideo : defeatVideo;
        videoPlayer.Play();

        // 监听视频放完
        videoPlayer.loopPointReached += (vp) => 
        {
            canReturnToMenu = true;
        };
#endif
    }
    public void OnVideoScreenClicked()
    {
        if (canReturnToMenu)
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}