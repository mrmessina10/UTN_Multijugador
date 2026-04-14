using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxSize = 100;

    private IObjectPool<Projectile> _pool;

    private void Awake()
    {
        Instance = this;
        _pool = new ObjectPool<Projectile>(
            CreateProjectile,
            OnGetFromPool,
            OnReleaseToPool,
            OnDestroyPooledObject,
            true,
            defaultCapacity,
            maxSize
        );
    }

    private Projectile CreateProjectile() => Instantiate(projectilePrefab);
    private void OnGetFromPool(Projectile p) => p.gameObject.SetActive(true);
    private void OnReleaseToPool(Projectile p) => p.gameObject.SetActive(false);
    private void OnDestroyPooledObject(Projectile p) => Destroy(p.gameObject);

    // ACTUALIZADO: Ahora recibe daño y penetración
    public void SpawnProjectile(Vector3 pos, Vector3 dir, float speed, int bounces, float damage, int penetration)
    {
        Projectile p = _pool.Get();

        // Pasamos los nuevos datos a la bala
        p.Initialize(pos, dir, speed, bounces, damage, penetration, (proj) => _pool.Release(proj));
    }
}
