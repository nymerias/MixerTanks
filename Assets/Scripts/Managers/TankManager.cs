using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAsAttribute("m_PlayerColor")]
        public Color _playerColor;                             // This is the color this tank will be tinted.
        [FormerlySerializedAsAttribute("m_SpawnPoint")]
        public Transform _spawnPoint;                          // The position and direction the tank will have when it spawns.
        [HideInInspector] public int _playerNumber;            // This specifies which player this the manager for.
        [HideInInspector] public string _coloredPlayerText;    // A string that represents the player with their number colored to match their tank.
        [HideInInspector] public GameObject _instance;         // A reference to the instance of the tank when it is created.
        [HideInInspector] public int _wins;                    // The number of wins this player has so far.


        [FormerlySerializedAsAttribute("m_Movement")]
        private TankMovement _movement;                        // Reference to tank's movement script, used to disable and enable control.
        [FormerlySerializedAsAttribute("m_Shooting")]
        private TankShooting _shooting;                        // Reference to tank's shooting script, used to disable and enable control.
        [FormerlySerializedAsAttribute("m_CanvasGameObject")]
        private GameObject _canvasGameObject;


        public void Setup()
        {
            // Get references to the components.
            _movement = _instance.GetComponent<TankMovement>();
            _shooting = _instance.GetComponent<TankShooting>();
            _canvasGameObject = _instance.GetComponentInChildren<Canvas>().gameObject;

            // Set the player numbers to be consistent across the scripts.
            _movement._playerNumber = _playerNumber;
            _shooting._playerNumber = _playerNumber;

            // Create a string using the correct color that says 'PLAYER 1' etc based on the tank's color and the player's number.
            _coloredPlayerText = "<color=#" + ColorUtility.ToHtmlStringRGB(_playerColor) + ">PLAYER " + _playerNumber + "</color>";

            // Get all of the renderers of the tank.
            MeshRenderer[] renderers = _instance.GetComponentsInChildren<MeshRenderer>();

            // Go through all the renderers...
            for (int i = 0; i < renderers.Length; i++)
            {
                // ... set their material color to the color specific to this tank.
                renderers[i].material.color = _playerColor;
            }
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
    }
}