using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace KEI
{
    //             if (!HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().liteMode)

    public class KEI_Settings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "KEI"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE; } }
        public override string Section { get { return "KEI"; } }
        public override string DisplaySection { get { return "KEI"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets
        {
            get { return false; }
        }

        [GameParameters.CustomParameterUI("Active",
            toolTip = "Determines if the mod is active in this game")]
        public bool active = false;

        [GameParameters.CustomParameterUI("LITE mode",
            toolTip = "Just give all local science, no window, no toolbar button")]
        public bool liteMode = false;

        [GameParameters.CustomParameterUI("Medium Mode",
            toolTip = "Experiment needs to be run one time at the KSC before all science for that experiment is given automatically\n" +
            "no window, no toolbar button")]
        public bool mediumMode = false;

        [GameParameters.CustomParameterUI("Full Mode",
            toolTip = "Window with science options is presented")]
        public bool fullMode = false;

        [GameParameters.CustomFloatParameterUI("Science Gain Display Time (secs)", minValue = 5.0f, maxValue = 15, stepCount = 101, displayFormat = "F1", asPercentage = false,
            toolTip = "T" +
            "How long the message will be displayed on screen for each science gain (only for LITE and Medium modes)")]
        public float displayTime = 10;


        [GameParameters.CustomParameterUI("Initted",
            toolTip = "Set to false to have the selection window show up at the Space Center")]
        public bool selectionMade = false;


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (!active && member.Name != "active")
                return false;
            if (member.Name != "displayTime" || liteMode || mediumMode)
                return true;

            return false;

        }

        bool initted = false;
        bool oldLiteMode, oldMediumMode, oldFullMode;
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (initted)
            {
                if (oldLiteMode != liteMode)
                {
                    mediumMode = fullMode = false;
                    active = true;
                }
                else
                if (oldMediumMode != mediumMode)
                {
                    liteMode = fullMode = false;
                    active = true;
                }
                else
                if (oldFullMode != fullMode)
                {
                    liteMode = mediumMode = false;
                    active = true;
                }
                if (!active)
                    liteMode = mediumMode = fullMode = false;
            }
            oldLiteMode = liteMode;
            oldMediumMode = mediumMode;
            oldFullMode = fullMode;
            initted = true;

                return true;
        }

    }
}
