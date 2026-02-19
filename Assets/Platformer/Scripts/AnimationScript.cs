using UnityEngine;
using System.Collections;

public class AnimationScript : MonoBehaviour
{
    public Material material;
    public int frames = 5;
    public float fps = 10f;

    float timer;
    int currentFrame;

    void Start()
    {
        material.mainTextureScale = new Vector2(-1f,- 1f / frames);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 1f / fps)
        {
            timer = 0f;

            currentFrame++;
            currentFrame %= frames;

            float offsetY = 1f - ((float)(currentFrame + 1) / frames);
            material.mainTextureOffset = new Vector2(0f, offsetY);
        }
    }
    [Header("Coin Pop")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private float popHeight = 1.0f;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float rotateSpeed = 360f; // degrees/sec

    public void PopCoin()
    {
        StartCoroutine(PopCoinRoutine());
    }

    private IEnumerator PopCoinRoutine()
    {
        GameObject coin = Instantiate(coinPrefab, transform.position + Vector3.up * 0.6f, Quaternion.identity);

        Vector3 start = coin.transform.position;
        Vector3 end = start + Vector3.up * popHeight;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / popDuration);

            // nice “pop” easing
            float eased = 1f - Mathf.Pow(1f - u, 3f);

            coin.transform.position = Vector3.Lerp(start, end, eased);
            coin.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            yield return null;
        }

        Destroy(coin);
    }
}
