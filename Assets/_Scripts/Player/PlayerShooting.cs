using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReaderSO inputReader;
    [SerializeField] private Transform firePoint;

    [Header("Weapon Configuration")]
    [SerializeField] private WeaponDataSO activeWeapon; // Referencia a las estadísticas

    private float _lastFireTime;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.OnShootEvent.AddListener(HandleShoot);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.OnShootEvent.RemoveListener(HandleShoot);
    }

    private void HandleShoot(bool isPressed)
    {
        // Seguridad: evitar errores si olvidaste asignar el arma en el editor
        if (activeWeapon == null)
        {
            Debug.LogWarning("No hay un WeaponDataSO asignado al Player!");
            return;
        }

        if (isPressed && Time.time >= _lastFireTime + activeWeapon.fireRate)
        {
            _lastFireTime = Time.time;
            FireServerRpc(firePoint.position, firePoint.forward);
        }
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 pos, Vector3 dir)
    {
        FireClientRpc(pos, dir);
    }

    [ClientRpc]
    private void FireClientRpc(Vector3 pos, Vector3 dir)
    {
        if (activeWeapon == null) return;

        // AQUÍ SE CORRIGE EL ERROR: Ahora enviamos los 6 parámetros exactos
        ProjectilePool.Instance.SpawnProjectile(
            pos,
            dir,
            activeWeapon.muzzleVelocity,
            activeWeapon.maxBounces,
            activeWeapon.damage,          // Argumento 5
            activeWeapon.penetrationCount // Argumento 6
        );
    }
}