using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class PlayerCameraSetup : NetworkBehaviour
{
    [Header("Settings")]
    [Tooltip("Opcional: Arrastra un transform hijo (ej. el cuello o torso) para afinar el encuadre. Si está vacío, usará la raíz del jugador.")]
    [SerializeField] private Transform targetPoint;

    public override void OnNetworkSpawn()
    {
        // Regla de oro: La cámara local solo se vincula al jugador que me pertenece
        if (!IsOwner) return;

        CinemachineCamera vCam = FindFirstObjectByType<CinemachineCamera>();

        if (vCam != null)
        {
            Transform followTarget = targetPoint != null ? targetPoint : transform;

            vCam.Follow = followTarget;
            vCam.LookAt = followTarget; 
        }
        else
        {
            Debug.LogError("PlayerCameraSetup: CinemachineCamera no encontrada en la escena.");
        }
    }
}