using System;
using KSP.IO;
using KSP.UI;
using UniLinq;
using UnityEngine;

namespace EngineIgnitor
{
	public class EngineIgnitorToolbox : PartModule
	{
        private StartState _startState = StartState.None;

        private double _engineIgnitorsAmount;
        private double _engineIgnitorsMaxAmount;
        private double _engineIgnitorsAmountEva;
        private double _engineIgnitorsMaxAmountEva;
        private readonly int _engineIgnitorsId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;

        public override void OnStart(StartState state)
	    {
	        _startState = state;
	    }

	    public override void OnUpdate()
	    {
            if (_startState == StartState.None || _startState == StartState.Editor) return;

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
                    eva.rootPart.RequestResource("EngineIgnitors", -1);
                    part.RequestResource("EngineIgnitors", 1);
                }
                else
                    ScreenMessages.PostScreenMessage("Can not take more...", 4.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
                ScreenMessages.PostScreenMessage("Requires engineer power.", 4.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }

#if false
    public class EngineIgnitorEVA : PartModule
    {
        private KerbalInfo _info = new KerbalInfo();
        private ProtoCrewMember _kerbal;

        public override void OnStart(StartState state)
        {
            _kerbal = vessel.GetVesselCrew().First();
            var evaKerbalExp = _kerbal.experienceTrait.Title;
            var evaKerbalMale = _kerbal.gender == ProtoCrewMember.Gender.Male;

            if (!evaKerbalExp.Equals("Engineer") || !evaKerbalMale)
            {
                part.RemoveResource("EngineIgnitors");
            }
            else
            {
                if (File.Exists<EngineIgnitorEVA>(_kerbal.name + ".sav"))
                {
                    using (var file = File.Open<EngineIgnitorEVA>(_kerbal.name + ".sav", FileMode.Open))
                    {
                        var buffer = new byte[file.Length];

                        file.Read(buffer, 0, buffer.Length);

                        var result = IOUtils.DeserializeFromBinary(buffer) as KerbalInfo;

                        if (result != null)
                        {
                            _info = result;
                            var currentTime = Planetarium.GetUniversalTime().GetHashCode();
                            var currentVessel = FlightGlobals.ActiveVessel.id.GetHashCode();

                            if (currentTime > _info.Time && currentVessel == _info.VesselId)
                            {
                                int resourceId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;
                                part.RequestResource(resourceId, -_info.FoundIgnitors);
                            }
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            double resourceAmount = 0f;
            double resourceMaxAmount = 0f;
            int resourceId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;
            if (part != null) part.GetConnectedResourceTotals(resourceId, out resourceAmount, out resourceMaxAmount);

            if (File.Exists<EngineIgnitorEVA>(_kerbal.name + ".sav"))
            {
                File.Delete<EngineIgnitorEVA>(_kerbal.name + ".sav");
            }
            if (resourceAmount > 0)
            {
                using (var file = File.Create<EngineIgnitorEVA>(_kerbal.name + ".sav"))
                {
                    var aaa1 = vessel;
                    var aaa2 = vessel.id;
                    var aaa3 = vessel.name;

                    var bbb1 = FlightGlobals.ActiveVessel;
                    var bbb2 = FlightGlobals.ActiveVessel.id.GetHashCode();
                    var bbb3 = FlightGlobals.ActiveVessel.name;

                    var info = new KerbalInfo
                    {
                        VesselId = FlightGlobals.ActiveVessel.id.GetHashCode(),
                        KerbalName = _kerbal.name,
                        Time = Planetarium.GetUniversalTime().GetHashCode(),
                        FoundIgnitors = resourceAmount
                    };

                    var result = IOUtils.SerializeToBinary(info);

                    file.Write(result, 0, result.Length);
                }
            }
        }
    }
#endif
}