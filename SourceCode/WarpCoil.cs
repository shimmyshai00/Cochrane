// Author: Shimrra Shai
// Date Created: UE+1666.1590 Ms (2022-10-18)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using KSP.Localization;
using UnityEngine;

using SDF.SmartResources;

namespace Cochrane
{
    // The warp coil states.
    public abstract class WarpCoilState
    {
        public virtual void OnActivateCore() { }
        public virtual void OnDeactivateCore() { }
        public virtual void OnFixedUpdate() { }
    }

    
    // The warp core background module.
    [SDF.Resources(
        inputs = new string[] { "WarpPlasma" },
        outputs = new string[] { },
        internals = new string[] { })
    ]
    [SDF.FlowParams(flowGroupName = "WarpDrive")]
    public class WarpCoilProcessor : FsmPartModule<WarpCoilStateBase>
    {
        public WarpCoilProcessor() : base(new WarpCoilInactive()) { }

        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_Cochrane_ModuleWarpCoil_ModuleName");
        }

        public void OnChargeCoil()
        {
            Debug.Log("WarpCoreProcessor: OnChargeCoil");
            CurState.OnChargeCoil();
        }

        public void OnStopChargingCoil()
        {
            Debug.Log("WarpCoreProcessor: OnStopChargingCoil");
            CurState.OnStopChargingCoil();
        }

        public void OnActivateCoil()
        {
            Debug.Log("WarpCoreProcessor: OnActivateCoil");
            CurState.OnActivateCoil();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            Debug.Log("WarpCoreProcessor: OnFixedUpdate");

            UpdateCoolGlo();
        }

        // From TheShadow's TrekDrive: get that cool-glo effect when charging
        private void UpdateCoolGlo()
        {
            if(ForegroundPart != null)
            {
                double chargeGigajoules = 0.0;
                double chargeRequiredGigajoules = GetFieldValue<double>("chargeRequiredGigajoules");

                if(CurState is WarpCoilPartiallyCharged)
                {
                    chargeGigajoules = (CurState as WarpCoilPartiallyCharged).currentChargeGigajoules;
                }
                else if(CurState is WarpCoilCharging)
                {
                    chargeGigajoules = (CurState as WarpCoilCharging).currentChargeGigajoules;
                }
                else if((CurState is WarpCoilReady) || (CurState is WarpCoilActive))
                {
                    chargeGigajoules = chargeRequiredGigajoules;
                }

                float colorValue = (float)(chargeGigajoules / chargeRequiredGigajoules);
                var glowColor = new Color(colorValue, colorValue, colorValue);
                ForegroundPart.GetPartRenderers().FirstOrDefault().material.SetColor("_EmissiveColor", glowColor);
            }
        }
    }
    
    // The warp core KSP module - just specifying the foreground KSPFields.
    public class WarpCoilModule : SmartPartModule<WarpCoilProcessor>
    {
        [KSPField]
        public double chargerPowerGigawatts = 0.1; // GW

        [KSPField]
        public double chargeRequiredGigajoules = 25.0; // GJ

        [KSPField]
        public double demandGigawatts = 0.0;

        // GUI
        [KSPField(groupName = "WarpCoilPAW", guiName = "Status", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        public string coilStatus = "Not Charged";

        [KSPEvent(groupName = "WarpCoilPAW", guiActive = true, active = true, guiActiveEditor = false, guiName = "Charge Warp Coil", guiActiveUnfocused = false)]
        public void ChargeCoil()
        {
            GetTypedBackgroundProcessor().OnChargeCoil();
        }

        [KSPEvent(groupName = "WarpCoilPAW", guiActive = true, active = true, guiActiveEditor = false, guiName = "Stop Charging", guiActiveUnfocused = false)]
        public void StopChargingCoil()
        {
            GetTypedBackgroundProcessor().OnStopChargingCoil();
        }

        [KSPEvent(groupName = "WarpCoilPAW", guiActive = false, active = true, guiActiveEditor = false, guiName = "Activate Warp Coil", guiActiveUnfocused = false)]
        public void ActivateCoil()
        {
            GetTypedBackgroundProcessor().OnActivateCoil();
        }

        public bool IsActive()
        {
            return GetTypedBackgroundProcessor().IsInState<WarpCoilActive>();
        }
    }
}
