﻿using Assets.Scripts.Mixer;
using Microsoft.Mixer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Facilitate the Mixer interactivity states
    /// </summary>
    class InteractiveStateMachine
    {
        private bool _p1Joined, _p2Joined = false;

        public bool AllPlayersJoined
        {
            get
            {
                return _p1Joined && _p2Joined;
            }
        }

        /// <summary>
        /// The scene that should be shown to users loading for the first time
        /// </summary>
        public string ParticipantStartGroup
        {
            get
            {
                return AllPlayersJoined ?
                    OnlineConstants.GROUP_HELP :
                    OnlineConstants.GROUP_START;
            }
        }

        public InteractiveParticipant ParticipantOne
        {
            get;
            set;
        }

        public InteractiveParticipant ParticipantTwo
        {
            get;
            set;
        }

        /// <summary>
        /// Set everone who is not a player to the help group
        /// </summary>
        public void SetViewersToGiveHelp()
        {
            MixerInteractive.Participants
                .Where(p => p != ParticipantOne && p != ParticipantTwo)
                .ToList()
                .ForEach(p => p.Group = MixerInteractive.GetGroup(OnlineConstants.GROUP_HELP));
        }

        public void SetAllParticipantsToLobby()
        {
            MixerInteractive.Participants.ToList()
                .ForEach(p => p.Group = MixerInteractive.GetGroup(OnlineConstants.GROUP_START));
        }

        public void ResetToDefault()
        {
            _p1Joined = _p2Joined = false;
            ParticipantOne = null;
            ParticipantTwo = null;

            MixerInteractive.OnInteractiveButtonEvent -= OnJoinButtonEvents;
            MixerInteractive.GetControl(OnlineConstants.CONTROL_P1_JOIN).SetDisabled(false);
            MixerInteractive.GetControl(OnlineConstants.CONTROL_P2_JOIN).SetDisabled(false);

            UpdateLobbyStatus();
        }

        /// <summary>
        /// Facilitate the lobby player join events
        /// </summary>
        public void HandlePlayerJoins()
        {
            MixerInteractive.OnInteractiveButtonEvent += OnJoinButtonEvents;
        }

        private void OnJoinButtonEvents(object sender, InteractiveButtonEventArgs ev)
        {
            if (ev.ControlID == OnlineConstants.CONTROL_P1_JOIN)
            {
                _p1Joined = true;
                ParticipantOne = ev.Participant;
                UpdateControlsAfterJoin(ev);
            }
            else if (ev.ControlID == OnlineConstants.CONTROL_P2_JOIN)
            {
                _p2Joined = true;
                ParticipantTwo = ev.Participant;
                UpdateControlsAfterJoin(ev);
            }
        }

        private void UpdateControlsAfterJoin(InteractiveButtonEventArgs ev)
        {
            MixerInteractive.GetControl(ev.ControlID).SetDisabled(true);
            ev.Participant.Group = MixerInteractive.GetGroup(OnlineConstants.GROUP_CONTROLS);

            UpdateLobbyStatus();
        }

        public void UpdateLobbyStatus()
        {
            var label = MixerInteractive.GetControl(OnlineConstants.CONTROL_STATUS) as InteractiveLabelControl;

            var waitingRed = "Waiting for red player\n";
            var redJoined = "Red player joined\n";

            var waitingBlue = "Waiting for blue player";
            var blueJoined = "Blue player joined";

            var message = (_p1Joined ? redJoined : waitingRed)
                + (_p2Joined ? blueJoined : waitingBlue);

            label.SetText(message);
        }

        public void OnParticipantStateChange(object sender, InteractiveParticipantStateChangedEventArgs ev)
        {
            if (ev.State == InteractiveParticipantState.Joined)
            {
                ev.Participant.Group = MixerInteractive.GetGroup(ParticipantStartGroup);
            }
            else if (ev.State == InteractiveParticipantState.Left)
            {
                HandleParticipantLeave(ev.Participant);
            }
        }

        /// <summary>
        /// Special leave handling for either of player 1 or 2
        /// </summary>
        private void HandleParticipantLeave(InteractiveParticipant participant)
        {
            //if (participant == ParticipantOne)
            //{

            //}
        }
    }
}
