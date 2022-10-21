// Author: Shimrra Shai
// Date Created: UE+1665.1952 Ms (2022-10-07)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using KSP.Localization;
using UnityEngine;

using SDF.SmartResources;

namespace Cochrane
{
    // Defines a base for state machine states.
    public abstract class FsmState<TState> where TState : FsmState<TState>
    {
        private IFsmPartModuleController<TState> mPartModuleController = null;

        protected IFsmPartModuleController<TState> PartModuleController
        {
            get
            {
                return mPartModuleController;
            }
        }

        public void SetPartModuleController(IFsmPartModuleController<TState> partModuleController)
        {
            mPartModuleController = partModuleController;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }

        public virtual void OnLoad(ConfigNode node) { }
        public virtual void OnSave(ConfigNode node) { }

        public virtual void OnVesselWasModified() { }

        public virtual void OnFlow(Dictionary<string, PartModuleProcessor.ResourceFlowStat> flowStats, 
            double deltaTime) { }

        public virtual void OnFixedUpdate() { }
    }
}