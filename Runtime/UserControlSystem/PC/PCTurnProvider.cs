using UnityEngine;
using UnityEngine.InputSystem;

namespace AnyVR.UserControlSystem.PC
{
    /// <summary>
    ///     Provides the functionality to turn the camera using mouse input on PC.
    /// </summary>
    public class PCTurnProvider : MonoBehaviour
    {
        [SerializeField] private Transform _turnOrigin;

        [SerializeField] [Tooltip("The speed at which the camera turns.")] [Range(0f, 100f)]
        private float _turnSpeed = 30f;

        [SerializeField] [Tooltip("Max/min degrees of pitching the camera")] [Range(-89f, 89f)]
        private float _pitchThreshhold = 60f;

        [SerializeField]
        [Tooltip(
            "The Input System Action that will be used to read Turn data from the mouse. Must be a Value Vector2 Control.")]
        private InputActionProperty _turnAction = new(new InputAction("Turn", expectedControlType: "Vector2"));
        public float PitchThreshold
        {
            get => _pitchThreshhold;
            set => _pitchThreshhold = value;
        }

        public float TurnSpeed
        {
            get => _turnSpeed;
            set => _turnSpeed = value;
        }

        private void Update()
        {
            Turn(_turnAction.action.ReadValue<Vector2>());
        }

        private void ApplyTurnConstraints()
        {
            float x = _turnOrigin.localEulerAngles.x;
            if (x > 180f)
            {
                x -= 360f;
            }

            x = Mathf.Clamp(x, -_pitchThreshhold, _pitchThreshhold);

            _turnOrigin.localEulerAngles = new Vector3(x, _turnOrigin.localEulerAngles.y, 0f);
        }

        private void Turn(Vector2 rotation)
        {
            if (rotation.sqrMagnitude < 0.01) { return; }

            float scale = TurnSpeed * Time.deltaTime;
            _turnOrigin.Rotate(Vector3.up, rotation.x * scale, Space.World);
            _turnOrigin.Rotate(Vector3.right, -rotation.y * scale, Space.Self);
            
            ApplyTurnConstraints();
        }
    }
}
