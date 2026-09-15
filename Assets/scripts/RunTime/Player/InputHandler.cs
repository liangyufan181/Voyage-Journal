using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private ShipEntity shipEntity;

    private void Start()
    {
        shipEntity = FindObjectOfType<ShipEntity>();
        if (shipEntity == null)
        {
            Debug.LogError("not find ship£¡");
        }
    }

    private void Update()
    {
        if (shipEntity == null) return;

        Vector2Int moveDir = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) moveDir = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) moveDir = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) moveDir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) moveDir = Vector2Int.right;

        if (moveDir != Vector2Int.zero)
        {
            shipEntity.TryMove(moveDir);
        }
    }
}