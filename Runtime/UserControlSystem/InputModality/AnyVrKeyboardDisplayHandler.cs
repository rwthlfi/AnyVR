using AnyVR.PlatformManagement;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

namespace AnyVR.UserControlSystem
{
    [RequireComponent(typeof(XRKeyboardDisplay))]
    public class AnyVrKeyboardDisplayHandler : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        internal static event Action<BaseEventData> OnSelect;
        internal static event Action<BaseEventData> OnDeselect;

        private void Start()
        {
            XRKeyboardDisplay display = GetComponent<XRKeyboardDisplay>();
            if (!PlatformInfo.IsXRPlatform())
            {
                display.enabled = false;
            }
        }

        void ISelectHandler.OnSelect(BaseEventData eventData)
        {
            OnSelect?.Invoke(eventData);
        }

        void IDeselectHandler.OnDeselect(BaseEventData eventData)
        {
            OnDeselect?.Invoke(eventData);
        }
    }
}
