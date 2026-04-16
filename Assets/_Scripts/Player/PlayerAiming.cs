using UnityEngine;
using Unity.Netcode;
using System.Globalization;
public class PlayerAiming : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReaderSO _inputReader;

    private Camera mainCamera;
    private Plane groundPlane;
    private Vector2 currentAimInput;

    private PlayerHealth _playerHealth;

    private void Awake()
    {
        mainCamera = Camera.main; // Cacheo la referencia a la cámara principal para optimizar el rendimiento
        groundPlane = new Plane(Vector3.up, Vector3.zero); // Plano horizontal en y=0

        _playerHealth = GetComponent<PlayerHealth>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        _inputReader.OnAimEvent.AddListener(HandleAimInput);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        _inputReader.OnAimEvent.RemoveListener(HandleAimInput);
    }

    private void HandleAimInput(Vector2 input)
    {
        currentAimInput = input;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (_playerHealth != null && _playerHealth.isDead.Value) return;
        // Si el jugador esta muerto, no puede apuntar

        ProcessAiming();
    }

    private void ProcessAiming()
    {
        Ray ray = mainCamera.ScreenPointToRay(currentAimInput);

        if ((groundPlane.Raycast(ray, out float hitDistance)))
        {
            Vector3 pointToLook = ray.GetPoint(hitDistance);

            pointToLook.y = transform.position.y; // Mantengo la altura del jugador para evitar que mire hacia arriba o abajo
            Vector3 lookDirection = pointToLook - transform.position;

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }
}
