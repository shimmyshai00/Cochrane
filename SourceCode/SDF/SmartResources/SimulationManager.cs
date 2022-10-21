// Author: Shimrra Shai
// Date Created: UE+1665.1925 Ms (2022-10-07)
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
        public class SimulationManager
        {
            private Dictionary<Guid, PartModuleProcessor> mBackgroundProcessors = 
                new Dictionary<Guid, PartModuleProcessor>();
            
            private Dictionary<Guid, List<Guid>> mBackgroundProcessorsOnVessel = new Dictionary<Guid, List<Guid>>();
            private Dictionary<Guid, Guid> mVesselReverseLookup = new Dictionary<Guid, Guid>();
            private FlowGraphManager mFlowGraphManager = new FlowGraphManager();

            private class FlowController : IPartFlowController
            {
                private FlowGraphManager mFlowGraphManager;
                private Guid mVesselGUID;
                private Guid mPartModuleProcessorGUID;

                public FlowController(FlowGraphManager flowGraphManager, Guid vesselGUID, Guid pmpGUID)
                {
                    mFlowGraphManager = flowGraphManager;
                    mVesselGUID = vesselGUID;
                    mPartModuleProcessorGUID = pmpGUID;
                }

                public void SetResourceProduction(string resourceName, double newProductionRate)
                {
                    mFlowGraphManager.SetSmartModuleProduction(mVesselGUID, mPartModuleProcessorGUID, resourceName, 
                        newProductionRate);
                }

                public void SetResourceConsumption(string resourceName, double newConsumptionRate)
                {
                    mFlowGraphManager.SetSmartModuleConsumption(mVesselGUID, mPartModuleProcessorGUID, resourceName, 
                        newConsumptionRate);
                }
            }

            public void AddVessel(Vessel v)
            {
                if(!mFlowGraphManager.HasFlowGraph(v.id))
                {
                    Debug.Log("SmartResources SimulationManager: Adding vessel " + v.id.ToString() + " (" + v.GetDisplayName() + ")");

                    if(v.loaded)
                    {
                        Debug.Log(" - Vessel is loaded");

                        // Scan the vessel for smart part modules and spawn their module processor components.
                        mBackgroundProcessorsOnVessel.Add(v.id, new List<Guid>());

                        foreach(Part part in v.parts)
                        {
                            foreach(SmartPartModuleBase smartPart in 
                                part.FindModulesImplementing<SmartPartModuleBase>())
                            {
                                // NB: conditional only for so long as no background processing yet
                                bool didCreateHere = false;
                                if(smartPart.GetBackgroundProcessor() == null)
                                {
                                    smartPart.SpawnBackgroundProcessorOnboard();
                                    didCreateHere = true;
                                }

                                var backgroundModule = smartPart.GetBackgroundProcessor();
                                mBackgroundProcessors.Add(backgroundModule.GUID, backgroundModule);
                                mBackgroundProcessorsOnVessel[v.id].Add(backgroundModule.GUID);
                                mVesselReverseLookup.Add(backgroundModule.GUID, v.id);
                                backgroundModule.SetPartModuleFlowController(new FlowController(mFlowGraphManager, 
                                    v.id, backgroundModule.GUID));

                                if(didCreateHere)
                                {
                                    backgroundModule.OnCreate();
                                }
                                
                                Debug.Log(" - Added smart module with GUID " + backgroundModule.GUID.ToString() + " (part persistent id: " + part.persistentId.ToString() + ")");
                            }
                        }

                        // Generate and add a flow graph for the vessel.
                        Debug.Log(" - Generating flow graph...");
                        if(!mFlowGraphManager.HasFlowGraph(v.id))
                        {
                            mFlowGraphManager.CreateFlowGraphForVessel(v, new BasicFlowGraphGenerator());
                        }
                        Debug.Log(" - Done.");

                        // Now bring up the modules.
                        foreach(var backgroundModuleGUID in mBackgroundProcessorsOnVessel[v.id])
                        {
                            mBackgroundProcessors[backgroundModuleGUID].OnReady();
                        }
                    }
                    else
                    {
                        Debug.Log(" - Vessel is NOT loaded");
                    }
                }
            }

            public void RemoveVessel(Vessel v)
            {
                Debug.Log("SmartResources: Removing vessel " + v.id.ToString() + " (" + v.GetDisplayName() + ")");
                if(mBackgroundProcessorsOnVessel.ContainsKey(v.id))
                {
                    foreach(Guid guid in mBackgroundProcessorsOnVessel[v.id])
                    {
                        mBackgroundProcessors.Remove(guid);
                        mVesselReverseLookup.Remove(guid);
                    }

                    mBackgroundProcessorsOnVessel.Remove(v.id);
                    mFlowGraphManager.DestroyVesselFlowGraph(v.id);
                }
            }

            public void RebuildFlowGraph(Vessel v)
            {
                // Right now, just recreate the flow graph - NOT EFFICIENT
                mFlowGraphManager.DestroyVesselFlowGraph(v.id);
                mFlowGraphManager.CreateFlowGraphForVessel(v, new BasicFlowGraphGenerator());
            }

            public void NotifyOfVesselChange(Vessel v)
            {
                foreach(Guid backgroundProcessorGUID in mBackgroundProcessorsOnVessel[v.id])
                {
                    mBackgroundProcessors[backgroundProcessorGUID].OnVesselWasModified();
                }
            }

            public void OnFixedUpdate(double curTime, double deltaTime)
            {
                var flowSimulator = new FlowSimulator();

                foreach(AggregateFlowGraph flowGraph in mFlowGraphManager.GetAllFlowGraphs())
                {
                    var flowResult = flowSimulator.SimulateFlows(flowGraph, curTime, deltaTime);
                    Dictionary<Guid, Dictionary<string, PartModuleProcessor.ResourceFlowStat>> vesselFlowStats = 
                        new Dictionary<Guid, Dictionary<string, PartModuleProcessor.ResourceFlowStat>>();
                    foreach(var consumerFlowTotal in flowResult.ConsumerFlowTotals)
                    {
                        var consumerNodeID = consumerFlowTotal.Key;
                        var parsedNodeID = consumerNodeID.Split(':');
                        var parsedObjectID = parsedNodeID[0].Split('.'); // should be "ManagedPartModule.(...)"
                        var parsedRoleID = parsedNodeID[1].Split('.'); // should be "Consumer.(...)"

                        if((parsedObjectID[0] == "ManagedPartModule") && (parsedRoleID[0] == "Consumer"))
                        {
                            Guid partModuleGUID = Guid.Parse(parsedObjectID[1]);
                            string resourceName = parsedRoleID[1];

                            if(!vesselFlowStats.ContainsKey(partModuleGUID))
                            {
                                vesselFlowStats.Add(partModuleGUID, 
                                    new Dictionary<string, PartModuleProcessor.ResourceFlowStat>());
                            }

                            if(!vesselFlowStats[partModuleGUID].ContainsKey(resourceName))
                            {
                                vesselFlowStats[partModuleGUID].Add(resourceName, 
                                    new PartModuleProcessor.ResourceFlowStat(0.0, 0.0));
                            }

                            vesselFlowStats[partModuleGUID][resourceName].InFlowAmount += consumerFlowTotal.Value;
                        }
                    }

                    foreach(var producerFlowTotal in flowResult.ProducerFlowTotals)
                    {
                        var producerNodeID = producerFlowTotal.Key;
                        var parsedNodeID = producerNodeID.Split(':');
                        var parsedObjectID = parsedNodeID[0].Split('.'); // should be "ManagedPartModule.(...)"
                        var parsedRoleID = parsedNodeID[1].Split('.'); // should be "Producer.(...)"

                        if((parsedObjectID[0] == "ManagedPartModule") && (parsedRoleID[0] == "Producer"))
                        {
                            Guid partModuleGUID = Guid.Parse(parsedObjectID[1]);
                            string resourceName = parsedRoleID[1];

                            if(!vesselFlowStats.ContainsKey(partModuleGUID))
                            {
                                vesselFlowStats.Add(partModuleGUID, 
                                    new Dictionary<string, PartModuleProcessor.ResourceFlowStat>());
                            }

                            if(!vesselFlowStats[partModuleGUID].ContainsKey(resourceName))
                            {
                                vesselFlowStats[partModuleGUID].Add(resourceName, 
                                    new PartModuleProcessor.ResourceFlowStat(0.0, 0.0));
                            }

                            vesselFlowStats[partModuleGUID][resourceName].OutFlowAmount += producerFlowTotal.Value;
                        }
                    }
                    
                    foreach(var flowStatPair in vesselFlowStats)
                    {
                        mBackgroundProcessors[flowStatPair.Key].OnFlow(flowStatPair.Value, deltaTime);
                    }
                }

                foreach(PartModuleProcessor partModuleProcessor in mBackgroundProcessors.Values)
                {
                    partModuleProcessor.OnFixedUpdate();
                }
            }
        }
    }
}