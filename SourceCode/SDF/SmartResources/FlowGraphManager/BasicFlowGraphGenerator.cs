// Author: Shimrra Shai
// Date Created: UE+1655.6729 Ms (2022-06-19)
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using SDF.SmartResources.FlowEngine;

using UnityEngine;

namespace SDF
{    
    namespace SmartResources
    {
        namespace FlowGraphManagerDetail
        {
            // This is the basic flow graph generator method. It creates a simple flow graph in which every resource is
            // aggregated into a vessel-wide tank. This essentially mimics the classic KSP uniform vessel drain flow 
            // mode.
            public class BasicFlowGraphGenerator : IFlowGraphGenerator
            {
                public void GenerateFlowGraphFor(AggregateFlowGraph flowGraph, Vessel v)
                {
                    Debug.Log("GenerateFlowGraphFor");
                    flowGraph.Clear();

                    // Gather all resources on all vessel parts.
                    Debug.Log(" - Phase 1");
                    var aggregateTankParts = new Dictionary<string, List<uint>>();
                    if(v.loaded)
                    {
                        foreach(Part part in v.parts)
                        {
                            foreach(PartResource partResource in part.Resources)
                            {
                                if(!aggregateTankParts.ContainsKey(partResource.resourceName))
                                {
                                    aggregateTankParts.Add(partResource.resourceName, new List<uint>());
                                }
                                
                                aggregateTankParts[partResource.resourceName].Add(part.persistentId);
                            }
                        }
                    }
                    else
                    {
                        foreach(ProtoPartSnapshot snap in v.protoVessel.protoPartSnapshots)
                        {
                            foreach(ProtoPartResourceSnapshot res in snap.resources)
                            {
                                if(!aggregateTankParts.ContainsKey(res.resourceName))
                                {
                                    aggregateTankParts.Add(res.resourceName, new List<uint>());
                                }
                                
                                aggregateTankParts[res.resourceName].Add(snap.persistentId);
                            }
                        }
                    }

                    foreach(KeyValuePair<string, List<uint>> kvp in aggregateTankParts)
                    {
                        var node = new AggregateTank("AggregateTank." + kvp.Key, v.id, kvp.Key);
                        foreach(uint partPersistentID in kvp.Value)
                        {
                            node.AddPart(partPersistentID);
                        }

                        flowGraph.AddFlowNode(node);
                    }

                    // Each part containing SmartPartModules is able to feed and drain the aggregate tank.
                    Debug.Log(" - Phase 2");
                    if(v.loaded)
                    {
                        foreach(Part part in v.parts)
                        {
                            foreach(var spm in part.FindModulesImplementing<SmartPartModuleBase>())
                            {
                                PartModuleProcessor pmp = spm.GetBackgroundProcessor();
                                FlowElement flowElement = FlowElementFactory.CreateManagedModuleElement(pmp);
                                flowGraph.AddFlowElement(flowElement);

                                foreach(string nodeID in flowElement.GetFlowNodeIDs())
                                {
                                    string[] splitID = nodeID.Split(':');
                                    if(splitID.Length > 1)
                                    {
                                        if(splitID[1].StartsWith("Consumer"))
                                        {
                                            string resourceName = splitID[1].Substring(9);
                                            flowGraph.ConnectFlowNodes("AggregateTank." + resourceName, nodeID);
                                        }
                                        else if(splitID[1].StartsWith("Producer"))
                                        {
                                            string resourceName = splitID[1].Substring(9);
                                            flowGraph.ConnectFlowNodes(nodeID, "AggregateTank." + resourceName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log(" - Unloaded vessel flowgraph gen NOT SUPPORTED YET");

                        /*
                        foreach(ProtoPartSnapshot snap in v.protoVessel.protoPartSnapshots)
                        {
                            foreach(var rmpm in snap.partPrefab.FindModulesImplementing<ResourceManagedPartModule>())
                            {
                                FlowElement flowElement = FlowElementFactory.CreateManagedModuleElement(rmpm);
                                flowGraph.AddFlowElement(flowElement);

                                foreach(string nodeID in flowElement.GetFlowNodeIDs())
                                {
                                    string[] splitID = nodeID.Split(':');
                                    if(splitID.Length > 1)
                                    {
                                        if(splitID[1].StartsWith("Consumer"))
                                        {
                                            string resourceName = splitID[1].Substring(9);
                                            flowGraph.ConnectFlowNodes("AggregateTank." + resourceName, nodeID);
                                        }
                                        else if(splitID[1].StartsWith("Producer"))
                                        {
                                            string resourceName = splitID[1].Substring(9);
                                            flowGraph.ConnectFlowNodes(nodeID, "AggregateTank." + resourceName);
                                        }
                                    }
                                }
                            }
                        }
                        */
                    }
                    
                    flowGraph.DebugDump();
                }
            }
        }
    }
}