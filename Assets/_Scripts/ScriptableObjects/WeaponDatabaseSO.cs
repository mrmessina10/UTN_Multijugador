using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponDatabaseSO : ScriptableObject
// Esta clase es un ScriptableObject que actúa como una base de datos para almacenar información de armas.
{
    public List<WeaponDataSO> allWeapons;

    public WeaponDataSO GetWeaponByID(int id)
    {
        if (id < 0 || id >= allWeapons.Count) return allWeapons[0]; //return default si id esta fuera de rango
        return allWeapons[id];
    }
    public int GetIDByWeapon(WeaponDataSO weapon)
    {
        return allWeapons.IndexOf(weapon);
    }
}