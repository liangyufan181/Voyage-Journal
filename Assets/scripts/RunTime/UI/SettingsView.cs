using UnityEngine;
using UnityEngine.UI;

public class SettingsView : MonoBehaviour
{
    public static SettingsView Instance { get; private set; }

    [Header("设置面板 UI")]
    [SerializeField] private Button settingsButton;    
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Toggle summaryToggle;      
    [SerializeField] private Button closeButton;   

    public static bool ShowNightSummary = true;

    private void Awake()
    {
        Instance = this;
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Start()
    {

        if (settingsButton != null) settingsButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (summaryToggle != null)
        {
            summaryToggle.isOn = !ShowNightSummary;
            summaryToggle.onValueChanged.AddListener(ToggleNightSummary);
        }
    }

    public void Open()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void Close()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void ToggleNightSummary(bool isChecked)
    {
        ShowNightSummary = !isChecked;
    }
}