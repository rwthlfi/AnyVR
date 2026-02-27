using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AnyVR.UserControlSystem
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class PCSecondarySelectionAction : MonoBehaviour
    {
        [SerializeField]
        private ActivateEvent _secondarySelectActivated = new();

        [SerializeField]
        private DeactivateEvent _secondarySelectDeactivated = new();
        /// <summary>
        ///     Event triggered when the secondary select action is activated and the interactable is selected.
        ///     Can be used to trigger events that are otherwise handled by VR affordances.
        /// </summary>
        public ActivateEvent SecondarySelectActivated => _secondarySelectActivated;
        /// <summary>
        ///     Event triggered when the secondary select action is deactivated and the interactable is selected.
        ///     <!/summary>
        public DeactivateEvent SecondarySelectDeactivated => _secondarySelectDeactivated;
    }
}
