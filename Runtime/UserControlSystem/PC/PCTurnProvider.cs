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

namespace AnyVR.UserControlSystem
{
    /// <summary>
    ///     Provides the functionality to turn the camera using mouse input on PC.
    /// </summary>
    public class PCTurnProvider : MonoBehaviour
    {
        // Private fields
        [SerializeField] private Transform _turnOrigin;

        [SerializeField] [Tooltip("The speed at which the camera turns.")] [Range(0f, 100f)]
        private float _turnSpeed = 30f;

        [SerializeField] [Tooltip("Max/min degrees of pitching the camera")] [Range(-89f, 89f)]
        private float _pitchThreshhold = 60f;

        [SerializeField]
        [Tooltip(
            "The Input System Action that will be used to read Turn data from the mouse. Must be a Value Vector2 Control.")]
        private InputActionProperty _turnAction = new(new InputAction("Turn", expectedControlType: "Vector2"));

        private Vector2 _turnRotation;

        // Properties
        public float TurnSpeed => _turnSpeed;

        private void Update()
        {
            Turn(_turnAction.action.ReadValue<Vector2>());
        }

        private void Turn(Vector2 rotation)
        {
            if (rotation.sqrMagnitude < 0.01)
            {
                return;
            }

            float scaledRotateSpeed = TurnSpeed * Time.deltaTime;
            _turnRotation.y += rotation.x * scaledRotateSpeed;
            _turnRotation.x = Mathf.Clamp(_turnRotation.x - (rotation.y * scaledRotateSpeed), -_pitchThreshhold,
                _pitchThreshhold);
            _turnOrigin.localEulerAngles = _turnRotation;
        }
    }
}