using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Observing;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace AnyVR.UserControlSystem.Interaction
{
    /// <summary>
    ///     A networked grab interactable for XR interactions.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(NetworkObserver))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(XRGeneralGrabTransformer))]
    [RequireComponent(typeof(XRSynchronizedInteractions))]
    public class XRNetworkedGrabInteractable : XRNetworkedBaseInteractable
    {
        // Right now, this class mainly ensures that all necessary components are present.
    }
}
