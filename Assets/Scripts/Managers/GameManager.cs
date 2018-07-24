using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Linq;
using Microsoft.Mixer;
using Assets.Scripts.Managers;
using Assets.Scripts.Mixer;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_NumRoundsToWin")]
        public int _numRoundsToWin = 5;
        [FormerlySerializedAsAttribute("m_StartDelay")]
        public float _startDelay = 3f;
        [FormerlySerializedAsAttribute("m_EndDelay")]
        public float _endDelay = 3f;
        [FormerlySerializedAsAttribute("m_CameraControl")]
        public CameraControl _cameraControl;       // Reference to the CameraControl script for control during different phases.
        [FormerlySerializedAsAttribute("m_MessageText")]
        public Text _messageText;                  // Reference to the overlay Text to display winning text, etc.
        [FormerlySerializedAsAttribute("m_TankPrefab")]
        public GameObject _tankPrefab;
        [FormerlySerializedAsAttribute("m_Tanks")]
        public TankManager[] _tanks;               // A collection of managers for enabling and disabling different aspects of the tanks.

        [FormerlySerializedAsAttribute("m_RoundNumber")]
        private int _roundNumber;
        [FormerlySerializedAsAttribute("m_StartWait")]
        private WaitForSeconds _startWait;
        [FormerlySerializedAsAttribute("m_EndWait")]
        private WaitForSeconds _endWait;
        [FormerlySerializedAsAttribute("m_RoundWinner")]
        private TankManager _roundWinner;
        [FormerlySerializedAsAttribute("m_GameWinner")]
        private TankManager _gameWinner;

        [HideInInspector]
        private InteractiveStateMachine _stateMachine;

        private void Start()
        {
            _stateMachine = new InteractiveStateMachine();

            MixerInteractive.GoInteractive();
            MixerInteractive.OnInteractivityStateChanged += OnMixerInteractivtyStarted;
            MixerInteractive.OnParticipantStateChanged += OnParticipantStateChange;

            _startWait = new WaitForSeconds(_startDelay);
            _endWait = new WaitForSeconds(_endDelay);

            SpawnAllTanks();
            SetCameraTargets();

            // Once the tanks have been created and the camera is using them as targets, start the game.
            StartCoroutine(GameLoop());
        }

        private void OnMixerInteractivtyStarted(object sender, InteractivityStateChangedEventArgs e)
        {
            if (MixerInteractive.InteractivityState == InteractivityState.InteractivityEnabled)
            {
                MixerInteractive.SetCurrentScene(OnlineConstants.SCENE_LOBBY);

                _stateMachine.UpdateLobbyStatus();
                _stateMachine.HandlePlayerJoins();
            }
        }

        private void OnParticipantStateChange(object sender, InteractiveParticipantStateChangedEventArgs ev)
        {
            ev.Participant.Group = MixerInteractive.GetGroup(_stateMachine.ParticipantStartGroup);
        }

        private void SpawnAllTanks()
        {
            foreach (var item in _tanks.Select((val, idx) => new { Tank = val, Index = idx }))
            {
                item.Tank._instance = Instantiate(_tankPrefab, item.Tank._spawnPoint.position, item.Tank._spawnPoint.rotation) as GameObject;
                item.Tank._playerNumber = item.Index + 1;
                item.Tank.Setup();
            }
        }

        private void SetCameraTargets()
        {
            Transform[] targets = new Transform[_tanks.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = _tanks[i]._instance.transform;
            }

            _cameraControl._targets = targets;
        }

        /// <summary>
        /// Called from start and will run each phase of the game one after another.
        /// </summary>
        private IEnumerator GameLoop()
        {
            ResetGame();

            yield return StartCoroutine(WaitingForPlayers());

            yield return StartCoroutine(RoundStarting());

            yield return StartCoroutine(RoundPlaying());

            yield return StartCoroutine(RoundEnding());

            if (_gameWinner != null)
            {
                SceneManager.LoadScene(0);
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues.
                // Note that this coroutine doesn't yield. This means that the current version of the GameLoop will end.
                StartCoroutine(GameLoop());
            }
        }

        private void ResetGame()
        {
            ResetAllTanks();
            DisableTankControl();

            _cameraControl.SetStartPositionAndSize();
        }

        private IEnumerator WaitingForPlayers()
        {
            _messageText.text = "WAITING FOR PLAYERS";

            while (!_stateMachine.AllPlayersJoined)
            {
                yield return null;
            }
        }

        private IEnumerator RoundStarting()
        {
            _roundNumber++;
            _messageText.text = "ROUND " + _roundNumber;

            yield return _startWait;
        }

        private IEnumerator RoundPlaying()
        {
            EnableTankControl();

            _messageText.text = string.Empty;   // Clear the text from the screen.

            while (!OneTankLeft())
            {
                yield return null;
            }
        }

        private IEnumerator RoundEnding()
        {
            DisableTankControl();

            _roundWinner = null;
            _roundWinner = GetRoundWinner();

            if (_roundWinner != null)
                _roundWinner._wins++;

            _gameWinner = GetGameWinner();

            string message = EndMessage();
            _messageText.text = message;

            yield return _endWait;
        }

        /// <summary>
        /// This is used to check if there is one or fewer tanks remaining and thus the round should end.
        /// </summary>
        private bool OneTankLeft()
        {
            int numTanksLeft = 0;
            for (int i = 0; i < _tanks.Length; i++)
            {
                if (_tanks[i]._instance.activeSelf)
                    numTanksLeft++;
            }

            return numTanksLeft <= 1;
        }

        /// <summary>
        /// Find out if there is a winner of the round.
        /// Called with the assumption that 1 or fewer tanks are currently active.
        /// </summary>
        private TankManager GetRoundWinner()
        {
            for (int i = 0; i < _tanks.Length; i++)
            {
                if (_tanks[i]._instance.activeSelf)
                    return _tanks[i];
            }

            return null;    // If null it is a draw
        }

        /// <summary>
        /// Finds out if there is a winner of the game.
        /// </summary>
        private TankManager GetGameWinner()
        {
            for (int i = 0; i < _tanks.Length; i++)
            {
                if (_tanks[i]._wins == _numRoundsToWin)
                    return _tanks[i];
            }

            // If no tanks have enough rounds to win, return null.
            return null;
        }

        /// <summary>
        /// Returns a string message to display at the end of each round.
        /// </summary>
        private string EndMessage()
        {
            string message = "DRAW!";

            if (_roundWinner != null)
                message = _roundWinner._coloredPlayerText + " WINS THE ROUND!";

            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message.
            for (int i = 0; i < _tanks.Length; i++)
            {
                message += _tanks[i]._coloredPlayerText + ": " + _tanks[i]._wins + " WINS\n";
            }

            if (_gameWinner != null)
                message = _gameWinner._coloredPlayerText + " WINS THE GAME!";

            return message;
        }

        /// <summary>
        /// Turn all the tanks back on and reset their positions and properties.
        /// </summary>
        private void ResetAllTanks()
        {
            _tanks.ToList().ForEach(x => x.Reset());
        }

        private void EnableTankControl()
        {
            _tanks.ToList().ForEach(x => x.EnableControl());
        }

        private void DisableTankControl()
        {
            _tanks.ToList().ForEach(x => x.DisableControl());
        }
    }
}