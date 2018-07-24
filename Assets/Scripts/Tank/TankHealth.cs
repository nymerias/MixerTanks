using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Complete
{
    public class TankHealth : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_StartingHealth")]
        public float _startingHealth = 100f;
        [FormerlySerializedAsAttribute("m_Slider")]
        public Slider _slider;                             // The slider to represent how much health the tank currently has.
        [FormerlySerializedAsAttribute("m_FillImage")]
        public Image _fillImage;                           // The image component of the slider.
        [FormerlySerializedAsAttribute("m_FullHealthColor")]
        public Color _fullHealthColor = Color.green;
        [FormerlySerializedAsAttribute("m_ZeroHealthColor")]
        public Color _zeroHealthColor = Color.red;
        [FormerlySerializedAsAttribute("m_ExplosionPrefab")]
        public GameObject _explosionPrefab;

        [FormerlySerializedAsAttribute("m_ExplosionAudio")]
        private AudioSource _explosionAudio;
        [FormerlySerializedAsAttribute("m_ExplosionParticles")]
        private ParticleSystem _explosionParticles;
        [FormerlySerializedAsAttribute("m_CurrentHealth")]
        private float _currentHealth;
        public float _defenceMultiplier = 1f;
        [FormerlySerializedAsAttribute("m_Dead")]
        private bool _dead;

        private void Awake()
        {
            _explosionParticles = Instantiate(_explosionPrefab).GetComponent<ParticleSystem>();
            _explosionAudio = _explosionParticles.GetComponent<AudioSource>();
            _explosionParticles.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _currentHealth = _startingHealth;
            _dead = false;

            SetHealthUI();
        }

        public void ReceiveHelp(float amount)
        {
            _currentHealth += amount;
            if (_currentHealth > _startingHealth)
            {
                _currentHealth = _startingHealth;
            }

            SetHealthUI();
        }

        public void TakeDamage(float amount)
        {
            _currentHealth -= amount * (1/_defenceMultiplier);

            SetHealthUI();

            if (_currentHealth <= 0f && !_dead)
            {
                OnDeath();
            }
        }

        private void SetHealthUI()
        {
            _slider.value = _currentHealth;

            // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
            _fillImage.color = Color.Lerp(_zeroHealthColor, _fullHealthColor, _currentHealth / _startingHealth);
        }

        private void OnDeath()
        {
            _dead = true;

            _explosionParticles.transform.position = transform.position;
            _explosionParticles.gameObject.SetActive(true);

            _explosionParticles.Play();
            _explosionAudio.Play();

            gameObject.SetActive(false);
        }
    }
}