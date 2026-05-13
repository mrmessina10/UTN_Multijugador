using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReaderSO inputReader;
    [SerializeField] private Transform firePoint;
    [SerializeField] private WeaponDatabaseSO weaponDatabase;

    [Header("Network State")]
    public NetworkVariable<int> currentWeaponID = new NetworkVariable<int>(0);
    public NetworkVariable<int> currentAmmo = new NetworkVariable<int>(5);
    public NetworkVariable<float> regenProgress = new NetworkVariable<float>(0);

    private WeaponDataSO _activeWeapon;
    // Propiedad pública para que la UI pueda leer los datos del arma actual
    public WeaponDataSO ActiveWeapon => _activeWeapon;

    private float _lastFireTime;
    private const float REGEN_TIME = 2.0f;

    private PlayerHealth _playerHealth;

    public override void OnNetworkSpawn()
    {
        _playerHealth = GetComponent<PlayerHealth>();

        currentWeaponID.OnValueChanged += (prev, next) => UpdateLocalWeapon(next);
        UpdateLocalWeapon(currentWeaponID.Value);

        if (IsOwner)
        {
            inputReader.OnShootEvent.AddListener(HandleShoot);

            // CONECTAR LA UI LOCAL
            PlayerAmmoUI ui = FindAnyObjectByType<PlayerAmmoUI>();
            if (ui != null) ui.Initialize(this);
        }
    }

    private void UpdateLocalWeapon(int id)
    {
        _activeWeapon = weaponDatabase.GetWeaponByID(id);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (_playerHealth != null && _playerHealth.isDead.Value) return; // Si el jugador esta muerto, no regenera municion

        if (_activeWeapon.isBaseWeapon && currentAmmo.Value < _activeWeapon.maxAmmo)
        {
            regenProgress.Value += Time.deltaTime / REGEN_TIME;
            if (regenProgress.Value >= 1.0f)
            {
                currentAmmo.Value++;
                regenProgress.Value = 0;
            }
        }
    }

    private void HandleShoot(bool isPressed)
    {
        if (_playerHealth != null && _playerHealth.isDead.Value) return; // Si el jugador esta muerto, no puede disparar

        if (isPressed && Time.time >= _lastFireTime + _activeWeapon.fireRate && currentAmmo.Value > 0)
        {
            _lastFireTime = Time.time;
            FireServerRpc();
        }
    }

    [ServerRpc]
    private void FireServerRpc()
    {
        if (_playerHealth != null && _playerHealth.isDead.Value) return; // Validación adicional en el servidor para evitar disparar si el jugador esta muerto

        if (currentAmmo.Value <= 0) return;

        currentAmmo.Value--;
        FireClientRpc(firePoint.position, firePoint.forward);

        if (!_activeWeapon.isBaseWeapon && currentAmmo.Value <= 0)
        {
            currentWeaponID.Value = 0;
            currentAmmo.Value = weaponDatabase.GetWeaponByID(0).maxAmmo;
        }
    }

    [ClientRpc]
    private void FireClientRpc(Vector3 pos, Vector3 dir)
    {
        ProjectilePool.Instance.SpawnProjectile(
            _activeWeapon.bulletPrefab, pos, dir,
            _activeWeapon.maxBounces, _activeWeapon.damage, _activeWeapon.penetrationCount,
            _activeWeapon.effectType, _activeWeapon.effectDuration
        );
    }

    public void EquipWeapon(int id, int ammo)
    {
        if (!IsServer) return;
        currentWeaponID.Value = id;
        currentAmmo.Value = ammo;
        regenProgress.Value = 0;
    }
}