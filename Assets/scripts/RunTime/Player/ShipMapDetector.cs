using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipMapDetector : MonoBehaviour
{
    private ShipEntity ship;

    private void Start()
    {
        ship = GetComponent<ShipEntity>();
    }

    public Vector3Int DetectAndResolveTile(bool wasFullSpeed, ref string hazardStory, ref string islandStory)
    {
        var playerData = GameManager.Instance.CurrentPlayerData;
        Vector2Int currentGrid = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

        GameManager.Instance.GenerateTileOnDemand(currentGrid);
        int foodAdd = 0;
        int waterAdd = 0;
        int hullAdd = 0;

        Collider2D[] endColliders = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        foreach (var col in endColliders)
        {
            // 让踩中的地块（及其子物体，如洋流箭头）永久显形
            SpriteRenderer[] srs = col.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                sr.enabled = true; // 开启图片渲染，使其在画面上永久亮起
            }
            if (col.CompareTag("Reef"))
            {
                int damage = wasFullSpeed ? 34 : 17;
                playerData.AddResources(0, 0, -damage, 0);
                hullAdd -= damage;
                hazardStory = wasFullSpeed ? "ReefFullSpeed" : "Reef";

                StartCoroutine(ShakeCamera(0.15f, 0.2f));
                Debug.LogWarning($"[危险] 触礁！受损 {damage}。当前耐久: {playerData.Hull}");
            }
            // 2. 检测主线剧情岛屿
            else if (col.CompareTag("Island"))
            {
                StoryIsland storyIsland = col.GetComponent<StoryIsland>();

                if (storyIsland != null)
                {
                    storyIsland.OnLanded();

                    // 呼叫完后，岛屿自己已经登记完毕，我们在这里获取最新的探索进度，记录给3天日记本
                    int count = GameManager.Instance.VisitedIslandCount;
                    islandStory = $"{count}/3";
                }
            }
            // 3. 精准检测普通迷惑岛
            else if (col.CompareTag("Decoy"))
            {
                playerData.AddResources(5, 5, 0, 0);
                foodAdd += 5;
                waterAdd += 5;
                islandStory = "Decoy";

                if (CaptainsJournalView.Instance != null)
                {
                    CaptainsJournalView.Instance.ShowDecoyIslandUI();
                }
            }
            else if (col.CompareTag("Storm"))
            {
                playerData.AddResources(0, 30, -15, 0);
                waterAdd += 30;
                hullAdd -= 15;
                hazardStory = "Storm";

                Vector2Int[] allDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                ship.lockedDirection = allDirs[Random.Range(0, 4)];
                Debug.LogWarning($"[天灾] 卷入风暴！下一回合方向锁定为 {ship.lockedDirection}");
            }
        }
        if (currentGrid == new Vector2Int(1, 0) && GameManager.Instance.CurrentQuest == QuestState.ReturnHome)
        {
            if (StoryCGPlayer.Instance != null)
            {
                StoryCGPlayer.Instance.PlayStoryCG(5); // 播放第五章归根大电影
            }
        }

        return new Vector3Int(foodAdd, waterAdd, hullAdd);
    }

    private bool IsDecoyIsland(Vector2Int grid)
    {
        if (grid == Vector2Int.zero) return false;
        if (grid == new Vector2Int(2, 5) || grid == new Vector2Int(6, 7) || grid == new Vector2Int(9, 3)) return false;
        return true;
    }

    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        Vector3 originalPos = Camera.main.transform.position;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null;
        }

        Camera.main.transform.position = originalPos;
    }
}