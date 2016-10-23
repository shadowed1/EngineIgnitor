using System.Collections.Generic;
using UniLinq;

namespace EngineIgnitor
{
	public class EngineIgnitorToolbox : PartModule
	{
        private StartState m_startState = StartState.None;

        private double EngineIgnitorsAmount;
        private double EngineIgnitorsMaxAmount;
        private double EngineIgnitorsAmountEVA;
        private double EngineIgnitorsMaxAmountEVA;
        private readonly int _engineIgnitorsId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;

        public override void OnStart(StartState state)
	    {
	        m_startState = state;
	    }

	    public override void OnUpdate()
	    {
            if (m_startState == StartState.None || m_startState == StartState.Editor) return;

            part.GetConnectedResourceTotals(_engineIgnitorsId, out EngineIgnitorsAmount, out EngineIgnitorsMaxAmount);

	        if (FlightGlobals.ActiveVessel != null)
	        {
                Events["TakeIgnitor"].guiActiveUnfocused = FlightGlobals.ActiveVessel.isEVA;
                Events["TakeIgnitor"].guiName = "Take Ignitor [" + EngineIgnitorsAmount + "/" + EngineIgnitorsMaxAmount + "]";
            }
	    }

        [KSPEvent(name = "TakeIgnitor", guiName = "Take Ignitor", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void TakeIgnitor()
        {
            var EVA = FlightGlobals.ActiveVessel;
            var EVAkerbalEXP = EVA.GetVesselCrew().First().experienceTrait.Title;
            EVA.rootPart.GetConnectedResourceTotals(_engineIgnitorsId, out EngineIgnitorsAmountEVA, out EngineIgnitorsMaxAmountEVA);
            if (EVAkerbalEXP.Equals("Engineer"))
            {
                if (EngineIgnitorsAmount > 0 && EngineIgnitorsAmountEVA < EngineIgnitorsMaxAmountEVA)
                {
                    EVA.rootPart.RequestResource("EngineIgnitors", -1);
                    part.RequestResource("EngineIgnitors", 1);
                }
                else
                    ScreenMessages.PostScreenMessage("Can not take more...", 4.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
                ScreenMessages.PostScreenMessage("Requires engineer power.", 4.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }

    public class EngineIgnitorHypergolicFluid : PartModule
    {
        private StartState m_startState = StartState.None;

        private double HypergolicFluidAmount;
        private double HypergolicFluidMaxAmount;
        private double HypergolicFluidAmountEVA;
        private double HypergolicFluidMaxAmountEVA;
        private readonly int _engineIgnitorsId = PartResourceLibrary.Instance.GetDefinition("HypergolicFluid").id;

        public override void OnStart(StartState state)
        {
            m_startState = state;
        }

        public override void OnUpdate()
        {
            if (m_startState == StartState.None || m_startState == StartState.Editor) return;

            part.GetConnectedResourceTotals(_engineIgnitorsId, out HypergolicFluidAmount, out HypergolicFluidMaxAmount);

            if (FlightGlobals.ActiveVessel != null)
            {
                Events["TakeHypergolicFluid"].guiActiveUnfocused = FlightGlobals.ActiveVessel.isEVA;
                Events["TakeHypergolicFluid"].guiName = "Take Hypergolic Fluid [" + HypergolicFluidAmount + "/" + HypergolicFluidMaxAmount + "]";
            }
        }

        [KSPEvent(name = "TakeHypergolicFluid", guiName = "Take Hypergolic Fluid", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void TakeIgnitor()
        {
            var EVA = FlightGlobals.ActiveVessel;
            var EVAkerbalEXP = EVA.GetVesselCrew().First().experienceTrait.Title;
            EVA.rootPart.GetConnectedResourceTotals(_engineIgnitorsId, out HypergolicFluidAmountEVA, out HypergolicFluidMaxAmountEVA);
            if (EVAkerbalEXP.Equals("Engineer"))
            {
                if (HypergolicFluidAmount > 0 && HypergolicFluidAmountEVA < HypergolicFluidMaxAmountEVA)
                {
                    EVA.rootPart.RequestResource("HypergolicFluid", -1);
                    part.RequestResource("HypergolicFluid", 1);
                }
                else
                    ScreenMessages.PostScreenMessage("Can not take more...", 4.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
                ScreenMessages.PostScreenMessage("Requires engineer power.", 4.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}