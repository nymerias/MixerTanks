using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Linq;
using Microsoft.Mixer;
using Assets.Scripts.Managers;
using Assets.Scripts.Mixer;
using System.Collections.Generic;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        public int _numRoundsToWin = 3;
        public float _startDelay = 3f;
        public float _endDelay = 3f;

        public CameraControl _cameraControl;       // Reference to the CameraControl script for control during different phases.
        public Text _messageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject _tankPrefab;

        public TankManager _redPlayer;
        public TankManager _bluePlayer;

        private int _roundNumber;
        private WaitForSeconds _startWait;
        private WaitForSeconds _endWait;
        private TankManager _roundWinner;
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

            //SpawnAllTanks();
            //SetCameraTargets();

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

        /// <summary>
        /// Called from start and will run each phase of the game one after another.
        /// </summary>
        private IEnumerator GameLoop()
        {
            //ResetGame();

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

        private List<TankManager> _playerTanks;

        private void SpawnAllTanks()
        {
            _playerTanks = new List<TankManager> { _redPlayer, _bluePlayer };
            _playerTanks.ForEach(tank =>
            {
                var currentTank = Instantiate(_tankPrefab, tank._spawnPoint.position, tank._spawnPoint.rotation);
                tank._instance = currentTank;
                tank.Setup();
            });
        }

        private void SetCameraTargets()
        {
            _cameraControl._targets = _playerTanks.Select(tank => tank._instance.transform).ToArray();
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

            //_tanks[1]._movement.participantId = _stateMachine.ParticipantOne.UserID;
            //_tanks[1]._shooting.participantId = _stateMachine.ParticipantOne.UserID;

            //_tanks[0]._movement.participantId = _stateMachine.ParticipantTwo.UserID;
            //_tanks[0]._shooting.participantId = _stateMachine.ParticipantTwo.UserID;

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
            _playerTanks.ForEach(tank =>
            {
                if (tank._instance.activeSelf)
                    numTanksLeft++;
            });

            return numTanksLeft <= 1;
        }

        /// <summary>
        /// Find out if there is a winner of the round.
        /// Called with the assumption that 1 or fewer tanks are currently active.
        /// </summary>
        private TankManager GetRoundWinner()
        {
            //for (int i = 0; i < _tanks.Length; i++)
            //{
            //    if (_tanks[i]._instance.activeSelf)
            //        return _tanks[i];
            //}

            return null;    // If null it is a draw
        }

        /// <summary>
        /// Finds out if there is a winner of the game.
        /// </summary>
        private TankManager GetGameWinner()
        {
            //for (int i = 0; i < _tanks.Length; i++)
            //{
            //    if (_tanks[i]._wins == _numRoundsToWin)
            //        return _tanks[i];
            //}

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
            //for (int i = 0; i < _tanks.Length; i++)
            //{
            //    message += _tanks[i]._coloredPlayerText + ": " + _tanks[i]._wins + " WINS\n";
            //}
            _playerTanks.ForEach(tank =>
            {
                message += tank._coloredPlayerText + ": " + tank._wins + " WINS\n";
            });

            if (_gameWinner != null)
                message = _gameWinner._coloredPlayerText + " WINS THE GAME!";

            return message;
        }

        /// <summary>
        /// Turn all the tanks back on and reset their positions and properties.
        /// </summary>
        private void ResetAllTanks()
        {
            //_tanks.ToList().ForEach(x => x.Reset());
        }

        private void EnableTankControl()
        {
            //_tanks.ToList().ForEach(x => x.EnableControl());
        }

        private void DisableTankControl()
        {
            //_tanks.ToList().ForEach(x => x.DisableControl());
        }
    }
}