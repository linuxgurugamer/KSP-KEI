using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.IO;

using ToolbarControl_NS;
using ClickThroughFix;

using static KEI.RegisterToolbar;

namespace KEI
{

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class ModeSelectWindow : MonoBehaviour
    {
        //GUI related members
        private int winId;
        private Rect winRect;
        private CelestialBody HomeBody;

        bool oldLiteMode, oldMediumMode, oldFullMode;
        bool active, liteMode, mediumMode, fullMode;

        string abbr;

        bool visible = false;

        const string DISPLAY_TEXT = "\nLITE mode\tJust give all available local science,\n\t\tno window, no toolbar button\n\n" +
                "Medium Mode\tExperiment needs to be run one time at the\n\t\tKSC before all science for that experiment is\n\t\tgiven automatically, no window, no toolbar button\n\n" +
                "Full Mode\t\tWindow with science options is presented\n\n\n" +
                "Active\t\tIndicates mod is active in this game.  Automatically\n\t\tenabled if any mode is selected\n";
        void Start()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                visible = !(HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().selectionMade);

                HomeBody = FlightGlobals.GetHomeBody();
                abbr = HomeBody.name.Substring(0, 1) + "EI";

                winId = GUIUtility.GetControlID(FocusType.Passive);
                winRect.width = 600;
                winRect.height = 300;
                winRect.x = (Screen.width - winRect.width) / 2;
                winRect.y = (Screen.height - winRect.height) / 2;
            }
            else
            {
                visible = false;
                HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().active = false;
            }
        }

        public void OnGUI()
        {
            if (!visible) return;

            winRect = ClickThruBlocker.GUILayoutWindow(
                winId,
                winRect,
                RenderMainWindow,
                HomeBody.name + " Environmental Institute Mode Selection",
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );
        }

        private void RenderMainWindow(int windowId)
        {
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            GUILayout.TextArea(DISPLAY_TEXT);
            GUILayout.Space(20);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                liteMode = GUILayout.Toggle(liteMode, " LITE Mode");
                if (liteMode)
                    active = true;
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                mediumMode = GUILayout.Toggle(mediumMode, " Medium Mode");
                if (mediumMode)
                    active = true;
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                fullMode = GUILayout.Toggle(fullMode, " Full Mode");
                if (fullMode)
                    active = true;
            }
            GUILayout.Space(10);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                active = GUILayout.Toggle(active, " Active in this game");
                if (!active)
                    liteMode = mediumMode = fullMode = false;
            }

            GUILayout.Space(20);

            if (oldLiteMode != liteMode)
            {
                mediumMode = fullMode = false;
            }
            else
            {
                if (oldMediumMode != mediumMode)
                {
                    liteMode = fullMode = false;
                }
                else
                {
                    if (oldFullMode != fullMode)
                    {
                        liteMode = mediumMode = false;
                    }
                }
            }

            oldLiteMode = liteMode;
            oldMediumMode = mediumMode;
            oldFullMode = fullMode;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (liteMode | mediumMode | fullMode)
                    active = true;
                GUI.enabled = (active & (liteMode | mediumMode | fullMode)) || !active;
                if (GUILayout.Button(active ? "Confirm" : "Confirm " + abbr + " is disabled", GUILayout.Width(180)))
                {
                    visible = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().active = active;
                    HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().liteMode = liteMode;
                    HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().mediumMode = mediumMode;
                    HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().fullMode = fullMode;
                    HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().selectionMade = true;
                    KEI.Instance.InitializeToolbarControl();
                }
                GUI.enabled = false;
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}

