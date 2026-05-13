using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("grafico de vida")]
    [SerializeField] private GameObject heartPrefab;
    [Tooltip("El objeto vacío con un Horizontal Layout Group")]
    [SerializeField] private Transform heartsContainer;

    private List<GameObject> _heartIcons = new List<GameObject>();
    private PlayerHealth _localPlayerHealth;

    private void Start()
    {
        StartCoroutine(FindLocalPlayerRoutine());
    }

    private void OnDestroy()
    {
        if (_localPlayerHealth != null)
        {
            _localPlayerHealth.OnHealthChanged -= UpdateHearts;
        }
    }

    private IEnumerator FindLocalPlayerRoutine()
    {
        // validacion en cascada para asegurar que el NetworkManager, el SpawnManager y el objeto del jugador local estén disponibles antes de intentar acceder a ellos.
        while (NetworkManager.Singleton == null ||
               NetworkManager.Singleton.SpawnManager == null ||
               NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() == null)
        {
            yield return null;
        }

        _localPlayerHealth = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerHealth>();

        if (_localPlayerHealth != null)
        {
            _localPlayerHealth.OnHealthChanged += UpdateHearts;
            UpdateHearts(_localPlayerHealth.currentHealth.Value, _localPlayerHealth.MaxHealth);
        }
    }

    private void UpdateHearts(int currentHealth, int maxHealth)
    {
        // Configuración inicial: Instanciar los corazones necesarios si no existen
        if (_heartIcons.Count != maxHealth)
        {
            InitializeHearts(maxHealth);
        }

        // Lógica visual: Apagar/Prender o Animar
        for (int i = 0; i < _heartIcons.Count; i++)
        {
            bool shouldBeActive = i < currentHealth;
            bool isCurrentlyActive = _heartIcons[i].activeSelf;

            if (shouldBeActive && !isCurrentlyActive)
            {
                // El jugador curó HP (o es el inicio de la ronda)
                _heartIcons[i].SetActive(true);
                AnimateHeartGain(i);
            }
            else if (!shouldBeActive && isCurrentlyActive)
            {
                // El jugador perdió HP
                AnimateHeartLoss(i);
            }
        }
    }

    private void InitializeHearts(int maxHealth)
    {
        // Limpiar cualquier corazón residual (útil si se reinicia la ronda)
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }
        _heartIcons.Clear();

        // Instanciar la cantidad exacta de HP máximo
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject newHeart = Instantiate(heartPrefab, heartsContainer);
            _heartIcons.Add(newHeart);
        }
    }

    // --- HOOKS PARA FUTURAS ANIMACIONES ---
    //TODO ANIMACIONES
    private void AnimateHeartLoss(int index)
    {
        if (_heartIcons[index].TryGetComponent(out Image heartImage))
        {
            // Obtenemos el color actual y bajamos el canal Alpha (Transparencia) a 0
            Color transparentColor = heartImage.color;
            transparentColor.a = 0f;
            heartImage.color = transparentColor;
        }
    }

    private void AnimateHeartGain(int index)
    {
        if (_heartIcons[index].TryGetComponent(out Image heartImage))
        {
            // Restauramos el canal Alpha a 1 (100% visible)
            Color visibleColor = heartImage.color;
            visibleColor.a = 1f;
            heartImage.color = visibleColor;
        }
    }
}