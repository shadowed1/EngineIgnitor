using System.Collections.Generic;

namespace EngineIgnitor
{
	public class EngineWrapper
	{
		public bool isModuleEngineFX = false;
		private ModuleEngines engine = null;
		private ModuleEnginesFX engineFX = null;

		public EngineWrapper(ModuleEngines engine)
		{
			isModuleEngineFX = false;
			this.engine = engine;
		}

		public EngineWrapper(ModuleEnginesFX engineFX)
		{
			isModuleEngineFX = true;
			this.engineFX = engineFX;
		}

		public Vessel vessel
		{
			get
			{
				if(isModuleEngineFX == false)
					return engine.vessel;
				else
					return engineFX.vessel;
			}
		}

		public void SetRunningGroupsActive(bool active)
		{
			if (isModuleEngineFX == false)
				engine.SetRunningGroupsActive(active);
			// Do not need to worry about ModuleEnginesFX.
		}

        //DennyTX
        public bool EngineIgnited
        {
            get
            {
                if (isModuleEngineFX == false)
                    return engine.EngineIgnited;
                else
                    return engineFX.EngineIgnited;
            }
            //set { engineFX.EngineIgnited = value; }
        }

        public string status
        {
            get
            {
                if (isModuleEngineFX == false)
                    return engine.status;
                else
                    return engineFX.status;
            }
        }
 

        public float requestedThrust
		{
			get
			{
				if (isModuleEngineFX == false)
					return engine.requestedThrottle;
				else
					return engineFX.requestedThrottle;
			}
		}

		public bool throttleLocked
		{
			get
			{
				if (isModuleEngineFX == false)
					return engine.throttleLocked;
				else
					return engineFX.throttleLocked;
			}
		}

		public List<Propellant> propellants
		{
			get
			{
				if (isModuleEngineFX == false)
					return engine.propellants;
				else
					return engineFX.propellants;
			}
		}

		public Part part
		{
			get
			{
				if (isModuleEngineFX == false)
					return engine.part;
				else
					return engineFX.part;
			}
		}

		public BaseEventList Events
		{
			get
			{
				if (isModuleEngineFX == false)
					return engine.Events;
				else
					return engineFX.Events;
			}
		}

		public void BurstFlameoutGroups()
		{
			if (isModuleEngineFX == false)
				engine.BurstFlameoutGroups();
			else
				engineFX.part.Effects.Event(engineFX.flameoutEffectName,engineFX.transform.hierarchyCount);
		}

		public bool allowShutdown
		{
			get
			{
				if (isModuleEngineFX == false)
					return engine.allowShutdown;
				else
					return engineFX.allowShutdown;
			}
		}

		public bool flameout
		{ 
			get
			{
				if (isModuleEngineFX == false)
					return engine.flameout;
				else
					return engineFX.getFlameoutState;
			}
		}
	}
}
