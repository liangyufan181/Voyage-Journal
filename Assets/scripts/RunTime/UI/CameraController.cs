using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform playerShip;
    [SerializeField] private float slideSpeed = 5.0f; // 相机切屏时的平滑速度

    private void Start()
    {
        GameObject ship = GameObject.FindWithTag("Player");
        if (ship != null)
        {
            playerShip = ship.transform;
            int w = GameManager.Instance.screenWidth;
            int h = GameManager.Instance.screenHeight;

            int shipX = Mathf.RoundToInt(playerShip.position.x);
            int shipY = Mathf.RoundToInt(playerShip.position.y);
            int screenX = (shipX < 0) ? -1 : shipX / w;
            int screenY = (shipY <= 8) ? 0 : 1;

            float targetX = (screenX * w) + (w - 1) / 2.0f;
            float targetY = (screenY * h) + (h - 1) / 2.0f + ((screenY == 1) ? 2 : 0);
            transform.position = new Vector3(targetX, targetY, -10f);
        }
    }

    private void LateUpdate()
    {
        if (playerShip == null) return;

        int w = GameManager.Instance.screenWidth;
        int h = GameManager.Instance.screenHeight;

        // 计算小船当前处于第几号屏幕
        int shipX = Mathf.RoundToInt(playerShip.position.x);
        int shipY = Mathf.RoundToInt(playerShip.position.y);
        int screenX = (shipX < 0) ? -1 : shipX / w;
        int screenY = (shipY <= 8) ? 0 : 1;

        float targetX = (screenX * w) + (w - 1) / 2.0f;
        float targetY = (screenY * h) + (h - 1) / 2.0f + ((screenY == 1) ? 2 : 0);


        // 平滑过渡
        Vector3 targetPos = new Vector3(targetX, targetY, -10f);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * slideSpeed);
    }
}