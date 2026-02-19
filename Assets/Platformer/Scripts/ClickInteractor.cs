using UnityEngine;
using UnityEngine.InputSystem;
public class ClickInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask hitMask = ~0; // everything by default
    [SerializeField] private float maxDistance = 100f;

    [Header("Brick Debris (Optional)")]
    [SerializeField] private GameObject brickDebrisPrefab; // optional

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitMask))
            {
                GameObject go = hit.collider.gameObject;

                // Brick: destroy
                if (go.CompareTag("Brick"))
                {
                    BreakBrick(go, hit.point, hit.normal);
                }
                // ? block: add coins
                else if (go.CompareTag("Question"))
                {
                    HitQuestionBlock(go);
                }
            }
        }
    }

    private void BreakBrick(GameObject brick, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Optional: spawn debris effect
        var breakable = brick.GetComponent<BreakableBrickScripted>();
        if (breakable != null)        {
            breakable.Break();
            return; // if using BreakableBrickScripted, it handles destruction itself
        }
        else
        {
            Destroy(brick); // fallback if no BreakableBrickScripted
        }
        return;
    }

    private void HitQuestionBlock(GameObject questionBlock)
    {
        // no limit, just add coins
        if (CoinUI.Instance != null)
            CoinUI.Instance.AddCoins(1);

        var pop = questionBlock.GetComponent<AnimationScript>();
        pop?.PopCoin();

        // Optional: bump animation (tiny upward nudge)
        // StartCoroutine(Bump(questionBlock.transform));
    }
}

