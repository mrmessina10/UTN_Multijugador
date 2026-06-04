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
        groundPlane = new Plane(Vector3.up, Vector3.zero);
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
        // Si la cámara es nula o fue destruida por el motor, la buscamos.
        if (mainCamera == null || !mainCamera.gameObject.activeInHierarchy)
        {
            mainCamera = Camera.main;

            // Si el motor aún no instanció la nueva cámara de Mapa1, abortamos el frame para no crashear.
            if (mainCamera == null) return;
        }

        Ray ray = mainCamera.ScreenPointToRay(currentAimInput);

        if ((groundPlane.Raycast(ray, out float hitDistance)))
        {
            Vector3 pointToLook = ray.GetPoint(hitDistance);
            pointToLook.y = transform.position.y;

            Vector3 lookDirection = pointToLook - transform.position;

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }
}
