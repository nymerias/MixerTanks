using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Complete
{
    public class TankShooting : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_PlayerNumber")]
        public int _playerNumber = 1;              // Used to identify the different players.
        [FormerlySerializedAsAttribute("m_Shell")]
        public Rigidbody _shell;                   // Prefab of the shell.
        [FormerlySerializedAsAttribute("m_FireTransform")]
        public Transform _fireTransform;           // A child of the tank where the shells are spawned.
        [FormerlySerializedAsAttribute("m_AimSlider")]
        public Slider _aimSlider;                  // A child of the tank that displays the current launch force.
        [FormerlySerializedAsAttribute("m_ShootingAudio")]
        public AudioSource _shootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        [FormerlySerializedAsAttribute("m_ChargingClip")]
        public AudioClip _chargingClip;            // Audio that plays when each shot is charging up.
        [FormerlySerializedAsAttribute("m_FireClip")]
        public AudioClip _fireClip;                // Audio that plays when each shot is fired.
        [FormerlySerializedAsAttribute("m_MinLaunchForce")]
        public float _minLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
        [FormerlySerializedAsAttribute("m_MaxLaunchForce")]
        public float _maxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
        [FormerlySerializedAsAttribute("m_MaxChargeTime")]
        public float _maxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.


        [FormerlySerializedAsAttribute("m_FireButton")]
        private string _fireButton;                // The input axis that is used for launching shells.
        [FormerlySerializedAsAttribute("m_CurrentLaunchForce")]
        private float _currentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        [FormerlySerializedAsAttribute("m_ChargeSpeed")]
        private float _chargeSpeed;                // How fast the launch force increases, based on the max charge time.
        [FormerlySerializedAsAttribute("m_Fired")]
        private bool _fired;                       // Whether or not the shell has been launched with this button press.

        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force and the UI
            _currentLaunchForce = _minLaunchForce;
            _aimSlider.value = _minLaunchForce;
        }

        private void Start()
        {
            // The fire axis is based on the player number.
            _fireButton = "Fire" + _playerNumber;

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            _chargeSpeed = (_maxLaunchForce - _minLaunchForce) / _maxChargeTime;
        }

        private void Update()
        {
            // The slider should have a default value of the minimum launch force.
            _aimSlider.value = _minLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (_currentLaunchForce >= _maxLaunchForce && !_fired)
            {
                // ... use the max force and launch the shell.
                _currentLaunchForce = _maxLaunchForce;
                Fire();
            }
            // Otherwise, if the fire button has just started being pressed...
            else if (Input.GetButtonDown(_fireButton))
            {
                // ... reset the fired flag and reset the launch force.
                _fired = false;
                _currentLaunchForce = _minLaunchForce;

                // Change the clip to the charging clip and start it playing.
                _shootingAudio.clip = _chargingClip;
                _shootingAudio.Play();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (Input.GetButton(_fireButton) && !_fired)
            {
                // Increment the launch force and update the slider.
                _currentLaunchForce += _chargeSpeed * Time.deltaTime;

                _aimSlider.value = _currentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (Input.GetButtonUp(_fireButton) && !_fired)
            {
                // ... launch the shell.
                Fire();
            }
        }

        private void Fire()
        {
            // Set the fired flag so only Fire is only called once.
            _fired = true;

            // Create an instance of the shell and store a reference to it's rigidbody.
            Rigidbody shellInstance =
                Instantiate(_shell, _fireTransform.position, _fireTransform.rotation) as Rigidbody;

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.velocity = _currentLaunchForce * _fireTransform.forward;

            // Change the clip to the firing clip and play it.
            _shootingAudio.clip = _fireClip;
            _shootingAudio.Play();

            // Reset the launch force.  This is a precaution in case of missing button events.
            _currentLaunchForce = _minLaunchForce;
        }
    }
}