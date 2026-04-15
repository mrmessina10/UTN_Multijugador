using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PickUpSpawner : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private WeaponDatabaseSO database;

    [Header("Settings")]
    [SerializeField] private float respawnTime = 15f;
    [Tooltip("altura a la que flotaran los pickups")]
    [SerializeField] private float heightOffset = 1.0f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        SpawnPickup();
    }

    private void SpawnPickup()
    {
        WeaponDataSO selectedWeapon = GetWeightedRandomWeapon();

        Vector3 spawnPosition = transform.position + (Vector3.up * heightOffset);

        GameObject pickupGo = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);

        if (pickupGo.TryGetComponent(out AmmoPickup ammoPickup))
        {
            ammoPickup.SetupFromSpawner(selectedWeapon, this);
        }

        pickupGo.GetComponent<NetworkObject>().Spawn();
    }

    private WeaponDataSO GetWeightedRandomWeapon()
    {
        // 1. Calculamos el total dinámicamente leyendo la base de datos
        float totalWeight = 0;
        foreach (WeaponDataSO weapon in database.allWeapons)
        {
            totalWeight += weapon.spawnWeight;
        }

        // 2. Tiramos el dado virtual
        float randomValue = Random.Range(0, totalWeight);

        // 3. Buscamos al ganador
        float cursor = 0;
        foreach (WeaponDataSO weapon in database.allWeapons)
        {
            cursor += weapon.spawnWeight;
            if (randomValue <= cursor)
            {
                return weapon;
            }
        }

        // Fallback
        return database.allWeapons[0];
    }

    public void NotifyPickupCollected()
    {
        if (!IsServer) return;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        SpawnPickup();
    }
}