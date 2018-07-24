using System;
using Assets.Scripts.Mixer;
using Microsoft.Mixer;
using System.Timers;


namespace Complete
{

    public interface HelpContract
    {
        void increaseHealth(int amount);
        void setSpeedMultiplier(float multiplier);
        void setAttackMultiplier(float multiplier);
        void setDefenceMultiplier(float multiplier);
    }

    public class GiveHelpManager
    {

        private HelpContract _helpContract;
        private static readonly string[] boosts = { "20HP", "speed boost", "attack boost", "defense boost" };

        public void SetUpGiveHelp(HelpContract helpContract)
        {
            _helpContract = helpContract;

            MixerInteractive.OnInteractiveButtonEvent += (source, ev) =>
            {
                System.Random rnd = new System.Random();
                int action = rnd.Next(0, 4);
                if (ev.ControlID == OnlineConstants.CONTROL_HELP_RED)
                {
                    GiveHelp(true, action);
                    TriggerCooldown(ev.ControlID);
                }
                else if (ev.ControlID == OnlineConstants.CONTROL_HELP_BLUE)
                {
                    GiveHelp(false, action);
                }
            };
        }

        private void TriggerCooldown(String controlID)
        {
            // Disable button for 10 seconds
            MixerInteractive.TriggerCooldown(controlID, 10000);
        }

        private InteractiveLabelControl getLabel()
        {
            return MixerInteractive.GetControl(OnlineConstants.CONTROL_INFO_UPDATE) as InteractiveLabelControl;
        }

        private void GiveHelp(bool red, int action)
        {
            switch (action)
            {
                case 0: // health up
                    _helpContract.increaseHealth(20);
                    break;
                case 1: // speed up
                    _helpContract.setSpeedMultiplier(2f);
                    break;
                case 2: // attack up
                    _helpContract.setAttackMultiplier(2f);
                    break;
                case 3: // defense up
                    _helpContract.setDefenceMultiplier(2f);
                    break;
            }
            getLabel().SetText("Gave " + boosts[action] + " to " + (red ? "Red!" : "Blue!"));
            if (action > 0)
            {
                StartHelpCooldown(red, action);
            }
        }

        private void StartHelpCooldown(bool red, int action)
        {
            Timer t = new Timer { Interval = 10000 };
            t.Elapsed += (sender, e) => ResetTankStatus(sender, e, red, action);
            t.Start();
        }

        private void ResetTankStatus(object sender, ElapsedEventArgs e, bool red, int action)
        {
            // The state object is the Timer object.
            Timer t = (Timer)sender;
            t.Stop();
            t.Dispose();

            switch (action)
            {
                case 1: // speed up
                    _helpContract.setSpeedMultiplier(1f);
                    break;
                case 2: // attack up
                    _helpContract.setAttackMultiplier(1f);
                    break;
                case 3: // defense up
                    _helpContract.setDefenceMultiplier(1f);
                    break;
            }
            getLabel().SetText(red ? "Red's" : "Blue's" + boosts[action] + " has worn off!");
        }

    }
}