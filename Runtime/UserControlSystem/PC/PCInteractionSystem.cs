// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2024 Engineering Hydrology, RWTH Aachen University.
// 
// AnyVR is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// AnyVR is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AnyVR.
// If not, see <https://www.gnu.org/licenses/>.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnyVR.UserControlSystem.PC
{
    /// <summary>
    ///     Represents a PC interaction system that extends XRBaseInteractor.
    ///     This class handles the interaction logic for PC input modality.
    /// </summary>
    public class PCInteractionSystem : XRBaseInteractor
    {
        /// <summary>
        ///     Represents the interaction mode for the PC interaction system.
        /// </summary>
        public enum InteractionMode
        {
            Toggle,
            Hold
        }

        [SerializeField] [Tooltip("Maximum range for interactable objects")]
        private float _maxInteractionDistance = 3f;

        [SerializeField] private Transform _interactionRaycastOrigin;

        [SerializeField] [Tooltip("If interaction should be toggleable or only when button is pressed.")]
        private InteractionMode _interactionMode;

        [SerializeField]
        [Tooltip("The Input System Action that will be used to interaction an object. Expects a 'Button' action.")]
        private InputActionProperty _interactionAction =
            new(new InputAction("Interaction", expectedControlType: "Button"));

        protected override void Start()
        {
            base.Start();
            allowHover = true;
            allowSelect = true;
            _interactionAction.action.started += TryStartInteraction;
            _interactionAction.action.canceled += TryEndInteraction;
        }

        protected void Update()
        {
            XRBaseInteractable interactable;

            if (!hasHover && IsThereObject(out interactable))
            {
                interactionManager.HoverEnter((IXRHoverInteractor)this, interactable);
            }

            else if (hasHover && !IsThereObject(out interactable))
            {
                UnhoverAll();
            }
        }

        private void UnhoverAll()
        {
            foreach (IXRHoverInteractable hovered in interactablesHovered)
            {
                interactionManager.HoverExit(this, hovered);
            }
        }

        private void UnselectAll()
        {
            foreach (IXRSelectInteractable selected in interactablesSelected)
            {
                interactionManager.SelectExit(this, selected);
            }
        }

        private void TryStartInteraction(InputAction.CallbackContext context)
        {
            if (_interactionMode == InteractionMode.Toggle)
            {
                ToggleInteraction(context);
            }
            else if (_interactionMode == InteractionMode.Hold)
            {
                TryStartInteractionHolding(context);
            }
        }


        private void ToggleInteraction(InputAction.CallbackContext context)
        {
            if (!hasSelection)
            {
                TryInteractWithObject(context);
            }
            else
            {
                TryReleaseObject(context);
            }
        }

        private void TryStartInteractionHolding(InputAction.CallbackContext context)
        {
            if (hasSelection)
            {
                return;
            }

            TryInteractWithObject(context);
        }

        private void TryInteractWithObject(InputAction.CallbackContext context)
        {
            if (hasHover)
            {
                SelectHoveredObject();
            }
        }

        private bool IsThereObject(out XRBaseInteractable target)
        {
            target = null;

            if (_interactionRaycastOrigin == null)
            {
                return false;
            }

            RaycastHit hit;

            if (Physics.Raycast(_interactionRaycastOrigin.position, _interactionRaycastOrigin.forward, out hit,
                    _maxInteractionDistance))
            {
                XRBaseInteractable interactionInteractable = hit.collider.gameObject.GetComponent<XRBaseInteractable>();

                if (interactionInteractable != null)
                {
                    target = interactionInteractable;
                    return true;
                }

                // Also tries parent object
                interactionInteractable = hit.collider.gameObject.GetComponentInParent<XRBaseInteractable>();

                if (interactionInteractable != null)
                {
                    target = interactionInteractable;
                    return true;
                }
            }

            return false;
        }

        private void TryEndInteraction(InputAction.CallbackContext context)
        {
            if (_interactionMode == InteractionMode.Toggle)
            {
                return;
            }

            TryReleaseObject(context);
        }

        private void TryReleaseObject(InputAction.CallbackContext context)
        {
            if (!hasSelection)
            {
                return;
            }

            ReleaseSelectedObject();
        }

        private void SelectHoveredObject()
        {
            interactionManager.SelectEnter(this, interactablesHovered[0].transform.GetComponent<IXRSelectInteractable>());
        }

        private void ReleaseSelectedObject()
        {
            interactionManager.SelectExit(this, firstInteractableSelected);
        }
    }
}
