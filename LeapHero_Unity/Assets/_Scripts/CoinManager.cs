using UnityEngine;
using TMPro; // Importante para mexer no texto novo

public class CoinManager : MonoBehaviour
{
    // Singleton: Permite chamar CoinManager.instance.AddCoins() de qualquer script
    public static CoinManager instance;

    #region Settings
    [Header("UI References")]
    public TextMeshProUGUI coinText; // Arraste o texto aqui
    #endregion

    #region Internal State
    private int currentCoins = 0;
    #endregion

    void Awake()
    {
        // Configuração do Singleton
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
    }

    // Método chamado quando pega uma moeda
    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        // Atualiza o texto na tela
        if (coinText != null)
        {
            coinText.text = "x " + currentCoins.ToString();
        }
    }
}