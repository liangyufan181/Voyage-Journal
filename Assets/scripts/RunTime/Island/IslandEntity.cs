using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandEntity : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
           TradeDialogView.Instance.Open();
        }
    }
}