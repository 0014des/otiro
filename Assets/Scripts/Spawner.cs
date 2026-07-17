using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("スポーン設定")]
    public GameObject fallingObjectPrefab;
    public float spawnInterval = 1.5f;
    public float minSpawnInterval = 0.3f;
    public float spawnRangeX = 8f;
    public float spawnHeight = 15f;

    [Header("難易度設定")]
    public float difficultyIncreaseRate = 0.02f;
    public float speedIncreaseRate = 0.1f;
    public float baseFallSpeed = 5f;

    private float timer = 0f;
    private float currentInterval;
    private float currentFallSpeed;
    private bool isSpawning = true;

    void Start()
    {
        currentInterval = spawnInterval;
        currentFallSpeed = baseFallSpeed;
    }

    void Update()
    {
        if (!isSpawning) return;

        timer += Time.deltaTime;

        if (timer >= currentInterval)
        {
            SpawnObject();
            timer = 0f;

            // 徐々に難易度を上げる
            currentInterval = Mathf.Max(minSpawnInterval, currentInterval - difficultyIncreaseRate);
            currentFallSpeed += speedIncreaseRate * Time.deltaTime;
        }
    }

    void SpawnObject()
    {
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        Vector3 spawnPos = new Vector3(randomX, spawnHeight, 0f);

        GameObject obj = Instantiate(fallingObjectPrefab, spawnPos, Quaternion.identity);
        FallingObject fo = obj.GetComponent<FallingObject>();
        if (fo != null)
        {
            fo.fallSpeed = currentFallSpeed;
        }

        // ランダムに色を変える
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = rend.material;
            Color randomColor = new Color(
                Random.Range(0.2f, 1f),
                Random.Range(0.2f, 1f),
                Random.Range(0.2f, 1f)
            );
            // URP対応
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", randomColor);
            else
                mat.color = randomColor;
        }

        // ランダムにサイズを変える
        float scale = Random.Range(0.5f, 1.5f);
        obj.transform.localScale = Vector3.one * scale;
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }
}
