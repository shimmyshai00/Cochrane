// Author: Shimrra Shai
// Date Created: UE+1665.1171 Ms (2022-10-06)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using KSP.Localization;
using UnityEngine;

using SDF.SmartResources;

namespace Cochrane
{
    // The warp core states.
    public abstract class WarpCoreState
    {
        public virtual void OnActivateCore() { }
        public virtual void OnDeactivateCore() { }
        public virtual void OnFixedUpdate() { }
    }

    
    // The warp core background module.
    [SDF.Resources(
        inputs = new string[] { "LqdDeuterium", "Antimatter" }, 
        outputs = new string[] { "WarpPlasma" },
        internals = new string[] { "Dilithium" })
    ]
    [SDF.FlowParams(flowGroupName = "WarpDrive")]
    public class WarpCoreProcessor : FsmPartModule<WarpCoreStateBase>
    {
        // physical properties of various materials
        public static float sWarpPlasmaDensity = PartResourceLibrary.Instance.GetDefinition("WarpPlasma").density;
        public static float sAntimatterDensity = PartResourceLibrary.Instance.GetDefinition("Antimatter").density;
        public static float sLqdDeuteriumDensity = PartResourceLibrary.Instance.GetDefinition("LqdDeuterium").density;

        public WarpCoreProcessor() : base(new WarpCoreInactive()) { }

        public override bool IsStageable()
        {
            return true;
        }

        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_Cochrane_ModuleWarpCore_ModuleName");
        }

        public void OnActivateCore()
        {
            Debug.Log("WarpCoreProcessor: OnActivateCore");
            CurState.OnActivateCore();
        }

        public void OnDeactivateCore()
        {
            Debug.Log("WarpCoreProcessor: OnDeactivateCore");
            CurState.OnDeactivateCore();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            Debug.Log("WarpCoreProcessor: OnFixedUpdate");

            if(CurState is WarpCoreActive)
            {
                UpdateCoolGlo(true);
            }
            else
            {
                UpdateCoolGlo(false);
            }
        }

        // From TheShadow's TrekDrive: get that cool-glo effect
        private void UpdateCoolGlo(bool gloStatus)
        {
            if(ForegroundPart != null)
            {
                float colorValue = 0.0f;
                if (gloStatus)
                {
                    colorValue = 1.0f;
                }

                var glowColor = new Color(colorValue, colorValue, colorValue);
                ForegroundPart.GetPartRenderers().FirstOrDefault().material.SetColor("_EmissiveColor", glowColor);
            }
        }
    }
    
    // The warp core KSP module - just specifying the foreground KSPFields.
    public class WarpCoreModule : SmartPartModule<WarpCoreProcessor>
    {
        [KSPField]
        public double coreGigawatts = 1.25; // GW

        [KSPField]
        public double coreEfficiency = 0.9975;

        [KSPField]
        public double damageTemperature = 1500; // K

        // GUI
        [KSPField(groupName = "WarpCorePAW", guiName = "Status", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        public string activationStatus = "Inactive";

        [KSPEvent(groupName = "WarpCorePAW", guiActive = true, active = true, guiActiveEditor = false, guiName = "Activate Warp Core", guiActiveUnfocused = false)]
        public void ActivateCore()
        {
            GetTypedBackgroundProcessor().OnActivateCore();
        }

        [KSPEvent(groupName = "WarpCorePAW", guiActive = false, active = true, guiActiveEditor = false, guiName = "Deactivate Warp Core", guiActiveUnfocused = false)]
        public void DeactivateCore()
        {
            GetTypedBackgroundProcessor().OnDeactivateCore();
        }
    }
}
