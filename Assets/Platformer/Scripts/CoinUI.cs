using TMPro;
using UnityEngine;

public class CoinUI : MonoBehaviour
{
    public static CoinUI Instance { get; private set; }
    public static int points;

    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text pointsText;
    private int coins;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UpdateText();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        points += 100; // Assuming each coin is worth 100 points
        UpdateText();
    }

    private void UpdateText()
    {
        if (coinText != null)
            coinText.text = $"\nx{coins}";
        if (pointsText != null)
            pointsText.text = $"Mario:\n {points}";
    }
}

