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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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

        private Coroutine _selectionCoroutine = null;
        private bool _isSelectionButtonPressed = false;
        private IXRHoverInteractable HoveredObject => hasHover ? interactablesHovered[0] : null;

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

        [Header("Throwing")]
        [SerializeField]
        private ThrowingSettings _throwingSettings = new ThrowingSettings
        {
            _throwDelay = 0.5f,
            _animSpeed = 1f,
            _maxThrowChargeDuration = 2f,
            _maxThrowForce = 10f
        };

        private Coroutine _throwCoroutine = null;
        private bool _isPreparingThrow = false;

        private UnityEvent<float> _onThrow = new();
        /// <summary>
        ///    Event invoked when a throw action is performed, passing the throw force as a float parameter.
        /// </summary>
        public UnityEvent<float> OnThrow => _onThrow;

        private UnityEvent<float> _onChargeThrow = new();
        /// <summary>
        ///   Event invoked when a throw is being charged, passing the current percentage of charge force as a float parameter.
        ///   Can be used to update UI or animations.
        /// </summary>
        public UnityEvent<float> OnChargeThrow => _onChargeThrow;

        public bool CanThrow => firstInteractableSelected is XRGrabInteractable;

        public bool shouldActivate => (firstInteractableSelected != null) && (firstInteractableSelected is IXRActivateInteractable);

        public bool shouldDeactivate => (firstInteractableSelected != null) && (firstInteractableSelected is IXRActivateInteractable);



        protected override void Awake()
        {
            base.Awake();
            if (s_instance == null)
            {
                s_instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        protected override void Start()
        {
            base.Start();
            allowHover = true;
            allowSelect = true;
            _selectionAction.action.started += HandleSelectionPress;
            _selectionAction.action.canceled += HandleSelectionRelease;
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

        private void HandleSelectionPress(InputAction.CallbackContext context)
        {
            _isSelectionButtonPressed = true;
            if (hasSelection || HoveredObject is XRGrabInteractable)
            {
                _selectionCoroutine = StartCoroutine(Co_HandleSelectionPress(context, OnComplete));
            }
            else
            {
                TryStartSelecting(context);
            }

            void OnComplete(float pressDuration)
            {
                if (pressDuration < _throwingSettings._throwDelay)
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
            }
        }

        private IEnumerator Co_HandleSelectionPress(InputAction.CallbackContext context, Action<float> callback)
        {
            float elapsedTime = 0f;
            while (_isSelectionButtonPressed)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
                if (elapsedTime >= _throwingSettings._throwDelay && CanThrow)
                {
                    StartThrowing(context);
                }
            }
            callback.Invoke(elapsedTime);
            _selectionCoroutine = null;
        }

        private void HandleSelectionRelease(InputAction.CallbackContext context)
        {
            _isSelectionButtonPressed = false;
            if (_isPreparingThrow)
            {
                StopThrowing(context);
            }
            else
            {
                if (_selectionMode == InteractionMode.Toggle)
                {
                    if (firstInteractableSelected is not XRGrabInteractable)
                    { 
                        TryReleaseObject(context);
                    }
                    return;
                }

                TryReleaseObject(context);
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

        private void StartThrowing(InputAction.CallbackContext context)
        {
            if (_isPreparingThrow || !CanThrow)
            {
                return;
            }
            _isPreparingThrow = true;
            _throwCoroutine = StartCoroutine(Co_PrepareThrow((force) =>
            {
                ReleaseSelectedObject();
                _onThrow.Invoke(force);
                _onChargeThrow.Invoke(0f);
                _isPreparingThrow = false;
                _throwCoroutine = null;
            }));
        }

        private void StopThrowing(InputAction.CallbackContext context)
        {
            _isPreparingThrow = false;
        }

        private IEnumerator Co_PrepareThrow(Action<float> callback)
        {
            float elapsedTime = 0f;
            float force = 0f;
            while (_isPreparingThrow && elapsedTime < _throwingSettings._maxThrowChargeDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / _throwingSettings._maxThrowChargeDuration);
                _onChargeThrow.Invoke(normalizedTime);
                force = normalizedTime * _throwingSettings._maxThrowForce;
                yield return null;
            }
            callback.Invoke(force);
        }



        [Serializable]
        private struct ThrowingSettings
        {
            [SerializeField]
            internal float _throwDelay;

            [SerializeField]
            internal float _animSpeed;

            [SerializeField] 
            internal float _maxThrowChargeDuration;

            [SerializeField]
            internal float _maxThrowForce;
        }

        #region Singleton
        private static PCInteractionSystem s_instance;
        public static PCInteractionSystem Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindAnyObjectByType<PCInteractionSystem>();
                    if (s_instance == null)
                    {
                        Debug.LogError("No PCInteractionSystem instance found in the scene.");
                    }
                }
                return s_instance;
            }
        }
        #endregion
    }
}
