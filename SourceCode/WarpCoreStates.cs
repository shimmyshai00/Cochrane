// Author: Shimrra Shai
// Date Created: UE+1653.6111 Ms (2022-05-26)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using KSP.Localization;
using UnityEngine;

using SDF.SmartResources;

namespace Cochrane
{
    public abstract class WarpCoreStateBase : FsmState<WarpCoreStateBase>
    {
        public virtual void OnActivateCore() { }
        public virtual void OnDeactivateCore() { }
    }

    public class WarpCoreInactive : WarpCoreStateBase
    {   
        public override void OnEnter()
        {
            Debug.Log("WarpCoreInactive OnEnter");

            PartModuleController.SetFieldValue<string>("activationStatus", "Inactive");

            PartModuleController.Events["ActivateCore"].guiActive = true;
            PartModuleController.Events["DeactivateCore"].guiActive = false;
        }

        public override void OnActivateCore()
        {
            PartModuleController.ChangeState(new WarpCoreActive());
        }
    }

    public class WarpCoreActive : WarpCoreStateBase
    {
        private bool mMatterDepleted = false;
        private bool mAntimatterDepleted = false;
        private bool mRegulatorFailure = false;

        public override void OnEnter()
        {
            Debug.Log("WarpCoreActive OnEnter");
            
            // Set the resource consumption required to generate a steady stream of warp plasma at the desired power
            // level.

            // Our head canon for how "warp plasma" works is that a large amount of matter fuel is used to soak up
            // the annihilation of a small portion thereof, which would otherwise produce pure gamma energy that 
            // could not be easily utilized, turning the rest into a plasma much hotter even than that in a fusion
            // reactor. This plasma is then contained and insulated using Star Trek magic to distribute it to needy 
            // systems. Because of the mass-energy equivalence the annihilation does not lose mass from the plasma, 
            // but rather just changes it from a low energy to an extremely energetic state. Thus to figure out the 
            // amount of matter consumed we just need to figure out how much buffer there is and we arbitrarily 
            // decided here 1000 GJ is buffered into 1 g of warp plasma, and 1 U of warp plasma stands for 1 GJ of
            // released energy.

            // Also, last but not least, we can't forget about the dilithium! Given that dilithium is magic Star
            // Trek handwavium, we can tweak it whatever way we please. Here, the numbers are chosen so that a 
            // "baseline" warp drive (1.25 GW, 99.75% efficiency) can run continuously for one Kerbal year before
            // suffering a breakdown, which is enough at warp 1.7 (approx. max speed) to travel 22 000 Tm from 
            // Kerbol.

            double coreGigawatts = PartModuleController.GetFieldValue<double>("coreGigawatts");
            double warpPlasmaGenerationRate = coreGigawatts;

            double rhoE = 1e+9; // now in GJ/t for consistency
            double warpPlasmaMassRateWanted = coreGigawatts / rhoE; // since 1 U WarpPlasma = 1 GJ
            double lqdDeuteriumMassRateWanted = warpPlasmaMassRateWanted;

            // you can guess what this is :)
            double c = 299792458.0; // in m/s

            double antimatterMassRateWanted = (coreGigawatts * 1e+6) / (c*c);

            double lqdDeuteriumRateWanted = lqdDeuteriumMassRateWanted / WarpCoreProcessor.sLqdDeuteriumDensity;
            double antimatterRateWanted = antimatterMassRateWanted / WarpCoreProcessor.sAntimatterDensity;

            double dilithiumLossRate = warpPlasmaGenerationRate / (1.25 * 0.9975 * 9203545.0);

            PartModuleController.SetResourceConsumption("LqdDeuterium", lqdDeuteriumRateWanted);
            PartModuleController.SetResourceConsumption("Antimatter", antimatterRateWanted);
            PartModuleController.SetResourceConsumption("Dilithium", dilithiumLossRate);
            PartModuleController.SetResourceProduction("WarpPlasma", warpPlasmaGenerationRate);

            PartModuleController.SetFieldValue<string>("activationStatus", "Active");

            PartModuleController.Events["ActivateCore"].guiActive = false;
            PartModuleController.Events["DeactivateCore"].guiActive = true;
        }

        public override void OnExit()
        {
            PartModuleController.SetResourceConsumption("LqdDeuterium", 0.0);
            PartModuleController.SetResourceConsumption("Antimatter", 0.0);
            PartModuleController.SetResourceConsumption("Dilithium", 0.0);
            PartModuleController.SetResourceProduction("WarpPlasma", 0.0);
        }

        public override void OnDeactivateCore()
        {
            PartModuleController.ChangeState(new WarpCoreInactive());
        }

        public void OnStarved(string ofWhat)
        {
            PartModuleController.ChangeState(new WarpCoreStarved(ofWhat));
        }

        public void OnRegulatorFailure()
        {
            PartModuleController.ChangeState(new WarpCoreUnsafe());
        }

        public override void OnFlow(Dictionary<string, PartModuleProcessor.ResourceFlowStat> flowStats, 
            double deltaTime) 
        { 
            Debug.Log("Active state SmartProcessResources deltaTime = " + deltaTime.ToString());
            
            // Check if we're still receiving matter and antimatter
            if(flowStats["LqdDeuterium"].InFlowAmount == 0.0)
            {
                mMatterDepleted = true;
            }
            
            if(flowStats["Antimatter"].InFlowAmount == 0.0)
            {
                mAntimatterDepleted = true;
            }
            
            // And the never-forget dilithium
            if(flowStats["Dilithium"].InFlowAmount == 0.0)
            {
                mRegulatorFailure = true;
            }

            // Finally, try to handle demand
            /*
            if(outDemandRates["WarpPlasma"] > 0.0)
            {
                fsm.StartResourceProduction("WarpPlasma", outDemandRates["WarpPlasma"]);
            }
            */
        }
        
        public override void OnFixedUpdate()
        {
            if(mRegulatorFailure)
            {
                OnRegulatorFailure();
            }
            else if(mMatterDepleted)
            {
                OnStarved("LqdDeuterium");
            }
            else if(mAntimatterDepleted)
            {
                OnStarved("Antimatter");
            }
        }
    }

    public class WarpCoreStarved : WarpCoreStateBase
    {
        private string mReason = "";

        public WarpCoreStarved(string reason)
        { 
            mReason = reason;
        }

        public override void OnEnter()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_WarpCoreFlameOut", mReason), 5.0f,
                false);

            PartModuleController.SetFieldValue<string>("activationStatus", "Flame-Out - " + mReason + " deprived");

            PartModuleController.Events["ActivateCore"].guiActive = false;
            PartModuleController.Events["DeactivateCore"].guiActive = true;
        }

        public override void OnDeactivateCore()
        {
            PartModuleController.ChangeState(new WarpCoreInactive());
        }
    }

    public class WarpCoreUnsafe : WarpCoreStateBase
    {
        public override void OnEnter()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_WarpCoreDilithiumToast"), 5.0f,
                false);

            PartModuleController.SetFieldValue<string>("activationStatus", "Unsafe - No Dilithium");

            PartModuleController.Events["ActivateCore"].guiActive = false;
            PartModuleController.Events["DeactivateCore"].guiActive = true;
        }

        public override void OnDeactivateCore()
        {
            PartModuleController.ChangeState(new WarpCoreInactive());
        }
    }
}