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
            Destroy(gameObject, _maxLifeTime);
        }

        private void OnTriggerEnter(Collider other)
        {
			// Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
            Collider[] colliders = Physics.OverlapSphere (transform.position, _explosionRadius, _tankMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                if (!targetRigidbody)
                    continue;
                
                targetRigidbody.AddExplosionForce(_explosionForce, transform.position, _explosionRadius);


                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                if (!targetHealth)
                    continue;

                float damage = CalculateDamage(targetRigidbody.position);
                targetHealth.TakeDamage(damage);
            }

            _explosionParticles.transform.parent = null;
            _explosionParticles.Play();
            _explosionAudio.Play();

            ParticleSystem.MainModule mainModule = _explosionParticles.main;
            Destroy(_explosionParticles.gameObject, mainModule.duration);

            Destroy(gameObject);
        }

        private float CalculateDamage(Vector3 targetPosition)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float explosionDistance = explosionToTarget.magnitude;
            float relativeDistance = (_explosionRadius - explosionDistance) / _explosionRadius;

            float damage = relativeDistance * _maxDamage;
            damage = Mathf.Max(0f, damage);

            return damage;
        }
    }
}