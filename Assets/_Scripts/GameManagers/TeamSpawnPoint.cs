using UnityEngine;

public class TeamSpawnPoint : MonoBehaviour
{
    public enum Team { Red, Blue }
    public Team team;

    private void Awake()
    {
        // Ocultar el cuadrado visualmente cuando arranca el juego, 
        // conservando su Transform para usarlo como referencia.
        GetComponent<MeshRenderer>().enabled = false;
    }
}