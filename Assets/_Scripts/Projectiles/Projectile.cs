using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform visualModel;
    public TrailRenderer trail;

    [Header("Settings: Flight & Visuals")]
    public float speed = 20f;
    public float arcHeight = 2f;
    public float visualArcDistance = 10f;
    public float distanceBonusPerBounce = 5f;
    public float initialDistanceCapacity = 15f;

    [Header("Settings: Transition & Ground")]
    public float transitionArcDistance = 5f;
    public float groundedSpeedMultiplier = 0.2f;
    public float groundedFriction = 0.98f;
    public float groundDuration = 2f;

    // --- VARIABLES DE GAMEPLAY ---
    // Setedas por el Weapon/Pool al disparar
    [HideInInspector] public Vector3 direction;
    [HideInInspector] public int remainingBounces;
    [HideInInspector] public float damage;
    [HideInInspector] public int remainingPenetration;
    [HideInInspector] public StatusEffect effectType;
    [HideInInspector] public float effectDuration;
    [HideInInspector] public ulong shooterNetworkId;

    // --- TRACKERS DE ESTADO INTERNO ---
    // Públicas pero ocultas para que los Estados puedan leerlas y modificarlas
    [HideInInspector] public float totalDistanceTraveled;
    [HideInInspector] public float currentMaxDistance;
    [HideInInspector] public float distanceSinceLastBounce;
    [HideInInspector] public Vector3 startVisualLocalPos;

    [HideInInspector] public float transitionDistance;
    [HideInInspector] public float apexHeight;
    [HideInInspector] public float currentGroundedSpeed;
    [HideInInspector] public float groundTimer;

    public Action<Projectile> onRelease;

    private IProjectileState _currentState;

    private void Awake()
    {
        if (visualModel != null) startVisualLocalPos = visualModel.localPosition;
        if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
    }

    public void Initialize(Vector3 pos, Vector3 dir, int bounces, float dmg, int pen, StatusEffect effect, float effectDur, ulong shooterId, Action<Projectile> releaseAction)
    {
        transform.position = pos;
        direction = dir.normalized;

        // Datos de combate
        remainingBounces = bounces;
        damage = dmg;
        remainingPenetration = pen;
        effectType = effect;
        effectDuration = effectDur;
        onRelease = releaseAction;

        // Reset de Trackers de Vuelo
        totalDistanceTraveled = 0f;
        distanceSinceLastBounce = 0f;
        currentMaxDistance = initialDistanceCapacity;

        // Reset de Trackers de Transición y Suelo
        transitionDistance = 0f;
        currentGroundedSpeed = speed * groundedSpeedMultiplier;
        groundTimer = 0f;

        shooterNetworkId = shooterId;

        if (visualModel != null)
            visualModel.localPosition = startVisualLocalPos;

        // Inyección del Estado Inicial (Flyweight Singleton)
        ChangeState(ActiveFlightState.Instance);

        gameObject.SetActive(true);
    }

    private void Update()
    {
        // El proyectil no toma decisiones. Delega el frame al estado actual.
        _currentState?.UpdateState(this);
    }

    public void ChangeState(IProjectileState newState)
    {
        _currentState = newState;
        _currentState.EnterState(this);
    }
}