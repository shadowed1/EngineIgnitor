using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EngineIgnitor
{
	public class ModuleExternalIgnitor : PartModule
	{
		public static List<ModuleExternalIgnitor> s_ExternalIgnitors = new List<ModuleExternalIgnitor>();

		[KSPField(isPersistant = false)]
		public int ignitionsAvailable = -1;

		[KSPField(isPersistant = true)]
		public int ignitionsRemained = -1;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Ignitions")]
		private string ignitionsAvailableString = "Infinite";

		[KSPField(isPersistant = false)]
		public string ignitorType = "universal";

		[KSPField(isPersistant = false)]
		public float igniteRange = 1.5f;

        private StartState m_startState = StartState.None;

		public override void OnStart(StartState state)
		{
			m_startState = state;

			if (state != StartState.None && state != StartState.Editor)
			{
				if(s_ExternalIgnitors.Contains(this) == false)
					s_ExternalIgnitors.Add(this);
			}
			
			if (state == StartState.Editor)
			{
				ignitionsRemained = ignitionsAvailable;
			}
		}

		public override void OnUpdate()
		{
			if (m_startState != StartState.None && m_startState != StartState.Editor)
			{
				if (ignitionsRemained != -1)
					ignitionsAvailableString = ignitorType + " - " + ignitionsRemained.ToString() + "/" + ignitionsAvailable.ToString();
				else
					ignitionsAvailableString = ignitorType + " - " + "Infinite";

				if (this.vessel == null)
				{
					s_ExternalIgnitors.Remove(this);
				}
			}
		}

		public override string GetInfo()
		{
			if (ignitionsAvailable != -1)
				return "Can ignite for " + ignitionsAvailable.ToString() + " time(s).\n" + "Ignitor type: " + ignitorType + "\n";
			else
				return "Can ignite for infinite times.\n" + "Ignitor type: " + ignitorType + "\n";
		}
	}
}
