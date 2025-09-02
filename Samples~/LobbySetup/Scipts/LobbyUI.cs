using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.Sample
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField]
        private UISessionPanel _uiSessionPanel;

        private void Start()
        {
            Assert.IsNotNull(_uiSessionPanel);
            _uiSessionPanel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _uiSessionPanel.gameObject.SetActive(!_uiSessionPanel.gameObject.activeSelf);
            }
        }
    }
}
