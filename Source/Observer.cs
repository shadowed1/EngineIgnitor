using System;
using KSP.IO;
using KSP.UI.Screens;
using UnityEngine;

namespace EngineIgnitor
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class Observer : MonoBehaviour
    {
        void Start()
        {
            GameEvents.OnVesselRecoveryRequested.Add(OnRecovery);
        }

        void OnDestroy()
        {
            GameEvents.OnVesselRecoveryRequested.Remove(OnRecovery);
        }

        private void OnRecovery(Vessel data)
        {
            //asdf
            foreach (var kerbal in data.GetVesselCrew())
            {
                if (File.Exists<EngineIgnitorEVA>(kerbal.name + ".sav"))
                    File.Delete<EngineIgnitorEVA>(kerbal.name + ".sav");
            }
        }
    }
}
