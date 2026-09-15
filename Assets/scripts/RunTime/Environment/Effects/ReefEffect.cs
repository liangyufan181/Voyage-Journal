using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReefEffect : MonoBehaviour
{
    [SerializeField] private int damageAmount = 17; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var playerData = GameManager.Instance.CurrentPlayerData;

            playerData.AddResources(0, 0, -damageAmount, 0);

            Debug.LogWarning($"SOS! Hit a reef! Hull damaged by {damageAmount}! Current Hull: {playerData.Hull}");
        }
    }
}