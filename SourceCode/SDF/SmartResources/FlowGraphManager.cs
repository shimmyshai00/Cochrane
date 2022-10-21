// Author: Shimrra Shai
// Date Created: UE+1655.2549 Ms (2022-06-14)
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
        // Defines a class for managing flow graphs.
        public class FlowGraphManager
        {
            private Dictionary<Guid, AggregateFlowGraph> mVesselFlowGraphs = new Dictionary<Guid, AggregateFlowGraph>();
            private FlowSolver mFlowGraphSolver = new FlowSolver();

            public bool HasFlowGraph(Guid vesselGUID)
            {
                return mVesselFlowGraphs.ContainsKey(vesselGUID);
            }

            public void CreateFlowGraphForVessel(Vessel v, IFlowGraphGenerator generator)
            {
                Debug.Log(" - Creating flow graph for vessel " + v.id.ToString() + " (" + v.GetDisplayName() + ")");

                var flowGraph = new AggregateFlowGraph();
                generator.GenerateFlowGraphFor(flowGraph, v);

                mVesselFlowGraphs.Add(v.id, flowGraph);
            }

            public void DestroyVesselFlowGraph(Guid vesselGUID)
            {
                Debug.Log(" - Destroying flow graph for vessel " + vesselGUID.ToString());
                mVesselFlowGraphs.Remove(vesselGUID);
            }

            public void PurgeAll()
            {
                Debug.Log(" - Purging whole flow graph manager");
                mVesselFlowGraphs.Clear();
            }

            public AggregateFlowGraph GetVesselFlowGraph(Guid vesselGUID)
            {
                return mVesselFlowGraphs[vesselGUID];
            }

            public List<Guid> GetAllVesselGUIDs()
            {
                return mVesselFlowGraphs.Keys.ToList();
            }

            public List<AggregateFlowGraph> GetAllFlowGraphs()
            {
                return mVesselFlowGraphs.Values.ToList();
            }

            public void SetSmartModuleProduction(Guid vesselGUID, Guid smartModuleGUID, string resourceName, 
                double productionRate)
            {
                if(mVesselFlowGraphs.ContainsKey(vesselGUID))
                {
                    string producerElementID = "ManagedPartModule." + smartModuleGUID.ToString();
                    string producerNodeID = producerElementID + ":Producer." + resourceName;
                    
                    Debug.Log("SmartResources FlowGraphManager: Setting production for node " + producerNodeID.ToString() + " of " + resourceName + " to " + productionRate.ToString() + " U/s");

                    var producerNode = mVesselFlowGraphs[vesselGUID].GetFlowNode(producerNodeID) as FlowProducer;
                    producerNode.ProductionRate = productionRate;

                    //Debug.Log("--- Flow Graph BEFORE solving ---");
                    //mVesselFlowGraphs[vesselGUID].DebugDump();
                    //Debug.Log("---------------------------------");
                    //Debug.Log("");
                    mFlowGraphSolver.SolveFlowGraph(mVesselFlowGraphs[vesselGUID]);
                    //Debug.Log("--- Flow Graph AFTER solving ---");
                    //mVesselFlowGraphs[vesselGUID].DebugDump();
                    //Debug.Log("---------------------------------");
                    //Debug.Log("");
                }
            }

            public void SetSmartModuleConsumption(Guid vesselGUID, Guid smartModuleGUID, string resourceName, 
                double consumptionRate)
            {
                Debug.Log("SetSmartModuleConsumption vessel GUID " + vesselGUID.ToString());
                if(mVesselFlowGraphs.ContainsKey(vesselGUID))
                {
                    string consumerElementID = "ManagedPartModule." + smartModuleGUID.ToString();
                    string consumerNodeID = consumerElementID + ":Consumer." + resourceName;

                    Debug.Log("SmartResources FlowGraphManager: Setting consumption for node " + consumerNodeID.ToString() + " of " + resourceName + " to " + consumptionRate.ToString() + " U/s");

                    var consumerNode = mVesselFlowGraphs[vesselGUID].GetFlowNode(consumerNodeID) as FlowConsumer;
                    Debug.Log("Setting Consumption for vessel " + vesselGUID.ToString() + " node " + consumerNodeID + " resource " + resourceName + " to " + consumptionRate.ToString());
                    consumerNode.ConsumptionRate = consumptionRate;

                    //Debug.Log("--- Flow Graph BEFORE solving ---");
                    //mVesselFlowGraphs[vesselGUID].DebugDump();
                    //Debug.Log("---------------------------------");
                    //Debug.Log("");
                    mFlowGraphSolver.SolveFlowGraph(mVesselFlowGraphs[vesselGUID]);
                    //Debug.Log("--- Flow Graph AFTER solving ---");
                    //mVesselFlowGraphs[vesselGUID].DebugDump();
                    //Debug.Log("---------------------------------");
                    //Debug.Log("");
                }
            }

            public void RelabelPart(Guid vesselGUID, uint oldPartPersistentID, uint newPartPersistentID)
            {
                /*
                if(mVesselFlowGraphs.ContainsKey(vesselGUID))
                {
                    string prevNodeStem = "Part." + oldPartPersistentID.ToString() + ".";
                    string newNodeStem = "Part." + newPartPersistentID.ToString() + ".";

                    foreach(string nodeID in mVesselFlowGraphs[vesselGUID].GetFlowNodeIDs())
                    {
                        if(nodeID.StartsWith(prevNodeStem))
                        {
                            string newNodeID = newNodeStem + nodeID.Substring(prevNodeStem.Length);
                            mVesselFlowGraphs[vesselGUID].RenameFlowNode(nodeID, newNodeID);
                        }
                    }
                }
                */
            }
        }
    }
}
