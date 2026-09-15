using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryCGPlayer : MonoBehaviour
{
    public static StoryCGPlayer Instance { get; private set; }

    [Header("CG UI 组件")]
    [SerializeField] private GameObject cgPanel;
    [SerializeField] private Image cgImage;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private GameObject skipButton; // 一键跳过按钮

    [SerializeField] private GameObject infoPanel;      // 情报小面板容器
    [SerializeField] private Image infoPanelImage;      // 小面板上的情报背景图
    [SerializeField] private TMP_Text infoText;         // 小面板上显示诗句的情报文字

    [Header("第一章：启程 (0,0) 的剧情插画")]
    [SerializeField] private List<Sprite> ch1Images;
    [SerializeField] private List<string> ch1Subtitles;

    [Header("第二章：初遇 (本) 的剧情插画")]
    [SerializeField] private List<Sprite> ch2Images;
    [SerializeField] private List<string> ch2Subtitles;

    [Header("第三章：守望 (老者) 的剧情插画")]
    [SerializeField] private List<Sprite> ch3Images;
    [SerializeField] private List<string> ch3Subtitles;

    [Header("第四章：邂逅 (芸) 的剧情插画")]
    [SerializeField] private List<Sprite> ch4Images;
    [SerializeField] private List<string> ch4Subtitles;

    [Header("第五章：归根 (返回0,0) 的剧情插画")]
    [SerializeField] private List<Sprite> ch5Images;
    [SerializeField] private List<string> ch5Subtitles;

    [Header("第一章情报：手绘浅滩海路线索图")]
    [SerializeField] private Sprite ch1InfoImage;
    [SerializeField] private string ch1InfoSubtitle;

    [Header("第二章情报：本的星纹贝壳线索图")]
    [SerializeField] private Sprite ch2InfoImage;
    [SerializeField] private string ch2InfoSubtitle;

    [Header("第三章情报：老者的黄铜星盘线索图")]
    [SerializeField] private Sprite ch3InfoImage;
    [SerializeField] private string ch3InfoSubtitle;

    private List<Sprite> currentImages;
    private List<string> currentSubtitles;
    private int currentSlideIndex = 0;
    private int currentPlayingIndex = 1;
    private Coroutine typewriterCoroutine;


    private bool IsInfoStory => currentPlayingIndex == 11 || currentPlayingIndex == 12 || currentPlayingIndex == 13;
    // 情报用 infoText，普通剧情用 subtitleText
    private TMP_Text ActiveText => IsInfoStory ? infoText : subtitleText;

    private void Awake()
    {
        Instance = this;
        cgPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        fadeOverlay.gameObject.SetActive(false);
    }

    private void Start()
    {
        PlayStoryCG(1);
    }
    public void PlayStoryCG(int storyIndex)
    {
        currentSlideIndex = 0;
        currentPlayingIndex = storyIndex;
        bool isInfo = IsInfoStory;
        if (isInfo)
        {
            if (cgPanel != null) cgPanel.SetActive(false);
            if (cgImage != null) cgImage.gameObject.SetActive(false);
            if (subtitleText != null) subtitleText.gameObject.SetActive(false);
            if (infoPanel != null) infoPanel.SetActive(true);
        }
        else
        {
            cgPanel.SetActive(true);
            if (cgImage != null) cgImage.gameObject.SetActive(true);
            if (subtitleText != null) subtitleText.gameObject.SetActive(true);
            if (infoPanel != null) infoPanel.SetActive(false);
        }
        // 显示一键跳过按钮
        if (skipButton != null) skipButton.SetActive(true);
        // 获取大管家里本次开局随机生成的三个岛屿坐标
        Vector2Int ben = GameManager.Instance.BenIslandCoords;
        Vector2Int elder = GameManager.Instance.ElderIslandCoords;
        Vector2Int yun = GameManager.Instance.YunIslandCoords;

        if (storyIndex == 1)
        {
            currentImages = ch1Images;
            currentSubtitles = ch1Subtitles;

        }
        else if (storyIndex == 2)
        {
            currentImages = ch2Images;
            currentSubtitles = ch2Subtitles;
        }
        else if (storyIndex == 3)
        {
            currentImages = ch3Images;
            currentSubtitles = ch3Subtitles;
        }
        else if (storyIndex == 4)
        {
            currentImages = ch4Images;
            currentSubtitles = ch4Subtitles;
        }
        else if (storyIndex == 5)
        {
            currentImages = ch5Images;
            currentSubtitles = ch5Subtitles;
        }
        else if (storyIndex == 11) 
        {
            currentImages = new List<Sprite> { ch1InfoImage };
            Vector2Int c = InfoCoord(GameManager.Instance.BenIslandCoords);
            currentSubtitles = new List<string> { ch1InfoSubtitle + $"\n\n{c.x}座浅滩缀碧波，{c.y}点白帆引归途。" };
        }
        else if (storyIndex == 12) 
        {
            currentImages = new List<Sprite> { ch2InfoImage };
            Vector2Int c = InfoCoord(GameManager.Instance.ElderIslandCoords);
            currentSubtitles = new List<string> { ch2InfoSubtitle + $"\n\n{c.x}星垂落断崖边，{c.y}重海浪绕山岩。" };
        }
        else if (storyIndex == 13) 
        {
            currentImages = new List<Sprite> { ch3InfoImage };
            Vector2Int c = InfoCoord(GameManager.Instance.YunIslandCoords);
            currentSubtitles = new List<string> { ch3InfoSubtitle + $"\n\n{c.x}道商帆连云起，{c.y}街灯火不夜天。" };
        }

        if (currentImages != null && currentImages.Count > 0)
        {
            StartCoroutine(TransitionToSlide(0));
        }
    }

    // 返航阶段(已探索完所有岛)时，情报坐标一律指向出发点 (0,0)
    private Vector2Int InfoCoord(Vector2Int islandCoord)
    {
        return GameManager.Instance.CurrentQuest == QuestState.ReturnHome ? Vector2Int.zero : islandCoord;
    }

    public void NextSlide()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
            if (ActiveText != null) ActiveText.text = currentSubtitles[currentSlideIndex];
            return;
        }

        currentSlideIndex++;
        if (currentSlideIndex < currentImages.Count)
        {
            StartCoroutine(TransitionToSlide(currentSlideIndex));
        }
        else
        {
            if (currentPlayingIndex == 5)
            {
                StartCoroutine(EndCGAndTriggerVictory());
            }
            else if (currentPlayingIndex == 1)
            {
                // 第一章启程播完：直接关闭进入大地图
                StartCoroutine(CloseCGDirectly());
            }
            else if (IsInfoStory)
            {
                StartCoroutine(EndInfoStory());
            }
            else
            {
                // 2、3、4章普通剧情看完：自动转场并重新打开商店
                StartCoroutine(EndCGAndOpenShop());
            }
        }
    }

    public void SkipCurrentStory()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        // 根据当前章节路由到对应的剧情结束转场
        if (currentPlayingIndex == 5)
        {
            StartCoroutine(EndCGAndTriggerVictory());
        }
        else if (currentPlayingIndex == 1)
        {
            StartCoroutine(CloseCGDirectly());
        }
        else if (IsInfoStory)
        {
            StartCoroutine(EndInfoStory());
        }
        else
        {
            StartCoroutine(EndCGAndOpenShop());
        }
    }
    private IEnumerator TransitionToSlide(int index)
    {
        // 情报小面板叠在商店上：不做全屏黑闪；普通剧情才全屏淡入淡出
        if (!IsInfoStory)
        {
            fadeOverlay.gameObject.SetActive(true);
            float fe = 0;
            while (fe < 0.4f)
            {
                fe += Time.deltaTime;
                fadeOverlay.color = new Color(0, 0, 0, fe / 0.4f);
                yield return null;
            }
        }

        // 情报写在独立小面板上，普通剧情写在全屏 CG 上
        if (IsInfoStory)
        {
            if (infoPanelImage != null && currentImages[index] != null) infoPanelImage.sprite = currentImages[index];
        }
        else
        {
            cgImage.sprite = currentImages[index];
        }
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypeText(currentSubtitles[index]));

        if (!IsInfoStory)
        {
            float fe2 = 0;
            while (fe2 < 0.4f)
            {
                fe2 += Time.deltaTime;
                fadeOverlay.color = new Color(0, 0, 0, 1f - (fe2 / 0.4f));
                yield return null;
            }
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    private IEnumerator EndInfoStory()
    {
        if (skipButton != null) skipButton.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        yield break;
    }

    private IEnumerator TypeText(string text)
    {
        TMP_Text target = ActiveText;
        if (target == null) yield break;
        target.text = "";
        foreach (char c in text)
        {
            target.text += c;
            yield return new WaitForSeconds(0.05f);
        }
        typewriterCoroutine = null;
    }

    private IEnumerator EndCGAndOpenShop()
    {
        fadeOverlay.gameObject.SetActive(true);
        float elapsed = 0;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0, 0, 0, elapsed / 0.5f);
            yield return null;
        }

        if (skipButton != null) skipButton.SetActive(false);
        cgPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);

        if (TradeDialogView.Instance != null)
        {
            TradeDialogView.Instance.Open();
        }

        elapsed = 0;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0, 0, 0, 1f - (elapsed / 0.5f));
            yield return null;
        }
        fadeOverlay.gameObject.SetActive(false);
    }

    private IEnumerator CloseCGDirectly()
    {
        fadeOverlay.gameObject.SetActive(true);
        float elapsed = 0;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0, 0, 0, elapsed / 0.5f);
            yield return null;
        }

        if (skipButton != null) skipButton.SetActive(false);
        cgPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);

        // 第一章启程动画看完/跳过之后，自动弹出新手教程（Tab 键也可随时回看）
        if (TutorialView.Instance != null) TutorialView.Instance.Open();

        elapsed = 0;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0, 0, 0, 1f - (elapsed / 0.5f));
            yield return null;
        }
        fadeOverlay.gameObject.SetActive(false);
    }

    private IEnumerator EndCGAndTriggerVictory()
    {
        fadeOverlay.gameObject.SetActive(true);
        float elapsed = 0;
        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0, 0, 0, elapsed / 0.8f);
            yield return null;
        }

        if (skipButton != null) skipButton.SetActive(false);
        cgPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);

        GameManager.Instance.UpdateQuest(QuestState.Victory);

        elapsed = 0;
        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0, 0, 0, 1f - (elapsed / 0.8f));
            yield return null;
        }
        fadeOverlay.gameObject.SetActive(false);
    }
}