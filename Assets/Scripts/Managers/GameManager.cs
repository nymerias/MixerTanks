using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        [FormerlySerializedAsAttribute("m_NumRoundsToWin")]
        public int _numRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        [FormerlySerializedAsAttribute("m_StartDelay")]
        public float _startDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        [FormerlySerializedAsAttribute("m_EndDelay")]
        public float _endDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
        [FormerlySerializedAsAttribute("m_CameraControl")]
        public CameraControl _cameraControl;       // Reference to the CameraControl script for control during different phases.
        [FormerlySerializedAsAttribute("m_MessageText")]
        public Text _messageText;                  // Reference to the overlay Text to display winning text, etc.
        [FormerlySerializedAsAttribute("m_TankPrefab")]
        public GameObject _tankPrefab;             // Reference to the prefab the players will control.
        [FormerlySerializedAsAttribute("m_Tanks")]
        public TankManager[] _tanks;               // A collection of managers for enabling and disabling different aspects of the tanks.

        [FormerlySerializedAsAttribute("m_RoundNumber")]
        private int _roundNumber;                  // Which round the game is currently on.
        [FormerlySerializedAsAttribute("m_StartWait")]
        private WaitForSeconds _startWait;         // Used to have a delay whilst the round starts.
        [FormerlySerializedAsAttribute("m_EndWait")]
        private WaitForSeconds _endWait;           // Used to have a delay whilst the round or game ends.
        [FormerlySerializedAsAttribute("m_RoundWinner")]
        private TankManager _roundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
        [FormerlySerializedAsAttribute("m_GameWinner")]
        private TankManager _gameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.

        private void Start()
        {
            // Create the delays so they only have to be made once.
            _startWait = new WaitForSeconds (_startDelay);
            _endWait = new WaitForSeconds (_endDelay);

            SpawnAllTanks();
            SetCameraTargets();

            // Once the tanks have been created and the camera is using them as targets, start the game.
            StartCoroutine(GameLoop());
        }

        private void SpawnAllTanks()
        {
            // For all the tanks...
            for (int i = 0; i < _tanks.Length; i++)
            {
                // ... create them, set their player number and references needed for control.
                _tanks[i]._instance =
                    Instantiate(_tankPrefab, _tanks[i]._spawnPoint.position, _tanks[i]._spawnPoint.rotation) as GameObject;
                _tanks[i]._playerNumber = i + 1;
                _tanks[i].Setup();
            }
        }

        private void SetCameraTargets()
        {
            // Create a collection of transforms the same size as the number of tanks.
            Transform[] targets = new Transform[_tanks.Length];

            // For each of these transforms...
            for (int i = 0; i < targets.Length; i++)
            {
                // ... set it to the appropriate tank transform.
                targets[i] = _tanks[i]._instance.transform;
            }

            // These are the targets the camera should follow.
            _cameraControl._targets = targets;
        }

        /// <summary>
        /// This is called from start and will run each phase of the game one after another.
        /// </summary>
        private IEnumerator GameLoop()
        {
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
            yield return StartCoroutine(RoundStarting ());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
            yield return StartCoroutine(RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
            yield return StartCoroutine(RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found.
            if (_gameWinner != null)
            {
                // If there is a game winner, restart the level.
                SceneManager.LoadScene(0);
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues.
                // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
                StartCoroutine(GameLoop());
            }
        }

        private IEnumerator RoundStarting()
        {
            // As soon as the round starts reset the tanks and make sure they can't move.
            ResetAllTanks();
            DisableTankControl();

            // Snap the camera's zoom and position to something appropriate for the reset tanks.
            _cameraControl.SetStartPositionAndSize();

            // Increment the round number and display text showing the players what round it is.
            _roundNumber++;
            _messageText.text = "ROUND " + _roundNumber;

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return _startWait;
        }

        private IEnumerator RoundPlaying()
        {
            // As soon as the round begins playing let the players control the tanks.
            EnableTankControl();

            // Clear the text from the screen.
            _messageText.text = string.Empty;

            // While there is not one tank left...
            while (!OneTankLeft())
            {
                // ... return on the next frame.
                yield return null;
            }
        }

        private IEnumerator RoundEnding()
        {
            // Stop tanks from moving.
            DisableTankControl();

            // Clear the winner from the previous round.
            _roundWinner = null;

            // See if there is a winner now the round is over.
            _roundWinner = GetRoundWinner();

            // If there is a winner, increment their score.
            if (_roundWinner != null)
                _roundWinner._wins++;

            // Now the winner's score has been incremented, see if someone has one the game.
            _gameWinner = GetGameWinner();

            // Get a message based on the scores and whether or not there is a game winner and display it.
            string message = EndMessage();
            _messageText.text = message;

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return _endWait;
        }

        /// <summary>
        /// This is used to check if there is one or fewer tanks remaining and thus the round should end.
        /// </summary>
        private bool OneTankLeft()
        {
            // Start the count of tanks left at zero.
            int numTanksLeft = 0;

            // Go through all the tanks...
            for (int i = 0; i < _tanks.Length; i++)
            {
                // ... and if they are active, increment the counter.
                if (_tanks[i]._instance.activeSelf)
                    numTanksLeft++;
            }

            // If there are one or fewer tanks remaining return true, otherwise return false.
            return numTanksLeft <= 1;
        }

        /// <summary>
        /// This function is to find out if there is a winner of the round.
        /// This function is called with the assumption that 1 or fewer tanks are currently active.
        /// </summary>
        private TankManager GetRoundWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < _tanks.Length; i++)
            {
                // ... and if one of them is active, it is the winner so return it.
                if (_tanks[i]._instance.activeSelf)
                    return _tanks[i];
            }

            // If none of the tanks are active it is a draw so return null.
            return null;
        }

        /// <summary>
        ///
        // This function is to find out if there is a winner of the game.
        /// </summary>
        private TankManager GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < _tanks.Length; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it.
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
            // By default when a round ends there are no winners so the default end message is a draw.
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that.
            if (_roundWinner != null)
                message = _roundWinner._coloredPlayerText + " WINS THE ROUND!";

            // Add some line breaks after the initial message.
            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message.
            for (int i = 0; i < _tanks.Length; i++)
            {
                message += _tanks[i]._coloredPlayerText + ": " + _tanks[i]._wins + " WINS\n";
            }

            // If there is a game winner, change the entire message to reflect that.
            if (_gameWinner != null)
                message = _gameWinner._coloredPlayerText + " WINS THE GAME!";

            return message;
        }

        /// <summary>
        /// This function is used to turn all the tanks back on and reset their positions and properties.
        /// </summary>
        private void ResetAllTanks()
        {
            for (int i = 0; i < _tanks.Length; i++)
            {
                _tanks[i].Reset();
            }
        }

        private void EnableTankControl()
        {
            for (int i = 0; i < _tanks.Length; i++)
            {
                _tanks[i].EnableControl();
            }
        }

        private void DisableTankControl()
        {
            for (int i = 0; i < _tanks.Length; i++)
            {
                _tanks[i].DisableControl();
            }
        }
    }
}