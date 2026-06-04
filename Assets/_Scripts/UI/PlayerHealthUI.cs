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
    private List<Image> _heartImages = new List<Image>();
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
        if (_heartIcons.Count != maxHealth)
        {
            InitializeHearts(maxHealth);
        }

        for (int i = 0; i < _heartIcons.Count; i++)
        {
            bool shouldBeActive = i < currentHealth;

            // Leemos el alpha actual para saber si el corazón está visible o no
            bool isVisuallyActive = _heartImages[i].color.a > 0f;

            if (shouldBeActive && !isVisuallyActive)
            {
                _heartIcons[i].SetActive(true);
                AnimateHeartGain(i);
            }
            else if (!shouldBeActive && isVisuallyActive)
            {
                AnimateHeartLoss(i);
            }
        }
    }

    private void InitializeHearts(int maxHealth)
    {
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }

        _heartIcons.Clear();
        _heartImages.Clear();

        for (int i = 0; i < maxHealth; i++)
        {
            GameObject newHeart = Instantiate(heartPrefab, heartsContainer);
            _heartIcons.Add(newHeart);
            _heartImages.Add(newHeart.GetComponent<Image>());
        }
    }

    // --- HOOKS PARA FUTURAS ANIMACIONES ---
    private void AnimateHeartLoss(int index)
    {
        Color transparentColor = _heartImages[index].color;
        transparentColor.a = 0f;
        _heartImages[index].color = transparentColor;
    }

    private void AnimateHeartGain(int index)
    {
        Color visibleColor = _heartImages[index].color;
        visibleColor.a = 1f;
        _heartImages[index].color = visibleColor;
    }
}