using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class MainMenuController : MonoBehaviour
{
    // 难度选择键名
    private const string DifficultyPrefKey = "GameDifficulty";

    public void StartGameEasy()
    {
        StartWithDifficulty("Easy");
    }

    public void StartGameHard()
    {
        StartWithDifficulty("Hard");
    }

    public void StartGame()
    {
        StartWithDifficulty("Normal");
    }

    // 写入难度后加载游戏场景
    private void StartWithDifficulty(string difficulty)
    {
        PlayerPrefs.SetString(DifficultyPrefKey, difficulty);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    // 点击退出游戏按钮时调用
    public void ExitGame()
    {
        Application.Quit(); 
        Debug.Log("Port returned. Game exited.");
    }
}
