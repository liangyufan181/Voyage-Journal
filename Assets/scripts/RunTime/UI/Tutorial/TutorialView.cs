using UnityEngine;
using UnityEngine.UI;


public class TutorialView : MonoBehaviour
{
    public static TutorialView Instance { get; private set; }

    [Header("面板引用")]
    [SerializeField] private GameObject panel;        
    [SerializeField] private Image display;           
    [SerializeField] private Sprite[] pages;          

    [Header("按钮（用代码自动绑定）")]
    [SerializeField] private Button prevBtn;        
    [SerializeField] private Button nextBtn;        
    [SerializeField] private Button closeBtn;          

    private int index = 0;
    private bool isOpen = false;

    private void Awake()
    {
        Instance = this;

        if (panel != null) panel.SetActive(false);

        if (prevBtn != null) prevBtn.onClick.AddListener(PrevPage);
        if (nextBtn != null) nextBtn.onClick.AddListener(NextPage);
        if (closeBtn != null) closeBtn.onClick.AddListener(Close);
    }

    private void Update()
    {
        // 按 Tab 键开/关教程
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        index = 0;
        isOpen = true;
        if (panel != null) panel.SetActive(true);
        RefreshButtons();
        ShowPage();
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    public void NextPage()
    {
        if (index < pages.Length - 1) { index++; ShowPage(); }
        RefreshButtons();
    }

    public void PrevPage()
    {
        if (index > 0) { index--; ShowPage(); }
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (prevBtn != null) prevBtn.gameObject.SetActive(index > 0);
        if (nextBtn != null) nextBtn.gameObject.SetActive(index < pages.Length - 1);
    }

    private void ShowPage()
    {
        if (display == null || pages == null || pages.Length == 0) return;
        display.sprite = pages[Mathf.Clamp(index, 0, pages.Length - 1)];
    }
}