// Author: Shimrra Shai
// Date Created: UE+1666.2004 Ms (2022-10-19)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using KSP.Localization;
using UnityEngine;

using SDF.SmartResources;

// Aand YESSS!!! the most important component of them all...
namespace Cochrane
{
    public class WarpAccelerator
    {
        private IFsmPartModuleController<WarpFieldGeneratorStateBase> mPMC;
        private bool mIsSpeedUnlocked;

        public double CurWarpFactor { get; private set; }
        public double GoalWarpFactor { get; private set; }
        public double MaxWarpFactor { get; private set; }
        public double WarpAccel { get; private set; }

        public WarpAccelerator(IFsmPartModuleController<WarpFieldGeneratorStateBase> pmc,
            double curWarpFactor, double goalWarpFactor, double maxWarpFactor, double warpAccel)
        {
            mPMC = pmc;
            mIsSpeedUnlocked = false;

            CurWarpFactor = curWarpFactor;
            GoalWarpFactor = goalWarpFactor;
            MaxWarpFactor = maxWarpFactor;
            WarpAccel = warpAccel;
        }

        public void BoostWarpFactor()
        {
            GoalWarpFactor += 0.25;
            if(GoalWarpFactor > MaxWarpFactor)
            {
                GoalWarpFactor = MaxWarpFactor;
            }

            UpdateDisplay();
        }

        public void ReduceWarpFactor()
        {
            GoalWarpFactor -= 0.25;
            if(GoalWarpFactor < 0.25)
            {
                GoalWarpFactor = 0.25;
            }

            UpdateDisplay();
        }

        public void ZeroWarpFactor()
        {
            CurWarpFactor = 0.0;

            UpdateDisplay();
        }

        public void UnlockAcceleration()
        {
            mIsSpeedUnlocked = true;
        }

        public void LockAcceleration()
        {
            mIsSpeedUnlocked = false;
        }

        public virtual void OnSave(ConfigNode configNode)
        {
            ConfigNode accelNode = null;
            if(!configNode.HasNode("WarpAccelerator"))
            {
                accelNode = configNode.AddNode("WarpAccelerator");
            }
            else
            {
                accelNode = configNode.GetNode("WarpAccelerator");
            }

            accelNode.AddValue("IsSpeedUnlocked", mIsSpeedUnlocked);
            accelNode.AddValue("CurWarpFactor", CurWarpFactor);
            accelNode.AddValue("GoalWarpFactor", GoalWarpFactor);
        }

        public virtual void OnLoad(ConfigNode configNode)
        {
            if(configNode.HasNode("WarpAccelerator"))
            {
                ConfigNode accelNode = configNode.GetNode("WarpAccelerator");

                double curWarpFactor = 0.0;
                double goalWarpFactor = 0.0;

                accelNode.TryGetValue("IsSpeedUnlocked", ref mIsSpeedUnlocked);
                accelNode.TryGetValue("CurWarpFactor", ref curWarpFactor);
                accelNode.TryGetValue("GoalWarpFactor", ref goalWarpFactor);

                CurWarpFactor = curWarpFactor;
                GoalWarpFactor = goalWarpFactor;
            }
        }

        public virtual void OnFixedUpdate() 
        {
            if(mIsSpeedUnlocked)
            {
                if(CurWarpFactor != GoalWarpFactor)
                {
                    if(CurWarpFactor < GoalWarpFactor)
                    {
                        CurWarpFactor += WarpAccel*TimeWarp.fixedDeltaTime;
                        CurWarpFactor = Math.Min(CurWarpFactor, GoalWarpFactor);
                    }
                    else if(CurWarpFactor > GoalWarpFactor)
                    {
                        CurWarpFactor -= WarpAccel*TimeWarp.fixedDeltaTime;
                        CurWarpFactor = Math.Max(GoalWarpFactor, CurWarpFactor);
                    }

                    UpdateDisplay();
                }
            }
        }

        private void UpdateDisplay()
        {
            mPMC.SetFieldValue<string>("curWarpFactorDisplay", CurWarpFactor.ToString("0.0#"));
            mPMC.SetFieldValue<string>("targetWarpFactorDisplay", GoalWarpFactor.ToString("0.0#"));
            mPMC.SetFieldValue<string>("maxWarpFactorDisplay", MaxWarpFactor.ToString("0.0#"));
        }
    }

    public abstract class WarpFieldGeneratorStateBase : FsmState<WarpFieldGeneratorStateBase>
    {
        public virtual void OnBoostWarpFactor() { }
        public virtual void OnReduceWarpFactor() { }
        public virtual void OnActivateWarpDrive() { }
        public virtual void OnDeactivateWarpDrive() { }
        public virtual void OnNacelleSwitchedOnOff() { }

        protected void EnumerateNacelles(out int numInstalledNacelles, out int numActiveNacelles)
        {
            // --- Contains TheShadow's TrekDrive code ---
            // Note this counter does NOT work for unloaded vessels so we must be in FLIGHT mode.
            numInstalledNacelles = 0;
            numActiveNacelles = 0;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if(PartModuleController.ForegroundPart != null)
                {
                    Vessel v = PartModuleController.ForegroundPart.vessel;
                    uint persistentId = PartModuleController.ForegroundPart.persistentId;

                    //mWarpCoils.Clear();
                    // Debug.Log ("[TrekDrive] Drive Setup Started.");

                    for (int i = 0; i < v.parts.Count; i++)
                    {
                        PartModuleList partModules = v.parts[i].Modules;
                        for (int j = 0; j < partModules.Count; j++)
                        {
                            if (partModules[j] is WarpCoilModule && v.parts[i].persistentId != persistentId)
                            {
                                numInstalledNacelles += 1;
                                if((partModules[j] as WarpCoilModule).IsActive())
                                {
                                    numActiveNacelles++;
                                }

                                //mWarpCoils.Add(partModules[j] as WarpCoilModule);
                                break;
                            }
                        }
                    }
                }
            }
            // -------------------------------------------
        }

        protected double GetLocalGravField()
        {
            // NOTE: no background processing support yet
            if(PartModuleController.ForegroundPart != null)
            {
                Vessel v = PartModuleController.ForegroundPart.vessel;
                Vector3d totalGravVector = new Vector3d(0.0, 0.0, 0.0);
                OrbitDriver curOrbitDriver = v.GetOrbitDriver();
                while((curOrbitDriver != null) && (curOrbitDriver.referenceBody != null))
                {
                    //Debug.Log("Orbit driver for body: " + curOrbitDriver.referenceBody.bodyDisplayName);

                    // This gives the radial vector from the center of the celestial body
                    Vector3d r = curOrbitDriver.pos;                    
                    //Debug.Log(" - Driver pos: " + r.ToString());

                    // Find the gravitational acceleration by Newton's gravitational law
                    const double G = 6.67430e-11;
                    Vector3d rUnit = Vector3d.Normalize(r);
                    //Debug.Log(" - Driver unit: " + rUnit.ToString());
                    double dist = Vector3d.Magnitude(r);
                    //Debug.Log(" - Driver dist: " + dist.ToString());
                    //Debug.Log(" - Driver mass: " + curOrbitDriver.referenceBody.Mass.ToString());
                    Vector3d gravVector = -(G*curOrbitDriver.referenceBody.Mass)/(dist*dist) * rUnit;
                    //Debug.Log(" - Driver contribution grav: " + gravVector.ToString());
                    totalGravVector += gravVector;

                    curOrbitDriver = curOrbitDriver.referenceBody.orbitDriver;
                }

                return Vector3d.Magnitude(totalGravVector) * 1000000.0;
            }
            else
            {
                return 0.0;
            }
        }

        protected String FormatGravity(double gravity)
        {
            return gravity.ToString("0.0") + " μm/s^2";
        }

        protected bool CheckGravity()
        {
            double gravRequired = PartModuleController.GetFieldValue<double>("maximumEngageField");
            return GetLocalGravField() < gravRequired;
        }

        protected void UpdateGravityDisplay()
        {
            double gravRequired = PartModuleController.GetFieldValue<double>("maximumEngageField");

            PartModuleController.SetFieldValue<String>("gravFieldDisplay", 
                GetLocalGravField().ToString("0.0") + " μm/s^2");
            PartModuleController.SetFieldValue<String>("maxGravFieldDisplay", 
                gravRequired.ToString("0.0") + " μm/s^2");
        }
    }

    public class WarpFieldGeneratorInactive : WarpFieldGeneratorStateBase
    {
        private WarpAccelerator mWarpAccel = null;

        public WarpFieldGeneratorInactive() { }

        public WarpFieldGeneratorInactive(WarpAccelerator warpAccel)
        {
            mWarpAccel = warpAccel;
        }
        
        public override void OnEnter()
        {
            Debug.Log("Cochrane: WarpFieldGenerator Inactive");

            if(mWarpAccel == null)
            {
                mWarpAccel = new WarpAccelerator(PartModuleController, 0.0, 1.0, 
                    PartModuleController.GetFieldValue<double>("maxWarpFactor"),
                    PartModuleController.GetFieldValue<double>("warpAccel"));
            }
                
            // Update status
            PartModuleController.SetFieldValue<string>("generatorStatus", "Inactive");

            PartModuleController.Events["ActivateWarpDrive"].guiActive = true;
            PartModuleController.Events["DeactivateWarpDrive"].guiActive = false;
        }

        public override void OnActivateWarpDrive()
        {
            /*
            Debug.Log("NumInstalledNacelles = " + fsm.NumInstalledNacelles.ToString());
            Debug.Log("NumActiveNacelles = " + fsm.NumActiveNacelles.ToString());
            Debug.Log("MinNacelles = " + fsm.MinNacelles.ToString());

            if (fsm.NumInstalledNacelles < fsm.MinNacelles)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_NotEnoughWarpCoils"), 
                    5.0f, false);
            }
            else if (fsm.NumActiveNacelles < fsm.MinNacelles)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_CoilsInactive"), 5.0f, 
                    false);
            }
            else
            {
                fsm.ChangeState(new WarpFieldGeneratorActive());
            }
            */

            // In order to jump to warp, enough warp nacelles must be both installed AND active.
            int numRequiredNacelles = PartModuleController.GetFieldValue<int>("minNacelles");
            int numInstalledNacelles = 0;
            int numActiveNacelles = 0;

            EnumerateNacelles(out numInstalledNacelles, out numActiveNacelles);

            if(numActiveNacelles >= numRequiredNacelles)
            {
                if(!CheckGravity())
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_GravityTooHigh"), 
                        5.0f, false);
                }
                else
                {
                    PartModuleController.ChangeState(new WarpFieldGeneratorActive(mWarpAccel));
                }
            }
            else
            {
                if (numInstalledNacelles < numRequiredNacelles)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_NotEnoughWarpCoils"), 
                        5.0f, false);
                }
                else if (numActiveNacelles < numRequiredNacelles)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_CoilsInactive"), 5.0f, 
                        false);
                }
            }
        }

        public override void OnBoostWarpFactor()
        {
            mWarpAccel.BoostWarpFactor();
        }

        public override void OnReduceWarpFactor()
        {
            mWarpAccel.ReduceWarpFactor();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            mWarpAccel.OnSave(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if(mWarpAccel != null)
            {
                mWarpAccel.OnLoad(node);
            }
        }

        public override void OnFixedUpdate()
        {
            /*
            if(mNacelleStatsNeedUpdate)
            {
                //fsm.UpdateNacelles();
                mNacelleStatsNeedUpdate = false;
            }
            */
            mWarpAccel.OnFixedUpdate();

            // Gravity field monitoring
            UpdateGravityDisplay();
        }
    }

    public class WarpFieldGeneratorActive : WarpFieldGeneratorStateBase
    {
        private bool mWarpPowerChanged = false;
        private bool mWarpPlasmaInsufficiency = false;
        private bool mNacelleFailure = false;
        private bool mGravityTooHigh = false;

        private WarpAccelerator mWarpAccel;

        // The fun stuff! This calculates the power draw and speed for a given warp factor. This is based off the
        // following famous graph:
        //
        //     https://www.calormen.com/star_trek/FAQs/warp_velocities-faq.htm
        //
        // The graph gives the log of the speed in lightspeeds versus the warp factor, and the trick is that while 
        // for warps up to 9 it is fairly pedestrian, from 9 to 10 it for some reason trails up and shoots to 
        // infinite speed at warp 10(!). Given this "convention" is used throughout most of the Star Trek canon, 
        // but was not in the original, and it's more fun, it's the one we will use here (however this author is not
        // sure if he'll push this mod far enough to do warp drives that can actually get to such a high #warp 
        // level.). Hence, the only trick is to find a fitting formula - the problem is, the graph wasn't 
        // originally generated by an equation - it was drawn by hand! Hence, we need to find an equation whose
        // graph suitably approximates it, which can then be used to create a function for the conversion from warp
        // factors to lightspeeds. Fortunately, some aficionados cited there came up with fairly close formulae and
        // we use one of them below.
        // 
        // What's particularly fun, though, and relevant for gameplay purposes, is the power graph. The 
        // power requirement has a "choppy" graph where that it rises up before each warp level then suddenly drops,
        // meaning that once you pass a level there is a "relief" on your drive systems and you can coast kinda 
        // fast. This mechanic looked interesting enough to this author for implementation, though it's important 
        // to note that the author does not trust the units on the right, because for one it's variously described
        // as either megajoules or megawatts, creating inconsistency as to what it even means, it also seems weird
        // that it's given as energy/power *per* unit of warp field, i.e. "cochranes", and in the canon it's
        // described that one cochrane of warp field is that required to go warp one, so there should be a
        // well-defined mapping from cochrane values to warp factors and conversely, and thus it makes the
        // intent of the graph unclear - is it meant to be the power to sustain a speed, or is it meant to be a
        // *derivative* of power, i.e. how much *extra* power is required to go faster? Because it produces a
        // cooler mechanic though, we will take the straight interpretation and liberty here and just normalize that
        // the Mighty Mite Mk. 1 warp system in this mod uses 1.21 GW :) to sustain warp 1 once suitably far from
        // a gravitational field source - that is, we take that the first peak on the graph is 1.21 GW, no 
        // ambiguity, for that particular warp system, and that in the limit of vanishing craft mass. Moreover, we
        // don't imagine an "instantaneous jump" to warp, but instead a gradual coast-up phase, so that there is
        // a transient 1.21 GW spike power required to "break the warp barrier". (NB: all this is TBA for later -
        // right now we just support speed up to warp 1 and leave it at that)
        // 
        // (Also TBA) Likewise, as the craft #mass is increased *and* so is the proximity to a gravitational well,
        // the power requirements increase. Over 100 tonnes of craft mass and the drive starts to demand 
        // proportionately greater power, so the Mighty Mite Mk. 1 system with 1.25 GW ceiling, just barely enough
        // to clear warp 1, cannot push a craft much larger than this. The gravitational-well mechanic is used as
        // a balancing technique so as to not let the warp drive ruin in-system flights. But basically, the idea
        // is that local gravity must not be greater than that at 150 000 Mega from Kerbol to engage: as one gets
        // closer, it rises by an inverse square type law, viz. it takes 1.21 GW at infinity, 1.24 GW at this
        // distance, and at 75 000 Mega it takes 4.96 GW, 37 500 Mega, 19.84 GW, 18 750 M, 79.36 GW, etc.
        private float FindLightspeedsForWarpFactor(float W)
        {
            float u(float x)
            {
                if (x < 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            float A = 0.03684678f;
            float n = 1.791275f;

            return (float)Math.Pow(W, Math.Pow(10.0f / 3.0f + u(W - 9) * A * (-Math.Log(10.0f - W)), n));
        }

        private float FindBaselinePowerForWarpFactor(float W)
        {
            // We want to use a curve fit here to each warp segment. TBA.
            return (float)PartModuleController.GetFieldValue<double>("warp1Gigawatts");
        }

        private Vessel GetForegroundVessel()
        {
            /*
            Guid vesselGUID = fsm.GetVesselGUID();
            Debug.Log("Cochrane: vesselGUID = " + vesselGUID.ToString());

            Vessel v = FlightGlobals.FindVessel(vesselGUID);
            if(v != null)
            {
                if(v.loaded)
                {
                    return v;
                }
            }

            return null;
            */
            if(PartModuleController.ForegroundPart != null)
            {
                return PartModuleController.ForegroundPart.vessel;
            }
            else
            {
                return null;
            }
        }

        private Part GetForegroundPart()
        {
            /*
            uint partPersistentID = fsm.GetPartPersistentID();
            Debug.Log("Cochrane: partPersistentID = " + partPersistentID.ToString());

            Part part = null;
            if(FlightGlobals.FindLoadedPart(partPersistentID, out part))
            {
                return part;
            }
            else
            {
                return null;
            }
            */
            return PartModuleController.ForegroundPart;
        }

        private void WarpMotionUpdate(double deltaTime)
        {
            Vessel vessel = GetForegroundVessel();
            if(vessel != null)
            {
                Part part = GetForegroundPart();

                double c = 299792458.0; // you know what that is :)
                double numLightspeeds = (double)FindLightspeedsForWarpFactor((float)mWarpAccel.CurWarpFactor);
                double worldSpaceVel = numLightspeeds * c;

                double distanceTranslated = worldSpaceVel * deltaTime;

                if(!vessel.packed) // real time
                {
                    Transform transform = vessel.ReferenceTransform;
                    Debug.Log("Vessel pos: " + transform.position.ToString());
                    Debug.Log("Warp speed request (x c): " + numLightspeeds.ToString());

                    Vector3d newPos = transform.position + (part.transform.up * (float)distanceTranslated);

                    if (FlightGlobals.VesselsLoaded.Count > 1)
                        vessel.SetPosition(newPos);
                    else
                        FloatingOrigin.SetOutOfFrameOffset(newPos);
                }
                else // time warp
                {
                    // translate the *orbit* instead
                    Transform refTransform = vessel.ReferenceTransform;

                    // because of the various funky reference frames we have in KSP, we have to do a bit of math here
                    // to get the orientation of the vessel's motion to work out correctly. In paritcular,
                    // UpdateFromStateVectors uses some form of orbit-relative reference frame, but the vessel-
                    // orienting transform uses a different one. We have to convert between these to get the direction 
                    // right, and to do that, we need to find something we know in common between the two. The most
                    // obvious choice here is the vessel velocity.
                    //var velocityInOrientingFrame = vessel.
                    var heading = part.transform.up;
                    var heading2 = new Vector3d(heading.x, heading.z, heading.y);
                    Vector3d newPos = vessel.orbit.pos + (heading2 * (float)distanceTranslated);

                    vessel.orbit.UpdateFromStateVectors(newPos, vessel.orbit.vel, vessel.orbit.referenceBody, 
                        Planetarium.GetUniversalTime());
                }

                if(!CheckGravity())
                {
                    mGravityTooHigh = true;
                }
            }
        }

        private void DropFromWarp()
        {
            Vessel ship = GetForegroundVessel();
            if(ship != null)
            {
                // Drop to time warp 1 first
                TimeWarp.SetRate(1, false, true);

                // Based on TheShadow's TrekDrive disengage code
                // If the orbit mode is set to "Easy" (the default), we execute this code to set an orbit.
                // If the orbit mode is set to "Realistic", nothing is done.
                bool orbitMode = true; // SS: just stick to easy for now
                if (orbitMode)
                {
                    CelestialBody curBody = ship.orbit.referenceBody;

                    // Go on rails before setting the orbit
                    ship.GoOnRails();

                    // Get the position vector from the reference body, then set the velocity vector based on the ship's forward vector
                    // with the magnitude set to that for a circular orbit at the current position
                    Vector3d position = ship.orbit.pos.xzy;
                    Vector3d velocity = Vector3d.Normalize(-1 * ship.GetFwdVector()) * Math.Sqrt(ship.orbit.referenceBody.gravParameter / Vector3d.Magnitude(position));

                    // Update the orbti using the above state vectors
                    ship.orbit.UpdateFromStateVectors(position, velocity, curBody, HighLogic.CurrentGame.UniversalTime);

                    // Now, we can safely go off rails
                    ship.GoOffRails();
                }
            }

            mWarpAccel.ZeroWarpFactor();
        }

        private bool CheckNacelles()
        {
            int numRequiredNacelles = PartModuleController.GetFieldValue<int>("minNacelles");
            int numInstalledNacelles = 0;
            int numActiveNacelles = 0;

            EnumerateNacelles(out numInstalledNacelles, out numActiveNacelles);

            if(numActiveNacelles >= numRequiredNacelles)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public WarpFieldGeneratorActive()
        {
            mWarpAccel = null;
        }

        public WarpFieldGeneratorActive(WarpAccelerator warpAccel)
        {
            mWarpAccel = warpAccel;
        }

        public override void OnVesselWasModified()
        {
            mNacelleFailure = CheckNacelles();
        }

        public override void OnNacelleSwitchedOnOff()
        {
            mNacelleFailure = CheckNacelles();
        }

        public override void OnFlow(Dictionary<string, PartModuleProcessor.ResourceFlowStat> flowStats, 
            double deltaTime) 
        { 
            // Make sure we're getting enough warp plasma to keep going.
            // Figure the amount of warp plasma needed for the current warp factor.
            double curWarpFactor = mWarpAccel.CurWarpFactor;
            double warpGigawatts = (double)FindBaselinePowerForWarpFactor((float)curWarpFactor);
            double neededGigajoules = warpGigawatts * deltaTime;

            // 0.999 factor to compensate for rounding error
            if(flowStats["WarpPlasma"].InFlowAmount < 0.999 * neededGigajoules)
            {
                Debug.Log("Cochrane: Warp plasma obtained = " + flowStats["WarpPlasma"].InFlowAmount.ToString() + " U, requested = " + (0.999 * neededGigajoules).ToString() + " U");

                // Insufficient plasma
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_EmrgThrottleDown"), 5.0f,
                    false);
                
                mWarpPlasmaInsufficiency = true;
            }
        }
        
        public override void OnEnter()
        {
            Debug.Log("Cochrane: WarpFieldGenerator Active");

            // Create the warp accelerator if not already there
            if(mWarpAccel == null)
            {
                mWarpAccel = new WarpAccelerator(PartModuleController, 0.0, 1.0, 
                    PartModuleController.GetFieldValue<double>("maxWarpFactor"),
                    PartModuleController.GetFieldValue<double>("warpAccel"));
            }

            // Start pulling the warp plasma
            double warpGigawatts = (double)FindBaselinePowerForWarpFactor((float)mWarpAccel.CurWarpFactor);

            PartModuleController.SetResourceConsumption("WarpPlasma", warpGigawatts);

            // Update status
            PartModuleController.SetFieldValue<string>("generatorStatus", "Active");
            
            PartModuleController.Events["ActivateWarpDrive"].guiActive = false;
            PartModuleController.Events["DeactivateWarpDrive"].guiActive = true;

            mWarpAccel.UnlockAcceleration();
        }

        public override void OnDeactivateWarpDrive()
        {
            PartModuleController.ChangeState(new WarpFieldGeneratorInactive(mWarpAccel));
        }

        public override void OnBoostWarpFactor()
        {
            mWarpAccel.BoostWarpFactor();
        }

        public override void OnReduceWarpFactor()
        {
            mWarpAccel.ReduceWarpFactor();
        }

        /*
        public override void OnStimulus(WarpFieldGeneratorBackgroundModule fsm, WarpFieldGeneratorStimulus stimulus)
        {
            if (stimulus == WarpFieldGeneratorStimulus.DEACTIVATE)
            {
                fsm.ChangeState(new WarpFieldGeneratorInactive());
            }
            else if (stimulus == WarpFieldGeneratorStimulus.BOOST_SPEED)
            {
                fsm.BoostWarpFactor();
                mWarpPowerChanged = true;
            }
            else if (stimulus == WarpFieldGeneratorStimulus.DROP_SPEED)
            {
                fsm.ReduceWarpFactor();
                mWarpPowerChanged = true;
            }
            else if (stimulus == WarpFieldGeneratorStimulus.PLASMA_INSUFFICIENCY)
            {
                fsm.ChangeState(new WarpFieldGeneratorInactive());
            }
            else if (stimulus == WarpFieldGeneratorStimulus.NACELLE_STATUS_CHANGED)
            {
                //fsm.UpdateNacelles();
                mNacelleStatsNeedUpdate = false;
            }
        }
        */

        public override void OnExit()
        {
            PartModuleController.SetResourceConsumption("WarpPlasma", 0.0);

            DropFromWarp();
            mWarpAccel.LockAcceleration();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            mWarpAccel.OnSave(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if(mWarpAccel != null)
            {
                mWarpAccel.OnLoad(node);
            }
        }

        public override void OnFixedUpdate()
        {
            if(mWarpPowerChanged || mNacelleFailure || mWarpPlasmaInsufficiency || mGravityTooHigh)
            {
                if(mWarpPowerChanged)
                {
                    double warpGigawatts = (double)FindBaselinePowerForWarpFactor((float)mWarpAccel.CurWarpFactor);

                    PartModuleController.SetResourceConsumption("WarpPlasma", warpGigawatts);

                    mWarpPowerChanged = false;
                }
                if(mNacelleFailure)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_WarpFieldCollapsed"), 
                        5.0f, false);

                    mNacelleFailure = false;
                    PartModuleController.ChangeState(new WarpFieldGeneratorInactive());
                }
                if(mWarpPlasmaInsufficiency)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_WarpFieldCollapsed"), 
                        5.0f, false);
                    
                    mWarpPowerChanged = false;
                    PartModuleController.ChangeState(new WarpFieldGeneratorInactive());
                }
                if(mGravityTooHigh)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_WarpFieldCollapsed"), 
                        5.0f, false);
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_Cochrane_Message_WarpFieldCollapsed"), 
                        5.0f, false);

                    mGravityTooHigh = false;
                    PartModuleController.ChangeState(new WarpFieldGeneratorInactive());
                }
            }
            else
            {
                mWarpAccel.OnFixedUpdate();
                WarpMotionUpdate(TimeWarp.fixedDeltaTime);
            }

            UpdateGravityDisplay();
        }
    }
}