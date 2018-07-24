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
        private TankManager _gameWinner = null;

        private InteractiveStateMachine _stateMachine;
        private List<TankManager> _playerTanks;

        private GiveHelpManager _giveHelpManager;

        private void Start()
        {
            _stateMachine = new InteractiveStateMachine();
            _giveHelpManager = new GiveHelpManager();
           
            MixerInteractive.GoInteractive();
            MixerInteractive.OnInteractivityStateChanged += OnMixerInteractivtyStarted;
            MixerInteractive.OnParticipantStateChanged += OnParticipantStateChange;
            MixerInteractive.OnInteractiveButtonEvent += OnGiveHelp;

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

        private void OnGiveHelp(object sender, InteractiveButtonEventArgs ev)
        {
            if (ev.ControlID == OnlineConstants.CONTROL_HELP_RED)
            {
                _giveHelpManager.GiveHelp(_redPlayer);
                MixerInteractive.TriggerCooldown(ev.ControlID, 10000);
            }
            else if (ev.ControlID == OnlineConstants.CONTROL_HELP_BLUE)
            {
                _giveHelpManager.GiveHelp(_bluePlayer);
                MixerInteractive.TriggerCooldown(ev.ControlID, 10000);
            }
        }

        /// <summary>
        /// Called from start and will run each phase of the game one after another.
        /// </summary>
        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(WaitingForPlayers());

            yield return StartCoroutine(InitializeGame());

            yield return StartCoroutine(PlayAllGameRounds());

            yield return StartCoroutine(DestroyGameSetup());

            //This coroutine doesn't yield. Means that the previously-current version of the GameLoop will end.
            StartCoroutine(GameLoop());
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

            _stateMachine.SetViewersToGiveHelp();

            yield return null;
        }

        /// <summary>
        /// Game Loop Step 3: Play all the game rounds till there is a winner
        /// </summary
        private IEnumerator PlayAllGameRounds()
        {
            while (_gameWinner == null)
            {
                yield return StartCoroutine(RoundIsAboutToStart());

                yield return StartCoroutine(RoundIsPlaying());

                yield return StartCoroutine(RoundHasEnded());
            }
        }

        /// <summary>
        /// Step 3, Part 1: Reset and get the round ready
        /// </summary>
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
        /// Step 3, Part 2: Current round, go till there is a winner
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
        /// Step 3, Part3: Round has ended, get ready for future rounds, look for winners
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
        /// Game Loop Step 4: Reset for new players
        /// </summary>
        private IEnumerator DestroyGameSetup()
        {
            _stateMachine.SetAllParticipantsToLobby();
            _stateMachine.ResetToDefault();

            Destroy(_bluePlayer._instance);
            Destroy(_redPlayer._instance);

            _bluePlayer.OnlineParticipant = null;
            _redPlayer.OnlineParticipant = null;

            _cameraControl._targets = new Transform[0];

            yield return null;
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
            var tankArray = _playerTanks.ToArray();
            for (int i = 0; i < tankArray.Length; i++)
            {
                if (tankArray[i]._instance.activeSelf)
                    return tankArray[i];
            }

            return null;
        }

        /// <summary>
        /// Finds out if there is a winner of the game.
        /// </summary>
        private TankManager GetGameWinner()
        {
            var tankArray = _playerTanks.ToArray();
            for (int i = 0; i < tankArray.Length; i++)
            {
                if (tankArray[i]._wins == _numRoundsToWin)
                    return tankArray[i];
            }

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