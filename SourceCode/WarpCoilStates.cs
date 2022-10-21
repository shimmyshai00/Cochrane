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
    public abstract class WarpCoilStateBase : FsmState<WarpCoilStateBase>
    {
        public virtual void OnChargeCoil() { }
        public virtual void OnStopChargingCoil() { }
        public virtual void OnActivateCoil() { }
    }

    public class WarpCoilInactive : WarpCoilStateBase
    {
        public override void OnEnter()
        {
            PartModuleController.SetFieldValue<string>("coilStatus", "Not Charged");

            PartModuleController.Events["ChargeCoil"].guiActive = true;
            PartModuleController.Events["StopChargingCoil"].guiActive = false;
            PartModuleController.Events["ActivateCoil"].guiActive = false;
        }

        public override void OnChargeCoil()
        {
            PartModuleController.ChangeState(new WarpCoilCharging(0.0));
        }
    }

    public class WarpCoilCharging : WarpCoilStateBase
    {
        public double currentChargeGigajoules = 0.0;
        public double goalChargeGigajoules = 0.0;

        public WarpCoilCharging()
        {
            // needs a zero-argument constructor for loadiung
        }

        public WarpCoilCharging(double currentChargeGigajoules)
        {
            this.currentChargeGigajoules = currentChargeGigajoules;
        }

        public override void OnEnter()
        {
            // Get charge parameters in foreground
            goalChargeGigajoules = PartModuleController.GetFieldValue<double>("chargeRequiredGigajoules");

            // Start drawing warp plasma
            // Here's where that established 1 U WarpPlasma = 1 GJ conversion is handy
            double warpPlasmaReqFlowRate = PartModuleController.GetFieldValue<double>("chargerPowerGigawatts");
            PartModuleController.SetResourceConsumption("WarpPlasma", warpPlasmaReqFlowRate);
            Debug.Log("Cochrane: Requesting consume of " + warpPlasmaReqFlowRate.ToString() + " U/s WarpPlasma");

            // Update the status
            PartModuleController.SetFieldValue<string>("coilStatus", "Charging - ");

            PartModuleController.Events["ChargeCoil"].guiActive = false;
            PartModuleController.Events["StopChargingCoil"].guiActive = true;
            PartModuleController.Events["ActivateCoil"].guiActive = false;
        }

        public override void OnExit()
        {
            PartModuleController.SetResourceConsumption("WarpPlasma", 0.0);
        }

        public override void OnLoad(ConfigNode configNode) 
        { 
            configNode.TryGetValue("CurrentChargeGigajoules", ref currentChargeGigajoules);
            configNode.TryGetValue("GoalChargeGigajoules", ref goalChargeGigajoules);
        }
        
        public override void OnSave(ConfigNode configNode) 
        { 
            configNode.AddValue("CurrentChargeGigajoules", currentChargeGigajoules);
            configNode.AddValue("GoalChargeGigajoules", goalChargeGigajoules);
        }

        public override void OnStopChargingCoil()
        {
            PartModuleController.ChangeState(new WarpCoilPartiallyCharged(currentChargeGigajoules));
        }

        public override void OnFlow(Dictionary<string, PartModuleProcessor.ResourceFlowStat> flowStats, 
            double deltaTime) 
        { 
            // Contribute the inflow toward the charge goal.
            double warpPlasmaObtained = flowStats["WarpPlasma"].InFlowAmount;
            Debug.Log("Cochrane: Obtained " + warpPlasmaObtained.ToString() + " U of WarpPlasma");

            currentChargeGigajoules += warpPlasmaObtained;
            if(currentChargeGigajoules > goalChargeGigajoules)
            {
                currentChargeGigajoules = goalChargeGigajoules;
            }
        }

        public override void OnFixedUpdate()
        {
            double chargePercentage = currentChargeGigajoules / goalChargeGigajoules * 100.0;
            var coilStatus = "Charging - " + chargePercentage.ToString("0.0") + "%";
            PartModuleController.SetFieldValue<string>("coilStatus", coilStatus);

            if(currentChargeGigajoules == goalChargeGigajoules)
            {
                PartModuleController.ChangeState(new WarpCoilReady());
            }
        }
    }

    public class WarpCoilPartiallyCharged : WarpCoilStateBase
    {
        public double currentChargeGigajoules = 0.0;
        public double goalChargeGigajoules = 0.0;

        public WarpCoilPartiallyCharged()
        {
            // needs a zero-argument constructor for loadiung
        }

        public WarpCoilPartiallyCharged(double currentChargeGigajoules)
        {
            this.currentChargeGigajoules = currentChargeGigajoules;
        }

        public override void OnEnter()
        {
            goalChargeGigajoules = PartModuleController.GetFieldValue<double>("chargeRequiredGigajoules");
            
            // Update the status
            double chargePercentage = currentChargeGigajoules / goalChargeGigajoules * 100.0;
            var coilStatus = "Partially charged - " + chargePercentage.ToString("0.0") + "%";

            PartModuleController.SetFieldValue<string>("coilStatus", coilStatus);

            PartModuleController.Events["ChargeCoil"].guiActive = true;
            PartModuleController.Events["StopChargingCoil"].guiActive = false;
            PartModuleController.Events["ActivateCoil"].guiActive = false;
        }

        public override void OnLoad(ConfigNode configNode) 
        { 
            configNode.TryGetValue("CurrentChargeGigajoules", ref currentChargeGigajoules);
            configNode.TryGetValue("GoalChargeGigajoules", ref goalChargeGigajoules);
        }
        
        public override void OnSave(ConfigNode configNode) 
        { 
            configNode.AddValue("CurrentChargeGigajoules", currentChargeGigajoules);
            configNode.AddValue("GoalChargeGigajoules", goalChargeGigajoules);
        }

        public override void OnChargeCoil()
        {
            PartModuleController.ChangeState(new WarpCoilCharging(currentChargeGigajoules));
        }
    }

    public class WarpCoilReady : WarpCoilStateBase
    {
        public override void OnEnter()
        {
            PartModuleController.SetFieldValue<string>("coilStatus", "Ready");
            
            PartModuleController.Events["ChargeCoil"].guiActive = false;
            PartModuleController.Events["StopChargingCoil"].guiActive = false;
            PartModuleController.Events["ActivateCoil"].guiActive = true;
        }

        public override void OnActivateCoil()
        {
            PartModuleController.ChangeState(new WarpCoilActive());
        }
    }

    public class WarpCoilActive : WarpCoilStateBase
    {
        private double mExpectedWarpPlasmaRate = 0.0; // GW (or U/s warp plasma)
        private bool mWarpPlasmaRateChanged = false;
        private bool mWarpPlasmaInsufficency = false;

        public override void OnEnter()
        {
            mExpectedWarpPlasmaRate = PartModuleController.GetFieldValue<double>("demandGigawatts");
            PartModuleController.SetResourceConsumption("WarpPlasma", mExpectedWarpPlasmaRate);

            // Update the status
            PartModuleController.SetFieldValue<string>("coilStatus", "Active");

            PartModuleController.Events["ChargeCoil"].guiActive = false;
            PartModuleController.Events["StopChargingCoil"].guiActive = false;
            PartModuleController.Events["ActivateCoil"].guiActive = false;
        }

        public override void OnExit()
        {
            PartModuleController.SetResourceConsumption("WarpPlasma", 0.0);
        }

        public override void OnLoad(ConfigNode configNode) 
        { 
            configNode.TryGetValue("ExpectedWarpPlasmaRate", ref mExpectedWarpPlasmaRate);
            configNode.TryGetValue("WarpPlasmaRateChanged", ref mWarpPlasmaRateChanged);
            configNode.TryGetValue("WarpPlasmaInsufficiency", ref mWarpPlasmaInsufficency);
        }
        
        public override void OnSave(ConfigNode configNode) 
        { 
            configNode.AddValue("ExpectedWarpPlasmaRate", mExpectedWarpPlasmaRate);
            configNode.AddValue("WarpPlasmaRateChanged", mWarpPlasmaRateChanged);
            configNode.AddValue("WarpPlasmaInsufficiency", mWarpPlasmaInsufficency);
        }

        public override void OnFlow(Dictionary<string, PartModuleProcessor.ResourceFlowStat> flowStats, 
            double deltaTime) 
        { 
            // Check if we got what we need
            double expectedWarpPlasma = mExpectedWarpPlasmaRate * deltaTime;
            double obtainedWarpPlasma = flowStats["WarpPlasma"].InFlowAmount;

            if(obtainedWarpPlasma < 0.999*expectedWarpPlasma)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_WarpCoilFlameOut"), 5.0f,
                false);

                mWarpPlasmaInsufficency = true;
            }
        }

        public override void OnFixedUpdate()
        {
            if(mWarpPlasmaRateChanged)
            {
                mExpectedWarpPlasmaRate = PartModuleController.GetFieldValue<double>("demandGigawatts");
            }

            if(mWarpPlasmaInsufficency)
            {
                PartModuleController.ChangeState(new WarpCoilInactive());
            }
        }
    }
}