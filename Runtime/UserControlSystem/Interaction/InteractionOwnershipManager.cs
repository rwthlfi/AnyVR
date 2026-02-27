using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnyVR.UserControlSystem.Interaction
{
    /// <summary>
    ///     Manages ownership requests for interactables which have a
    ///     <see cref="XRNetworkedGrabInteractable">
    ///         component
    ///         when they are selected or deselected by an interactor.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractor))]
    public class InteractionOwnershipManager : MonoBehaviour
    {
        private void Awake()
        {
            XRBaseInteractor xRBaseInteractor = GetComponent<XRBaseInteractor>();
            xRBaseInteractor.selectEntered.AddListener(OnSelectEntered);
            xRBaseInteractor.selectExited.AddListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject.transform.TryGetComponent(out INetworkOwnableInteractable networkOwnableInteractable))
            {
                Debug.Log("[InteractionOwnershipManager] Requesting ownership for interactable.", args.interactableObject.transform);
                networkOwnableInteractable.RequestOwnership();
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactableObject.transform.TryGetComponent(out INetworkOwnableInteractable networkOwnableInteractable))
            {
                Debug.Log("[InteractionOwnershipManager] Releasing ownership for interactable.", args.interactableObject.transform);
                networkOwnableInteractable.ReleaseOwnership();
            }
        }
    }
}
