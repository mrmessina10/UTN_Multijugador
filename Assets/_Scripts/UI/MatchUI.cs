using UnityEngine;
using TMPro;
using System;
using Unity.Netcode;

public class MatchUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI matchStateText;
    [SerializeField] private TextMeshProUGUI redTeamScoreText;
    [SerializeField] private TextMeshProUGUI blueTeamScoreText;
    [SerializeField] private TextMeshProUGUI redPlayersAliveText;
    [SerializeField] private TextMeshProUGUI bluePlayersAliveText;

    private void Update()
    {
        if (MatchManager.Instance == null || MatchManager.Instance.Score == null) return;
        if (NetworkManager.Singleton == null) return;

        double remainingTime = MatchManager.Instance.stateEndTime.Value - NetworkManager.Singleton.ServerTime.Time;
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, (float)remainingTime));
        timerText.text = time.ToString(@"mm\:ss");

        roundText.text = $"Round {MatchManager.Instance.Score.currentRound.Value}";
        matchStateText.text = FormatMatchState(MatchManager.Instance.currentState.Value);

        redTeamScoreText.text = $"Red: {MatchManager.Instance.Score.redTeamScore.Value}";
        blueTeamScoreText.text = $"Blue: {MatchManager.Instance.Score.blueTeamScore.Value}";

        redPlayersAliveText.text = $"Alive: {MatchManager.Instance.Score.redPlayersAlive.Value}";
        bluePlayersAliveText.text = $"Alive: {MatchManager.Instance.Score.bluePlayersAlive.Value}";
    }

    private string FormatMatchState(MatchManager.MatchState state)
    {
        return state switch
        {
            MatchManager.MatchState.WaitingForPlayers => "Waiting for Players...",
            MatchManager.MatchState.RoundStarting => "Get Ready!",
            MatchManager.MatchState.RoundActive => $"Round {MatchManager.Instance.Score.currentRound.Value}",
            MatchManager.MatchState.RoundEnded => "Round Over",
            _ => ""
        };
    }
}