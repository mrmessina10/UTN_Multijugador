using UnityEngine;
using TMPro;
using System;
using Unity.Netcode;

public class MatchUI : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI timerText; // Este texto hará de Warmup y de Reloj
    [SerializeField] private TextMeshProUGUI redTeamScoreText;
    [SerializeField] private TextMeshProUGUI blueTeamScoreText;
    [SerializeField] private TextMeshProUGUI kdText;

    [Header("Match Flow Panels")]
    [SerializeField] private GameObject setupPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI winnerText;

    private void Update()
    {
        if (MatchManager.Instance == null || MatchManager.Instance.Score == null) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        bool isSettingUp = MatchManager.Instance.isSettingUp.Value;
        bool isMatchActive = MatchManager.Instance.IsMatchActive();
        bool isMatchEnded = MatchManager.Instance.IsMatchEnded();

        // Apaga o prende los fondos oscuros
        setupPanel.SetActive(isSettingUp);
        victoryPanel.SetActive(isMatchEnded);

        if (isMatchEnded)
        {
            DetermineWinner();
            return;
        }

        // --- MANEJO DEL TEXTO CENTRAL (RELOJ / WARMUP) ---
        if (isSettingUp)
        {
            // Muestra el 5, 4, 3, 2, 1
            timerText.text = $"00:0{MatchManager.Instance.warmupCountdown.Value}";
            timerText.color = Color.yellow; // Opcional: distinguirlo visualmente
        }
        else if (isMatchActive)
        {
            timerText.color = Color.white;
            double remainingTime = MatchManager.Instance.stateTimer.Value - NetworkManager.Singleton.ServerTime.Time;
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, (float)remainingTime));
            timerText.text = time.ToString(@"mm\:ss");
        }

        // --- MANEJO DE PUNTAJES (Se actualiza siempre menos al terminar) ---
        if (isMatchActive || isSettingUp)
        {
            redTeamScoreText.text = MatchManager.Instance.Score.redTeamScore.Value.ToString();
            blueTeamScoreText.text = MatchManager.Instance.Score.blueTeamScore.Value.ToString();

            if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerScore localScore))
                {
                    kdText.text = $"KD : {localScore.kills.Value}/{localScore.deaths.Value}";
                }
            }
        }
    }

    private void DetermineWinner()
    {
        int redScore = MatchManager.Instance.Score.redTeamScore.Value;
        int blueScore = MatchManager.Instance.Score.blueTeamScore.Value;

        if (redScore > blueScore)
        {
            winnerText.text = "RED WINS!";
            winnerText.color = Color.red;
        }
        else if (blueScore > redScore)
        {
            winnerText.text = "BLUE WINS!";
            winnerText.color = new Color(0.2f, 0.6f, 1f);
        }
        else
        {
            winnerText.text = "TIE!?";
            winnerText.color = Color.white;
        }
    }
}