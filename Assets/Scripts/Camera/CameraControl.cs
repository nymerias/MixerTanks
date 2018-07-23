
using UnityEngine;
using System.Linq;
using UnityEngine.Serialization;

namespace Complete
{
    public class CameraControl : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_DampTime")]
        public float _dampTime = 0.2f;                 // Approximate time for the camera to refocus.
        [FormerlySerializedAsAttribute("m_ScreenEdgeBuffer")]
        public float _screenEdgeBuffer = 4f;           // Space between the top/bottom most target and the screen edge.
        [FormerlySerializedAsAttribute("m_MinSize")]
        public float _minSize = 6.5f;                  // The smallest orthographic size the camera can be.
        [HideInInspector] public Transform[] _targets; // All the targets the camera needs to encompass.

        [FormerlySerializedAsAttribute("m_Camera")]
        private Camera _camera;
        [FormerlySerializedAsAttribute("m_ZoomSpeed")]
        private float _zoomSpeed;                      // Reference speed for the smooth damping of the orthographic size.
        [FormerlySerializedAsAttribute("m_MoveVelocity")]
        private Vector3 _moveVelocity;                 // Reference velocity for the smooth damping of the position.
        [FormerlySerializedAsAttribute("m_DesiredPosition")]
        private Vector3 _desiredPosition;

        private void Awake()
        {
            _camera = GetComponentInChildren<Camera>();
        }

        private void FixedUpdate()
        {
            Move();
            Zoom();
        }

        private void Move()
        {
            FindAveragePosition();

            // Smoothly transition to that position.
            transform.position = Vector3.SmoothDamp(transform.position, _desiredPosition, ref _moveVelocity, _dampTime);
        }

        /// <summary>
        /// Find the average position of the targets.
        /// </summary>
        private void FindAveragePosition()
        {
            Vector3 averagePos = new Vector3();
            int numTargets = 0;

            // Add all target positions positions together.
            _targets.ToList().ForEach(target =>
            {
                if (target.gameObject.activeSelf) {
                    // Add to the average and increment the number of targets in the average.
                    averagePos += target.position;
                    numTargets++;
                }
            });

            // If there are targets divide the sum of the positions by the number of them to find the average.
            if (numTargets > 0)
                averagePos /= numTargets;

            // Keep the same y value.
            averagePos.y = transform.position.y;

            _desiredPosition = averagePos;
        }

        /// <summary>
        /// Find the required size based on the desired position and smoothly transition to that size.
        /// </summary>
        private void Zoom()
        {
            float requiredSize = FindRequiredSize();
            _camera.orthographicSize = Mathf.SmoothDamp (_camera.orthographicSize, requiredSize, ref _zoomSpeed, _dampTime);
        }

        private float FindRequiredSize()
        {
            // Find the position the camera rig is moving towards in its local space.
            Vector3 desiredLocalPos = transform.InverseTransformPoint(_desiredPosition);

            float cameraSize = 0f;
            _targets.ToList().ForEach(target =>
            {
                if (target.gameObject.activeSelf) {
                    // Otherwise, find the position of the target in the camera's local space.
                    Vector3 targetLocalPos = transform.InverseTransformPoint(target.position);

                    // Find the position of the target from the desired position of the camera's local space.
                    Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;

                    // Choose the largest out of the current size and the distance of the tank 'up' or 'down' from the camera.
                    cameraSize = Mathf.Max(cameraSize, Mathf.Abs(desiredPosToTarget.y));

                    // Choose the largest out of the current size and the calculated size based on the tank being to the left or right of the camera.
                    cameraSize = Mathf.Max(cameraSize, Mathf.Abs(desiredPosToTarget.x) / _camera.aspect);
                }
            });

            cameraSize += _screenEdgeBuffer;          // Add the edge buffer to the size.
            cameraSize = Mathf.Max(cameraSize, _minSize);   // Make sure the camera's size isn't below the minimum.

            return cameraSize;
        }

        public void SetStartPositionAndSize()
        {
            FindAveragePosition();

            transform.position = _desiredPosition;
            _camera.orthographicSize = FindRequiredSize();  // Find and set the required size of the camera.
        }
    }
}