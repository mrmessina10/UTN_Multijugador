using UnityEngine;
using TMPro;
using System;
using Unity.Netcode;

public class MatchUI : MonoBehaviour
{
    [Header("UI Elements (TDM)")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI matchStateText;
    [SerializeField] private TextMeshProUGUI redTeamScoreText;
    [SerializeField] private TextMeshProUGUI blueTeamScoreText;
    [SerializeField] private TextMeshProUGUI kdText;

    private void Update()
    {
        if (MatchManager.Instance == null || MatchManager.Instance.Score == null) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        // 1. TIEMPO DE PARTIDA (Calculado directo del StateTimer de la FSM)
        double remainingTime = MatchManager.Instance.stateTimer.Value - NetworkManager.Singleton.ServerTime.Time;
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, (float)remainingTime));
        timerText.text = time.ToString(@"mm\:ss");

        // 2. ESTADO DE PARTIDA
        matchStateText.text = MatchManager.Instance.IsMatchActive() ? "" : "Transición / Esperando...";

        // 3. PUNTAJE TDM
        redTeamScoreText.text = $"Red: {MatchManager.Instance.Score.redTeamScore.Value}";
        blueTeamScoreText.text = $"Blue: {MatchManager.Instance.Score.blueTeamScore.Value}";

        // 4. K/D DEL JUGADOR LOCAL
        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerScore localScore))
            {
                kdText.text = $"K/D: {localScore.kills.Value} / {localScore.deaths.Value}";
            }
        }
    }
}