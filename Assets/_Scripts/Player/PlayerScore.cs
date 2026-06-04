using Unity.Netcode;

public class PlayerScore : NetworkBehaviour
{
    public NetworkVariable<int> kills = new NetworkVariable<int>(0);
    public NetworkVariable<int> deaths = new NetworkVariable<int>(0);

    public void AddKill()
    {
        if (!IsServer) return;
        kills.Value++;
    }

    public void AddDeath()
    {
        if (!IsServer) return;
        deaths.Value++;
    }
}