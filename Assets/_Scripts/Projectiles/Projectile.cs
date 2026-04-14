using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private int _remainingBounces;
    private float _damage;
    private int _remainingPenetration;

    // variables para los Status Effects
    private StatusEffect _effectType;
    private float _effectDuration;

    private float _maxLifetime = 5f;
    private float _currentLifetime;
    private Action<Projectile> _onRelease;

    public void Initialize(Vector3 position, Vector3 direction, float speed, int bounces, float damage, int penetration, StatusEffect effectType, float effectDuration, Action<Projectile> onRelease)
    {
        transform.position = position;
        _direction = direction.normalized;
        _speed = speed;
        _remainingBounces = bounces;
        _damage = damage;
        _remainingPenetration = penetration;
        _effectType = effectType;
        _effectDuration = effectDuration;
        _onRelease = onRelease;
        _currentLifetime = 0;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        _currentLifetime += Time.deltaTime;
        if (_currentLifetime > _maxLifetime)
        {
            _onRelease?.Invoke(this);
            return;
        }

        float moveDistance = _speed * Time.deltaTime;
        CheckCollision(moveDistance);
        transform.position += _direction * moveDistance;
    }

    private void CheckCollision(float distance)
    {
        if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, distance + 0.1f))
        {
            if (hit.collider.TryGetComponent(out IDamageable damageableTarget))
            {
                damageableTarget.TakeDamage(_damage, _effectType, _effectDuration);

                if (_remainingPenetration > 0)
                {
                    _remainingPenetration--;
                    transform.position = hit.point + _direction * 0.1f;
                }
                else
                {
                    _onRelease?.Invoke(this);
                }
            }
            else
            {
                if (_remainingBounces > 0)
                {
                    _direction = Vector3.Reflect(_direction, hit.normal);
                    _direction.y = 0;
                    _remainingBounces--;
                    transform.position = hit.point + _direction * 0.05f;
                }
                else
                {
                    _onRelease?.Invoke(this);
                }
            }
        }
    }
}