using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;
using Microsoft.Mixer;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_PlayerNumber")]
        public int _playerNumber = 1;
        [FormerlySerializedAsAttribute("m_Speed")]
        public float _speed = 12f;
        [FormerlySerializedAsAttribute("m_TurnSpeed")]
        public float _turnSpeed = 180f;
        [FormerlySerializedAsAttribute("m_MovementAudio")]
        public AudioSource _movementAudio;
        [FormerlySerializedAsAttribute("m_EngineIdling")]
        public AudioClip _engineIdling;
        [FormerlySerializedAsAttribute("m_EngineDriving")]
        public AudioClip _engineDriving;
        [FormerlySerializedAsAttribute("m_PitchRange")]
        public float _pitchRange = 0.2f;
        [HideInInspector]
        public uint participantId;

        [FormerlySerializedAsAttribute("m_MovementAxisName")]
        private string _movementAxisName;
        [FormerlySerializedAsAttribute("m_TurnAxisName")]
        private string _turnAxisName;
        [FormerlySerializedAsAttribute("m_Rigidbody")]
        private Rigidbody _rigidbody;              // Reference used to move the tank.
        [FormerlySerializedAsAttribute("m_MovementInputValue")]
        private float _movementInputValue;
        [FormerlySerializedAsAttribute("m_TurnInputValue")]
        private float _turnInputValue;
        [FormerlySerializedAsAttribute("m_OriginalPitch")]
        private float _originalPitch;
        [FormerlySerializedAsAttribute("m_particleSystems")]
        private ParticleSystem[] _particleSystems;
        [HideInInspector]
        private InteractivityManager interactivityManager;

        private TankDirection tankDirection;

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        void OnEnable()
        {
            _rigidbody.isKinematic = false;

            _movementInputValue = 0f;
            _turnInputValue = 0f;

            // We grab all the Particle systems child of that Tank to be able to Stop/Play them on Deactivate/Activate
            // It is needed because we move the Tank when spawning it, and if the Particle System is playing while we do that
            // it "think" it move from (0,0,0) to the spawn point, creating a huge trail of smoke
            _particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < _particleSystems.Length; ++i)
            {
                _particleSystems[i].Play();
            }
        }

        private void OnDisable()
        {
            _rigidbody.isKinematic = true;
            _particleSystems.ToList().ForEach(x => x.Stop());
        }

        void Start()
        {
            // The axes names are based on player number.
            _movementAxisName = "Vertical" + _playerNumber;
            _turnAxisName = "Horizontal" + _playerNumber;

            // Store the original pitch of the audio source.
            _originalPitch = _movementAudio.pitch;

            //Instantiate InteractivityManager
            interactivityManager = InteractivityManager.SingletonInstance;

            tankDirection = new TankDirection(participantId);
        }

        void Update()
        {
            _movementInputValue = Input.GetAxis(_movementAxisName);
            _turnInputValue = Input.GetAxis(_turnAxisName);

            EngineAudio();
        }

        void EngineAudio()
        {
            // If there is no input the tank is stationary
            if (Mathf.Abs(_movementInputValue) < 0.1f && Mathf.Abs(_turnInputValue) < 0.1f)
            {
                // And if the audio source is currently playing the driving clip
                if (_movementAudio.clip == _engineDriving)
                {
                    _movementAudio.clip = _engineIdling;
                    _movementAudio.pitch = Random.Range(_originalPitch - _pitchRange, _originalPitch + _pitchRange);
                    _movementAudio.Play();
                }
            }
            else if (_movementAudio.clip == _engineIdling)
            {
                // Change the clip to driving and play.
                _movementAudio.clip = _engineDriving;
                _movementAudio.pitch = Random.Range(_originalPitch - _pitchRange, _originalPitch + _pitchRange);
                _movementAudio.Play();
            }
        }

        void FixedUpdate()
        {
            Move();
            Turn();
        }

        /// <summary>
        /// Move the tank by moving the rigidbody
        /// </summary>
        private void Move()
        {
            // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
            _movementInputValue = tankDirection.VerticalDirection();

            Vector3 movement = transform.forward * _movementInputValue * _speed * Time.deltaTime;
            _rigidbody.MovePosition(_rigidbody.position + movement);
        }

        /// <summary>
        /// Rotate the tank by rotating the rigid body
        /// </summary>
        private void Turn()
        {
            _turnInputValue = tankDirection.HorizontalDirection();

            float turn = _turnInputValue * _turnSpeed * Time.deltaTime;

            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
        }
    }

    public class TankDirection
    {
        private InteractiveButtonControl forwardButton;
        private InteractiveButtonControl backButton;
        private InteractiveButtonControl leftButton;
        private InteractiveButtonControl rightButton;

        private bool movingForward;
        private bool movingBackward;
        private bool movingLeft;
        private bool movingRight;

        private uint userId;

        public TankDirection(uint userId)
        {
            this.userId = userId;
            forwardButton = InteractivityManager.SingletonInstance.GetButton("forward");
            backButton = InteractivityManager.SingletonInstance.GetButton("back");
            leftButton = InteractivityManager.SingletonInstance.GetButton("left");
            rightButton = InteractivityManager.SingletonInstance.GetButton("right");
        }

        public float VerticalDirection()
        {
            if (forwardButton.GetButtonDown(userId))
            {
                movingForward = true;
                movingBackward = false;
            }
            else if (backButton.GetButtonDown(userId))
            {
                movingForward = false;
                movingBackward = true;
            }

            if (forwardButton.GetButtonUp(userId))
            {
                movingForward = false;
            }
            else if (backButton.GetButtonUp(userId))
            {
                movingBackward = false;
            }

            if (movingForward)
            {
                return 1;
            }

            if (movingBackward)
            {
                return -1;
            }

            return 0;
        }

        public float HorizontalDirection()
        {
            if (leftButton.GetButtonDown(userId))
            {
                movingLeft = true;
                movingRight = false;
            }

            if (rightButton.GetButtonDown(userId))
            {
                movingLeft = false;
                movingRight = true;
            }

            if (leftButton.GetButtonUp(userId))
            {
                movingLeft = false;
            }

            if (rightButton.GetButtonUp(userId))
            {
                movingRight = false;
            }

            if (movingRight)
            {
                return 1;
            }

            if (movingLeft)
            {
                return -1;
            }

            return 0;
        }
    }
}