
using UnityEngine;
using ToolbarControl_NS;

namespace EngineIgnitor
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(Control.MODID, Control.MODNAME);
        }
    }
}