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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnyVR.UserControlSystem.PC
{
    /// <summary>
    ///     Represents a PC interaction system that extends XRBaseInteractor.
    ///     This class handles the interaction logic for PC input modality.
    /// </summary>
    public class PCInteractionSystem : XRBaseInteractor, IXRActivateInteractor
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
        private InteractionMode _selectionMode;

        [SerializeField]
        [Tooltip("The Input System Action that will be used to interaction an object. Expects a 'Button' action.")]
        private InputActionProperty _selectionAction =
            new(new InputAction("Interaction", expectedControlType: "Button"));

        [Header("Selection Interaction")]
        [SerializeField, Tooltip("Primary interaction action, uses XRI Activated event.")]
        private InputActionProperty _primaryInteractionAction =
            new(new InputAction("Primary Interaction", expectedControlType: "Button"));

        [SerializeField]
        [Tooltip("Secondary interaction action, can be used to trigger secondary interactions that would otherwise be handled by VR affordances. " +
            "Needs a PCSecondarySelectionAction Component on the interactable.")]
        private InputActionProperty _secondaryInteractionAction =
            new(new InputAction("Secondary Interaction", expectedControlType: "Button"));

        public bool shouldActivate => (firstInteractableSelected != null) && (firstInteractableSelected is IXRActivateInteractable);

        public bool shouldDeactivate => (firstInteractableSelected != null) && (firstInteractableSelected is IXRActivateInteractable);

        protected override void Start()
        {
            base.Start();
            allowHover = true;
            allowSelect = true;
            _selectionAction.action.started += TryStartSelection;
            _selectionAction.action.canceled += TryEndInteraction;
            _primaryInteractionAction.action.started += StartPrimarySelectionAction;
            _primaryInteractionAction.action.canceled += CancelPrimarySelectionAction;
            _secondaryInteractionAction.action.started += StartSecondarySelectionAction;
            _secondaryInteractionAction.action.canceled += CancelSecondarySelectionAction;
            EnableSelectionInteractions(false);
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

        private void DeselectAll()
        {
            foreach (IXRSelectInteractable selected in interactablesSelected)
            {
                interactionManager.SelectExit(this, selected);
            }
        }

        private void TryStartSelection(InputAction.CallbackContext context)
        {
            if (_selectionMode == InteractionMode.Toggle)
            {
                ToggleSelection(context);
            }
            else if (_selectionMode == InteractionMode.Hold)
            {
                TryStartSelecting(context);
            }
        }


        private void ToggleSelection(InputAction.CallbackContext context)
        {
            if (!hasSelection)
            {
                TrySelectObject(context);
            }
            else
            {
                TryReleaseObject(context);
            }
        }

        private void TryStartSelecting(InputAction.CallbackContext context)
        {
            if (hasSelection)
            {
                return;
            }

            TrySelectObject(context);
        }

        private void TrySelectObject(InputAction.CallbackContext context)
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
            if (_selectionMode == InteractionMode.Toggle)
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
            EnableSelectionInteractions(true);
        }

        private void ReleaseSelectedObject()
        {
            interactionManager.SelectExit(this, firstInteractableSelected);
            EnableSelectionInteractions(false);
        }

        private void StartPrimarySelectionAction(InputAction.CallbackContext context)
        {
            if (firstInteractableSelected is IXRActivateInteractable activateInteractable)
            {
                ActivateEventArgs args = new()
                {
                    interactorObject = this,
                    interactableObject = activateInteractable
                };
                activateInteractable.activated.Invoke(args);
            }
        }

        private void CancelPrimarySelectionAction(InputAction.CallbackContext context)
        {
            if (firstInteractableSelected is IXRActivateInteractable activateInteractable)
            {
                DeactivateEventArgs args = new()
                {
                    interactorObject = this,
                    interactableObject = activateInteractable
                };
                activateInteractable.deactivated.Invoke(args);
            }
        }

        private void StartSecondarySelectionAction(InputAction.CallbackContext context)
        {
            if (firstInteractableSelected is IXRActivateInteractable activateInteractable)
            {
                if (firstInteractableSelected.transform.TryGetComponent(out PCSecondarySelectionAction secondarySelectionAction))
                {
                    ActivateEventArgs args = new()
                    {
                        interactorObject = this,
                        interactableObject = activateInteractable
                    };
                    secondarySelectionAction.SecondarySelectActivated.Invoke(args);
                }
            }
        }

        private void CancelSecondarySelectionAction(InputAction.CallbackContext context)
        {
            if (firstInteractableSelected is IXRActivateInteractable activateInteractable)
            {
                if (firstInteractableSelected.transform.TryGetComponent(out PCSecondarySelectionAction secondarySelectionAction))
                {
                    DeactivateEventArgs args = new()
                    {
                        interactorObject = this,
                        interactableObject = activateInteractable
                    };
                    secondarySelectionAction.SecondarySelectDeactivated.Invoke(args);
                }
            }
        }

        private void EnableSelectionInteractions(bool v)
        {
            if (v)
            {
                _primaryInteractionAction.action.Enable();
                _secondaryInteractionAction.action.Enable();
            }
            else
            {
                _primaryInteractionAction.action.Disable();
                _secondaryInteractionAction.action.Disable();
            }
        }

        public void GetActivateTargets(List<IXRActivateInteractable> targets)
        {
            targets.Clear(); // As expected by Unity, cf. documentation.
            if (firstInteractableSelected is IXRActivateInteractable activateInteractable)
            {
                targets.Add(activateInteractable);
            }
        }
    }
}
