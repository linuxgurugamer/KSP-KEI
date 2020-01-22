using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.IO;

using ToolbarControl_NS;
using ClickThroughFix;

namespace KEI
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(KEI.MODID, KEI.MODNAME);
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class KEI : MonoBehaviour
    {
        private class AvailableExperiment
        {
            public ScienceExperiment experiment;
            public float possibleGain;
            public bool done;
        }

        public static string ROOT_PATH;
        private static string CONFIG_BASE_FOLDER;

        private static string KEI_BASE_FOLDER;
        private static string KEI_PLUGINDATA;



        public static KEI Instance;
        private bool isActive;

        private List<AvailableExperiment> availableExperiments;
        private List<ScienceExperiment> unlockedExperiments;
        private List<string> kscBiomes;
        private CelestialBody HomeBody;
        private string[] excludedExperiments = null;
        private string[] excludedManufacturers = null;

        //GUI related members
        //private ApplicationLauncherButton appLauncherButton;
        private int mainWindowId;
        private Rect mainWindowRect;
        private bool mainWindowVisible;
        private Vector2 mainWindowScrollPosition;

        //Public procedures
        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            ROOT_PATH = KSPUtil.ApplicationRootPath;
            CONFIG_BASE_FOLDER = ROOT_PATH + "GameData/";

            KEI_BASE_FOLDER = CONFIG_BASE_FOLDER + "KEI/";
            KEI_PLUGINDATA = KEI_BASE_FOLDER + "PluginData/";


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

                GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
                GameEvents.OnKSCFacilityUpgraded.Add(OnKSCFacilityUpgraded);
                GameEvents.onGUIRnDComplexSpawn.Add(SwitchOff);
                GameEvents.onGUIAstronautComplexSpawn.Add(SwitchOff);

                ModuleManagerPostLoad();
            }
        }
        //bool flag = false;
        void OnDestroy()
        {
            if (isActive)
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
                GameEvents.OnKSCFacilityUpgraded.Remove(OnKSCFacilityUpgraded);
                GameEvents.onGUIRnDComplexSpawn.Remove(SwitchOff);
                GameEvents.onGUIAstronautComplexSpawn.Remove(SwitchOff);
                //if (appLauncherButton != null)
                //	ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
                //flag = true;
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
                toolbarControl = null;
            }
        }

        private void OnKSCFacilityUpgraded(Upgradeables.UpgradeableFacility facility, int level)
        {
            Log.Info("OnKSCFacilityUpgraded");
            toolbarControl.SetFalse();
            //appLauncherButton.SetFalse();
        }

        private void SwitchOff()
        {
            toolbarControl.SetFalse();
            //appLauncherButton.SetFalse();
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
            if (SResearchAndDevelopment.GetExperiment("surfaceSample").IsUnlocked())
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
                        foreach (var biome in kscBiomes)
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
        }

        ToolbarControl toolbarControl;
        internal const string MODID = "KEI_NS";
        internal const string MODNAME = "Kerbin Environmental Institute";
        //GUI related functions
        private void OnAppLauncherReady()
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

        private void ShowMainWindow()
        {
            GetKscBiomes();
            GetExperiments();
            GainScience(unlockedExperiments, true);
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

            // appLauncherButton.SetFalse();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
