using System.Collections;
using UnityEngine;

public class BreakableBrickScripted : MonoBehaviour
{
    [Header("Chunk Prefab")]
    [SerializeField] private GameObject chunkPrefab;

    [Header("Break Settings")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float upHeight = 1.2f;
    [SerializeField] private float outDistance = 0.6f;
    [SerializeField] private float spinSpeed = 540f; // degrees/sec
    [SerializeField] private int chunks = 4; // 4 or 8

    public void Break()
    {
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        // spawn chunks around the brick center
        Vector3 center = transform.position;

        // hide the brick immediately (so it "breaks")
        // If you prefer Destroy(gameObject) right away, do that after spawning chunks.
        var renderer = GetComponent<Renderer>();
        if (renderer) renderer.enabled = false;

        // create offsets for 4 chunks (top-left, top-right, bottom-left, bottom-right)
        Vector3[] baseOffsets4 =
        {
            new Vector3(-0.25f,  0.25f, 0f),
            new Vector3( 0.25f,  0.25f, 0f),
            new Vector3(-0.25f, -0.25f, 0f),
            new Vector3( 0.25f, -0.25f, 0f),
        };

        // for 8 chunks, add “front/back” (Z) offsets too
        Vector3[] baseOffsets8 =
        {
            new Vector3(-0.25f,  0.25f, -0.15f),
            new Vector3( 0.25f,  0.25f, -0.15f),
            new Vector3(-0.25f, -0.25f, -0.15f),
            new Vector3( 0.25f, -0.25f, -0.15f),
            new Vector3(-0.25f,  0.25f,  0.15f),
            new Vector3( 0.25f,  0.25f,  0.15f),
            new Vector3(-0.25f, -0.25f,  0.15f),
            new Vector3( 0.25f, -0.25f,  0.15f),
        };

        Vector3[] baseOffsets = (chunks == 8) ? baseOffsets8 : baseOffsets4;
        int count = baseOffsets.Length;

        Transform[] spawned = new Transform[count];
        Vector3[] startPos = new Vector3[count];
        Vector3[] outDir = new Vector3[count];
        float[] spinDir = new float[count];

        // use brick size so it scales nicely
        Vector3 brickSize = GetComponent<Renderer>() ? GetComponent<Renderer>().bounds.size : Vector3.one;

        for (int i = 0; i < count; i++)
        {
            // offset within the brick volume
            Vector3 localOffset = Vector3.Scale(baseOffsets[i], brickSize);
            Vector3 pos = center + localOffset;

            GameObject c = Instantiate(chunkPrefab, pos, Quaternion.identity);
            spawned[i] = c.transform;
            startPos[i] = pos;

            // outward direction from center (ignore Y so it spreads sideways)
            Vector3 d = (pos - center);
            d.y = 0f;
            outDir[i] = (d.sqrMagnitude > 0.0001f) ? d.normalized : Vector3.right;

            spinDir[i] = (Random.value < 0.5f) ? -1f : 1f;
        }

        float t = 0f;
        while (t < duration)
        {
            Debug.Log("animating...");
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            // arc: up then down slightly (parabola)
            float arc = 4f * u * (1f - u); // peaks at u=0.5

            for (int i = 0; i < count; i++)
            {
                if (!spawned[i]) continue;

                Vector3 p = startPos[i]
                            + outDir[i] * (outDistance * u)
                            + Vector3.up * (upHeight * arc);

                spawned[i].position = p;
                spawned[i].Rotate(0f, spinDir[i] * spinSpeed * Time.deltaTime, 0f, Space.World);
            }

            yield return null;
        }

        // cleanup chunks
        for (int i = 0; i < count; i++)
        {
            if (spawned[i]) Destroy(spawned[i].gameObject);
        }

        // finally destroy the brick object
        Destroy(gameObject);
    }
}
