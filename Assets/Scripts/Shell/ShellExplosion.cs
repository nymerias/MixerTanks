using UnityEngine;
using UnityEngine.Serialization;

namespace Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_TankMask")]
        public LayerMask _tankMask;                        // Used to filter what the explosion affects, this should be set to "Players".
        [FormerlySerializedAsAttribute("m_ExplosionParticles")]
        public ParticleSystem _explosionParticles;         // Reference to the particles that will play on explosion.
        [FormerlySerializedAsAttribute("m_ExplosionAudio")]
        public AudioSource _explosionAudio;                // Reference to the audio that will play on explosion.
        [FormerlySerializedAsAttribute("m_MaxDamage")]
        public float _maxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
        [FormerlySerializedAsAttribute("m_ExplosionForce")]
        public float _explosionForce = 1000f;              // The amount of force added to a tank at the centre of the explosion.
        [FormerlySerializedAsAttribute("m_MaxLifeTime")]
        public float _maxLifeTime = 2f;                    // The time in seconds before the shell is removed.
        [FormerlySerializedAsAttribute("m_ExplosionRadius")]
        public float _explosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.

        private void Start()
        {
            // If it isn't destroyed by then, destroy the shell after it's lifetime.
            Destroy(gameObject, _maxLifeTime);
        }

        private void OnTriggerEnter(Collider other)
        {
			// Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
            Collider[] colliders = Physics.OverlapSphere (transform.position, _explosionRadius, _tankMask);

            // Go through all the colliders...
            for (int i = 0; i < colliders.Length; i++)
            {
                // ... and find their rigidbody.
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();

                // If they don't have a rigidbody, go on to the next collider.
                if (!targetRigidbody)
                    continue;

                // Add an explosion force.
                targetRigidbody.AddExplosionForce(_explosionForce, transform.position, _explosionRadius);

                // Find the TankHealth script associated with the rigidbody.
                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();

                // If there is no TankHealth script attached to the gameobject, go on to the next collider.
                if (!targetHealth)
                    continue;

                // Calculate the amount of damage the target should take based on it's distance from the shell.
                float damage = CalculateDamage(targetRigidbody.position);

                // Deal this damage to the tank.
                targetHealth.TakeDamage(damage);
            }

            // Unparent the particles from the shell.
            _explosionParticles.transform.parent = null;

            // Play the particle system.
            _explosionParticles.Play();

            // Play the explosion sound effect.
            _explosionAudio.Play();

            // Once the particles have finished, destroy the gameobject they are on.
            ParticleSystem.MainModule mainModule = _explosionParticles.main;
            Destroy(_explosionParticles.gameObject, mainModule.duration);

            // Destroy the shell.
            Destroy(gameObject);
        }

        private float CalculateDamage(Vector3 targetPosition)
        {
            // Create a vector from the shell to the target.
            Vector3 explosionToTarget = targetPosition - transform.position;

            // Calculate the distance from the shell to the target.
            float explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
            float relativeDistance = (_explosionRadius - explosionDistance) / _explosionRadius;

            // Calculate damage as this proportion of the maximum possible damage.
            float damage = relativeDistance * _maxDamage;

            // Make sure that the minimum damage is always 0.
            damage = Mathf.Max(0f, damage);

            return damage;
        }
    }
}