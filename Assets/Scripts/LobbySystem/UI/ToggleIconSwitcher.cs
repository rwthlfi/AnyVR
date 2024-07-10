using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI
{
    [RequireComponent(typeof(Toggle))]
    [ExecuteInEditMode]
    public class ToggleIconSwitcher : MonoBehaviour
    {
        [SerializeField] private Image _offImage;

        private void Start()
        {
            Toggle toggle = GetComponent<Toggle>();
            _offImage.enabled = !toggle.isOn;
            toggle.onValueChanged.AddListener(b =>
            {
                _offImage.enabled = !b;
            });
        }
    }
}