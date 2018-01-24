using System.Collections.Generic;
using UnityEngine;
using System;

namespace EngineIgnitor
{
    public class ModuleExternalIgnitor : PartModule
    {
        public static List<ModuleExternalIgnitor> ExternalIgnitors = new List<ModuleExternalIgnitor>();

        [KSPField(isPersistant = false)]
        public int IgnitionsAvailable = -1;

        [KSPField(isPersistant = true)]
        public int IgnitionsRemained = -1;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Ignitions")]
        private string _ignitionsAvailableString = "Infinite";

        [KSPField(isPersistant = false)]
        public string IgnitorType = "universal";

        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Engines in range")]
        public string _enginesInRange = "NO";

        [KSPField(guiName = "Ignitor range", isPersistant = false, guiActiveEditor = true, guiActive = true, guiFormat = "N1")]
        public float IgniteRange = 1f;

        [KSPField(guiName = "Distance to engine", isPersistant = false, guiActiveEditor = true, guiActive = true, guiFormat = "N1")]
        public float _distanceToEngine;

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (ExternalIgnitors.Contains(this) == false)
                    ExternalIgnitors.Add(this);
            }
        }

        private void Update()
        {
            if (!Control.IgnitorActive)
                return;
            _distanceToEngine = 999;

            if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch.ship != null)
            {
                foreach (var p in EditorLogic.fetch.ship.parts)
                {
                    if (p.FindModuleImplementing<ModuleEngineIgnitor>() == true)
                    {
                        _distanceToEngine = Math.Min(_distanceToEngine, Vector3.Distance(part.transform.position, p.transform.position));
                    }
                }
                if (Math.Round(_distanceToEngine, 1) <= IgniteRange && EditorLogic.fetch.ship != null) _enginesInRange = "YES";
                else _enginesInRange = "NO";
            }
            else
            {
                foreach (var p in vessel.parts)
                {
                    if (p.FindModuleImplementing<ModuleEngineIgnitor>() == true)
                    {
                        _distanceToEngine = Math.Min(_distanceToEngine, Vector3.Distance(part.transform.position, p.transform.position));
                    }
                }
                if (Math.Round(_distanceToEngine, 1) <= IgniteRange && vessel != null) _enginesInRange = "YES";
                else _enginesInRange = "NO";
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (IgnitionsRemained != -1)
                    _ignitionsAvailableString = IgnitorType + " - " + IgnitionsRemained + "/" + IgnitionsAvailable;
                else
                    _ignitionsAvailableString = IgnitorType + " - " + "Infinite";

                if (vessel == null)
                {
                    ExternalIgnitors.Remove(this);
                }
            }
        }

        public override string GetInfo()
        {
            if (IgnitionsAvailable != -1)
                return "Can ignite for " + IgnitionsAvailable + " time(s).\n" + "Ignitor type: " + IgnitorType + "\n";
            else
                return "Can ignite for infinite times.\n" + "Ignitor type: " + IgnitorType + "\n";
        }
    }
}
