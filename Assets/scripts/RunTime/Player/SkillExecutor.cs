using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillExecutor : MonoBehaviour
{
    public void UseLookout()
    {
        if (CaptainsJournalView.Instance != null && CaptainsJournalView.Instance.IsOpen) return;
        var playerData = GameManager.Instance.CurrentPlayerData;
        if (playerData.ConsumeResources(1, 1))
        {
            ShipEntity ship = FindObjectOfType<ShipEntity>();
            if (ship != null)
            {
                ship.PlayLookoutAnim();
                int baseRadius =
                    GameManager.Instance.Difficulty == GameDifficulty.Easy ? 3 :
                    GameManager.Instance.Difficulty == GameDifficulty.Hard ? 2 : 2;
                int radius = baseRadius + (GameManager.Instance.isAstrolabeCalibrated ? 1 : 0);

                int shipX = Mathf.RoundToInt(ship.transform.position.x);
                int shipY = Mathf.RoundToInt(ship.transform.position.y);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        GameManager.Instance.GenerateTileOnDemand(new Vector2Int(shipX + dx, shipY + dy));
                    }
                }
                Physics2D.SyncTransforms(); // 确保新生成的 Collider 立即能被雷达扫到


                float searchRadius = radius * 1.0f;
                Collider2D[] detectedObjects = Physics2D.OverlapCircleAll(ship.transform.position, searchRadius);

                int revealedCount = 0; // 记录本次照亮了几个隐藏地貌

                // 遍历雷达范围内扫到的所有物体，强行让它们永久显形
                foreach (var col in detectedObjects)
                {
                    SpriteRenderer[] srs = col.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var sr in srs)
                    {
                        if (!sr.enabled)
                        {
                            sr.enabled = true; 
                            revealedCount++;
                        }
                    }
                }
            }
        }
    }

    
    public void UseFishing()
    {
        if (CaptainsJournalView.Instance != null && CaptainsJournalView.Instance.IsOpen) return;
        var playerData = GameManager.Instance.CurrentPlayerData;
        if (playerData.Hull > 10)
        {
            ShipEntity ship = FindObjectOfType<ShipEntity>();
            if (ship != null)
            {
                ship.PlayFishingAnim();
            }
            int foodGained = Random.Range(15, 31);
            playerData.AddResources(foodGained, 0, -10, 0);
            Debug.Log($"[技能] 下网打捞成功！损耗 10 点耐久，捕获了 {foodGained} 份食物补给！");
        }
        else
        {
            Debug.LogWarning("船体耐久过于脆弱，无法承受下网打捞！");
        }
    }

    public void UseFullSpeed()
    {
        if (CaptainsJournalView.Instance != null && CaptainsJournalView.Instance.IsOpen) return;
        ShipEntity ship = FindObjectOfType<ShipEntity>();
        if (ship != null && !ship.isFullSpeedActive)
        {
            ship.isFullSpeedActive = true;
            ship.PlayFullSpeedAnimation();
            Debug.Log("[技能] 已激活全速航行！下一回合航行格数翻倍，请小心暗礁！");
        }
    }

    public void UseDrifting()
    {
        if (CaptainsJournalView.Instance != null && CaptainsJournalView.Instance.IsOpen) return;
        ShipEntity ship = FindObjectOfType<ShipEntity>();
        if (ship != null)
        {
            ship.PlayDriftAnim();
            ship.ExecuteRandomDrift();
        }
    }
}