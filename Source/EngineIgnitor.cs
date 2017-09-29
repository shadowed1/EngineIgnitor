using System;
using System.Collections.Generic;
using System.Linq;
using KSP.IO;
using UnityEngine;

namespace EngineIgnitor
{
    public class OnboardIgnitorResource
    {
        public int Id;
        public string Name;
        public float Request;
        public double Amount;
        public double MaxAmount;
    }

    public class ModuleEngineIgnitor : PartModule
    {
        private bool _isEngineMouseOver;

        public enum EngineIgnitionState
        {
            INVALID = -1,
            NOT_IGNITED = 0,
            HIGH_TEMP = 1,
            IGNITED = 2,
        }

        public bool InRange;
        public bool IsExternal;

        [KSPField(isPersistant = false)]
        public int IgnitionsAvailable = -1; //-1: Infinite. 0: Unavailable. 1~...: As is.

        [KSPField(isPersistant = true)]
        public int IgnitionsRemained = -1;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Ignitions")]
        public string IgnitionsAvailableString = "";

        [KSPField(isPersistant = false)]
        public float AutoIgnitionTemperature = 800;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Auto-Ignite")]
        public string AutoIgnitionState = "?/800";

        // In case we have multiple engines...
        [KSPField(isPersistant = false)]
        public int EngineIndex = 0;

        [KSPField(isPersistant = false)]
        public string IgnitorType = "T0";

        [KSPField(isPersistant = false)]
        public bool UseUllageSimulation = true;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Flow")]
        public string UllageState;      

        private float _fuelFlowStability;
        private float _oldFuelFlowStability;

        // List of all engines. So we can pick the one we are corresponding to.
        private List<EngineWrapper> _engines = new List<EngineWrapper>();
        private EngineWrapper _engine = null;

        // A state for the FSM.
        [KSPField(isPersistant = false, guiActive = true, guiName = "Engine State")]
        private EngineIgnitionState _engineState = EngineIgnitionState.INVALID;

        private StartState _startState = StartState.None;

        public List<string> IgnitorResourcesStr;
        public List<IgnitorResource> IgnitorResources;


        public override void OnStart(StartState state)
        {
           
            _startState = state;
            _engines.Clear();
            foreach (PartModule module in part.Modules)
            {
                if (module is ModuleEngines)
                {
                    _engines.Add(new EngineWrapper(module as ModuleEngines));
                }
                if (module is ModuleEnginesFX)
                {
                    _engines.Add(new EngineWrapper(module as ModuleEnginesFX));
                }
            }
            _engine = _engines.Count > EngineIndex ? _engines[EngineIndex] : null;

            if (state == StartState.Editor) IgnitionsRemained = IgnitionsAvailable;

            IgnitorResources.Clear();
            foreach (string str in IgnitorResourcesStr) IgnitorResources.Add(IgnitorResource.FromString(str));

        }

        public override void OnAwake()
        {
            base.OnAwake();
            if (IgnitorResources == null) IgnitorResources = new List<IgnitorResource>();
            if (IgnitorResourcesStr == null) IgnitorResourcesStr = new List<string>();


            if (part.Modules.Contains("ModuleEngines") | part.Modules.Contains("ModuleEnginesFX")) //is part an engine?
            {
                foreach (PartModule pm in part.Modules) //change from part to partmodules
                {
                    if (pm.moduleName == "ModuleEngines") //find partmodule engine on th epart
                    {
                        em = (ModuleEngines)pm;
                        break;
                    }
                    if (pm.moduleName == "ModuleEnginesFX") //find partmodule engine on th epart
                    {
                        emfx = (ModuleEnginesFX)pm;
                        break;
                    }
                }
            }

        }

        public override string GetInfo()
        {
            if (IgnitionsAvailable != -1)
                return "Can ignite for " + IgnitionsAvailable + " time(s).\n" + "Ignitor type: " + IgnitorType + "\n";
            return "Can ignite for infinite times.\n" + "Ignitor type: " + IgnitorType + "\n";
        }

        public void OnMouseEnter()
        {
            if (HighLogic.LoadedSceneIsEditor) _isEngineMouseOver = true;
        }

        public void OnMouseExit()
        {
            if (HighLogic.LoadedSceneIsEditor) _isEngineMouseOver = false;
        }

        void OnGUI()
        {

            if (_isEngineMouseOver == false) return;

            string ignitorInfo = "Ignitor: ";

            if (IgnitionsRemained == -1) ignitorInfo += IgnitorType + "(Infinite).";
            else ignitorInfo += IgnitorType + " (" + IgnitionsRemained + ").";

            string resourceRequired = "No resource requirement for ignition.";

            if (IgnitorResources.Count > 0)
            {
                resourceRequired = "Ignition requires: ";
                for (int i = 0; i < IgnitorResources.Count; ++i)
                {
                    IgnitorResource resource = IgnitorResources[i];
                    resourceRequired += resource.Name + "(" + resource.Amount.ToString("F1") + ")";
                    if (i != IgnitorResources.Count - 1) resourceRequired += ", ";
                    else resourceRequired += ".";
                }
            }

            var ullageInfo = UseUllageSimulation ? "Need settling down fuel before ignition." : "Ullage simulation disabled.";

            Vector2 screenCoords = Camera.main.WorldToScreenPoint(part.transform.position);
            Rect ignitorInfoRect = new Rect(screenCoords.x - 100.0f, Screen.height - screenCoords.y - 10, 200.0f, 20.0f);
            GUIStyle ignitorInfoStyle = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold };
            ignitorInfoStyle.alignment = TextAnchor.MiddleCenter;
            ignitorInfoStyle.normal.textColor = Color.red;
            GUI.Label(ignitorInfoRect, ignitorInfo, ignitorInfoStyle);
            Rect ignitorResourceListRect = new Rect(screenCoords.x - 100.0f, Screen.height - screenCoords.y + 10.0f, 200.0f, 20.0f);
            GUI.Label(ignitorResourceListRect, resourceRequired, ignitorInfoStyle);
            Rect ullageInfoRect = new Rect(screenCoords.x - 100.0f, Screen.height - screenCoords.y + 30.0f, 200.0f, 20.0f);
            GUI.Label(ullageInfoRect, ullageInfo, ignitorInfoStyle);
        }

        private void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || _engine == null || !_engine.allowShutdown) return;

            if (vessel.Landed)
            {
                IsExternal = CheckExternalIgnitor();
                if (IsExternal)
                    IgnitionsAvailableString = "Provided from Ground" + " : [ " + IgnitionsRemained + "/" + IgnitionsAvailable + " ]";
                else if (IgnitionsRemained != -1)
                    IgnitionsAvailableString = IgnitorType + " : [ " + IgnitionsRemained + "/" + IgnitionsAvailable + " ]";
            }
            else
            {
                IsExternal = false;
                if (IgnitionsRemained != -1)
                    IgnitionsAvailableString = IgnitorType + " : [ " + IgnitionsRemained + "/" + IgnitionsAvailable + " ]";
            }


            if (part != null)
                AutoIgnitionState = part.temperature.ToString("F1") + "/" + AutoIgnitionTemperature.ToString("F1");
            else
                AutoIgnitionState = "?/" + AutoIgnitionTemperature.ToString("F1");



            var totalRes = new List<OnboardIgnitorResource>();
            for (int i = 0; i < IgnitorResources.Count; i++)
            {

                double resourceAmount = 0f;
                double resourceMaxAmount = 0f;
                int resourceId = PartResourceLibrary.Instance.GetDefinition(IgnitorResources[i].Name).id;


                if (part != null) part.GetConnectedResourceTotals(resourceId, out resourceAmount, out resourceMaxAmount);
                var foundResource = new OnboardIgnitorResource
                {
                    Id = resourceId,
                    Name = IgnitorResources[i].Name,
                    Request = IgnitorResources[i].Amount,
                    Amount = resourceAmount,
                    MaxAmount = resourceMaxAmount
                };
                totalRes.Add(foundResource);
            }


            if (FlightGlobals.ActiveVessel != null)
            {
                Events["ReloadIgnitor"].guiActiveUnfocused = FlightGlobals.ActiveVessel.isEVA;
                Events["ReloadIgnitor"].guiName = "Reload Ignitor (" + IgnitionsAvailableString + ")";
            }

            var oldState = _engineState;
            DecideNewState(oldState);

            _oldFuelFlowStability = _fuelFlowStability;
            CheckUllageState();

            var isIgnited = IgnitionProcess(oldState, IsExternal, totalRes);

            IgnitionResult(IsExternal, isIgnited);
        }

        private void DecideNewState(EngineIgnitionState oldState)
        {
            if (_engine.requestedThrust <= 0.0f || _engine.flameout || (_engine.EngineIgnited == false && _engine.allowShutdown))
            {
                if (_engine.part.temperature >= AutoIgnitionTemperature)
                    _engineState = EngineIgnitionState.HIGH_TEMP;
                else
                    _engineState = EngineIgnitionState.NOT_IGNITED;
            }
            else
            {
                if (oldState != EngineIgnitionState.IGNITED)
                {
                    //When changing from not-ignited to ignited, we must ensure that the throttle is non-zero or locked (SRBs)
                    if (vessel.ctrlState.mainThrottle > 0.0f || _engine.throttleLocked)
                        _engineState = EngineIgnitionState.IGNITED;
                    else
                        _engineState = EngineIgnitionState.NOT_IGNITED;
                }
            }
        }

        ModuleEngines em;
        ModuleEnginesFX emfx;

        private void CheckUllageState()
        {
            if (UseUllageSimulation)
            {
                Vector3d x = new Vector3d() ;

                if (em != null)
                    x = em.thrustTransforms[0].forward;
                if (emfx != null)
                    x = emfx.thrustTransforms[0].forward;
               
                Vector3d geeForceVector = vessel.obt_velocity - vessel.lastVel - vessel.graviticAcceleration / TimeWarp.fixedDeltaTime;
                var a = 180 - Vector3.Angle(x, geeForceVector);

                Log.Info("vessel.acceleration_immediate: " + vessel.acceleration_immediate);
                Log.Info("Angle: " + a);
                Log.Info("vessel.geeForce_immediate: " + vessel.geeForce_immediate + ", vessel.geeForce: " + vessel.geeForce);
                Log.Info("Math.Cos(a) * vessel.geeForce_immediate: " + (Math.Cos(a) * vessel.geeForce_immediate).ToString());
                if (a < 90 && Math.Cos(a) * vessel.geeForce_immediate >= 0.01 || vessel.Landed)
                {
                    UllageState = "Stable";
                    _fuelFlowStability = 1.0f;
                    return;
                }
                else
                {
                    Log.Info("Unstable");
                    _fuelFlowStability = (float)HighLogic.CurrentGame.Parameters.CustomParams<EI>().ChanceWhenUnstable / 100;
                    UllageState = "UnStable (Success Chance: " + HighLogic.CurrentGame.Parameters.CustomParams<EI>().ChanceWhenUnstable + "%)";
                    
                    return;
                }
            }
            else
            {
                UllageState = "Very Stable";
                _fuelFlowStability = 1.0f;
            }
            //return true;
        }

        private bool IgnitionProcess(EngineIgnitionState oldState, bool isExternal, List<OnboardIgnitorResource> aaa)
        {
            if (oldState == EngineIgnitionState.NOT_IGNITED && _engineState == EngineIgnitionState.IGNITED)
            {
                if (isExternal)
                {
                    IgnitionsRemained--;
                    return true;
                }

                if (IgnitionsRemained > 0 || IgnitionsRemained == -1)
                {
                    IgnitionsRemained--;
                    if (IgnitorResources.Count > 0)
                    {
                        string aa = null;
                        foreach (var a in aaa)
                        {
                            if (a.Amount >= a.Request)
                                part.RequestResource(a.Id, a.Request);
                            else
                            {
                                aa = a.Name;
                                break;
                            }
                        }
                        if (aa != null)
                        {
                            ScreenMessages.PostScreenMessage("DO NOT HAVE ENOUGH " + aa, 3f, ScreenMessageStyle.UPPER_CENTER);
                            _engineState = EngineIgnitionState.NOT_IGNITED;
                            return false;
                        }
                    }
                }

                float minPotential = 1.0f;
                if (UseUllageSimulation)
                {
                    minPotential *= _oldFuelFlowStability;
                    var chance = UnityEngine.Random.Range(0.0f, 1.0f);
                    var attempt = chance <= minPotential;
                    Debug.Log("EngineIgnitor: minPotential: " + minPotential.ToString() + ", chance: " + chance.ToString());
                    ScreenMessages.PostScreenMessage("Chance of ignition success: " + minPotential + ", Random: " + chance.ToString(), 5, ScreenMessageStyle.UPPER_CENTER);
                    if (!attempt)
                    {
                        ScreenMessages.PostScreenMessage("Ignition failed due to fuel flow instability", 3f, ScreenMessageStyle.UPPER_CENTER);
                        _engineState = EngineIgnitionState.NOT_IGNITED;
                        return false;
                    }
                }
            }
            if (oldState == EngineIgnitionState.HIGH_TEMP && _engineState == EngineIgnitionState.IGNITED)
            {
                _engineState = EngineIgnitionState.IGNITED;
                return true;
            }
            return true;
        }

        private void IgnitionResult(bool isExternal, bool isIgnited)
        {
            if (_engineState == EngineIgnitionState.NOT_IGNITED && ((IgnitionsRemained == 0 && !isExternal) || !isIgnited))
            {
                if (_engine.EngineIgnited)
                {
                    if (IgnitionsRemained == 0)
                        ScreenMessages.PostScreenMessage("NO AVAILABLE IGNITIONS", 3f, ScreenMessageStyle.UPPER_CENTER);
                    _engine.BurstFlameoutGroups();
                    _engine.SetRunningGroupsActive(false);
                    foreach (BaseEvent baseEvent in _engine.Events)
                    {
                        if (baseEvent.name.IndexOf("shutdown", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            baseEvent.Invoke();
                        }
                    }
                    _engine.SetRunningGroupsActive(false);
                }
            }
        }

        private bool CheckExternalIgnitor()
        {
            InRange = false;
            for (int i = 0; i < ModuleExternalIgnitor.ExternalIgnitors.Count; ++i)
            {
                ModuleExternalIgnitor itor = ModuleExternalIgnitor.ExternalIgnitors[i];
                if (itor.vessel == null || itor.vessel.transform == null || itor.part == null || itor.part.transform == null)
                {
                    ModuleExternalIgnitor.ExternalIgnitors.RemoveAt(i);
                    --i;
                }
            }
            foreach (ModuleExternalIgnitor extIgnitor in ModuleExternalIgnitor.ExternalIgnitors)
            {
                if (extIgnitor.vessel == null || extIgnitor.vessel.transform == null || extIgnitor.part == null ||
                    extIgnitor.part.transform == null)
                    ModuleExternalIgnitor.ExternalIgnitors.Remove(extIgnitor);
                Vector3 range = new Vector3();
                if (extIgnitor.vessel != null && extIgnitor.part != null && extIgnitor.vessel.transform != null)
                    range = extIgnitor.vessel.transform.TransformPoint(extIgnitor.part.orgPos) -
                             _engine.vessel.transform.TransformPoint(_engine.part.orgPos);
                InRange = range.magnitude < extIgnitor.IgniteRange;
            }
            if (InRange) return true;
            return false;
        }

        [KSPEvent(name = "ReloadIgnitor", guiName = "Reload Ignitor", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ReloadIgnitor()
        {
            if (IgnitionsAvailable == -1 || IgnitionsRemained == IgnitionsAvailable) return;
            var eva = FlightGlobals.ActiveVessel;
            var evaKerbalExp = eva.GetVesselCrew().First().experienceTrait.Title;
            double engineIgnitorsAmountEva = 0;
            double engineIgnitorsMaxAmountEva = 0;
            int engineIgnitorsId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;
            eva.rootPart.GetConnectedResourceTotals(engineIgnitorsId, out engineIgnitorsAmountEva, out engineIgnitorsMaxAmountEva);

            if (evaKerbalExp.Equals("Engineer"))
            {
                if (IgnitionsRemained < IgnitionsAvailable && engineIgnitorsAmountEva > 0)
                {
                    eva.rootPart.RequestResource("EngineIgnitors", 1);
                    IgnitionsRemained++;
                }
                else
                    ScreenMessages.PostScreenMessage("Nothing to load", 4.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
                ScreenMessages.PostScreenMessage("Requires engineer power.", 4.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        public override void OnSave(ConfigNode node)
        {
            foreach (IgnitorResource ignitorResource in IgnitorResources)
            {
                ignitorResource.Save(node.AddNode("IGNITOR_RESOURCE"));
            }
            base.OnSave(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            IgnitorResourcesStr = new List<string>();
            IgnitorResources = new List<IgnitorResource>();

            foreach (ConfigNode subNode in node.GetNodes("IGNITOR_RESOURCE"))
            {
                if (subNode.HasValue("name") == false || subNode.HasValue("amount") == false)
                {
                    continue;
                }
                IgnitorResource newIgnitorResource = new IgnitorResource();
                newIgnitorResource.Load(subNode);
                IgnitorResources.Add(newIgnitorResource);
                IgnitorResourcesStr.Add(newIgnitorResource.ToString());
            }
        }
    }
}
