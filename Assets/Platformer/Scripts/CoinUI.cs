using TMPro;
using UnityEngine;

public class CoinUI : MonoBehaviour
{
    public static CoinUI Instance { get; private set; }

    [SerializeField] private TMP_Text coinText;
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
        UpdateText();
    }

    private void UpdateText()
    {
        if (coinText != null)
            coinText.text = $"\nx{coins}";
    }
}

