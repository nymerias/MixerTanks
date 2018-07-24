using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;
using Microsoft.Mixer;

namespace Complete
{
    /// <summary>
    /// This class is to manage various settings on a tank.
    /// It works with the GameManager class to control how the tanks behave
    /// and whether or not players have control of their tank in the
    /// different phases of the game.
    /// </summary>
    [Serializable]
    public class TankManager
    {
        public string _playerName;
        [FormerlySerializedAsAttribute("m_PlayerColor")]
        public Color _playerColor;
        [FormerlySerializedAsAttribute("m_SpawnPoint")]
        public Transform _spawnPoint;
        [HideInInspector] public int _playerNumber;
        [HideInInspector] public string _coloredPlayerText;
        [HideInInspector] public GameObject _instance;
        [HideInInspector] public int _wins;

        [FormerlySerializedAsAttribute("m_Movement")]
        public TankMovement _movement;  //TODO: Make a getter/setter for this
        [FormerlySerializedAsAttribute("m_Shooting")]
        public TankShooting _shooting;
        [FormerlySerializedAsAttribute("m_CanvasGameObject")]
        private GameObject _canvasGameObject;

        private InteractiveParticipant _participant;
        public InteractiveParticipant OnlineParticipant
        {
            get { return _participant; }
            set
            {
                _participant = value;
                if (_participant != null)
                {
                    _movement.participantId = _participant.UserID;
                    _shooting.participantId = _participant.UserID;

                    _coloredPlayerText = "<color=#" + ColorUtility.ToHtmlStringRGB(_playerColor) + ">" + _participant.UserName + "</color>";
                }
            }
        }

        public void Setup()
        {
            ToggleSounds(false);

            _movement = _instance.GetComponent<TankMovement>();
            _shooting = _instance.GetComponent<TankShooting>();
            _canvasGameObject = _instance.GetComponentInChildren<Canvas>().gameObject;

            _movement._playerNumber = _playerNumber;
            _shooting._playerNumber = _playerNumber;

            MeshRenderer[] renderers = _instance.GetComponentsInChildren<MeshRenderer>();
            renderers.ToList().ForEach(x => x.material.color = _playerColor);
        }

        /// <summary>
        /// Used during the phases of the game where the player shouldn't be able to control their tank.
        /// </summary>
        public void DisableControl()
        {
            _movement.enabled = false;
            _shooting.enabled = false;

            _canvasGameObject.SetActive(false);
        }

        /// <summary>
        /// Used during the phases of the game where the player should be able to control their tank.
        /// </summary>
        public void EnableControl()
        {
            ToggleSounds(true);

            _movement.enabled = true;
            _shooting.enabled = true;

            _canvasGameObject.SetActive(true);
        }

        /// <summary>
        /// Used at the start of each round to put the tank into it's default state.
        /// </summary>
        public void Reset()
        {
            _instance.transform.position = _spawnPoint.position;
            _instance.transform.rotation = _spawnPoint.rotation;

            _instance.SetActive(false);
            _instance.SetActive(true);
        }

        public void ToggleSounds(bool toggle)
        {
            var engineIdle = _instance.GetComponents<AudioSource>();
            engineIdle.ToList().ForEach(x =>
            {
                if (toggle)
                    x.Play();
                else
                    x.Stop();
            });
        }
    }
}