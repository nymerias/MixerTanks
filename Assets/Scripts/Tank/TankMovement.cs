using UnityEngine;
using UnityEngine.Serialization;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_PlayerNumber")]
        public int _playerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        [FormerlySerializedAsAttribute("m_Speed")]
        public float _speed = 12f;                 // How fast the tank moves forward and back.
        [FormerlySerializedAsAttribute("m_TurnSpeed")]
        public float _turnSpeed = 180f;            // How fast the tank turns in degrees per second.
        [FormerlySerializedAsAttribute("m_MovementAudio")]
        public AudioSource _movementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        [FormerlySerializedAsAttribute("m_EngineIdling")]
        public AudioClip _engineIdling;            // Audio to play when the tank isn't moving.
        [FormerlySerializedAsAttribute("m_EngineDriving")]
        public AudioClip _engineDriving;           // Audio to play when the tank is moving.
        [FormerlySerializedAsAttribute("m_PitchRange")]
        public float _pitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

        [FormerlySerializedAsAttribute("m_MovementAxisName")]
        private string _movementAxisName;          // The name of the input axis for moving forward and back.
        [FormerlySerializedAsAttribute("m_TurnAxisName")]
        private string _turnAxisName;              // The name of the input axis for turning.
        [FormerlySerializedAsAttribute("m_Rigidbody")]
        private Rigidbody _rigidbody;              // Reference used to move the tank.
        [FormerlySerializedAsAttribute("m_MovementInputValue")]
        private float _movementInputValue;         // The current value of the movement input.
        [FormerlySerializedAsAttribute("m_TurnInputValue")]
        private float _turnInputValue;             // The current value of the turn input.
        [FormerlySerializedAsAttribute("m_OriginalPitch")]
        private float _originalPitch;              // The pitch of the audio source at the start of the scene.
        [FormerlySerializedAsAttribute("m_particleSystems")]
        private ParticleSystem[] _particleSystems; // References to all the particles systems used by the Tanks

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            // When the tank is turned on, make sure it's not kinematic.
            _rigidbody.isKinematic = false;

            // Also reset the input values.
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
            // When the tank is turned off, set it to kinematic so it stops moving.
            _rigidbody.isKinematic = true;

            // Stop all particle system so it "reset" it's position to the actual one instead of thinking we moved when spawning
            for (int i = 0; i < _particleSystems.Length; ++i)
            {
                _particleSystems[i].Stop();
            }
        }

        private void Start()
        {
            // The axes names are based on player number.
            _movementAxisName = "Vertical" + _playerNumber;
            _turnAxisName = "Horizontal" + _playerNumber;

            // Store the original pitch of the audio source.
            _originalPitch = _movementAudio.pitch;
        }

        private void Update()
        {
            // Store the value of both input axes.
            _movementInputValue = Input.GetAxis(_movementAxisName);
            _turnInputValue = Input.GetAxis(_turnAxisName);

            EngineAudio();
        }

        private void EngineAudio()
        {
            // If there is no input (the tank is stationary)...
            if (Mathf.Abs(_movementInputValue) < 0.1f && Mathf.Abs(_turnInputValue) < 0.1f)
            {
                // ... and if the audio source is currently playing the driving clip...
                if (_movementAudio.clip == _engineDriving)
                {
                    // ... change the clip to idling and play it.
                    _movementAudio.clip = _engineIdling;
                    _movementAudio.pitch = Random.Range(_originalPitch - _pitchRange, _originalPitch + _pitchRange);
                    _movementAudio.Play();
                }
            }
            else
            {
                // Otherwise if the tank is moving and if the idling clip is currently playing...
                if (_movementAudio.clip == _engineIdling)
                {
                    // ... change the clip to driving and play.
                    _movementAudio.clip = _engineDriving;
                    _movementAudio.pitch = Random.Range(_originalPitch - _pitchRange, _originalPitch + _pitchRange);
                    _movementAudio.Play();
                }
            }
        }

        private void FixedUpdate()
        {
            // Adjust the rigidbodies position and orientation in FixedUpdate.
            Move();
            Turn();
        }

        private void Move()
        {
            // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
            Vector3 movement = transform.forward * _movementInputValue * _speed * Time.deltaTime;

            // Apply this movement to the rigidbody's position.
            _rigidbody.MovePosition(_rigidbody.position + movement);
        }

        private void Turn()
        {
            // Determine the number of degrees to be turned based on the input, speed and time between frames.
            float turn = _turnInputValue * _turnSpeed * Time.deltaTime;

            // Make this into a rotation in the y axis.
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

            // Apply this rotation to the rigidbody's rotation.
            _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
        }
    }
}