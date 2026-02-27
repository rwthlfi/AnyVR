using FishNet.Object;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnyVR.UserControlSystem
{
    public class XRNetworkedSocketInteractionFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
        public bool canProcess => isActiveAndEnabled;

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            bool canHover = interactable.transform.gameObject.TryGetComponent(out NetworkObject netObj);
            canHover = canHover && netObj.Owner.ClientId == -1;
            return canHover;
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            bool canHover = interactable.transform.gameObject.TryGetComponent(out NetworkObject netObj);
            canHover = canHover && netObj.Owner.ClientId == -1;
            return canHover;
        }
    }
}
