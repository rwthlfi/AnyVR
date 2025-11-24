// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2025 Engineering Hydrology, RWTH Aachen University.
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

using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AnyVR.UserControlSystem.Interaction
{
    /// <summary>
    ///     Synchronizes selected XR Interaction Toolkit interaction events over the network.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class XRSynchronizedInteractions : NetworkBehaviour
    {
        [SerializeField] [Tooltip("Check to synchronize first/last hover events over the network.")]
        private bool _synchronizeFirstLastHoverEvents;

        [SerializeField] [Tooltip("Check to synchronize hover events over the network.")]
        private bool _synchronizeHoverEvents;

        [SerializeField] [Tooltip("Check to synchronize first/last select events over the network.")]
        private bool _synchronizeFirstLastSelectEvents;

        [SerializeField] [Tooltip("Check to synchronize select events over the network.")]
        private bool _synchronizeSelectEvents;

        [SerializeField] [Tooltip("Check to synchronize first/last focus events over the network.")]
        private bool _synchronizeFirstLastFocusEvents;

        [SerializeField] [Tooltip("Check to synchronize focus events over the network.")]
        private bool _synchronizeFocusEvents;

        [SerializeField] [Tooltip("Check to synchronize activate events over the network.")]
        private bool _synchronizeActivateEvents;

        private XRBaseInteractable _interactable;
        private PCSecondarySelectionAction _pcSecondarySelectionAction;
        private bool _suppressSynchronization;

        private void Start()
        {
            _interactable = GetComponent<XRBaseInteractable>();
            TryGetComponent(out _pcSecondarySelectionAction);

            SubscribeFirstLastHoverSynchronization();
            SubscribeHoverSynchronization();
            SubscribeFirstLastSelectSynchronization();
            SubscribeSelectSynchronization();
            SubscribeFirstLastFocusSynchronization();
            SubscribeFocusSynchronization();
            SubscribeActivateSynchronization();
            SubscribeActivateSecondarySynchronization();
        }

        #region First/Last Hover

        private void SubscribeFirstLastHoverSynchronization()
        {
            if (_synchronizeFirstLastHoverEvents)
            {
                _interactable.firstHoverEntered.AddListener(OnFirstHoverEntered);
                _interactable.lastHoverExited.AddListener(OnLastHoverExited);
            }
        }

        private void OnFirstHoverEntered(HoverEnterEventArgs hoverEnterEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeFirstHoverEntered();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeFirstHoverEntered(NetworkConnection caller = null)
        {
            BroadCastFirstHoverEntered(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastFirstHoverEntered(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.firstHoverEntered.Invoke(new HoverEnterEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        private void OnLastHoverExited(HoverExitEventArgs hoverExitEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeLastHoverExited();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeLastHoverExited(NetworkConnection caller = null)
        {
            BroadCastLastHoverExited(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastLastHoverExited(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.lastHoverExited.Invoke(new HoverExitEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        #endregion

        #region Hover

        private void SubscribeHoverSynchronization()
        {
            if (_synchronizeHoverEvents)
            {
                _interactable.hoverEntered.AddListener(OnHoverEntered);
                _interactable.hoverExited.AddListener(OnHoverExited);
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs hoverEnterEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeHoverEntered();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeHoverEntered(NetworkConnection caller = null)
        {
            BroadCastHoverEntered(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastHoverEntered(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.hoverEntered.Invoke(new HoverEnterEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        private void OnHoverExited(HoverExitEventArgs hoverExitEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeHoverExited();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeHoverExited(NetworkConnection caller = null)
        {
            BroadCastHoverExited(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastHoverExited(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.hoverExited.Invoke(new HoverExitEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        #endregion

        #region First/Last Select

        private void SubscribeFirstLastSelectSynchronization()
        {
            if (_synchronizeFirstLastSelectEvents)
            {
                _interactable.firstSelectEntered.AddListener(OnFirstSelectEntered);
                _interactable.lastSelectExited.AddListener(OnLastSelectExited);
            }
        }

        private void OnFirstSelectEntered(SelectEnterEventArgs selectEnterEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeFirstSelectEntered();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeFirstSelectEntered(NetworkConnection caller = null)
        {
            BroadCastFirstSelectEntered(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastFirstSelectEntered(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.firstSelectEntered.Invoke(new SelectEnterEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        private void OnLastSelectExited(SelectExitEventArgs selectExitEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeLastSelectExited();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeLastSelectExited(NetworkConnection caller = null)
        {
            BroadCastLastSelectExited(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastLastSelectExited(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.lastSelectExited.Invoke(new SelectExitEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        #endregion

        #region Select

        private void SubscribeSelectSynchronization()
        {
            if (_synchronizeSelectEvents)
            {
                _interactable.selectEntered.AddListener(OnSelectEntered);
                _interactable.selectExited.AddListener(OnSelectExited);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs selectEnterEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeSelectEntered();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeSelectEntered(NetworkConnection caller = null)
        {
            BroadCastSelectEntered(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastSelectEntered(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.selectEntered.Invoke(new SelectEnterEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        private void OnSelectExited(SelectExitEventArgs selectExitEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeSelectExited();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeSelectExited(NetworkConnection caller = null)
        {
            BroadCastSelectExited(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastSelectExited(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.selectExited.Invoke(new SelectExitEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        #endregion

        #region First/Last Focus

        private void SubscribeFirstLastFocusSynchronization()
        {
            if (_synchronizeFirstLastFocusEvents)
            {
                _interactable.firstFocusEntered.AddListener(OnFirstFocusEntered);
                _interactable.lastFocusExited.AddListener(OnLastFocusExited);
            }
        }

        private void OnFirstFocusEntered(FocusEnterEventArgs focusEnterEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeFirstFocusEntered();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeFirstFocusEntered(NetworkConnection caller = null)
        {
            BroadCastFirstFocusEntered(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastFirstFocusEntered(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.firstFocusEntered.Invoke(new FocusEnterEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        private void OnLastFocusExited(FocusExitEventArgs focusExitEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeLastFocusExited();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeLastFocusExited(NetworkConnection caller = null)
        {
            BroadCastLastFocusExited(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastLastFocusExited(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.lastFocusExited.Invoke(new FocusExitEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        #endregion

        #region Focus

        private void SubscribeFocusSynchronization()
        {
            if (_synchronizeFocusEvents)
            {
                _interactable.focusEntered.AddListener(OnFocusEntered);
                _interactable.focusExited.AddListener(OnFocusExited);
            }
        }

        private void OnFocusEntered(FocusEnterEventArgs focusEnterEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeFocusEntered();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeFocusEntered(NetworkConnection caller = null)
        {
            BroadCastFocusEntered(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastFocusEntered(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.focusEntered.Invoke(new FocusEnterEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        private void OnFocusExited(FocusExitEventArgs focusExitEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeFocusExited();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeFocusExited(NetworkConnection caller = null)
        {
            BroadCastFocusExited(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastFocusExited(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.focusExited.Invoke(new FocusExitEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        #endregion

        #region Activate (Primary Action)

        private void SubscribeActivateSynchronization()
        {
            if (_synchronizeActivateEvents)
            {
                _interactable.activated.AddListener(OnActivated);
                _interactable.deactivated.AddListener(OnDeactivated);
            }
        }

        private void OnActivated(ActivateEventArgs activateEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeActivated();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeActivated(NetworkConnection caller = null)
        {
            BroadCastActivated(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastActivated(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.activated.Invoke(new ActivateEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        private void OnDeactivated(DeactivateEventArgs deactivateEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeDeactivated();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeDeactivated(NetworkConnection caller = null)
        {
            BroadCastDeactivated(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastDeactivated(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _interactable.deactivated.Invoke(new DeactivateEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        #endregion

        #region Activate (Secondary Action)

        private void SubscribeActivateSecondarySynchronization()
        {
            if (_synchronizeActivateEvents && _pcSecondarySelectionAction != null)
            {
                _pcSecondarySelectionAction.SecondarySelectActivated.AddListener(OnActivatedSecondary);
                _pcSecondarySelectionAction.SecondarySelectDeactivated.AddListener(OnDeactivatedSecondary);
            }
        }

        private void OnActivatedSecondary(ActivateEventArgs activateEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeActivatedSecondary();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeActivatedSecondary(NetworkConnection caller = null)
        {
            BroadCastActivatedSecondary(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastActivatedSecondary(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _pcSecondarySelectionAction.SecondarySelectActivated.Invoke(new ActivateEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        private void OnDeactivatedSecondary(DeactivateEventArgs deactivateEventArgs)
        {
            if (!_suppressSynchronization)
            {
                SynchronizeDeactivatedSecondary();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SynchronizeDeactivatedSecondary(NetworkConnection caller = null)
        {
            BroadCastDeactivatedSecondary(caller);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void BroadCastDeactivatedSecondary(NetworkConnection caller = null)
        {
            if (caller == InstanceFinder.ClientManager.Connection)
            {
                return;
            }
            _suppressSynchronization = true;
            _pcSecondarySelectionAction.SecondarySelectDeactivated.Invoke(new DeactivateEventArgs
            {
                interactableObject = _interactable, interactorObject = null
            });
            _suppressSynchronization = false;
        }

        #endregion
    }
}
