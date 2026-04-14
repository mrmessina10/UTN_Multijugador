using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Visuals")]
    public string weaponName;
    public GameObject bulletPrefab; // Por si cada arma usa un modelo de bala distinto

    [Header("Movement Stats")]
    public float muzzleVelocity = 20f;
    public int maxBounces = 2;

    [Header("Combat Stats")]
    public float damage = 10f;
    public float fireRate = 0.2f;
    public int penetrationCount = 0; // Cuántos enemigos atraviesa antes de destruirse
}
