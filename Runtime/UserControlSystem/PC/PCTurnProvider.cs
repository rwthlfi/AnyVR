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
        public float PitchThreshold
        {
            get => _pitchThreshhold;
            set => _pitchThreshhold = value;
        }

        [SerializeField]
        [Tooltip(
            "The Input System Action that will be used to read Turn data from the mouse. Must be a Value Vector2 Control.")]
        private InputActionProperty _turnAction = new(new InputAction("Turn", expectedControlType: "Vector2"));

        // Properties
        public float TurnSpeed 
        {
            get => _turnSpeed;
            set => _turnSpeed = value;
        }

        private void Update()
        {
            Turn(_turnAction.action.ReadValue<Vector2>());
        }

        private void Turn(Vector2 rotation)
        {
            Vector2 _turnRotation = _turnOrigin.eulerAngles;
            if (rotation.sqrMagnitude < 0.01)
            {
                return;
            }

            float scaledRotateSpeed = TurnSpeed * Time.deltaTime;
            _turnRotation.y += rotation.x * scaledRotateSpeed;
            float inDegrees = _turnRotation.x - (rotation.y * scaledRotateSpeed);
            if (inDegrees > 180f)
            {
                inDegrees = 360f - inDegrees;
            }
            else
            {
                inDegrees = -inDegrees;
            }
            inDegrees = Mathf.Clamp(inDegrees, -_pitchThreshhold, _pitchThreshhold);
            if (inDegrees > 0f)
            {
                inDegrees = 360f - inDegrees;
            }
            else
            {
                inDegrees = -inDegrees;
            }
            _turnRotation.x = inDegrees;


            _turnOrigin.localEulerAngles = _turnRotation;
        }
    }
}