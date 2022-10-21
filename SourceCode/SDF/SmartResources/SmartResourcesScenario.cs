// Author: Shimrra Shai
// Date Created: UE+1665.1168 Ms (2022-10-06)
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using SDF.SmartResources.FlowEngine;
using SDF.SmartResources.FlowGraphManagerDetail;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        //[KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
        [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
        public class SmartResourcesScenario : ScenarioModule
        {
            private SimulationManager mSimulationManager = new SimulationManager();

            public SmartResourcesScenario()
            {
                Debug.Log("SmartResources: (constructor)");
            }

            public override void OnAwake()
            {
                Debug.Log("SmartResources: OnAwake");

                base.OnAwake();

                GameEvents.onVesselCreate.Add(onVesselCreate);
                GameEvents.onVesselLoaded.Add(onVesselLoaded);
                GameEvents.onVesselUnloaded.Add(onVesselUnloaded);
                GameEvents.onVesselWasModified.Add(onVesselWasModified);
                GameEvents.onPartDie.Add(onPartDie); // nb this kind of notification needs more testing
                GameEvents.onPartDestroyed.Add(onPartDestroyed);
                //GameEvents.onVesselRecovered.Add(onVesselRecovered);
                GameEvents.onVesselWillDestroy.Add(onVesselWillDestroy);
                GameEvents.onVesselDestroy.Add(onVesselDestroy);
                //GameEvents.onVesselTerminated.Add(onVesselTerminated);
            }

            public void OnDestroy()
            {
                Debug.Log("SmartResources: OnDestroy");

                GameEvents.onVesselCreate.Remove(onVesselCreate);
                GameEvents.onVesselLoaded.Remove(onVesselLoaded);
                GameEvents.onVesselUnloaded.Remove(onVesselUnloaded);
                GameEvents.onVesselWasModified.Remove(onVesselWasModified);
                //GameEvents.onPartDie.Remove(onPartDie); // nb this kind of notification needs more testing
                //GameEvents.onPartDestroyed.Remove(onPartDestroyed);
                //GameEvents.onVesselRecovered.Remove(onVesselRecovered);
                GameEvents.onVesselWillDestroy.Remove(onVesselWillDestroy);
                GameEvents.onVesselDestroy.Remove(onVesselDestroy);
                //GameEvents.onVesselTerminated.Remove(onVesselTerminated);
            }

            public override void OnLoad(ConfigNode node)
            {
                Debug.Log("SmartResources: OnLoad");

                base.OnLoad(node);
            }

            public override void OnSave(ConfigNode node)
            {
                Debug.Log("SmartResources: OnSave");

                base.OnSave(node);
            }

            public void onVesselCreate(Vessel v)
            {
                Debug.Log("SmartResources: onVesselCreate");
                Debug.Log(" - Vessel: " + v.id.ToString() + " (" + v.GetDisplayName() + ")");

                mSimulationManager.AddVessel(v);
            }

            public void onVesselLoaded(Vessel v)
            {
                Debug.Log("SmartResources: onVesselLoaded");
                Debug.Log(" - Vessel " + v.id.ToString() + " (" + v.GetDisplayName() + ")" + " becomes loaded");

                // NB! Only good for now so long as we don't yet have background processing support
                mSimulationManager.AddVessel(v);
            }

            public void onVesselUnloaded(Vessel v)
            {
                Debug.Log("SmartResources: onVesselUnloaded");
                Debug.Log(" - Vessel " + v.id.ToString() + " (" + v.GetDisplayName() + ")" + " becomes unloaded");
                mSimulationManager.RemoveVessel(v);
            }

            public void onVesselWasModified(Vessel v)
            {
                Debug.Log("SmartResources: onVesselWasModified");
                mSimulationManager.RebuildFlowGraph(v);
                mSimulationManager.NotifyOfVesselChange(v);
            }

            public void onPartDie(Part part)
            {
                Debug.Log("SmartResources: onPartDie");
            }

            public void onPartDestroyed(Part part)
            {
                Debug.Log("SmartResources: onPartDestroyed");
            }

            private void onVesselWillDestroy(Vessel v)
            {
                Debug.Log("SmartResources: onVesselWillDestroy");
                mSimulationManager.RemoveVessel(v);
            }

            private void onVesselDestroy(Vessel v)
            {
                Debug.Log("SmartResources: onVesselDestroy");
                mSimulationManager.RemoveVessel(v);
            }

            public void FixedUpdate()
            {
                mSimulationManager.OnFixedUpdate(Planetarium.GetUniversalTime(), TimeWarp.fixedDeltaTime);
            }
        }
    }
}