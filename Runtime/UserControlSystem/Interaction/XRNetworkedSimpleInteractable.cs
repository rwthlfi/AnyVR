using FishNet.Object;
using FishNet.Observing;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AnyVR.UserControlSystem.Interaction
{
    /// <summary>
    ///     A networked simple interactable for XR interactions.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkObserver))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(XRSynchronizedInteractions))]
    public class XRNetworkedSimpleInteractable : XRNetworkedBaseInteractable
    {
        // This class can be expanded in the future for simple interactable objects.
    }
}
