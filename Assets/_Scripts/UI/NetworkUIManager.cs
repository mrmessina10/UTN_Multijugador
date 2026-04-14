using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject uiContainer; // Referencia al Canvas o Panel
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            HideUI();
        });

        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            HideUI();
        });
    }

    private void HideUI()
    {
        // Apagamos el contenedor visual que asignemos en el inspector
        if (uiContainer != null)
        {
            uiContainer.SetActive(false);
        }
    }
}
