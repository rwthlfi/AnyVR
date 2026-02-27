using AnyVR.UserControlSystem.PC;
using UnityEngine;

namespace AnyVR.UserControlSystem
{
    public class PCScreenCursorHider : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _screenCursorObjects;

        private void Start()
        {
            PCCursorLockHandler.OnCursorUnlockToggle.AddListener(OnCursorUnlockToggle);
        }

        private void OnCursorUnlockToggle(bool isCursorUnlocked)
        {
            foreach (GameObject item in _screenCursorObjects)
            {
                item.SetActive(!isCursorUnlocked);
            }
        }
    }
}
