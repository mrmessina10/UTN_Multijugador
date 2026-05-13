using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    // Diccionario que vincula cada Prefab con su propio Pool
    private Dictionary<GameObject, IObjectPool<Projectile>> _pools = new Dictionary<GameObject, IObjectPool<Projectile>>();

    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxSize = 100;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnProjectile(GameObject prefab, Vector3 pos, Vector3 dir, int bounces, float damage, int penetration, StatusEffect effectType, float effectDuration)
    {
        // Si no existe un pool para este prefab, lo creamos
        if (!_pools.ContainsKey(prefab))
        {
            _pools.Add(prefab, CreateNewPool(prefab));
        }

        Projectile p = _pools[prefab].Get();

        p.Initialize(pos, dir, bounces, damage, penetration, effectType, effectDuration, (proj) => _pools[prefab].Release(proj));
    }

    private IObjectPool<Projectile> CreateNewPool(GameObject prefab)
    {
        return new ObjectPool<Projectile>(
            createFunc: () => Instantiate(prefab).GetComponent<Projectile>(),
            actionOnGet: (p) => p.gameObject.SetActive(true),
            actionOnRelease: (p) => p.gameObject.SetActive(false),
            actionOnDestroy: (p) => Destroy(p.gameObject),
            collectionCheck: true,
            defaultCapacity,
            maxSize
        );
    }
}