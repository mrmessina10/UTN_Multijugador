using UnityEngine;

public class LevelController : MonoBehaviour
{
    
    [SerializeField] private AudioClip musicaDeEsteMapa;

    private void Start()
    {
        if (AudioManager.instance != null)
        {
            
            AudioManager.instance.PlayMusic(musicaDeEsteMapa);
        }
    }
}