using Unity.Netcode;
using UnityEngine;

public class AmmoPickup : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private WeaponDataSO weaponToGive;
    [SerializeField] private WeaponDatabaseSO database;

    [Header("Visuals")]
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.25f;

    private Vector3 _startPos;
    private bool _isInitialized = false;

    private void Start()
    {
        _startPos = transform.position;
        _isInitialized = true;
    }
    public override void OnNetworkSpawn()
    {
        _startPos = transform.position;

        if (weaponToGive != null && meshRenderer != null)
        {
            meshRenderer.material.color = weaponToGive.weaponColor;
        }
    }

    private void Update()
    {
        float newY = _startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);

        transform.Rotate(Vector3.up, 45f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerShooting shooting))
        {
            int id = database.GetIDByWeapon(weaponToGive);
            int ammoAmount = weaponToGive.maxAmmo;

            shooting.EquipWeapon(id, ammoAmount);

            GetComponent<NetworkObject>().Despawn(false);

            gameObject.SetActive(false);
        }
    }
}