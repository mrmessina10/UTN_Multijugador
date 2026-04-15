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

    // Variables inyectadas por el Spawner
    private WeaponDataSO _weaponToGive;
    private PickUpSpawner _mySpawner;

    private Vector3 _startPos;
    private bool _isInitialized = false;

    // El Spawner llama a esto antes de OnNetworkSpawn
    //el spawner se encarga de crear el pickup y asignarle el arma que va a dar.
    public void SetupFromSpawner(WeaponDataSO weapon, PickUpSpawner spawner)
    {
        _weaponToGive = weapon;
        _mySpawner = spawner;
    }

    public override void OnNetworkSpawn()
    {
        _startPos = transform.position;

        if (_weaponToGive != null && meshRenderer != null)
        {
            meshRenderer.material.color = _weaponToGive.weaponColor;
        }

        _isInitialized = true;
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
        if (other.TryGetComponent(out PlayerShooting shooting))
        {
            if (shooting.IsOwner && meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }

            if (IsServer)
            {
                int id = database.GetIDByWeapon(_weaponToGive);
                int ammoAmount = _weaponToGive.maxAmmo;

                shooting.EquipWeapon(id, ammoAmount);

                // Le avisamos al spot que empiece a contar el tiempo
                if (_mySpawner != null) _mySpawner.NotifyPickupCollected();

                // Destruimos el objeto en toda la red
                // (Al instanciarlo dinámicamente, Despawn() aplica un Destroy por defecto)
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}