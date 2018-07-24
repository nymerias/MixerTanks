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

        private InteractiveStateMachine _stateMachine;
        private List<TankManager> _playerTanks;

        private void Start()
        {
            _stateMachine = new InteractiveStateMachine();

            MixerInteractive.GoInteractive();
            MixerInteractive.OnInteractivityStateChanged += OnMixerInteractivtyStarted;
            MixerInteractive.OnParticipantStateChanged += OnParticipantStateChange;

            _startWait = new WaitForSeconds(_startDelay);
            _endWait = new WaitForSeconds(_endDelay);

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
            if (ev.State == InteractiveParticipantState.Joined)
            {
                ev.Participant.Group = MixerInteractive.GetGroup(_stateMachine.ParticipantStartGroup);
            }
            //TODO: We may want to handle the "leaving" state for any of the current set of players
        }

        /// <summary>
        /// Called from start and will run each phase of the game one after another.
        /// </summary>
        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(WaitingForPlayers());

            yield return StartCoroutine(InitializeGame());

            yield return StartCoroutine(RoundIsAboutToStart());

            yield return StartCoroutine(RoundIsPlaying());

            yield return StartCoroutine(RoundHasEnded());

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

        /// <summary>
        /// Game Loop Step 1: Wait for Mixer players to join
        /// </summary>
        private IEnumerator WaitingForPlayers()
        {
            _messageText.text = "WAITING FOR PLAYERS";

            while (!_stateMachine.AllPlayersJoined)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Game Loop Step 2: Initialize & setup tanks
        /// </summary>
        private IEnumerator InitializeGame()
        {
            _playerTanks = new List<TankManager> { _redPlayer, _bluePlayer };
            _playerTanks.ForEach(tank =>
            {
                var currentTank = Instantiate(_tankPrefab, tank._spawnPoint.position, tank._spawnPoint.rotation);
                tank._instance = currentTank;
                tank.Setup();
            });

            //Currently we are going to assume that we need both P1 & P2
            _redPlayer.OnlineParticipant = _stateMachine.ParticipantOne;
            _bluePlayer.OnlineParticipant = _stateMachine.ParticipantTwo;

            _cameraControl._targets = _playerTanks.Select(tank => tank._instance.transform).ToArray();

            yield return null;
        }

        /// <summary>
        /// Game Loop Step 3: Show all users that the round is about to start
        /// </summary>
        /// <returns></returns>
        private IEnumerator RoundIsAboutToStart()
        {
            //Disable control while we wait for the round number to disappear
            _playerTanks.ToList().ForEach(tank =>
            {
                tank.Reset();
                tank.DisableControl();
            });

            _cameraControl.SetStartPositionAndSize();

            _roundNumber++;
            _messageText.text = "ROUND " + _roundNumber;

            yield return _startWait;
        }

        /// <summary>
        /// Game Loop Step 4: Current round in progress
        /// </summary>
        private IEnumerator RoundIsPlaying()
        {
            //Now enable so users can play the game
            _playerTanks.ToList().ForEach(x => x.EnableControl());

            _messageText.text = string.Empty;

            while (!OneTankLeft())
            {
                yield return null;
            }
        }

        /// <summary>
        /// Game Loop Step 5: Current round has ended
        /// </summary>
        private IEnumerator RoundHasEnded()
        {
            _playerTanks.ToList().ForEach(x => x.DisableControl());

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
            // If no tanks have enough rounds to win, return null.
            return _playerTanks.First(tank => tank._wins == _numRoundsToWin);
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
            _playerTanks.ForEach(tank =>
            {
                message += tank._coloredPlayerText + ": " + tank._wins + " WINS\n";
            });

            if (_gameWinner != null)
                message = _gameWinner._coloredPlayerText + " WINS THE GAME!";

            return message;
        }
    }
}