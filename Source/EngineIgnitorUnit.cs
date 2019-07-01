using System;
using KSP.IO;
using KSP.UI;
using UniLinq;
using UnityEngine;

namespace EngineIgnitor
{
	public class EngineIgnitorToolbox : PartModule
	{

        private double _engineIgnitorsAmount;
        private double _engineIgnitorsMaxAmount;
        private double _engineIgnitorsAmountEva;
        private double _engineIgnitorsMaxAmountEva;
        private readonly int _engineIgnitorsId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;


	    public  void Update()
	    {
            if (HighLogic.LoadedSceneIsEditor) return;
            if (!Control.IgnitorActive)
                return;

            part.GetConnectedResourceTotals(_engineIgnitorsId, out _engineIgnitorsAmount, out _engineIgnitorsMaxAmount);

	        if (FlightGlobals.ActiveVessel != null)
	        {
                Events["TakeIgnitor"].guiActiveUnfocused = FlightGlobals.ActiveVessel.isEVA;
                Events["TakeIgnitor"].guiName = "Take Ignitor [" + _engineIgnitorsAmount + "/" + _engineIgnitorsMaxAmount + "]";
            }
	    }

        [KSPEvent(name = "TakeIgnitor", guiName = "Take Ignitor", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void TakeIgnitor()
        {
            var eva = FlightGlobals.ActiveVessel;
            var evaKerbalExp = eva.GetVesselCrew().First().experienceTrait.Title;
            eva.rootPart.GetConnectedResourceTotals(_engineIgnitorsId, out _engineIgnitorsAmountEva, out _engineIgnitorsMaxAmountEva);
            if (evaKerbalExp.Equals("Engineer"))
            {
                if (_engineIgnitorsAmount > 0 && _engineIgnitorsAmountEva < _engineIgnitorsMaxAmountEva)
                {
                    eva.rootPart.RequestResource("EngineIgnitors", (double)-1f);
                    part.RequestResource("EngineIgnitors", (double)1f);
                }
                else
                    ScreenMessages.PostScreenMessage("Can not take more...", 4.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
                ScreenMessages.PostScreenMessage("Requires engineer power.", 4.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}