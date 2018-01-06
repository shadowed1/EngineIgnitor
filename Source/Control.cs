using System;
using System.Diagnostics;
using System.Reflection;
using KSP.UI.Screens;
using UnityEngine;

namespace EngineIgnitor
{
    [KSPAddon(KSPAddon.Startup.Flight | KSPAddon.Startup.EditorAny, false)]
    public class Control : MonoBehaviour
    {
        //internal static bool ToolbarIsStock = true;
       // internal static bool ToolbarTypeToggleActive = false;
        private const string BlizzyToolbarIconActive = "EngineIgnitor/Icons/ignitor_on_24";
        private const string BlizzyToolbarIconInactive = "EngineIgnitor/Icons/ignitor_off_24";
        private const string StockToolbarIconActive = "EngineIgnitor/Icons/ignitor_on_32";
        private const string StockToolbarIconInactive = "EngineIgnitor/Icons/ignitor_off_32";
       // private const string KeyToolbarIsStock = "toolbarIsStock";
        private const string KeyToolbarIconActive = "toolbarActiveIcon";
        private const string KeyToolbarIconInactive = "toolbarInactiveIcon";

        private const string ToolTip = "EngineIgnitor";
        private ApplicationLauncherButton engineIgnitorStockButton;
        private IButton engineIgnitorBlizzyButton;

        private string toolbarIconActive;
        private string toolbarIconInactive;

        public static bool IgnitorActive = true;

        enum ToolBarSelected { stock, blizzy, none };
        ToolBarSelected activeToolbarType = ToolBarSelected.none;

        void RemoveStockButton()
        {
            if (this.engineIgnitorStockButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(this.engineIgnitorStockButton);
                this.engineIgnitorStockButton = null;
            }
        }
        void RemoveBlizzyButton()
        {
            if (this.engineIgnitorBlizzyButton != null)
            {
                this.engineIgnitorBlizzyButton.Destroy();
                this.engineIgnitorBlizzyButton = null;
            }
        }
        void SetBlizzySettings()
        {
            if (activeToolbarType == ToolBarSelected.stock)
            {
                RemoveStockButton();
            }
            this.toolbarIconActive = BlizzyToolbarIconActive;
            this.toolbarIconInactive = BlizzyToolbarIconInactive;

            this.engineIgnitorBlizzyButton = ToolbarManager.Instance.add("EngineIgnitor", "engineIgnitorButton");
            this.engineIgnitorBlizzyButton.ToolTip = ToolTip;
            this.engineIgnitorBlizzyButton.OnClick += this.engineIgnitorButton_Click;
            this.engineIgnitorBlizzyButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.FLIGHT);


            activeToolbarType = ToolBarSelected.blizzy;
            this.UpdateToolbarIcon();
        }
        void SetStockSettings()
        {
            if (activeToolbarType == ToolBarSelected.blizzy)
            {
                RemoveBlizzyButton();
            }
            // Blizzy toolbar not available, or Stock Toolbar selected Let's go stock :(
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
            OnGUIAppLauncherReady();

            this.toolbarIconActive = StockToolbarIconActive;
            this.toolbarIconInactive = StockToolbarIconInactive;

            activeToolbarType = ToolBarSelected.stock;
            this.UpdateToolbarIcon();
        }
        public void Awake()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<EI>().allowTestMode)
                return;
            // Are we using Blizzy's Toolbar?
            //ToolbarIsStock = !HighLogic.CurrentGame.Parameters.CustomParams<EI>().useBlizzy;

            if (ToolbarManager.ToolbarAvailable && HighLogic.CurrentGame.Parameters.CustomParams<EI>().useBlizzy)
            {
                SetBlizzySettings();
            }
            else
            {
                SetStockSettings();
            }

        }

        public void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<EI>().allowTestMode)
                return;

            // this is needed because of a bug in KSP with event onGUIAppLauncherReady.
            if (activeToolbarType == ToolBarSelected.stock)
            {
                OnGUIAppLauncherReady();
            }
        }

        public void OnDestroy()
        {
            if (activeToolbarType == ToolBarSelected.stock)
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
                GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);

                RemoveStockButton();
            }
            else
            {
                RemoveBlizzyButton();
            }
        }

        private void UpdateToolbarIcon()
        {
            if (activeToolbarType == ToolBarSelected.blizzy)
            {
                this.engineIgnitorBlizzyButton.TexturePath = IgnitorActive ? this.toolbarIconActive : this.toolbarIconInactive;
            }
            else
            {
                this.engineIgnitorStockButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(IgnitorActive ? this.toolbarIconActive : this.toolbarIconInactive, false));
            }
        }
        private void OnGUIAppLauncherReady()
        {
            // Setup PW Stock Toolbar button
            bool hidden = false;
            if (ApplicationLauncher.Ready && (engineIgnitorStockButton == null || !ApplicationLauncher.Instance.Contains(engineIgnitorStockButton, out hidden)))
            {
                engineIgnitorStockButton = ApplicationLauncher.Instance.AddModApplication(
                    ToggleIgnitorActive,
                    ToggleIgnitorActive,
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                    (Texture)GameDatabase.Instance.GetTexture(StockToolbarIconActive, false));
                UpdateToolbarIcon();
                //engineIgnitorStockButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(IgnitorActive ? this.toolbarIconActive : this.toolbarIconInactive, false));
            }
        }

        private void OnGUIAppLauncherDestroyed()
        {
            if (engineIgnitorStockButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(engineIgnitorStockButton);
                engineIgnitorStockButton = null;
            }
        }

        void ToggleIgnitorActive()
        {
            IgnitorActive = !IgnitorActive;
            UpdateToolbarIcon();

        }
        private void engineIgnitorButton_Click(ClickEvent e)
        {
            this.ToggleIgnitorActive();
        }

        internal void ToolbarTypeToggle()
        {
            // ToolbarIsStock value has not yet changed, so we evaluate the value against the fact it will be changing.
            if (activeToolbarType == ToolBarSelected.stock)
            {
                // Was Stock bar, so let't try to use Blizzy's toolbar
                if (ToolbarManager.ToolbarAvailable)
                {
                    // Okay, Blizzy toolbar is available, so lets switch.
                    OnGUIAppLauncherDestroyed();
                    GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
                    GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
                   // ToolbarIsStock = false;
                    SetBlizzySettings();
                }
                else
                {
                    // We failed to activate the toolbar, so revert to stock
                    SetStockSettings();
                }
            }
            else
            {
                // Use stock Toolbar
                SetStockSettings();
            }
        }

        public void OnGUI()
        {
            if (ToolbarManager.ToolbarAvailable && activeToolbarType == ToolBarSelected.stock && HighLogic.CurrentGame.Parameters.CustomParams<EI>().useBlizzy)
            {
                ToolbarTypeToggle();
            }
            if ( (!ToolbarManager.ToolbarAvailable || !HighLogic.CurrentGame.Parameters.CustomParams<EI>().useBlizzy) && activeToolbarType == ToolBarSelected.blizzy)
            {
                ToolbarTypeToggle();
            }
        }
    }
}
