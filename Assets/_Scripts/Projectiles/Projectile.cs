using UnityEngine;
using System;

// Interfaz básica para todo lo que pueda recibir daño (Jugadores, Enemigos, Cajas)
public interface IDamageable
{
    void TakeDamage(float amount);
}

public class Projectile : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private int _remainingBounces;
    private float _damage;
    private int _remainingPenetration;

    private float _maxLifetime = 5f;
    private float _currentLifetime;
    private Action<Projectile> _onRelease;

    // Recibe daño y penetración
    public void Initialize(Vector3 position, Vector3 direction, float speed, int bounces, float damage, int penetration, Action<Projectile> onRelease)
    {
        transform.position = position;
        _direction = direction.normalized;
        _speed = speed;
        _remainingBounces = bounces;
        _damage = damage;
        _remainingPenetration = penetration;
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
            // 1. Verificar si impactamos algo que puede recibir daño (Ej: Enemigo/Jugador)
            if (hit.collider.TryGetComponent(out IDamageable damageableTarget))
            {
                damageableTarget.TakeDamage(_damage);

                // Lógica de Penetración
                if (_remainingPenetration > 0)
                {
                    _remainingPenetration--;
                    // Movemos la bala un poco hacia adelante para evitar que colisione 
                    // con el mismo enemigo en el próximo frame
                    transform.position = hit.point + _direction * 0.1f;
                }
                else
                {
                    _onRelease?.Invoke(this); // Sin penetración, la bala se destruye
                }
            }
            // 2. Si es una pared o entorno duro
            else
            {
                // Lógica de Rebote
                if (_remainingBounces > 0)
                {
                    _direction = Vector3.Reflect(_direction, hit.normal);
                    _direction.y = 0; // Mantener en el plano horizontal 2D
                    _remainingBounces--;
                    transform.position = hit.point + _direction * 0.05f;
                }
                else
                {
                    _onRelease?.Invoke(this); // Sin rebotes, la bala se destruye
                }
            }
        }
    }
}