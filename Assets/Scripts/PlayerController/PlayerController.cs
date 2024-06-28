using FishNet.Connection;
using FishNet.Object;
using LobbySystem;
using TMPro;
using UnityEngine;

namespace AnyVR.PlayerController
{
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private float _walkingSpeed = 7.5f;
        [SerializeField] private float _runningSpeed = 10.5f;
        [SerializeField] private float _jumpSpeed = 8.0f;
        [SerializeField] private float _gravity = 20.0f;
        [SerializeField] private float _lookSpeed = 2.0f;
        [SerializeField] private float _lookXLimit = 45.0f;

        [HideInInspector] public bool _canMove = true;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private Transform _ownerObjects;
        [SerializeField] private TextMeshProUGUI _userNameLabel;

        private CharacterController _characterController;
        private Vector3 _moveDirection = Vector3.zero;
        private float _rotationX;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            // We are grounded, so recalculate move direction based on axes
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            // Press Left Shift to run
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float curSpeedX = _canMove ? (isRunning ? _runningSpeed : _walkingSpeed) * Input.GetAxis("Vertical") : 0;
            float curSpeedY = _canMove ? (isRunning ? _runningSpeed : _walkingSpeed) * Input.GetAxis("Horizontal") : 0;
            float movementDirectionY = _moveDirection.y;
            _moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            if (Input.GetButton("Jump") && _canMove && _characterController.isGrounded)
            {
                _moveDirection.y = _jumpSpeed;
            }
            else
            {
                _moveDirection.y = movementDirectionY;
            }

            // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
            // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
            // as an acceleration (ms^-2)
            if (!_characterController.isGrounded)
            {
                _moveDirection.y -= _gravity * Time.deltaTime;
            }

            // Move the controller
            _characterController.Move(_moveDirection * Time.deltaTime);

            // Player and Camera rotation
            if (!_canMove)
            {
                return;
            }

            _rotationX += -Input.GetAxis("Mouse Y") * _lookSpeed;
            _rotationX = Mathf.Clamp(_rotationX, -_lookXLimit, _lookXLimit);
            _playerCamera.transform.localRotation = Quaternion.Euler(_rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * _lookSpeed, 0);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
            {
                _playerCamera.gameObject.SetActive(true);
            }

            _userNameLabel.text = PlayerNameTracker.GetPlayerName(Owner);
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            _ownerObjects.gameObject.SetActive(IsOwner);
        }
    }
}