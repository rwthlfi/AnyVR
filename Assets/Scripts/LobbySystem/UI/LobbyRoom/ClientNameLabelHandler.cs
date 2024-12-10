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

using TMPro;
using UnityEngine;

namespace LobbySystem.UI.LobbyRoom
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ClientNameLabelHandler : MonoBehaviour
    {
        [SerializeField] private OnlinePlayerHandler _onlinePlayer;

        private TextMeshProUGUI _nameLabel;

        private void Awake()
        {
            _nameLabel = GetComponent<TextMeshProUGUI>();
            _onlinePlayer.SetClientId += (id, isOwner) =>
            {
                _nameLabel.SetText(PlayerNameTracker.GetPlayerName(id));
                if (isOwner)
                {
                    transform.parent.gameObject.layer = LayerMask.NameToLayer("OwnerRenderLayer");
                }
            };
        }
    }
}