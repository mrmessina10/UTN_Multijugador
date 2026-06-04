using UnityEngine;

[CreateAssetMenu(fileName = "NewMatchSettings", menuName = "Game/Match Settings")]
public class MatchSettingsSO : ScriptableObject
{
    [Header("Timers")]
    public float startDelay = 3f;
    public float roundDuration = 60f;
    public float endDelay = 5f;

    [Header("Rules")]
    public int minPlayersToStart = 2;
    public int roundsToWin = 3;
}