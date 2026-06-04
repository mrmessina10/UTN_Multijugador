using UnityEngine;
public enum StatusEffect //enum para los efectos de estado que los proyectiles pueden aplicar
{
    None,
    Stun,
    Slow,
    Poison
}

// 2. La Interfaz actualizada para recibir estados
public interface IDamageable
{
    void TakeDamage(float amount, StatusEffect effect, float effectDuration, ulong killerId);
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Visuals")]
    public string weaponName;
    public GameObject bulletPrefab;

    [Header("Movement Stats")]
    public float muzzleVelocity = 20f;
    public int maxBounces = 2;

    [Header("Combat Stats")]
    public float damage = 1f; // Daño base (ahora es 1 por defecto)
    public float fireRate = 0.5f;
    public int penetrationCount = 0;

    [Header("Status Effects")]
    public StatusEffect effectType = StatusEffect.None;
    public float effectDuration = 0f;

    [Header("Visual Identity")]
    public Color weaponColor = Color.white; // Color tematico de cada arma para legibilidad

    [Header("PickUp Logic")]
    public bool isBaseWeapon = true;
    public int maxAmmo = 5;

    [Header("Spawn Randomizer Settings")]
    [Tooltip("Higher weight = higher spawn chance. (e.g., 30 for common, 5 for rare)")]
    public float spawnWeight = 10f;
}
