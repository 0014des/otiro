using UnityEngine;

public class FallingObject : MonoBehaviour
{
    [Header("落下設定")]
    public float fallSpeed = 5f;

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        // 画面外に出たら削除
        if (transform.position.y < -2f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.isInvincible)
            {
                // 無敵時はすり抜けるか弾く（ここでは削除するだけにする）
                Destroy(gameObject);
                return;
            }

            // ゲームマネージャーに通知
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.GameOver();
            }
            Destroy(gameObject);
        }
    }
}
