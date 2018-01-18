
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;


namespace KEI
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    public class KEI_S : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "KEI"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "KEI"; } }
        public override string DisplaySection { get { return "KEI"; } }
        public override int SectionOrder { get { return 3; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomParameterUI("Use Blizzy Toolbar", toolTip = "If available")]
        public bool useBlizzy = false;




        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {

            return true;
        }


        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {

            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
