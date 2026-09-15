using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShipEntity : MonoBehaviour
{
    [SerializeField] private float gridSize = 1.0f;
    private Vector3 targetPosition;
    private Vector3 previousPosition;

    [HideInInspector] public int currentSightRadius = 1;
    [HideInInspector] public bool isFullSpeedActive = false;
    [HideInInspector] public Vector2Int lockedDirection = Vector2Int.zero;
    [Header("黑屏切屏遮罩 Image")]
    [SerializeField] private Image fadeOverlay; 
    private Animator animator;

    private bool isTransitioning = false; // 记录是否正在切屏
    private void Start()
    {
        // 1. 自动对齐网格坐标
        float snappedX = Mathf.Round(transform.position.x);
        float snappedY = Mathf.Round(transform.position.y);
        transform.position = new Vector3(snappedX, snappedY, 0);

        targetPosition = transform.position;
        previousPosition = transform.position;
        currentSightRadius = 1;


        //修复：开局第一帧脚底雷达探测
        //游戏启动时，船只在不移动的情况下，立刻扫描脚底下是否有起点岛
        Collider2D[] startColliders = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        foreach (var col in startColliders)
        {
            if (col.CompareTag("Island"))
            {
                StoryIsland storyIsland = col.GetComponent<StoryIsland>();
                if (storyIsland != null)
                {
                    // 发现起点岛，立刻在第 1 帧自动触发第一章启程大 CG！
                    storyIsland.OnLanded();
                }
            }
        }
        animator = GetComponent<Animator>();
        // ==============================================================================
    }

    public void TryMove(Vector2Int direction)
    {
        if (GameManager.Instance.CurrentQuest == QuestState.Victory ||
            GameManager.Instance.CurrentQuest == QuestState.Defeat) return;

        if (CaptainsJournalView.Instance != null && CaptainsJournalView.Instance.IsOpen) return;
        if (isTransitioning) return; // 正在切屏时，锁死按键

        int curX = Mathf.RoundToInt(transform.position.x);
        int curY = Mathf.RoundToInt(transform.position.y);

        int nextX = curX + direction.x;
        int nextY = curY + direction.y;

        if (nextX < 1 || nextX > 36 || nextY < 0 || nextY > 19)
        {
            Debug.LogWarning("前方是无尽的深海虚空，已达世界最边缘，切莫偏航！");
            return;
        }

        //处理 17 和 20 的跨屏传送
        bool isScreenTransition = false;
        int finalX = nextX;
        int finalY = nextY;

        //横向分屏传送（17和20对齐，避开 18、19 边框）
        if (direction.x > 0 && curX == 17)
        {
            isScreenTransition = true;
            finalX = 20; // 17往右走：传送到 20
        }
        else if (direction.x < 0 && curX == 20)
        {
            isScreenTransition = true;
            finalX = 17; // 20往左走：传送到 17
        }

        if (direction.y > 0 && curY == 8)
        {
            isScreenTransition = true;
            finalY = 11; // 8 往上走：穿过 9、10 边框，传送到 11 
        }
        else if (direction.y < 0 && curY == 11)
        {
            isScreenTransition = true;
            finalY = 8;  // 11 往下走：穿过 9、10 边框，传送到 8
        }

        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayerData == null)
        {
            Debug.LogError("[致命] GameManager 或 CurrentPlayerData 未初始化，无法移动！");
            return;
        }
        var playerData = GameManager.Instance.CurrentPlayerData;

        if (lockedDirection != Vector2Int.zero)
        {
            direction = lockedDirection;
            lockedDirection = Vector2Int.zero;
        }

        if (isScreenTransition)
        {
            //如果是切屏传送：在本地直接扣除 1 物资，然后播放黑屏渐变传送
            if (playerData.ConsumeResources(1, 1))
            {
                StartCoroutine(PerformScreenTransition(new Vector3(finalX, finalY, 0)));
            }
        }
        else
        {
            //如果是同屏内正常移动： 交给 SailingCycleManager 驱动和结算
            if (SailingCycleManager.Instance != null)
            {
                SailingCycleManager.Instance.ProcessDayTurn(direction);
            }
        }
    }

    public void ExecuteRandomDrift()
    {
        if (CaptainsJournalView.Instance != null && CaptainsJournalView.Instance.IsOpen) return;
        if (GameManager.Instance.CurrentQuest == QuestState.Victory ||
            GameManager.Instance.CurrentQuest == QuestState.Defeat) return;
        if (isTransitioning) return;

        int curX = Mathf.RoundToInt(transform.position.x);
        int curY = Mathf.RoundToInt(transform.position.y);

        // 随波逐流边界防漏探测：由于 Y 轴最大值变为了 19，这里也自动同步对齐
        List<Vector2Int> allowedDirs = new List<Vector2Int>();
        if (curY + 1 <= 19) allowedDirs.Add(Vector2Int.up);
        if (curY - 1 >= 0) allowedDirs.Add(Vector2Int.down);
        if (curX - 1 >= 1) allowedDirs.Add(Vector2Int.left);
        if (curX + 1 <= 36) allowedDirs.Add(Vector2Int.right);

        if (allowedDirs.Count == 0) return;

        Vector2Int randomDir = allowedDirs[Random.Range(0, allowedDirs.Count)];
        Debug.Log($"<color=orange>[随波逐流] 随机移动：{randomDir}！</color>");
        PlayDriftAnim();
        Drift(randomDir);
    }

    public void Drift(Vector2Int direction)
    {
        MoveAndResolveFog(direction, 1);

        if (SailingCycleManager.Instance != null)
        {
            SailingCycleManager.Instance.ProcessDriftSettle(direction);
        }
    }

    public void MoveAndResolveFog(Vector2Int direction, int distance)
    {
        previousPosition = transform.position;

        targetPosition += new Vector3(direction.x, direction.y, 0) * distance * gridSize;
        transform.position = targetPosition;

        currentSightRadius = 1;
    }

    public bool StepOnce(Vector2Int direction)
    {
        int curX = Mathf.RoundToInt(transform.position.x);
        int curY = Mathf.RoundToInt(transform.position.y);

        int nextX = curX + direction.x;
        int nextY = curY + direction.y;

        // 世界边界（X1-36、Y0-19），越界则无法移动
        if (nextX < 1 || nextX > 36 || nextY < 0 || nextY > 19) return false;

        // 切屏传送：一步跨过边框(X18/19、Y9/10)落到对侧
        int finalX = nextX;
        int finalY = nextY;
        if (direction.x > 0 && curX == 17) finalX = 20;
        else if (direction.x < 0 && curX == 20) finalX = 17;
        if (direction.y > 0 && curY == 8) finalY = 11;
        else if (direction.y < 0 && curY == 11) finalY = 8;

        previousPosition = transform.position;
        targetPosition = new Vector3(finalX, finalY, 0);
        transform.position = targetPosition;
        currentSightRadius = 1;
        return true;
    }

    public void PlayLookoutAnim()
    {
        if (animator != null) animator.SetTrigger("Lookout");
    }

    public void PlayFishingAnim()
    {
        if (animator != null) animator.SetTrigger("Fishing");
    }

    public void PlayDriftAnim()
    {
        if (animator != null) animator.SetTrigger("Drift");
    }
    public void PlayFullSpeedAnimation()
    {
        // 触发状态机中的 FullSpeed 信号，播放冲刺动画
        if (animator != null) animator.SetTrigger("FullSpeed");
    }

    private IEnumerator PerformScreenTransition(Vector3 finalPosition)
    {
        isTransitioning = true;
        
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            
            // 屏幕在 0.3 秒内变黑
            float elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.color = new Color(0, 0, 0, elapsed / 0.3f);
                yield return null;
            }
        }

        // 在全黑时，将小船瞬间移动传送到新屏幕的对立位置
        previousPosition = transform.position;
        transform.position = finalPosition;
        targetPosition = finalPosition;

        // 落地结算
        if (SailingCycleManager.Instance != null)
        {
            SailingCycleManager.Instance.ProcessDriftSettle(Vector2Int.zero); 
        }

        // 屏幕在 0.3 秒内重新亮起
        if (fadeOverlay != null)
        {
            float elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.color = new Color(0, 0, 0, 1f - (elapsed / 0.3f));
                yield return null;
            }
            fadeOverlay.gameObject.SetActive(false);
        }
        
        isTransitioning = false;
        Debug.Log($"[切屏成功] 船只已平移传送至新海域：{transform.position}");
    }
}