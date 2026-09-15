using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameChapter
{
    Chapter1_Departure,  
    Chapter2_FirstMeet,  
    Chapter3_Watch,       
    Chapter4_Encounter,   
    Chapter5_Return       
}

public enum GameDifficulty
{
    Easy,     // 简单模式
    Normal,   // 普通模式
    Hard      // 困难模式
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public PlayerData CurrentPlayerData { get; private set; }
    public GameChapter CurrentChapter { get; private set; } = GameChapter.Chapter1_Departure;
    public QuestState CurrentQuest { get; private set; } = QuestState.ExploreIslands;

    [Header("三大岛屿特色升级状态")]
    public bool isHerbalCrafted = false;       
    public bool isAstrolabeCalibrated = false;   
    public bool isSailReinforced = false;       

    [Header("分屏规格设置")]
    public int screenWidth = 19; 
    public int screenHeight = 9; 

    [Header("3个主线岛屿与风暴预制体")]
    [SerializeField] private GameObject island1_Ben_Prefab;       // 浅滩岛
    [SerializeField] private GameObject island2_Elder_Prefab;     // 星落屿
    [SerializeField] private GameObject island3_Yun_Prefab;       // 绸市岛
    [SerializeField] private GameObject stormPrefab;              // 风暴地块预制体
    [Header("可视化元素预制体")]
    [SerializeField] private GameObject reefVisualPrefab;      // 暗礁
    [Tooltip("洋流箭头预制体（4个方向，按顺序拖入：上/下/左/右）。")]
    [SerializeField] private GameObject[] currentArrowPrefabs;    // 洋流箭头（4个方向：上/下/左/右）
    [Tooltip("风向箭头预制体（4个方向，按顺序拖入：上/下/左/右）。")]
    [SerializeField] private GameObject[] windArrowPrefabs;       // 风向箭头（4个方向：上/下/左/右）

    [Header("起点故乡岛换装")]
    [SerializeField] private Sprite goldenIslandSprite;


    private static readonly Vector2Int[] DirOrder = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public GameDifficulty Difficulty { get; private set; } = GameDifficulty.Normal;

    public Vector2Int BenIslandCoords { get; private set; }
    public Vector2Int ElderIslandCoords { get; private set; }
    public Vector2Int YunIslandCoords { get; private set; }


    public Dictionary<Vector2Int, Vector2Int> CurrentMap { get; private set; } = new Dictionary<Vector2Int, Vector2Int>();
    public Dictionary<Vector2Int, Vector2Int> WindMap { get; private set; } = new Dictionary<Vector2Int, Vector2Int>();
    public HashSet<Vector2Int> ReefMap { get; private set; } = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> DecoyIslands { get; private set; } = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> generatedGrids = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> safeCorridor = new HashSet<Vector2Int>(); //
    private int visitedIslandCount = 0;
    public int VisitedIslandCount => visitedIslandCount;

    private void Awake()
    {
        Screen.SetResolution(1920,1080, false); // 宽, 高, 是否全屏
        Instance = this;
        LoadDifficultyFromPrefs();
        // 把 CurrentPlayerData 初始化提前到 Awake，确保在任何 Start/Update 之前就可用
        InitializePlayerData();
        SpawnStoryIslandsRandomly();
        BuildSafeCorridor();
    }

    // 根据难度初始化主角资源
    private void InitializePlayerData()
    {
        int startFood, startWater, startHull, startGold;
        switch (Difficulty)
        {
            case GameDifficulty.Easy:
                startFood = 130; startWater = 130; startHull = 130; startGold = 650;
                break;
            case GameDifficulty.Hard:
                startFood = 85; startWater = 85; startHull = 100; startGold = 450;
                break;
            default: // Normal
                startFood = 100; startWater = 100; startHull = 110; startGold = 500;
                break;
        }
        CurrentPlayerData = new PlayerData(startFood, startWater, startHull, startGold);

        EventCenter.BroadcastResourceChanged(
            CurrentPlayerData.Food,
            CurrentPlayerData.Water,
            CurrentPlayerData.Hull,
            CurrentPlayerData.Gold
        );

        EventCenter.BroadcastQuestChanged(CurrentQuest);
    }

    // 从 PlayerPrefs 读取主菜单选择的难度，未选择时默认 Normal
    private void LoadDifficultyFromPrefs()
    {
        string saved = PlayerPrefs.GetString("GameDifficulty", "Normal");
        switch (saved)
        {
            case "Easy":
                Difficulty = GameDifficulty.Easy;
                break;
            case "Hard":
                Difficulty = GameDifficulty.Hard;
                break;
            default:
                Difficulty = GameDifficulty.Normal;
                break;
        }
        Debug.Log($"<color=cyan>[难度] 当前游戏难度：{Difficulty}</color>");
    }

    private void Start()
    {

    }

    private void SpawnStoryIslandsRandomly()
    {
        BenIslandCoords = new Vector2Int(Random.Range(2, 17), Random.Range(11, 19));
        if (island1_Ben_Prefab != null)
            Instantiate(island1_Ben_Prefab, new Vector3(BenIslandCoords.x, BenIslandCoords.y, 0), Quaternion.identity);

        ElderIslandCoords = new Vector2Int(Random.Range(21, 36), Random.Range(11, 19));
        if (island2_Elder_Prefab != null)
            Instantiate(island2_Elder_Prefab, new Vector3(ElderIslandCoords.x, ElderIslandCoords.y, 0), Quaternion.identity);

        YunIslandCoords = new Vector2Int(Random.Range(21, 36), Random.Range(1, 8));
        if (island3_Yun_Prefab != null)
            Instantiate(island3_Yun_Prefab, new Vector3(YunIslandCoords.x, YunIslandCoords.y, 0), Quaternion.identity);

        Debug.Log($"<color=lime>浅滩(屏A): {BenIslandCoords} | 星落(屏C): {ElderIslandCoords} | 绸市(屏B): {YunIslandCoords}</color>");
    }

    public void GenerateTileOnDemand(Vector2Int grid)
    {
        if (generatedGrids.Contains(grid)) return;
        if (grid.x < 1 || grid.x > 36 || grid.y < 0 || grid.y > 19) return; // 限定活动范围

        if ((grid.x == 18 || grid.x == 19) || (grid.y == 9 || grid.y == 10)) return;
        if (grid == new Vector2Int(1, 0)) return; // 避开起点
        if (grid == BenIslandCoords || grid == ElderIslandCoords || grid == YunIslandCoords) return; // 避开主线岛

        generatedGrids.Add(grid);

        // 根据难度决定暗礁率与风暴概率
        float reefChanceNear, reefChanceFar, stormChance;
        switch (Difficulty)
        {
            case GameDifficulty.Easy:
                reefChanceNear = 0.05f; reefChanceFar = 0.08f; stormChance = 0.03f;
                break;
            case GameDifficulty.Hard:
                reefChanceNear = 0.18f; reefChanceFar = 0.28f; stormChance = 0.08f;
                break;
            default: // Normal
                reefChanceNear = 0.10f; reefChanceFar = 0.15f; stormChance = 0.05f;
                break;
        }
        bool onSafeCorridor = safeCorridor != null && safeCorridor.Contains(grid);
        if (onSafeCorridor)
        {
            reefChanceNear = 0f;
            reefChanceFar = 0f;
            stormChance = 0f;
        }

        float reefChance = (grid.y > 10) ? reefChanceFar : reefChanceNear;

        if (ReefSuppressedByFlowOrWind(grid)) reefChance *= 0.15f;

        float rand = Random.value;
        float currentZone = reefChance + 0.15f;
        float windZone = reefChance + 0.30f;
        float stormZone = windZone + stormChance;

        // 1. 生成暗礁
        if (rand < reefChance)
        {
            ReefMap.Add(grid);
            if (reefVisualPrefab != null)
            {
                Instantiate(reefVisualPrefab, new Vector3(grid.x, grid.y, 0), Quaternion.identity);
                Physics2D.SyncTransforms(); 
            }
        }
        // 2. 生成洋流
        else if (rand < currentZone)
        {
            int dirIndex = Random.Range(0, 4);
            // 规则5：2×2 范围内只能有一个方向的洋流——若邻近已有洋流，方向与之一致
            if (TryFindNearbyDirection(CurrentMap, grid, 1, out Vector2Int nearCurrent))
            {
                int idx = System.Array.IndexOf(DirOrder, nearCurrent);
                if (idx >= 0) dirIndex = idx;
            }
            CurrentMap[grid] = DirOrder[dirIndex];
            try { SpawnCurrentArrow(dirIndex, grid); }
            catch (System.Exception e) { Debug.LogWarning($"[洋流生成异常] {e.Message}"); }
        }
        // 3. 生成风向区
        else if (rand < windZone)
        {
            // 规则2/3：风向与风向不能紧邻，中间最少隔一个格子——若周围8格已有风向则本格不生成
            if (!TryFindNearbyDirection(WindMap, grid, 1, out _))
            {
                int winDir = Random.Range(0, 4);
                // 规则1：4×4 范围内只能有一个方向的风向——远处（2格内）已有风向则方向与之一致
                if (TryFindNearbyDirection(WindMap, grid, 2, out Vector2Int nearWind))
                {
                    int idx = System.Array.IndexOf(DirOrder, nearWind);
                    if (idx >= 0) winDir = idx;
                }
                WindMap[grid] = DirOrder[winDir];
                try { SpawnWindArrow(winDir, grid); }
                catch (System.Exception e) { Debug.LogWarning($"[风向生成异常] {e.Message}"); }
            }
        }
        // 4. 生成移动风暴
        else if (rand < stormZone)
        {
            if (stormPrefab != null)
            {
                Instantiate(stormPrefab, new Vector3(grid.x, grid.y, 0), Quaternion.identity);
                Physics2D.SyncTransforms();
            }
        }
        // 5. 生成普通迷惑岛
        else if (rand < stormZone + 0.03f)
        {
            DecoyIslands.Add(grid);
        }
    }

    // 实例化指定方向的洋流箭头预制体
    private void SpawnCurrentArrow(int dirIndex, Vector2Int grid)
    {
        if (dirIndex < 0 || dirIndex >= currentArrowPrefabs.Length) return;
        GameObject prefab = currentArrowPrefabs[dirIndex];
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, new Vector3(grid.x, grid.y, 0), prefab.transform.rotation);
        CurrentEffect ce = go.GetComponent<CurrentEffect>();
        if (ce != null) ce.driftDirection = DirOrder[dirIndex];
        Physics2D.SyncTransforms();
    }

    private void SpawnWindArrow(int dirIndex, Vector2Int grid)
    {
        if (dirIndex < 0 || dirIndex >= windArrowPrefabs.Length) return;
        GameObject prefab = windArrowPrefabs[dirIndex];
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, new Vector3(grid.x, grid.y, 0), prefab.transform.rotation);
        WindArea wa = go.GetComponent<WindArea>();
        if (wa != null) wa.windDirection = DirOrder[dirIndex];
        Physics2D.SyncTransforms();
    }


    private bool TryFindNearbyDirection(Dictionary<Vector2Int, Vector2Int> map, Vector2Int grid, int range, out Vector2Int dir)
    {
        foreach (var kv in map)
        {
            Vector2Int g = kv.Key;
            if (g == grid) continue;
            if (Mathf.Abs(g.x - grid.x) <= range && Mathf.Abs(g.y - grid.y) <= range)
            {
                dir = kv.Value;
                return true;
            }
        }
        dir = Vector2Int.zero;
        return false;
    }

    // 规则4：判断本格是否处于某个风向/洋流的"正前方"（该格左侧的风/流指向本格）此时暗礁概率应降低
    private bool ReefSuppressedByFlowOrWind(Vector2Int grid)
    {
        foreach (var dir in DirOrder)
        {
            Vector2Int from = grid - dir;
            if (WindMap.TryGetValue(from, out Vector2Int wd) && wd == dir) return true;
            if (CurrentMap.TryGetValue(from, out Vector2Int cd) && cd == dir) return true;
        }
        return false;
    }


    private HashSet<Vector3> visitedIslandPositions = new HashSet<Vector3>();

    public void RegisterIslandVisit(Vector3 pos)
    {
        if (visitedIslandPositions.Add(pos))
        {
            visitedIslandCount++;

            if (visitedIslandCount >= 3)
            {
                ActivateGoldenIsland();
                UpdateQuest(QuestState.ReturnHome); // 自动开启返航
            }
            else
            {
                EventCenter.BroadcastQuestChanged(CurrentQuest); // 刷新UI进度
            }
        }
    }

    public bool IsIslandVisited(Vector3 pos)
    {
        return visitedIslandPositions.Contains(pos);
    }

    public int PickRandomUnvisitedInfoIndex()
    {
        List<int> unvisited = new List<int>();
        if (!IsIslandVisited(new Vector3(BenIslandCoords.x, BenIslandCoords.y, 0))) unvisited.Add(11);
        if (!IsIslandVisited(new Vector3(ElderIslandCoords.x, ElderIslandCoords.y, 0))) unvisited.Add(12);
        if (!IsIslandVisited(new Vector3(YunIslandCoords.x, YunIslandCoords.y, 0))) unvisited.Add(13);
        if (unvisited.Count == 0) { unvisited.Add(11); unvisited.Add(12); unvisited.Add(13); }
        return unvisited[Random.Range(0, unvisited.Count)];
    }
    // ==============================================================================
    // ==============================================================================
    // 返程阶段：把起点/故乡的岛屿贴图替换成“黄金色岛屿”图，作为返程目标标记
    private void ActivateGoldenIsland()
    {
        if (goldenIslandSprite == null) return;

        StoryIsland[] islands = FindObjectsOfType<StoryIsland>();
        foreach (var si in islands)
        {
            if (si.storyType == IslandStoryType.Chapter1_Departure)
            {
                SpriteRenderer sr = si.GetComponent<SpriteRenderer>();
                if (sr == null) sr = si.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null)
                {
                    sr.sprite = goldenIslandSprite;
                    Debug.Log("<color=gold>[返程] 起点故乡岛已变成黄金色岛屿目标！</color>");
                }
                break;
            }
        }
    }

    private void BuildSafeCorridor()
    {
        safeCorridor = new HashSet<Vector2Int>();
        Vector2Int home = new Vector2Int(1, 0);
        StoryIsland[] islands = FindObjectsOfType<StoryIsland>();
        foreach (var si in islands)
        {
            if (si.storyType == IslandStoryType.Chapter1_Departure)
            {
                home = new Vector2Int(Mathf.RoundToInt(si.transform.position.x), Mathf.RoundToInt(si.transform.position.y));
                break;
            }
        }

        Vector2Int[] waypoints =
        {
            home,
            BenIslandCoords,
            ElderIslandCoords,
            YunIslandCoords,
            home
        };

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            AddManhattanPath(safeCorridor, waypoints[i], waypoints[i + 1]);
        }
    }

    private void AddManhattanPath(HashSet<Vector2Int> set, Vector2Int a, Vector2Int b)
    {
        int x = a.x, y = a.y;
        set.Add(new Vector2Int(x, y));
        while (x != b.x) { x += (b.x > x) ? 1 : -1; set.Add(new Vector2Int(x, y)); }
        while (y != b.y) { y += (b.y > y) ? 1 : -1; set.Add(new Vector2Int(x, y)); }
    }

    public void AdvanceChapter(GameChapter nextChapter)
    {
        CurrentChapter = nextChapter;
        CaptainsJournalView.Instance.PlayChapterStory(nextChapter);
    }

    public void UpdateQuest(QuestState newState)
    {
        CurrentQuest = newState;
        EventCenter.BroadcastQuestChanged(CurrentQuest);
    }
}