using Unity.Netcode;

public class MatchEndedState : IMatchState
{
    public void Enter(MatchManager manager)
    {
        manager.stateTimer.Value = NetworkManager.Singleton.ServerTime.Time + manager.endDelay;
    }

    public void Tick(MatchManager manager)
    {
        if (NetworkManager.Singleton.ServerTime.Time >= manager.stateTimer.Value)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(manager.mainMenuSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    public void Exit(MatchManager manager) { }
}