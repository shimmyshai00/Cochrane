// Author: Shimrra Shai
// Date Created: UE+1666.2000 Ms (2022-10-19)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using KSP.Localization;
using UnityEngine;

using SDF.SmartResources;

namespace Cochrane
{
    // The warp field generator states.
    public abstract class WarpFieldGeneratorState
    {
        public virtual void OnBoostWarpFactor() { }
        public virtual void OnReduceWarpFactor() { }
        public virtual void OnActivateWarpDrive() { }
        public virtual void OnDeactivateWarpDrive() { }
        public virtual void OnNacelleSwitchedOnOff() { }
    }

    
    // The warp core background module.
    [SDF.Resources(
        inputs = new string[] { "WarpPlasma" }, 
        outputs = new string[] { },
        internals = new string[] { })
    ]
    [SDF.FlowParams(flowGroupName = "WarpDrive")]
    public class WarpFieldGeneratorProcessor : FsmPartModule<WarpFieldGeneratorStateBase>
    {
        public WarpFieldGeneratorProcessor() : base(new WarpFieldGeneratorInactive()) { }

        public override bool IsStageable()
        {
            return true;
        }

        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_Cochrane_ModuleWarpFieldGenerator_ModuleName");
        }

        public void OnBoostWarpFactor()
        {
            Debug.Log("WarpFieldGeneratorProcessor: OnBoostWarpFactor");
            CurState.OnBoostWarpFactor();
        }

        public void OnReduceWarpFactor()
        {
            Debug.Log("WarpFieldGeneratorProcessor: OnReduceWarpFactor");
            CurState.OnReduceWarpFactor();
        }

        public void OnActivateWarpDrive()
        {
            Debug.Log("WarpFieldGeneratorProcessor: OnActivateWarpDrive");
            CurState.OnActivateWarpDrive();
        }

        public void OnDeactivateWarpDrive()
        {
            Debug.Log("WarpFieldGeneratorProcessor: OnDeactivateWarpDrive");
            CurState.OnDeactivateWarpDrive();
        }

        public void OnNacelleSwitchedOnOff()
        {
            Debug.Log("WarpFieldGeneratorProcessor: OnNacelleSwitchedOnOff");
            CurState.OnNacelleSwitchedOnOff();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            Debug.Log("WarpFieldGeneratorProcessor: OnFixedUpdate");
        }
    }
    
    // The warp field generator KSP module - just specifying the foreground KSPFields.
    public class WarpFieldGeneratorModule : SmartPartModule<WarpFieldGeneratorProcessor>
    {
        [KSPField]
        public int minNacelles = 2;

        [KSPField]
        public double maxWarpFactor = 2.0;

        [KSPField]
        public double warpAccel = 0.5; // warps/s

        [KSPField]
        public double warp1Gigawatts = 1.21; // GW

        [KSPField]
        public double maximumEngageField = 50.0; // μm/s^2

        // GUI
        [KSPField(groupName = "WarpFieldGeneratorPAW", guiName = "Status", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        public string generatorStatus = "Inactive";

        [KSPField(groupName = "WarpFieldGeneratorPAW", guiName = "Current Warp Factor: ", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        public string curWarpFactorDisplay = "1.0";

        [KSPField(groupName = "WarpFieldGeneratorPAW", guiName = "Target Warp Factor: ", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        public string targetWarpFactorDisplay = "1.0";

        [KSPField(groupName = "WarpFieldGeneratorPAW", guiName = "Drive Max Warp Factor: ", isPersistant = true, guiActiveEditor = true, guiActive = true)]
        public string maxWarpFactorDisplay = "2.0";

        [KSPField(groupName = "WarpFieldGeneratorPAW", guiName = "Local Gravitational Field: ", isPersistant = true, guiActiveEditor = true, guiActive = true)]
        public String gravFieldDisplay = "0.0 μm/s^2";

        [KSPField(groupName = "WarpFieldGeneratorPAW", guiName = "Field Limit: ", isPersistant = true, guiActiveEditor = true, guiActive = true)]
        public String maxGravFieldDisplay = "50.0 um/s^2";

        [KSPEvent(groupName = "WarpFieldGeneratorPAW", guiActive = true, active = true, guiActiveEditor = false, guiName = "Increase Warp Speed", guiActiveUnfocused = false)]
        public void BoostWarpFactor()
        {
            GetTypedBackgroundProcessor().OnBoostWarpFactor();
        }

        [KSPEvent(groupName = "WarpFieldGeneratorPAW", guiActive = true, active = true, guiActiveEditor = false, guiName = "Decrease Warp Speed", guiActiveUnfocused = false)]
        public void ReduceWarpFactor()
        {
            GetTypedBackgroundProcessor().OnReduceWarpFactor();
        }

        [KSPEvent(groupName = "WarpFieldGeneratorPAW", guiActive = true, active = true, guiActiveEditor = false, guiName = "Activate Warp Drive", guiActiveUnfocused = false)]
        public void ActivateWarpDrive()
        {
            GetTypedBackgroundProcessor().OnActivateWarpDrive();
        }

        [KSPEvent(groupName = "WarpFieldGeneratorPAW", guiActive = true, active = true, guiActiveEditor = false, guiName = "Drop Out of Warp", guiActiveUnfocused = false)]
        public void DeactivateWarpDrive()
        {
            GetTypedBackgroundProcessor().OnDeactivateWarpDrive();
        }

        public void OnNacelleSwitchedOnOff()
        {
            GetTypedBackgroundProcessor().OnNacelleSwitchedOnOff();
        }
    }
}
