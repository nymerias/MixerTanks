using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Microsoft.Mixer;

namespace Complete
{
    public class TankShooting : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_PlayerNumber")]
        public int _playerNumber = 1;
        [FormerlySerializedAsAttribute("m_Shell")]
        public Rigidbody _shell;
        [FormerlySerializedAsAttribute("m_FireTransform")]
        public Transform _fireTransform;
        [FormerlySerializedAsAttribute("m_AimSlider")]
        public Slider _aimSlider;
        [FormerlySerializedAsAttribute("m_ShootingAudio")]
        public AudioSource _shootingAudio;
        [FormerlySerializedAsAttribute("m_ChargingClip")]
        public AudioClip _chargingClip;
        [FormerlySerializedAsAttribute("m_FireClip")]
        public AudioClip _fireClip;
        [FormerlySerializedAsAttribute("m_MinLaunchForce")]
        public float _minLaunchForce = 15f;
        [FormerlySerializedAsAttribute("m_MaxLaunchForce")]
        public float _maxLaunchForce = 30f;
        [FormerlySerializedAsAttribute("m_MaxChargeTime")]
        public float _maxChargeTime = 0.75f;

        [FormerlySerializedAsAttribute("m_FireButton")]
        private string _fireButton;                // The input axis that is used for launching shells.
        [FormerlySerializedAsAttribute("m_CurrentLaunchForce")]
        private float _currentLaunchForce;
        [FormerlySerializedAsAttribute("m_ChargeSpeed")]
        private float _chargeSpeed;
        [FormerlySerializedAsAttribute("m_Fired")]
        private bool _fired = true;

        public uint participantId;

        private void OnEnable()
        {
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
            if (MixerInteractive.Participants.Count == 0)
            {
                Debug.Log("No participants yet, firing not allowed");
                return;
            }

            _aimSlider.value = _minLaunchForce;

            if (_currentLaunchForce >= _maxLaunchForce && !_fired)
            {
                _currentLaunchForce = _maxLaunchForce;
                Fire();
            }
            else if (InteractivityManager.SingletonInstance.GetButton("fire").GetButtonDown(participantId))
            {
                _fired = false;
                _currentLaunchForce = _minLaunchForce;

                _shootingAudio.clip = _chargingClip;
                _shootingAudio.Play();
            }
            else if (!InteractivityManager.SingletonInstance.GetButton("fire").GetButtonUp(participantId) && !_fired)
            {
                _currentLaunchForce += _chargeSpeed * Time.deltaTime;
                _aimSlider.value = _currentLaunchForce;
            }
            else if (InteractivityManager.SingletonInstance.GetButton("fire").GetButtonUp(participantId) && !_fired)
            {
                Fire();
            }
        }

        private void Fire()
        {
            _fired = true;

            Rigidbody shellInstance =Instantiate(_shell, _fireTransform.position, _fireTransform.rotation) as Rigidbody;
            shellInstance.velocity = _currentLaunchForce * _fireTransform.forward;

            _shootingAudio.clip = _fireClip;
            _shootingAudio.Play();

            _currentLaunchForce = _minLaunchForce;
        }
    }
}