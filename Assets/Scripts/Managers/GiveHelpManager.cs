using System;
using Assets.Scripts.Mixer;
using Microsoft.Mixer;
using System.Timers;


namespace Complete
{

    public interface HelpContract
    {
        String getUsername();
        void increaseHealth(int amount);
        void setSpeedMultiplier(float multiplier);
        void setAttackMultiplier(float multiplier);
        void setDefenceMultiplier(float multiplier);
    }

    public class GiveHelpManager
    {

        private static readonly string[] boosts = { "20HP", "speed boost", "attack boost", "defense boost" };

        private InteractiveLabelControl getViewerLabel()
        {
            return MixerInteractive.GetControl(OnlineConstants.CONTROL_VIEWER_UPDATE) as InteractiveLabelControl;
        }

        private InteractiveLabelControl getPlayerLabel()
        {
            return MixerInteractive.GetControl(OnlineConstants.CONTROL_PLAYER_UPDATE) as InteractiveLabelControl;
        }

        public void GiveHelp(HelpContract helpContract, String helperName)
        {
            System.Random rnd = new System.Random();
            int action = rnd.Next(0, 4);

            switch (action)
            {
                case 0: // health up
                    helpContract.increaseHealth(20);
                    break;
                case 1: // speed up
                    helpContract.setSpeedMultiplier(2f);
                    break;
                case 2: // attack up
                    helpContract.setAttackMultiplier(2f);
                    break;
                case 3: // defense up
                    helpContract.setDefenceMultiplier(2f);
                    break;
            }
            String update = helperName + " gave " + boosts[action] + " to " + helpContract.getUsername() + "!";
            getViewerLabel().SetText(update);
            getPlayerLabel().SetText(update);
            if (action > 0)
            {
                StartHelpCooldown(helpContract, action);
            }
        }

        private void StartHelpCooldown(HelpContract helpContract, int action)
        {
            Timer t = new Timer { Interval = 10000 };
            t.Elapsed += (sender, e) => ResetTankStatus(sender, e, helpContract, action);
            t.Start();
        }

        private void ResetTankStatus(object sender, ElapsedEventArgs e, HelpContract helpContract, int action)
        {
            // The state object is the Timer object.
            Timer t = (Timer)sender;
            t.Stop();
            t.Dispose();

            switch (action)
            {
                case 1: // speed up
                    helpContract.setSpeedMultiplier(1f);
                    break;
                case 2: // attack up
                    helpContract.setAttackMultiplier(1f);
                    break;
                case 3: // defense up
                    helpContract.setDefenceMultiplier(1f);
                    break;
            }
            String update = helpContract.getUsername() + "'s " + boosts[action] + " has worn off!";
            getViewerLabel().SetText(update);
            getPlayerLabel().SetText(update);
        }

    }
}