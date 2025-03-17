using AnyVr.LobbySystem;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVr.Samples.NewLobbySetup
{
    public class OverlayUI : MonoBehaviour
    {
        [SerializeField] private Button leaveButton;

        private void Start()
        {
            leaveButton.onClick.AddListener(() =>
            {
                LobbyHandler.GetInstance()?.Leave();
            });
            LobbyHandler.GetInstance()?.SetMuteSelf(false);
        }
    }
}