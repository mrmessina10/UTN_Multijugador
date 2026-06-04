public interface IMatchState
{
    void Enter(MatchManager manager);
    void Tick(MatchManager manager);
    void Exit(MatchManager manager);
}