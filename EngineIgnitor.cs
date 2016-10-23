using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EngineIgnitor
{
	[Serializable]
	public class IgnitorResource : IConfigNode
	{
		[SerializeField]
		public string name;
		[SerializeField]
		public float amount;

		public float currentAmount;

        public void Load(ConfigNode node)
		{
			name = node.GetValue("name");
			if (node.HasValue("amount"))
			{
				amount = Mathf.Max(0.0f, float.Parse(node.GetValue("amount")));
			}
		}

		public void Save(ConfigNode node)
		{
			node.AddValue("name", name);
			node.AddValue("amount", Mathf.Max(0.0f, amount));
		}

		public override string ToString()
		{
			return name + "(" + amount.ToString("F3") + ")";
		}

		public static IgnitorResource FromString(string str)
		{
			IgnitorResource ir = new IgnitorResource();
			int indexL = str.LastIndexOf('('); int indexR = str.LastIndexOf(')');
			ir.name = str.Substring(0, indexL);
			ir.amount = float.Parse(str.Substring(indexL + 1, indexR - indexL - 1));
			return ir;
		}
	}

	public class ModuleEngineIgnitor : PartModule
	{
		public enum EngineIgnitionState
		{
			INVALID = -1,
			NOT_IGNITED = 0,
			HIGH_TEMP = 1,
			IGNITED = 2,
		}

        private double HypergolicFluidAmount = 0;
        private double HypergolicFluidMaxAmount = 0;
        private double HypergolicFluidAmountEVA = 0;
        private double HypergolicFluidMaxAmountEVA = 0;
        private double HypergolicFluidRemains = 0;
        private double EngineIgnitorsAmount;
        private double EngineIgnitorsMaxAmount;
        private double EngineIgnitorsAmountEVA;
        private double EngineIgnitorsMaxAmountEVA;
        private double ElectricChargeAmount = 0f;
        private double ElectricChargeMaxAmount = 0f;

        private bool _ignited = false;
        private bool m_isEngineMouseOver = false;

        private float fuelFlowStability;

        [KSPField(isPersistant = false)]
        public int ignitionsAvailable = -1; //-1: Infinite. 0: Unavailable. 1~...: As is.

        [KSPField(isPersistant = true)]
        public int ignitionsRemained = -1;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Ignitions")]
        public string ignitionsAvailableString = "Infinite";

		[KSPField(isPersistant = false)]
        public float autoIgnitionTemperature = 800;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Auto-Ignite")]
        public string autoIgnitionState = "?/800";

		// In case we have multiple engines...
		[KSPField(isPersistant = false)]
        public int engineIndex = 0;

		[KSPField(isPersistant = false)]
        public string ignitorType = "T0";

		[KSPField(isPersistant = false)]
        public bool useUllageSimulation;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Flow")]
        public string ullageState;

        [KSPField]
        public float chanceWhenUnstable;

        [KSPField]
        public int ECforIgnition = 2;

        // List of all engines. So we can pick the one we are corresponding to.
        private List<EngineWrapper> engines = new List<EngineWrapper>();

		// And that's it.
		private EngineWrapper engine = null;

		// A state for the FSM.
		[KSPField(isPersistant = false, guiActive = true, guiName = "Engine State")]
		private EngineIgnitionState engineState = EngineIgnitionState.INVALID;

		private StartState m_startState = StartState.None;

		public List<string> ignitorResourcesStr;
		public List<IgnitorResource> ignitorResources;

		public override void OnStart(StartState state)
		{
			m_startState = state;
			engines.Clear();
			foreach (PartModule module in this.part.Modules)
			{
				if (module is ModuleEngines)
				{
					engines.Add(new EngineWrapper(module as ModuleEngines));
				}
                //else
				if (module is ModuleEnginesFX)
				{
					engines.Add(new EngineWrapper(module as ModuleEnginesFX));
				}
			}
			if (engines.Count > engineIndex)
				engine = engines[engineIndex];
			else
				engine = null;

			if (state == StartState.Editor)
				ignitionsRemained = ignitionsAvailable;

			if (useUllageSimulation == false)
				ullageState = "Very Stable";

			ignitorResources.Clear();
			foreach (string str in ignitorResourcesStr)
				ignitorResources.Add(IgnitorResource.FromString(str));
		}

		public override void OnAwake()
		{
			base.OnAwake();
			if (ignitorResources == null)
				ignitorResources = new List<IgnitorResource>();
			if (ignitorResourcesStr == null)
				ignitorResourcesStr = new List<string>();
		}

		public override string GetInfo()
		{
			if (ignitionsAvailable != -1)
				return "Can ignite for " + ignitionsAvailable + " time(s).\n" + "Ignitor type: " + ignitorType + "\n";
			else
				return "Can ignite for infinite times.\n" + "Ignitor type: " + ignitorType + "\n";
		}

		public void OnMouseEnter()
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				m_isEngineMouseOver = true;
			}
		}

		public void OnMouseExit()
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				m_isEngineMouseOver = false;
			}
		}

        void OnGUI()
        {
            //Debug.Log("ModuleEngineIgnitor: OnGUI() " + ignitorResources.Count.ToString());
            if (m_isEngineMouseOver == false) return;

            string ignitorInfo = "Ignitor: ";
            if (ignitionsRemained == -1)
                ignitorInfo += ignitorType + "(Infinite).";
            else
                ignitorInfo += ignitorType + " (" + ignitionsRemained + ").";

            string resourceRequired = "No resource requirement for ignition.";
            if (ignitorResources.Count > 0)
            {
                resourceRequired = "Ignition requires: ";
                for (int i = 0; i < ignitorResources.Count; ++i)
                {
                    IgnitorResource resource = ignitorResources[i];
                    resourceRequired += resource.name + "(" + resource.amount.ToString("F1") + ")";
                    if (i != ignitorResources.Count - 1)
                    {
                        resourceRequired += ", ";
                    }
                    else
                    {
                        resourceRequired += ".";
                    }
                }
            }

            string ullageInfo;
            if (useUllageSimulation)
            {
                ullageInfo = "Need settling down fuel before ignition.";
            }
            else
            {
                ullageInfo = "Ullage simulation disabled.";
            }

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

		public override void OnUpdate()
		{
			if (m_startState == StartState.None || m_startState == StartState.Editor) return;
            if (engine == null) return;
            if (engine.allowShutdown == false) return;

            if (ignitionsRemained != -1)
				ignitionsAvailableString = ignitorType + " - [" + ignitionsRemained + "/" + ignitionsAvailable +"]";
			else
				ignitionsAvailableString = ignitorType + " - " + "Infinite";

			if (part != null)
				autoIgnitionState = part.temperature.ToString("F1") + "/" + autoIgnitionTemperature.ToString("F1");
			else
				autoIgnitionState = "?/" + autoIgnitionTemperature.ToString("F1");

            int EngineIgnitorsId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;
            part.GetConnectedResourceTotals(EngineIgnitorsId, out EngineIgnitorsAmount, out EngineIgnitorsMaxAmount);

            int HypergolicFluidId = PartResourceLibrary.Instance.GetDefinition("HypergolicFluid").id;
            engine.part.GetConnectedResourceTotals(HypergolicFluidId, out HypergolicFluidAmount, out HypergolicFluidMaxAmount);

            int ElectricityId = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
            part.GetConnectedResourceTotals(ElectricityId, out ElectricChargeAmount, out ElectricChargeMaxAmount);

            if (FlightGlobals.ActiveVessel != null)
            {
                Events["ReloadIgnitor"].guiActiveUnfocused = FlightGlobals.ActiveVessel.isEVA;
                Events["ReloadIgnitor"].guiName = "Reload Ignitor (" + ignitionsAvailableString + ")";
                Events["ReloadHypergolicFluid"].guiActiveUnfocused = FlightGlobals.ActiveVessel.isEVA;
                Events["ReloadHypergolicFluid"].guiName = "Reload Hypergolic Fluid (" + HypergolicFluidAmount + "/" + HypergolicFluidMaxAmount + ")";
            }

            float oldFuelFlowStability = fuelFlowStability;
		    CheckUllageSimulation();

            bool externalIgnitorAvailable = CheckExternalIgnitor();

            EngineIgnitionState oldState = engineState;
            // Decide new state.
            if (engine.requestedThrust <= 0.0f || engine.flameout == true || (engine.EngineIgnited == false && engine.allowShutdown == true))
            {
                if (engine.part.temperature >= autoIgnitionTemperature)
				{
					engineState = EngineIgnitionState.HIGH_TEMP;
				}
				else
				{
					engineState = EngineIgnitionState.NOT_IGNITED;
				}
			}
            else
            {
                if (oldState != EngineIgnitionState.IGNITED)
				{
					//When changing from not-ignited to ignited, we must ensure that the throttle is non-zero or locked (SRBs)
					if (vessel.ctrlState.mainThrottle > 0.0f || engine.throttleLocked == true)
					{
						engineState = EngineIgnitionState.IGNITED;
					}
				}
			}

            // This flag is for low-resource state.
            bool preferShutdown = false;

		    // Here comes the state transition process.
			if (oldState == EngineIgnitionState.NOT_IGNITED && engineState == EngineIgnitionState.IGNITED)
			{
				if (ignitionsRemained > 0 || ignitionsRemained == -1 || externalIgnitorAvailable == true)
				{
                    if (externalIgnitorAvailable == false)
                    {
                        if (ignitionsRemained > 0 && HypergolicFluidAmount > 0)
                        {
                            ignitionsRemained--;
                            part.RequestResource(ElectricityId, ECforIgnition);
                        }
                    }

                    if (ignitorResources.Count > 0)
					{
                        if (!externalIgnitorAvailable) // == true && externalIgnitor.provideRequiredResources == true))
						{
							foreach (IgnitorResource resource in ignitorResources)
							{
							    if (HypergolicFluidAmount >= resource.amount)
							    {
                                    resource.currentAmount = part.RequestResource(resource.name, resource.amount);
                                    if (ignitionsRemained == 0 && (HypergolicFluidAmount - resource.amount) > 0)
                                    {
                                        HypergolicFluidRemains = HypergolicFluidAmount - resource.amount;
                                    }
                                }
							    else
							    {
                                    engineState = EngineIgnitionState.NOT_IGNITED;
                                    preferShutdown = true;
                                }
                            }
                        }
                    }

                    float minPotential = 1.0f;
                    if (useUllageSimulation == true)
                    {
                        minPotential *= oldFuelFlowStability;
                        _ignited = UnityEngine.Random.Range(0.0f, 1.0f) <= minPotential;
                        if (_ignited == false)
                        {
                            engineState = EngineIgnitionState.NOT_IGNITED;
                            preferShutdown = true;
                        }
                    }
                }
            }
			else if (oldState == EngineIgnitionState.HIGH_TEMP && engineState == EngineIgnitionState.IGNITED)
			{ 
				// Yeah we can auto-ignite without consuming ignitor.
				engineState = EngineIgnitionState.IGNITED;
			}

            // Finally we need to handle the thrust generation. i.e. forcibly shutdown the engine when needed. WARNINGS.
            if (engineState == EngineIgnitionState.NOT_IGNITED && ((ignitionsRemained == 0 && externalIgnitorAvailable == false) || preferShutdown == true))
			{
                if (engine.EngineIgnited == true)
                {
                    engine.BurstFlameoutGroups();
					engine.SetRunningGroupsActive(false);

                    if (oldState != EngineIgnitionState.IGNITED)
                    {
                        part.RequestResource(ElectricityId, ECforIgnition);
                        if (HypergolicFluidAmount == 0 && ignitionsRemained > 0)
                        {
                            ignitionsRemained--;
                        }
                        if (HypergolicFluidRemains != 0)
                        {
                            foreach (IgnitorResource resource in ignitorResources)
                            {
                                resource.currentAmount = part.RequestResource(resource.name, resource.amount);
                            }
                        }
                    }
                    if (ignitionsRemained == 0)
                        ScreenMessages.PostScreenMessage("NO AVAILABLE IGNITIONS", 3f, ScreenMessageStyle.UPPER_CENTER);
                    if (HypergolicFluidAmount == 0)
                        ScreenMessages.PostScreenMessage("NOT IGNITED BECAUSE OF LUCK OF HYPERGOLIC", 3f, ScreenMessageStyle.UPPER_CENTER);
                    if (useUllageSimulation && !_ignited)
                        ScreenMessages.PostScreenMessage("NOT IGNITED BECAUSE OF FUEL FLOW UNSTABILITY", 3f, ScreenMessageStyle.UPPER_CENTER);

                    foreach (BaseEvent baseEvent in engine.Events)
					{
						if (baseEvent.name.IndexOf("shutdown", StringComparison.CurrentCultureIgnoreCase) >= 0)
						{
                            baseEvent.Invoke();
						}
					}
					engine.SetRunningGroupsActive(false);
				}
			}
        }


        private bool CheckExternalIgnitor()
        //private bool CheckExternalIgnitor(ModuleExternalIgnitor externalIgnitor)
	    {
	        bool inRange = false;
            for (int i = 0; i < ModuleExternalIgnitor.s_ExternalIgnitors.Count; ++i)
            {
                ModuleExternalIgnitor itor = ModuleExternalIgnitor.s_ExternalIgnitors[i];
                if (itor.vessel == null || itor.vessel.transform == null || itor.part == null || itor.part.transform == null)
                {
                    ModuleExternalIgnitor.s_ExternalIgnitors.RemoveAt(i);
                    --i;
                }
            }
            foreach (ModuleExternalIgnitor extIgnitor in ModuleExternalIgnitor.s_ExternalIgnitors)
	        {
	            if (extIgnitor.vessel == null || extIgnitor.vessel.transform == null || extIgnitor.part == null ||
	                extIgnitor.part.transform == null)
	                ModuleExternalIgnitor.s_ExternalIgnitors.Remove(extIgnitor);
	            inRange = (extIgnitor.vessel.transform.TransformPoint(extIgnitor.part.orgPos) -
	             engine.vessel.transform.TransformPoint(engine.part.orgPos)).magnitude < extIgnitor.igniteRange;
	        }
            if (inRange) 
            {
                return true;
            }
                return false;
        }

	    private void CheckUllageSimulation()
	    {
	        if (useUllageSimulation == true)
	        {
	            if (vessel.geeForce_immediate >= 0.01)
	            {
	                ullageState = "Stable";
	                fuelFlowStability = 1.0f;
	            }
	            else
	            {
	                ullageState = "UnStable (Chance " + chanceWhenUnstable + ")";
	                fuelFlowStability = chanceWhenUnstable;
	            }
	        }
	        else
	        {
	            ullageState = "Very Stable";
	            fuelFlowStability = 1.0f;
	        }
	    }

	    [KSPEvent(name = "ReloadIgnitor", guiName = "Reload Ignitor", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
		public void ReloadIgnitor()
	    {
            if (ignitionsAvailable == -1 || ignitionsRemained == ignitionsAvailable) return;

            var EVA = FlightGlobals.ActiveVessel;
		    var EVAkerbalEXP = EVA.GetVesselCrew().First().experienceTrait.Title;
            int EngineIgnitorsId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;
            EVA.rootPart.GetConnectedResourceTotals(EngineIgnitorsId, out EngineIgnitorsAmountEVA, out EngineIgnitorsMaxAmountEVA);

            if (EVAkerbalEXP.Equals("Engineer"))
	        {
	            if (ignitionsRemained < ignitionsAvailable && EngineIgnitorsAmountEVA > 0)
	            {
                    EVA.rootPart.RequestResource("EngineIgnitors", 1);
                    ignitionsRemained++;
                }
                else
                    ScreenMessages.PostScreenMessage("Nothing to load", 4.0f, ScreenMessageStyle.UPPER_CENTER);
            }
	        else
                ScreenMessages.PostScreenMessage("Requires engineer power.", 4.0f, ScreenMessageStyle.UPPER_CENTER);
		}

        [KSPEvent(name = "ReloadHypergolicFluid", guiName = "Reload Hypergolic Fluid", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ReloadHypergolicFluid()
        {
            if (HypergolicFluidAmount == HypergolicFluidMaxAmount) return;

            var EVA = FlightGlobals.ActiveVessel;
            var EVAkerbalEXP = EVA.GetVesselCrew().First().experienceTrait.Title;
            int HypergolicFluidId = PartResourceLibrary.Instance.GetDefinition("HypergolicFluid").id;
            EVA.rootPart.GetConnectedResourceTotals(HypergolicFluidId, out HypergolicFluidAmountEVA, out HypergolicFluidMaxAmountEVA);

            if (EVAkerbalEXP.Equals("Engineer"))
            {
                if (HypergolicFluidAmount < HypergolicFluidMaxAmount && HypergolicFluidAmountEVA > 0)
                {
                    EVA.rootPart.RequestResource("HypergolicFluid", 1);
                    part.RequestResource("HypergolicFluid", -1);
                }
                else
                    ScreenMessages.PostScreenMessage("Nothing to load", 4.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
                ScreenMessages.PostScreenMessage("Requires engineer power.", 4.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        public override void OnSave(ConfigNode node)
		{
			foreach (IgnitorResource ignitorResource in ignitorResources)
			{
				ignitorResource.Save(node.AddNode("IGNITOR_RESOURCE"));
			}
			base.OnSave(node);
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			ignitorResourcesStr = new List<string>();
			ignitorResources = new List<IgnitorResource>();

			foreach (ConfigNode subNode in node.GetNodes("IGNITOR_RESOURCE"))
			{
				if (subNode.HasValue("name") == false || subNode.HasValue("amount") == false)
				{
					continue;
				}
				IgnitorResource newIgnitorResource = new IgnitorResource();
				newIgnitorResource.Load(subNode);
				ignitorResources.Add(newIgnitorResource);
				ignitorResourcesStr.Add(newIgnitorResource.ToString());
			}
        }
	}
}
