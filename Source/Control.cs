using KSP.UI.Screens;
using UnityEngine;
using ToolbarControl_NS;

namespace EngineIgnitor
{
    [KSPAddon(KSPAddon.Startup.Flight | KSPAddon.Startup.EditorAny, false)]
    public class Control : MonoBehaviour
    {
        internal const string MODID = "EngineIgnitor";
        internal const string MODNAME = "Engine Igniter";

        private const string BlizzyToolbarIconActive = "EngineIgnitor/Icons/ignitor_on_24";
        private const string BlizzyToolbarIconInactive = "EngineIgnitor/Icons/ignitor_off_24";
        private const string StockToolbarIconActive = "EngineIgnitor/Icons/ignitor_on_32";
        private const string StockToolbarIconInactive = "EngineIgnitor/Icons/ignitor_off_32";
        private const string KeyToolbarIconActive = "toolbarActiveIcon";
        private const string KeyToolbarIconInactive = "toolbarInactiveIcon";
        private const string ToolTip = "EngineIgnitor";
        public static bool IgnitorActive = true;

        ToolbarControl toolbarControl;

        public void Awake()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<EI>().allowTestMode)
                return;
        }

        public void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<EI>().allowTestMode)
                return;

            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(ToggleIgnitorActive, ToggleIgnitorActive,
                        ApplicationLauncher.AppScenes.FLIGHT |
                        ApplicationLauncher.AppScenes.MAPVIEW,
                        MODID,
                        "engineIgnitorButton",
                        StockToolbarIconInactive,
                        StockToolbarIconActive,
                        BlizzyToolbarIconInactive,
                        BlizzyToolbarIconActive,

                        MODNAME
                );

        }

        public void OnDestroy()
        {
            toolbarControl.OnDestroy();
            Destroy(toolbarControl);
        }

        void ToggleIgnitorActive()
        {
            IgnitorActive = !IgnitorActive;
            ScreenMessages.PostScreenMessage("IgnitorActive: " + IgnitorActive, 3f, ScreenMessageStyle.UPPER_CENTER);

        }
    }
}
