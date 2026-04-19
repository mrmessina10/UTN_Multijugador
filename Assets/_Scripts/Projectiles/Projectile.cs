using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    [Header("Visual Decoupling (Estética)")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float arcHeight = 2f;
    [Tooltip("Distancia que le toma al modelo visual completar una parábola (salto)")]
    [SerializeField] private float visualArcDistance = 10f;

    [Header("Gameplay (Potencia y Vida Útil)")]
    [SerializeField] private float speed = 20f;
    [Tooltip("Multiplicador de velocidad cuando el proyectil está en el suelo")]
    [SerializeField] private float groundedSpeedMultiplier = 0.2f;
    [Tooltip("Distancia total inicial que puede recorrer antes de caer")]
    [SerializeField] private float initialDistanceCapacity = 15f;
    [Tooltip("Cuánta distancia extra (potencia) gana por cada rebote exitoso")]
    [SerializeField] private float distanceBonusPerBounce = 5f;
    [SerializeField] private float groundDuration = 2f;

    private Vector3 _direction;
    private int _remainingBounces;
    private float _damage;
    private int _remainingPenetration;

    private StatusEffect _effectType;
    private float _effectDuration;

    private float _totalDistanceTraveled;
    private float _currentMaxDistance;
    private float _distanceSinceLastBounce;

    private bool _isGrounded;
    private float _groundTimer;
    private Action<Projectile> _onRelease;
    private Vector3 _startVisualLocalPos;

    private void Awake()
    {
        if (visualModel != null)
            _startVisualLocalPos = visualModel.localPosition;

        if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
    }

    public void Initialize(Vector3 position, Vector3 direction, int bounces, float damage, int penetration, StatusEffect effectType, float effectDuration, Action<Projectile> onRelease)
    {
        transform.position = position;
        _direction = direction.normalized;
        _remainingBounces = bounces;
        _damage = damage;
        _remainingPenetration = penetration;
        _effectType = effectType;
        _effectDuration = effectDuration;
        _onRelease = onRelease;

        _totalDistanceTraveled = 0f;
        _distanceSinceLastBounce = 0f;
        _currentMaxDistance = initialDistanceCapacity;

        _isGrounded = false;
        _groundTimer = 0f;

        if (visualModel != null)
            visualModel.localPosition = _startVisualLocalPos;

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        // velocidad actual según el estado
        float currentSpeed = _isGrounded ? speed * groundedSpeedMultiplier : speed;
        float moveDistance = currentSpeed * Time.deltaTime;

        if (!_isGrounded)
        {
            _totalDistanceTraveled += moveDistance;
            _distanceSinceLastBounce += moveDistance;

            CheckCollision(moveDistance);
            UpdateVisualArc();

            if (_totalDistanceTraveled >= _currentMaxDistance)
            {
                SetGrounded();
            }
        }
        else
        {
            HandleGroundedState();
        }

        transform.position += _direction * moveDistance;
    }

    private void UpdateVisualArc()
    {
        if (visualModel != null && arcHeight > 0f)
        {
            float progress = Mathf.Clamp01(_distanceSinceLastBounce / visualArcDistance);
            float currentHeight = Mathf.Sin(progress * Mathf.PI) * arcHeight;
            visualModel.localPosition = _startVisualLocalPos + new Vector3(0f, currentHeight, 0f);
        }
    }

    private void SetGrounded()
    {
        _isGrounded = true;
        _groundTimer = 0f;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        }

        if (visualModel != null) visualModel.localPosition = _startVisualLocalPos;
        if (trail != null) trail.emitting = false;
    }

    private void HandleGroundedState()
    {
        _groundTimer += Time.deltaTime;
        if (_groundTimer >= groundDuration)
        {
            _onRelease?.Invoke(this);
        }
    }

    private void CheckCollision(float distance)
    {
        // Raycast solo para detectar impactos mientras está en vuelo
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
                    SetGrounded();
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

                    _currentMaxDistance += distanceBonusPerBounce;
                    _distanceSinceLastBounce = 0f;
                }
                else
                {
                    SetGrounded();
                }
            }
        }
    }
}