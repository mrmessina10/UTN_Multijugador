using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Paneles Superpuestos")]
    [SerializeField] private GameObject hostPanel;    
    [SerializeField] private GameObject joinPanel;    
    [SerializeField] private GameObject optionsPanel; 
    [SerializeField] private GameObject exitPanel;

    private void Start()
    {
        
        CloseAllPanels();
    }

    
    public void CloseAllPanels()
    {
        if (hostPanel != null) hostPanel.SetActive(false);
        if (joinPanel != null) joinPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    
    public void OnClick_Host()
    {
        CloseAllPanels();
        if (hostPanel != null) hostPanel.SetActive(true);
        Debug.Log("Abriendo Servidor... (Próximamente lógica de NGO)");
    }

    
    public void OnClick_Join()
    {
        CloseAllPanels();
        if (joinPanel != null) joinPanel.SetActive(true);
        Debug.Log("Mostrando recuadro para escribir el código y unirse...");
    }

    
    public void OnClick_Options()
    {
        CloseAllPanels();
        if (optionsPanel != null) optionsPanel.SetActive(true);
        Debug.Log("Abriendo panel de opciones...");
    }

    
    public void OnClick_Exit()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit(); 
    }

    public void ConfirmExit()
{
        #if UNITY_EDITOR
            
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            
            Application.Quit();
        #endif
}
}
