using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.IO;

using ToolbarControl_NS;
using KSP_Log;

namespace KEI
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        internal static Log Log = null;

        internal static string ROOT_PATH;
        internal static string CONFIG_BASE_FOLDER;

        internal static string KEI_BASE_FOLDER;
        internal static string KEI_PLUGINDATA;

        void Awake()
        {
#if DEBUG
            Log = new Log("KEI", Log.LEVEL.INFO);
#else
                Log = new Log("KEI", Log.LEVEL.ERROR);
#endif
        }
        void Start()
        {
            ROOT_PATH = KSPUtil.ApplicationRootPath;
            CONFIG_BASE_FOLDER = ROOT_PATH + "GameData/";

            KEI_BASE_FOLDER = CONFIG_BASE_FOLDER + "KEI/";
            KEI_PLUGINDATA = KEI_BASE_FOLDER + "PluginData/";


            ToolbarControl.RegisterMod(KEI.MODID, KEI.MODNAME);
        }
    }

}
