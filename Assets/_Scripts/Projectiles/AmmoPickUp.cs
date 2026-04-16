using Unity.Netcode;
using UnityEngine;

public class AmmoPickup : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private WeaponDatabaseSO database;

    [Header("Visuals")]
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.25f;

    [Header("Network State")]
    // Inicializamos en -1 para luego asignarle un valor asi ocultamos latencia.
    public NetworkVariable<int> networkWeaponID = new NetworkVariable<int>(-1);

    private WeaponDataSO _weaponToGive;
    private PickUpSpawner _mySpawner;
    private Vector3 _startPos;
    private bool _isInitialized = false;

    public void SetupFromSpawner(WeaponDataSO weapon, PickUpSpawner spawner)
    {
        _mySpawner = spawner;
        networkWeaponID.Value = database.GetIDByWeapon(weapon);
    }

    public override void OnNetworkSpawn()
    {
        _startPos = transform.position;

        // Suscripción al evento de cambio
        networkWeaponID.OnValueChanged += (prev, current) =>
        {
            if (current != -1) UpdateWeaponState(current);
        };

        // Si somos el Host, lo procesamos al instante
        if (networkWeaponID.Value != -1)
        {
            UpdateWeaponState(networkWeaponID.Value);
        }

        _isInitialized = true;
    }

    private void UpdateWeaponState(int weaponID)
    {
        _weaponToGive = database.GetWeaponByID(weaponID);

        if (_weaponToGive != null && meshRenderer != null)
        {
            meshRenderer.material.color = _weaponToGive.weaponColor;

            // Solo encendemos el Renderer visual cuando ya tenemos el color aplicado.
            meshRenderer.enabled = true;
        }
    }

    private void Update()
    {
        if (!_isInitialized) return;

        float newY = _startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
        transform.Rotate(Vector3.up, 45f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Solo el servidor maneja la lógica de recogida

        if (!NetworkObject.IsSpawned) return; // Validación de seguridad para evitar errores si el objeto ya fue recogido

        if (other.TryGetComponent(out PlayerShooting shooting))
        {
            if (shooting.IsOwner && meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }

            if (IsServer)
            {
                // validación por seguridad no dar munición si ID = -1
                if (_weaponToGive == null) return;

                int ammoAmount = _weaponToGive.maxAmmo;
                shooting.EquipWeapon(networkWeaponID.Value, ammoAmount);

                if (_mySpawner != null) _mySpawner.NotifyPickupCollected();
                NetworkObject.Despawn(true);
            }
        }
    }
}