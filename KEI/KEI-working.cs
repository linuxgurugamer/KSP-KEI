#if true
using System;
using System.Collections;
using System.IO;
using System.Text;
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
    class KEI : MonoBehaviour
    {
        internal static KEI Instance;

        ToolbarControl toolbarControl;
        internal const string MODID = "KEI_NS";
        internal const string MODNAME = "Kerbin Environmental Institute";

        private bool isActive;

        private List<AvailableExperiment> availableExperiments;
        private List<ScienceExperiment> unlockedExperiments;

        private List<string> kscBiomes;
        private CelestialBody HomeBody;
        private string[] excludedExperiments = null;
        private string[] excludedManufacturers = null;

        //GUI related members
        private int mainWindowId;
        private Rect mainWindowRect;
        private bool mainWindowVisible;
        private Vector2 mainWindowScrollPosition;

        //Public procedures
        public void Awake()
        {
            Instance = this;
            isActive = false;
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                isActive = true;
                availableExperiments = new List<AvailableExperiment>();
                unlockedExperiments = new List<ScienceExperiment>();
                kscBiomes = new List<string>();
                mainWindowRect = new Rect();
                mainWindowScrollPosition = new Vector2();
                mainWindowVisible = false;
            }
        }


        public void ModuleManagerPostLoad()
        {
            if (excludedExperiments == null)
            {
                List<string> expList = new List<string>();
                ConfigNode[] excludedNode = GameDatabase.Instance.GetConfigNodes("KEI_EXCLUDED_EXPERIMENTS");

                if (excludedNode != null)
                {
                    for (int i = excludedNode.Length - 1; i >= 0; i--)
                    {
                        string[] types = excludedNode[i].GetValues("experiment");
                        expList.AddRange(types);
                    }
                }
                else
                    Log.Error("Missing config file");

                excludedExperiments = expList.Distinct().ToArray();

                foreach (var s in excludedExperiments)
                    Log.Info("Excluded experiment: " + s);

            }
            if (excludedManufacturers == null)
            {
                List<string> expList = new List<string>();
                ConfigNode[] excludedNode = GameDatabase.Instance.GetConfigNodes("KEI_EXCLUDED_MANUFACTURERS");
                if (excludedNode != null)
                {
                    for (int i = excludedNode.Length - 1; i >= 0; i--)
                    {
                        string[] types = excludedNode[i].GetValues("manufacturer");
                        expList.AddRange(types);
                    }
                }
                else
                    Log.Error("Missing config file");

                excludedManufacturers = expList.Distinct().ToArray();

                foreach (var s in excludedManufacturers)
                    Log.Info("Excluded manufacturer: " + s);
            }
        }

        public void Start()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                isActive = true;

                HomeBody = FlightGlobals.GetHomeBody();
                mainWindowId = GUIUtility.GetControlID(FocusType.Passive);
                mainWindowRect.width = 400;
                mainWindowRect.x = (Screen.width - 400) / 2;
                mainWindowRect.y = Screen.height / 4;
                mainWindowScrollPosition.Set(0, 0);

                InitializeToolbarControl();
                GameEvents.OnKSCFacilityUpgraded.Add(OnKSCFacilityUpgraded);
                GameEvents.onGUIRnDComplexSpawn.Add(SwitchOff);
                GameEvents.onGUIAstronautComplexSpawn.Add(SwitchOff);

                ModuleManagerPostLoad();
                StartCoroutine(DelayCheckMode());
            }
        }

        IEnumerator DelayCheckMode()
        {
            yield return new WaitForSeconds(2);
            CheckMode();

        }

        void OnDestroy()
        {
            if (isActive)
            {
                GameEvents.OnKSCFacilityUpgraded.Remove(OnKSCFacilityUpgraded);
                GameEvents.onGUIRnDComplexSpawn.Remove(SwitchOff);
                GameEvents.onGUIAstronautComplexSpawn.Remove(SwitchOff);
            }
            Instance = null;
        }

        private void OnKSCFacilityUpgraded(Upgradeables.UpgradeableFacility facility, int level)
        {
            toolbarControl.SetFalse();
            CheckMode();
        }

        private void SwitchOff()
        {
            if (toolbarControl != null)
                toolbarControl.SetFalse();
        }

        public void OnGUI()
        {
            if (!isActive) return;
            if (mainWindowVisible)
            {

                mainWindowRect = ClickThruBlocker.GUILayoutWindow(
                    mainWindowId,
                    mainWindowRect,
                    RenderMainWindow,
                    HomeBody.name + " Environmental Institute",
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(true)
                );
            }
        }

        void CheckMode()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().liteMode)
            {
                UpdateScienceInfo();
                availableExperiments.ForEach(x => x.done = true);
                GainScience(availableExperiments.Select(x => x.experiment).ToList(), false);
            }
            if (HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().mediumMode)
            {
                UpdateScienceInfo();
                availableExperiments.ForEach(x => x.done = true);
                GainCompletedScience(availableExperiments.Select(x => x.experiment).ToList());
            }

        }

        private void GetKscBiomes()
        {

            // Find KSC biomes - stolen from [x] Science source code :D
            kscBiomes.Clear();
            kscBiomes = UnityEngine.Object.FindObjectsOfType<Collider>()
                .Where(x => x.gameObject.layer == 15)
                .Select(x => x.gameObject.tag)
                .Where(x => x != "Untagged")
                .Where(x => !x.Contains("KSC_Runway_Light"))
                .Where(x => !x.Contains("KSC_Pad_Flag_Pole"))
                .Where(x => !x.Contains("Ladder"))
                .Select(x => Vessel.GetLandedAtString(x))
                .Select(x => x.Replace(" ", ""))
                .Distinct()
                .ToList();
        }

        // List available experiments
        private void GetExperiments()
        {

            unlockedExperiments.Clear();
            availableExperiments.Clear();


            List<AvailablePart> parts = PartLoader.Instance.loadedParts;

            // EVA Reports available from the beginning
            unlockedExperiments.Add(ResearchAndDevelopment.GetExperiment("evaReport"));

            // Don't check RnD level to determine if surface samples are available, in case another mod messes with the Facility Levels.
            //Instead just check the experiment is available directly.
            if (ResearchAndDevelopment.GetExperiment("surfaceSample").IsUnlocked())
                unlockedExperiments.Add(ResearchAndDevelopment.GetExperiment("surfaceSample"));

            foreach
            (
                AvailablePart part in parts.Where
                (
                    x => ResearchAndDevelopment.PartTechAvailable(x) &&
                        !excludedManufacturers.Contains(x.manufacturer) &&
                        ResearchAndDevelopment.PartModelPurchased(x)
                )
            )
            {
                // Part has some modules
                if (part.partPrefab.Modules != null)
                {
                    // Check science modules
                    foreach (ModuleScienceExperiment ex in part.partPrefab.Modules.OfType<ModuleScienceExperiment>())
                    {
                        if (ex.experimentID == null)
                        {
                            Log.Error("part's " + part.name + " experimentID is null");
                            continue;
                        }
                        // Remove experiments with empty ids, by [Kerbas-ad-astra](https://github.com/Kerbas-ad-astra)
                        // Remove Surface Experiments Pack experiments not meant to run in atmosphere
                        if (ex.experimentID != "" && !excludedExperiments.Contains(ex.experimentID))
                        {
                            unlockedExperiments.AddUnique<ScienceExperiment>(ResearchAndDevelopment.GetExperiment(ex.experimentID));
                        }
                    }
                }
            }
        }

        private void GainCompletedScience(List<ScienceExperiment> experiments)
        {
            List<ScienceExperiment> completedExperiments = new List<ScienceExperiment>();
            foreach (ScienceExperiment experiment in experiments.Where(x => x.IsAvailableWhile(ExperimentSituations.SrfLanded, HomeBody)))
            {
                foreach (string biome in kscBiomes)
                {

                    ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(
                        experiment,
                        ExperimentSituations.SrfLanded,
                        HomeBody,
                        biome,
                        null
                    );

                    if (subject.science > 0)
                        completedExperiments.Add(experiment);
                }
            }

            foreach (var experiment in completedExperiments)
            {
                float gain = 0.0f;
                foreach (string biome in kscBiomes)
                {
                    ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(
                        experiment,
                        ExperimentSituations.SrfLanded,
                        HomeBody,
                        biome,
                        null
                    );

                    if (subject.science < subject.scienceCap)
                    {
                        // We want to get full science reward
                        subject.subjectValue = 1.0f;
                        gain += ResearchAndDevelopment.Instance.SubmitScienceData(
                            subject.scienceCap * subject.dataScale,
                            subject
                        );

                    }
                }
                if (gain >= 0.01f)
                    Report(experiment, gain);
            }
        }

        private void GainScience(List<ScienceExperiment> experiments, bool analyze)
        {
            // Let's get science objects in all KSC biomes
            try
            {
                foreach (ScienceExperiment experiment in experiments.Where(x => x.IsAvailableWhile(ExperimentSituations.SrfLanded, HomeBody)))
                {
                    float gain = 0.0f;
                    try
                    {
                        foreach (string biome in kscBiomes)
                        {
                            try
                            {
                                ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(
                                    experiment,
                                    ExperimentSituations.SrfLanded,
                                    HomeBody,
                                    biome,
                                    null
                                );

                                if (subject.science < subject.scienceCap)
                                {
                                    if (analyze)
                                    {
                                        gain += (subject.scienceCap - subject.science) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
                                    }
                                    else
                                    {
                                        // We want to get full science reward
                                        subject.subjectValue = 1.0f;
                                        gain += ResearchAndDevelopment.Instance.SubmitScienceData(
                                            subject.scienceCap * subject.dataScale,
                                            subject
                                        );
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Exception found getting subject, message: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception found getting next biome, message: " + ex.Message);
                    }
                    if (gain >= 0.01f)
                    {
                        if (analyze)
                            availableExperiments.Add(
                                new AvailableExperiment
                                {
                                    experiment = experiment,
                                    possibleGain = gain,
                                    done = false
                                }
                            );
                        else
                            Report(experiment, gain);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception found getting next experiment, message: " + ex.Message);
            }
        }

        private void Report(ScienceExperiment experiment, float gain)
        {
            StringBuilder msg = new StringBuilder();
            string[] template;
            if (System.IO.File.Exists(KEI_PLUGINDATA + experiment.id + ".msg"))
            //if (File.Exists<KEI>(experiment.id + ".msg"))
            {
                template = System.IO.File.ReadAllLines(KEI_PLUGINDATA + experiment.id + ".msg");
                //template = File.ReadAllLines<KEI>(experiment.id + ".msg");

            }
            else
            {
                //template = File.ReadAllLines<KEI>("unknownExperiment.msg");
                template = System.IO.File.ReadAllLines(KEI_PLUGINDATA + "unknownExperiment.msg");

                msg.AppendLine("TOP SECRET!");
                msg.AppendLine("Project " + experiment.experimentTitle);
                msg.AppendLine("Eat this report after reading");
                msg.AppendLine("And drink some coffee");
                msg.AppendLine("****");
            }
            foreach (var line in template)
            {
                msg.AppendLine(line);
            }
            msg.AppendLine("");
            msg.AppendLine(string.Format("<color=#B4D455>Total science gain: {0}</color>", gain.ToString("0.00")));

            MessageSystem.Message message = new MessageSystem.Message(
                "New Email",
                msg.ToString(),
                MessageSystemButton.MessageButtonColor.GREEN,
                MessageSystemButton.ButtonIcons.MESSAGE
            );
            MessageSystem.Instance.AddMessage(message);
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().fullMode)
                ScreenMessages.PostScreenMessage(experiment.experimentTitle + " science gain: " + gain.ToString("0.00"), HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().displayTime);
        }

        //GUI related functions
        internal void InitializeToolbarControl()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<KEI_Settings>().fullMode)
            {
                if (toolbarControl == null)
                {
                    string abbr = HomeBody.name.Substring(0, 1) + "EI";
                    toolbarControl = gameObject.AddComponent<ToolbarControl>();
                    toolbarControl.AddToAllToolbars(ShowMainWindow, HideMainWindow,
                                ApplicationLauncher.AppScenes.SPACECENTER,
                                MODID,
                                "KEIButton",
                                "KEI/Textures/kei-icon_32",
                                "KEI/Textures/kei-icon_24",

                                abbr
                        );
                }
            }
            else
            {
                if (toolbarControl != null)
                {
                    toolbarControl.OnDestroy();
                    Destroy(toolbarControl);
                    toolbarControl = null;
                }
            }
        }

        void UpdateScienceInfo()
        {
            Log.Info("UpdateScienceInfo");
            GetKscBiomes();
            GetExperiments();
            GainScience(unlockedExperiments, true);
        }

        private void ShowMainWindow()
        {
            UpdateScienceInfo();
            mainWindowVisible = true;
        }

        private void HideMainWindow()
        {
            mainWindowVisible = false;
        }

        private void RenderMainWindow(int windowId)
        {
            GUILayout.BeginVertical();
            if (availableExperiments.Count > 0)
            {
                mainWindowScrollPosition = GUILayout.BeginScrollView(mainWindowScrollPosition, GUILayout.Height(Screen.height / 2));
                //				foreach (var available in availableExperiments)
                for (var i = 0; i < availableExperiments.Count; i++)
                {
                    if (availableExperiments[i].done) GUI.enabled = false;
                    if (GUILayout.Button(availableExperiments[i].experiment.experimentTitle + " " + availableExperiments[i].possibleGain.ToString("0.00"), GUILayout.Height(25)))
                    {
                        var l = new List<ScienceExperiment>();
                        l.Add(availableExperiments[i].experiment);
                        GainScience(l, false);
                        availableExperiments[i].done = true;
                    }
                    if (!GUI.enabled) GUI.enabled = true;
                }
                GUILayout.EndScrollView();
                GUILayout.Space(10);
                if (availableExperiments.Where(x => !x.done).Count() == 0) GUI.enabled = false;
                if (GUILayout.Button("Make me happy! " + availableExperiments.Where(x => !x.done).Select(x => x.possibleGain).Sum().ToString("0.00"), GUILayout.Height(25)))
                {
                    availableExperiments.ForEach(x => x.done = true);
                    GainScience(availableExperiments.Select(x => x.experiment).ToList(), false);
                }
                if (!GUI.enabled) GUI.enabled = true;
            }
            else
            {

                GUILayout.Label("Nothing to do here, go research something.");
            }
            if (GUILayout.Button("Close", GUILayout.Height(25)))
                toolbarControl.SetFalse();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
#endif