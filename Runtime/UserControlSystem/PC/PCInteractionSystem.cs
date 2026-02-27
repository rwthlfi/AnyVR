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
    /// Provides PC-specific interaction capabilities for the XR Interaction Toolkit.
    /// This class extends <see cref="XRBaseInteractor"/> to handle mouse and keyboard input for interacting with XR objects in a PC environment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The PCInteractionSystem supports different interaction behaviors based on object types:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Grabbable Objects (XRGrabInteractable):</b> Toggle-based selection with throw charging capabilities. First press grabs the object, second press held down charges a throw, quick second press releases the object.</description></item>
    /// <item><description><b>Non-Grabbable Objects:</b> Button-like behavior where interaction is maintained only while the selection button is pressed.</description></item>
    /// </list>
    /// <para>
    /// The system integrates with Unity's XR Interaction Toolkit by overriding key methods and properties while maintaining compatibility with existing XR components and events.
    /// </para>
    /// </remarks>
    /// <seealso cref="XRBaseInteractor"/>
    /// <seealso cref="IXRActivateInteractor"/>
    public class PCInteractionSystem : XRBaseInteractor, IXRActivateInteractor
    {
        [SerializeField]
        [Tooltip("Maximum range for interactable objects")]
        private float _maxInteractionDistance = 3f;

        [SerializeField]
        private Transform _interactionRaycastOrigin;

        [SerializeField]
        [Tooltip("The Input System Action that will be used to interaction an object. Expects a 'Button' action.")]
        private InputActionProperty _selectionAction =
            new(new InputAction("Interaction", expectedControlType: "Button"));

        [Header("Selection Interaction")]
        [SerializeField]
        [Tooltip("Primary interaction action, uses XRI Activated event.")]
        private InputActionProperty _primaryInteractionAction =
            new(new InputAction("Primary Interaction", expectedControlType: "Button"));

        [SerializeField]
        [Tooltip("Secondary interaction action, can be used to trigger secondary interactions that would otherwise be handled by VR affordances. " +
            "Needs a PCSecondarySelectionAction Component on the interactable.")]
        private InputActionProperty _secondaryInteractionAction =
            new(new InputAction("Secondary Interaction", expectedControlType: "Button"));

        [SerializeField]
        private ThrowingSettings _throwingSettings = new()
        {
            _throwDelay = 0.5f,
            _animSpeed = 1f,
            _maxThrowChargeDuration = 1.5f,
            _maxThrowForce = 10f
        };

        /// <summary>
        /// Gets the maximum distance at which objects can be interacted with.
        /// </summary>
        /// <value>
        /// The maximum interaction range in Unity units. Objects beyond this distance cannot be targeted for interaction.
        /// </value>
        public float MaxInteractionDistance => _maxInteractionDistance;

        /// <summary>
        /// Gets the world position from which interaction raycasts originate.
        /// </summary>
        /// <value>
        /// The world position of the interaction raycast origin transform. This is typically the camera or cursor position.
        /// </value>
        public Vector3 InteractionOrigin => _interactionRaycastOrigin.position;

        /// <summary>
        /// Gets the direction in which interaction raycasts are cast.
        /// </summary>
        /// <value>
        /// The forward direction of the interaction raycast origin transform. This determines the direction of object detection.
        /// </value>
        public Vector3 InteractionDirection => _interactionRaycastOrigin.forward;

        /// <summary>
        /// Gets the currently hovered interactable object, if any.
        /// </summary>
        /// <value>
        /// The first <see cref="IXRHoverInteractable"/> in the hover list, or <c>null</c> if no object is being hovered.
        /// </value>
        public IXRHoverInteractable HoveredObject => hasHover ? interactablesHovered[0] : null;

        /// <summary>
        /// Gets the currently selected interactable object, if any.
        /// </summary>
        /// <value>
        /// The first <see cref="IXRSelectInteractable"/> in the selection list, or <c>null</c> if no object is selected.
        /// </value>
        public IXRSelectInteractable SelectedObject => hasSelection ? interactablesSelected[0] : null;

        /// <summary>
        /// Event invoked when a throw action is performed.
        /// </summary>
        /// <value>
        /// A <see cref="UnityEvent{T}"/> that passes the throw force as a float parameter when a throw is executed.
        /// Subscribers can use this to apply physics forces or trigger throw-related effects.
        /// </value>
        public UnityEvent<float> OnThrow { get; } = new();

        /// <summary>
        /// Event invoked continuously while a throw is being charged.
        /// </summary>
        /// <value>
        /// A <see cref="UnityEvent{T}"/> that passes the current throw force percentage (0-1) as a float parameter.
        /// Can be used to update UI elements, animations, or visual feedback during throw charging.
        /// </value>
        public UnityEvent<float> OnChargeThrow { get; } = new();

        /// <summary>
        /// Gets a value indicating whether the currently selected object can be thrown.
        /// </summary>
        /// <value>
        /// <c>true</c> if the selected object is an <see cref="XRGrabInteractable"/>; otherwise, <c>false</c>.
        /// Only grabbable objects support the throw mechanic.
        /// </value>
        public bool CanThrow => firstInteractableSelected is XRGrabInteractable;

        [Header("Debug")]
        [SerializeField, Tooltip("Whether to log interaction events to the console.")]
        private bool _logInteractionStateChanges = false;
        [SerializeField, Tooltip("Whether the selection button is currently pressed.")]
        private bool _isSelectionButtonPressed = false;
        [SerializeField, Tooltip("Duration for which the selection button has been continuously pressed.")]
        private float _selectionButtonPressDuration = 0f;
        [SerializeField, Tooltip("Current throw force being charged.")]
        private float _currentThrowForce = 0f;
        [SerializeField] private GameObject _hoveredObject = null;
        [SerializeField] private GameObject _selectedObject = null;

        private Coroutine _throwCoroutine;
        private bool _isChargingThrow = false;
        private bool _isGrabbableObject = false;
        private bool _shouldAllowSelection = false;
        private bool _pendingSelectionRequest = false;

        #region Unity Lifecycle & XR Overrides

        protected override void Awake()
        {
            InitializeSingleton();
        }

        protected override void Start()
        {
            base.Start();
            SubscribeToInputActions();

            allowHover = true;

            EnableSelectionInteractions(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            hoverEntered.AddListener(OnHoverEntered);
            hoverExited.AddListener(OnHoverExited);
            selectEntered.AddListener(OnSelectEntered);
            selectExited.AddListener(OnSelectExited);
        }

        protected override void OnDisable()
        {
            hoverEntered.RemoveListener(OnHoverEntered);
            hoverExited.RemoveListener(OnHoverExited);
            selectEntered.RemoveListener(OnSelectEntered);
            selectExited.RemoveListener(OnSelectExited);

            StopThrowCharging();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            UnsubscribeFromInputActions();

            base.OnDestroy();
        }

        protected void Update()
        {
            // Handles pending selection requests after XRI processing.
            if (_pendingSelectionRequest && hasHover && !hasSelection)
            {
                _pendingSelectionRequest = false;
                _shouldAllowSelection = true;

                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] Activating selection for hovered object");
                }
            }
            // Resets the flag after a frame to prevent multiple selections.
            else if (_shouldAllowSelection && hasSelection)
            {
                _shouldAllowSelection = false;
                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] Resetting selection flag after successful selection");
                }
            }

            // Updates throw charging if active.
            if (_isChargingThrow && _isSelectionButtonPressed && hasSelection)
            {
                UpdateThrowCharging();
            }

            UpdateDebugInfo();
        }

        /// <summary>
        /// Gets a value indicating whether this interactor is currently active for selection.
        /// </summary>
        /// <value>
        /// <c>true</c> if the base interactor is active and either selection is pending or an object is already selected; otherwise, <c>false</c>.
        /// This property controls when the XR Interaction Manager allows this interactor to select objects.
        /// </value>
        public override bool isSelectActive => base.isSelectActive && (_shouldAllowSelection || hasSelection);

        /// <summary>
        /// Populates a list with interactable objects that this interactor can potentially interact with.
        /// </summary>
        /// <param name="targets">
        /// The list to populate with valid targets. This list is cleared before being populated.
        /// </param>
        /// <remarks>
        /// This method performs a raycast from the <see cref="InteractionOrigin"/> in the <see cref="InteractionDirection"/>
        /// to detect interactable objects within the <see cref="MaxInteractionDistance"/>. It checks both the hit object
        /// and its parent for interactable components.
        /// </remarks>
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if (_interactionRaycastOrigin == null)
            {
                return;
            }

            Ray ray = new(_interactionRaycastOrigin.position, _interactionRaycastOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxInteractionDistance))
            {
                if (_logInteractionStateChanges)
                {
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
                }

                if (TryFindValidObject(hit.collider.gameObject, out IXRInteractable interactable) ||
                    (hit.collider.transform.parent != null &&
                     TryFindValidObject(hit.collider.transform.parent.gameObject, out interactable)))
                {
                    targets.Add(interactable);
                }
            }
            else if (_logInteractionStateChanges)
            {
                Debug.DrawRay(ray.origin, ray.direction * _maxInteractionDistance, Color.red);
            }
        }

        #endregion

        #region XR Event Handlers

        protected new void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (_logInteractionStateChanges)
            {
                Debug.Log($"[PCInteractionSystem] Hover entered: {args.interactableObject.transform.name}");
            }
        }

        protected new void OnHoverExited(HoverExitEventArgs args)
        {
            if (_logInteractionStateChanges)
            {
                Debug.Log($"[PCInteractionSystem] Hover exited: {args.interactableObject.transform.name}");
            }
        }

        protected new void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_logInteractionStateChanges)
            {
                Debug.Log($"[PCInteractionSystem] Select entered: {args.interactableObject.transform.name}");
            }

            EnableSelectionInteractions(true);

            // Determines object type and behavior.
            _isGrabbableObject = args.interactableObject is XRGrabInteractable;

            if (_logInteractionStateChanges)
            {
                Debug.Log($"[PCInteractionSystem] Object is grabbable: {_isGrabbableObject}");
            }
        }

        protected new void OnSelectExited(SelectExitEventArgs args)
        {
            if (_logInteractionStateChanges)
            {
                Debug.Log($"[PCInteractionSystem] Select exited: {args.interactableObject.transform.name}");
            }

            EnableSelectionInteractions(false);
            StopThrowCharging();

            _selectionButtonPressDuration = 0f;
            _currentThrowForce = 0f;
            _isGrabbableObject = false;

            // Resets selection control.
            _shouldAllowSelection = false;
            _pendingSelectionRequest = false;
        }

        #endregion

        #region Input Handling

        private void SubscribeToInputActions()
        {
            _selectionAction.action.started += HandleSelectionPress;
            _selectionAction.action.canceled += HandleSelectionRelease;
            _primaryInteractionAction.action.started += StartPrimarySelectionAction;
            _primaryInteractionAction.action.canceled += CancelPrimarySelectionAction;
            _secondaryInteractionAction.action.started += StartSecondarySelectionAction;
            _secondaryInteractionAction.action.canceled += CancelSecondarySelectionAction;
        }

        private void UnsubscribeFromInputActions()
        {
            _selectionAction.action.started -= HandleSelectionPress;
            _selectionAction.action.canceled -= HandleSelectionRelease;
            _primaryInteractionAction.action.started -= StartPrimarySelectionAction;
            _primaryInteractionAction.action.canceled -= CancelPrimarySelectionAction;
            _secondaryInteractionAction.action.started -= StartSecondarySelectionAction;
            _secondaryInteractionAction.action.canceled -= CancelSecondarySelectionAction;
        }

        private void HandleSelectionPress(InputAction.CallbackContext _)
        {
            _isSelectionButtonPressed = true;
            _selectionButtonPressDuration = 0f;

            if (!hasSelection && hasHover)
            {
                // First press: Requests selection for next frame.
                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] First press - requesting selection.");
                }

                _pendingSelectionRequest = true;
            }
            else if (hasSelection && _isGrabbableObject && !_isChargingThrow)
            {
                // Second press on grabbable: Start potential throw charging.
                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] Second press on grabbable - monitoring for throw/release.");
                }

                StartThrowMonitoring();
            }
            else if (hasSelection && !_isGrabbableObject)
            {
                // Non-grabbable: Releases immediately (button behavior).
                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] Press on non-grabbable - releasing immediately.");
                }

                ReleaseSelection();
            }
        }

        private void HandleSelectionRelease(InputAction.CallbackContext _)
        {
            _isSelectionButtonPressed = false;

            if (_logInteractionStateChanges)
            {
                Debug.Log($"[PCInteractionSystem] Selection button released. IsChargingThrow: {_isChargingThrow}.");
            }

            if (_isChargingThrow)
            {
                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] Button released during throw charging - executing throw.");
                }

                ExecuteThrow();
            }
            else if (hasSelection && !_isGrabbableObject)
            {
                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] Button released on non-grabbable - releasing.");
                }

                ReleaseSelection();
            }
            else if (hasSelection && _isGrabbableObject && _selectionButtonPressDuration > 0 && _selectionButtonPressDuration < _throwingSettings._throwDelay)
            {
                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] Short press detected - releasing grabbable object.");
                }

                ReleaseSelection();
            }

            StopThrowCharging();
        }

        private void ReleaseSelection()
        {
            if (hasSelection)
            {
                _shouldAllowSelection = false;
                interactionManager.SelectExit(this, firstInteractableSelected);
            }
        }

        #endregion

        #region Throwing System

        private void StartThrowMonitoring()
        {
            if (!CanThrow || _isChargingThrow)
            {
                return;
            }

            _throwCoroutine = StartCoroutine(ThrowMonitoringCoroutine());
        }

        private IEnumerator ThrowMonitoringCoroutine()
        {
            while (_isSelectionButtonPressed && hasSelection)
            {
                _selectionButtonPressDuration += Time.deltaTime;

                if (_selectionButtonPressDuration >= _throwingSettings._throwDelay)
                {
                    if (_logInteractionStateChanges)
                    {
                        Debug.Log("[PCInteractionSystem] Throw delay reached - starting charge.");
                    }

                    _isChargingThrow = true;
                    break;
                }

                yield return null;
            }

            // If button was released before throw delay, it will be handled in HandleSelectionRelease.
        }

        private void UpdateThrowCharging()
        {
            _selectionButtonPressDuration += Time.deltaTime;

            float throwChargeDuration = _selectionButtonPressDuration - _throwingSettings._throwDelay;
            _currentThrowForce = Mathf.Lerp(0f, _throwingSettings._maxThrowForce,
                throwChargeDuration / _throwingSettings._maxThrowChargeDuration);

            OnChargeThrow.Invoke(_currentThrowForce);

            // Auto-throw at max charge.
            if (_selectionButtonPressDuration > _throwingSettings._throwDelay + _throwingSettings._maxThrowChargeDuration)
            {
                if (_logInteractionStateChanges)
                {
                    Debug.Log("[PCInteractionSystem] Max charge reached - auto-throwing");
                }

                ExecuteThrow();
            }
        }

        private void ExecuteThrow()
        {
            if (CanThrow && hasSelection)
            {
                if (_logInteractionStateChanges)
                {
                    Debug.Log($"[PCInteractionSystem] Executing throw with force: {_currentThrowForce}");
                }

                OnThrow.Invoke(_currentThrowForce);
                ReleaseSelection();
            }

            StopThrowCharging();
        }

        private void StopThrowCharging()
        {
            _isChargingThrow = false;
            _currentThrowForce = 0f;
            _selectionButtonPressDuration = 0f;

            if (_throwCoroutine != null)
            {
                StopCoroutine(_throwCoroutine);
                _throwCoroutine = null;
            }
        }

        #endregion

        #region Activation Handling

        /// <summary>
        /// Populates a list with activatable interactable objects that this interactor can currently activate.
        /// </summary>
        /// <param name="targets">
        /// The list to populate with activatable targets. This list is cleared before being populated.
        /// If the currently selected object implements <see cref="IXRActivateInteractable"/>, it will be added to this list.
        /// </param>
        /// <remarks>
        /// This method is called by the XR Interaction Manager to determine which objects can be activated by this interactor.
        /// Only the currently selected object (if any) can be activated.
        /// </remarks>
        public void GetActivateTargets(List<IXRActivateInteractable> targets)
        {
            targets.Clear();
            if (firstInteractableSelected is IXRActivateInteractable activateInteractable)
            {
                targets.Add(activateInteractable);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this interactor should activate the currently selected object.
        /// </summary>
        /// <value>
        /// <c>true</c> if an object is selected and it implements <see cref="IXRActivateInteractable"/>; otherwise, <c>false</c>.
        /// </value>
        public bool shouldActivate => hasSelection && firstInteractableSelected is IXRActivateInteractable;

        /// <summary>
        /// Gets a value indicating whether this interactor should deactivate the currently selected object.
        /// </summary>
        /// <value>
        /// <c>true</c> if an object is selected and it implements <see cref="IXRActivateInteractable"/>; otherwise, <c>false</c>.
        /// </value>
        public bool shouldDeactivate => hasSelection && firstInteractableSelected is IXRActivateInteractable;

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

        private void EnableSelectionInteractions(bool value)
        {
            if (value)
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

        #endregion

        #region Helper Methods

        private bool TryFindValidObject(GameObject go, out IXRInteractable interactable)
        {
            interactable = null;
            return go != null && go.TryGetComponent(out interactable);
        }

        private void UpdateDebugInfo()
        {
            if (Application.isEditor)
            {
                _hoveredObject = HoveredObject?.transform.gameObject;
                _selectedObject = SelectedObject?.transform.gameObject;
            }
        }

        #endregion

        #region Throwing Settings

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

        #endregion

        #region Singleton

        private static PCInteractionSystem s_instance;

        /// <summary>
        /// Gets the singleton instance of the PCInteractionSystem.
        /// </summary>
        /// <value>
        /// The active PCInteractionSystem instance in the scene, or <c>null</c> if none exists.
        /// </value>
        /// <remarks>
        /// This property provides global access to the PCInteractionSystem instance. If no instance is found,
        /// it will search for one in the scene and log an error if none is found.
        /// </remarks>
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

        private void InitializeSingleton()
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
        /// <summary>
        /// Determines if the Interactable is valid for selection this frame.
        /// Prevents multi-selection by only allowing selection when no objects are currently selected or when explicitly requested.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactable can be selected this frame.</returns>
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            // If we're already selecting this interactable, allow it to maintain selection
            if (IsSelecting(interactable))
            {
                return base.CanSelect(interactable);
            }

            // For new selections, only allow if we don't have a selection or if explicitly requested
            var canSelect = base.CanSelect(interactable) && (!hasSelection || _shouldAllowSelection);

            if (_logInteractionStateChanges && !canSelect && hasSelection)
            {
                Debug.Log($"[PCInteractionSystem] Blocking new selection of {interactable.transform.name} - already have selection of {firstInteractableSelected.transform.name}");
            }

            return canSelect;
        }

        #endregion
    }
}
