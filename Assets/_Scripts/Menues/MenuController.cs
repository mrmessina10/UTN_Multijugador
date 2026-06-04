using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;

public class MenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject clientLobbyPanel; // Nuevo panel para el cliente conectado
    [SerializeField] private GameObject optionsPanel;

    [Header("Networking UI")]
    [SerializeField] private TextMeshProUGUI hostCodeText;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TextMeshProUGUI joinErrorText; // Texto de feedback de errores para el cliente
    [SerializeField] private TextMeshProUGUI hostPlayerListText; // Texto de lista dentro de Host_Panel
    [SerializeField] private TextMeshProUGUI clientPlayerListText; // Texto de lista dentro de ClientLobby_Panel

    [Header("Match Settings")]
    [SerializeField] private int maxConnections = 4;
    [SerializeField] private string gameSceneName = "GameScene";

    private async void Start()
    {
        CloseAllPanels();

        // Nos suscribimos a los eventos de red globales de Netcode
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        await InitializeUGS();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private async Task InitializeUGS()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Connected to UGS. Player ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGS Init Error: {e.Message}");
        }
    }

    public void CloseAllPanels()
    {
        if (hostPanel != null) hostPanel.SetActive(false);
        if (joinPanel != null) joinPanel.SetActive(false);
        if (clientLobbyPanel != null) clientLobbyPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    // --- HOST LOGIC ---
    public async void OnClick_Host()
    {
        CloseAllPanels();
        if (hostPanel != null) hostPanel.SetActive(true);
        hostCodeText.text = "Creating Relay Room...";

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            hostCodeText.text = $"Room Code: {joinCode}";

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // IMPORTANTE: Arrancamos el host inmediatamente para abrir el canal de escucha,
            // pero nos quedamos en esta escena esperando jugadores.
            NetworkManager.Singleton.StartHost();
            UpdatePlayerListUI();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay Create Failed: {e.Message}");
            hostCodeText.text = "Connection Error.";
        }
    }

    public void OnClick_StartMatch()
    {
        // El Host decide cuándo pasar a la acción. Transiciona a todos los clientes conectados.
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // --- JOIN LOGIC ---
    public void OnClick_Join()
    {
        CloseAllPanels();
        if (joinPanel != null) joinPanel.SetActive(true);
        if (joinErrorText != null) joinErrorText.text = "";
    }

    public async void ConfirmJoinMatch()
    {
        string cleanCode = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(cleanCode))
        {
            if (joinErrorText != null) joinErrorText.text = "Code cannot be empty.";
            return;
        }

        if (joinErrorText != null) joinErrorText.text = "Connecting to Relay...";

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(cleanCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            if (joinErrorText != null) joinErrorText.text = "Establishing Handshake...";
            NetworkManager.Singleton.StartClient();

            // Nota: No cambiamos de panel aquí porque StartClient es asíncrono a nivel de red.
            // Esperamos al callback de éxito "OnClientConnected".
        }
        catch (System.Exception e)
        {
            // Captura errores de código inválido, expirado o fallos de red de la API
            if (joinErrorText != null) joinErrorText.text = $"Error: {e.Message}";
            Debug.LogError($"Relay Join Failed: {e.Message}");
        }
    }

    // --- NETCODE CALLBACKS (LOBBY MANAGEMENT) ---
    private void OnClientConnected(ulong clientId)
    {
        UpdatePlayerListUI();

        // Si soy el cliente local que se acaba de conectar con éxito
        if (!NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
        {
            CloseAllPanels();
            if (clientLobbyPanel != null) clientLobbyPanel.SetActive(true);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        UpdatePlayerListUI();

        // Si el cliente local pierde conexión con el host de forma abrupta
        if (!NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
        {
            CloseAllPanels();
            if (joinPanel != null) joinPanel.SetActive(true);
            if (joinErrorText != null) joinErrorText.text = "Disconnected from Host.";
        }
    }

    private void UpdatePlayerListUI()
    {
        if (NetworkManager.Singleton == null) return;

        // Construimos el string con los IDs de red de los clientes conectados actualmente
        string playerListString = "Connected Players:\n";
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            playerListString += $"- Player_{id} {(id == NetworkManager.Singleton.LocalClientId ? "(You)" : "")}\n";
        }

        // Repartimos el texto según el rol de la instancia
        if (NetworkManager.Singleton.IsServer)
        {
            if (hostPlayerListText != null) hostPlayerListText.text = playerListString;
        }
        else
        {
            if (clientPlayerListText != null) clientPlayerListText.text = playerListString;
        }
    }

    // --- NAVIGATION OTHERS ---
    public void OnClick_Options()
    {
        CloseAllPanels();
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void OnClick_BackToMain()
    {
        // Si el Host o Cliente cancelan la espera, cerramos las conexiones activas de Netcode
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
        CloseAllPanels();
    }

    public void OnClick_Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}