using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerAmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image regenFill;
    private PlayerShooting _player;

    public void Initialize(PlayerShooting player)
    {
        _player = player;
    }

    private void Update()
    {
        if (_player == null || _player.ActiveWeapon == null) return;

        int max = _player.ActiveWeapon.maxAmmo;
        ammoText.text = $"{_player.currentAmmo.Value} / {max}";

        regenFill.fillAmount = _player.regenProgress.Value;

        regenFill.gameObject.SetActive(_player.ActiveWeapon.isBaseWeapon);
    }
}
