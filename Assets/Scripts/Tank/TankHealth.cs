using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Complete
{
    public class TankHealth : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_StartingHealth")]
        public float _startingHealth = 100f;               // The amount of health each tank starts with.
        [FormerlySerializedAsAttribute("m_Slider")]
        public Slider _slider;                             // The slider to represent how much health the tank currently has.
        [FormerlySerializedAsAttribute("m_FillImage")]
        public Image _fillImage;                           // The image component of the slider.
        [FormerlySerializedAsAttribute("m_FullHealthColor")]
        public Color _fullHealthColor = Color.green;       // The color the health bar will be when on full health.
        [FormerlySerializedAsAttribute("m_ZeroHealthColor")]
        public Color _zeroHealthColor = Color.red;         // The color the health bar will be when on no health.
        [FormerlySerializedAsAttribute("m_ExplosionPrefab")]
        public GameObject _explosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies.


        [FormerlySerializedAsAttribute("m_ExplosionAudio")]
        private AudioSource _explosionAudio;               // The audio source to play when the tank explodes.
        [FormerlySerializedAsAttribute("m_ExplosionParticles")]
        private ParticleSystem _explosionParticles;        // The particle system the will play when the tank is destroyed.
        [FormerlySerializedAsAttribute("m_CurrentHealth")]
        private float _currentHealth;                      // How much health the tank currently has.
        [FormerlySerializedAsAttribute("m_Dead")]
        private bool _dead;

        private void Awake()
        {
            // Instantiate the explosion prefab and get a reference to the particle system on it.
            _explosionParticles = Instantiate(_explosionPrefab).GetComponent<ParticleSystem>();

            // Get a reference to the audio source on the instantiated prefab.
            _explosionAudio = _explosionParticles.GetComponent<AudioSource>();

            // Disable the prefab so it can be activated when it's required.
            _explosionParticles.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            // When the tank is enabled, reset the tank's health and whether or not it's dead.
            _currentHealth = _startingHealth;
            _dead = false;

            // Update the health slider's value and color.
            SetHealthUI();
        }

        public void TakeDamage(float amount)
        {
            // Reduce current health by the amount of damage done.
            _currentHealth -= amount;

            // Change the UI elements appropriately.
            SetHealthUI();

            // If the current health is at or below zero and it has not yet been registered, call OnDeath.
            if (_currentHealth <= 0f && !_dead)
            {
                OnDeath();
            }
        }

        private void SetHealthUI()
        {
            // Set the slider's value appropriately.
            _slider.value = _currentHealth;

            // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
            _fillImage.color = Color.Lerp(_zeroHealthColor, _fullHealthColor, _currentHealth / _startingHealth);
        }

        private void OnDeath()
        {
            // Set the flag so that this function is only called once.
            _dead = true;

            // Move the instantiated explosion prefab to the tank's position and turn it on.
            _explosionParticles.transform.position = transform.position;
            _explosionParticles.gameObject.SetActive(true);

            // Play the particle system of the tank exploding.
            _explosionParticles.Play();

            // Play the tank explosion sound effect.
            _explosionAudio.Play();

            // Turn the tank off.
            gameObject.SetActive(false);
        }
    }
}