using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraSetup : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        //busca incluso si el objeto está apagado (inactivo)
        CinemachineCamera cinemachineCam = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);

        if (cinemachineCam != null)
        {
            cinemachineCam.Follow = transform;

            //el objeto se activa si estaba apagado
            cinemachineCam.gameObject.SetActive(true);

            Debug.Log($"[ÉXITO] Cámara encontrada y activada. Siguiendo a: {gameObject.name}");
        }
        else
        {
            Debug.LogError("[ERROR] Definitivamente no hay ninguna CinemachineCamera en la Jerarquía de la escena.");
        }
    }
}
