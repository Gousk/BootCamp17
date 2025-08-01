using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RaceManager : MonoBehaviour
{
    [Tooltip("Sıralamayı gösterecek Text (TMP) UI")]
    public TextMeshProUGUI infoText;

    private readonly List<int> finishOrder = new List<int>();   // Geçiş sırası
    private const int totalPlayers = 4;

    /// Oyuncu bitiş çizgisini geçtiğinde buradan haber alırız
    public void PlayerFinished(int playerID)
    {
        if (finishOrder.Contains(playerID)) return;      // Aynı oyuncu iki kez eklenmesin

        finishOrder.Add(playerID);
        string msg = $"{playerID}. Oyuncu bitiş çizgisini geçti!";  // Türkçe bildirim
        Debug.Log(msg);

        if (infoText != null)
            infoText.text += msg + "\n";

        if (finishOrder.Count == totalPlayers)
        {
            Debug.Log("Yarış bitti! Sıra: " + string.Join(" › ", finishOrder));
            if (infoText != null)
                infoText.text += "\nYarış bitti ➜ " + string.Join(" › ", finishOrder);
        }
    }
}
