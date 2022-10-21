// Author: Shimrra Shai
// Date Created: UE+1655.6736 Ms (2022-06-19)
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Solves a flow graph to give the maximum flow of each resource type between the flow nodes. That is, this
            // computes the flows borne on each edge of the graph according to emplaced demands and outflows and 
            // adjusts the fields accordingly.
            public class FlowSolver
            {
                // The flow algorithm used here is the famous "Ford-Fulkerson" max-flow algorithm, which tries to 
                // maximize the amount of flow from resource sources to sinks through the junctions. This allows us to 
                // split and combine flows in a variety of ways as well as simulate bottlenecks.
                //
                // Note that the proper F-F algorithm requires a single source and sink, but here, we typically have
                // multiple sources and sinks (output and input ports, respectively) for any given resource. To make
                // the algorithm go, the standard approach is to _aggregate_ the sources and sinks into single 
                // fictitious "super" sources and sinks with a total provision equal to the originals and linked to 
                // them wiih links of unlimited capacity. Moreover, because the algorithm also requires preparing a
                // "working" copy of the flow graph anyways, we can simply roll those two steps together into one.
                //
                // This method builds the inner flow graph from a given flow graph, as well as a correspondence table
                // indicating how to map the edges of the inner flow graph back to the original.
                private InnerFlowGraph InternalizeFlowGraph(AggregateFlowGraph flowGraph, string resourceName)
                {
                    InnerFlowGraph rv = new InnerFlowGraph();
                    Debug.Log(" Temp graph build phase 1");

                    List<FlowNode> flowNodesForThisResource = new List<FlowNode>();
                    double totalProductionRate = 0.0;
                    double totalConsumptionRate = 0.0;
                    double totalTankCapacity = 0.0;
                    foreach(string flowNodeID in flowGraph.GetFlowNodeIDs())
                    {
                        FlowNode flowNode = flowGraph.GetFlowNode(flowNodeID);
                        if(flowNode.ResourceName == resourceName)
                        {
                            flowNodesForThisResource.Add(flowNode);
                            flowNode.BakeToInnerFlowGraph(rv);
                            if(flowNode is FlowProducer)
                            {
                                totalProductionRate += (flowNode as FlowProducer).ProductionRate;
                            }
                            else if(flowNode is FlowConsumer)
                            {
                                totalConsumptionRate += (flowNode as FlowConsumer).ConsumptionRate;
                            }
                            else if(flowNode is FlowTank)
                            {
                                totalTankCapacity += (flowNode as FlowTank).Capacity;
                            }
                        }
                    }

                    Debug.Log(" Temp graph build phase 2");

                    // When connecting the new flow nodes, we need to be aware of that tanks can sometimes serve double
                    // duty as source and sink.
                    foreach(FlowNode flowNode in flowNodesForThisResource)
                    {
                        if(flowNode is IOutFlowNode)
                        {
                            IOutFlowNode outNode = flowNode as IOutFlowNode;
                            foreach(OutFlowEdge outEdge in outNode.OutEdges)
                            {
                                IInFlowNode dstNode = outEdge.DestinationNode;

                                if(!rv.AreInnerFlowNodesJoined(flowNode.NodeID, dstNode.NodeID))
                                {
                                    InnerFlowEdge ife = rv.JoinInnerFlowNodes(flowNode.NodeID, dstNode.NodeID);
                                    ife.MainGraphOutFlowEdgeRef = outEdge;
                                }
                            }
                        }

                        if(flowNode is IInFlowNode)
                        {
                            IInFlowNode inNode = flowNode as IInFlowNode;
                            foreach(InFlowEdge inEdge in inNode.InEdges)
                            {
                                IOutFlowNode srcNode = inEdge.SourceNode;

                                if(!rv.AreInnerFlowNodesJoined(srcNode.NodeID, flowNode.NodeID))
                                {
                                    InnerFlowEdge ife = rv.JoinInnerFlowNodes(srcNode.NodeID, flowNode.NodeID);
                                    ife.MainGraphInFlowEdgeRef = inEdge;
                                }
                                else
                                {
                                    foreach(InnerFlowEdge ife in rv.GetInnerFlowNode(flowNode.NodeID).InEdges)
                                    {
                                        if(ife.SrcNode.NodeID == srcNode.NodeID)
                                        {
                                            ife.MainGraphInFlowEdgeRef = inEdge;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // When dealing with tanks that are acting as sources or sinks, we should limit their flow 
                    // capacities so they don't "hog" all the #flow for themselves, but instead only buffer excess 
                    // production or demand
                    Debug.Log(" Temp graph build phase 3");
                    double totalTankOutflowRate = 0.0;
                    double totalTankInflowRate = 0.0;
                    if(totalConsumptionRate > totalProductionRate)
                    {
                        totalTankOutflowRate = totalConsumptionRate - totalProductionRate;
                    }
                    else
                    {
                        totalTankInflowRate = totalProductionRate - totalConsumptionRate;
                    }

                    foreach(InnerFlowEdge sourceEdge in rv.GetInnerFlowNode("_aggregateSource").OutEdges)
                    {
                        string destInnerID = sourceEdge.DestNode.NodeID;
                        if(destInnerID.EndsWith(".Source"))
                        {
                            // This is a tank acting as a source. Cap it.
                            string tankNodeID = destInnerID.Substring(0, destInnerID.Length - 7);
                            FlowTank tank = flowGraph.GetFlowNode(tankNodeID) as FlowTank;
                            double proportion = tank.Capacity / totalTankCapacity;

                            sourceEdge.FlowCapacity = totalTankOutflowRate * proportion;
                        }
                    }

                    foreach(InnerFlowEdge sinkEdge in rv.GetInnerFlowNode("_aggregateSink").InEdges)
                    {
                        string srcInnerID = sinkEdge.SrcNode.NodeID;
                        if(srcInnerID.EndsWith(".Sink"))
                        {
                            // This is a tank acting as a source. Cap it.
                            string tankNodeID = srcInnerID.Substring(0, srcInnerID.Length - 5);
                            FlowTank tank = flowGraph.GetFlowNode(tankNodeID) as FlowTank;
                            double proportion = tank.Capacity / totalTankCapacity;

                            sinkEdge.FlowCapacity = totalTankInflowRate * proportion;
                        }
                    }

                    Debug.Log(" Temp graph built!");
                    //rv.DebugDump();

                    return rv;
                }

                private InnerFlowEdge GetInnerEdge(InnerFlowGraph ifg, string fromNodeID, string toNodeID)
                {
                    InnerFlowNode srcNode = ifg.GetInnerFlowNode(fromNodeID);
                    foreach(InnerFlowEdge outEdge in srcNode.OutEdges)
                    {
                        if(outEdge.DestNode.NodeID == toNodeID)
                        {
                            return outEdge;
                        }
                    }

                    return null;
                }

                private void BackcopyFlows(AggregateFlowGraph flowGraph, string resourceName, 
                    InnerFlowGraph fromThisInnerGraph)
                {
                    // Just use the included references
                    foreach(string innerNodeID in fromThisInnerGraph.GetInnerFlowNodeIDs())
                    {
                        InnerFlowNode innerNode = fromThisInnerGraph.GetInnerFlowNode(innerNodeID);
                        foreach(InnerFlowEdge inEdge in innerNode.InEdges)
                        {
                            if(inEdge.MainGraphInFlowEdgeRef != null)
                            {
                                inEdge.MainGraphInFlowEdgeRef.FlowBorne = inEdge.FlowBorne;
                            }
                        }

                        foreach(InnerFlowEdge outEdge in innerNode.OutEdges)
                        {
                            if(outEdge.MainGraphOutFlowEdgeRef != null)
                            {
                                outEdge.MainGraphOutFlowEdgeRef.FlowBorne = outEdge.FlowBorne;
                            }
                        }
                    }
                }

                private bool FindPathWithPositiveCapacity(InnerFlowGraph innerFlowGraph, List<InnerFlowEdge> pathRecv)
                {
                    // Do a depth-first traverse.
                    bool DepthFirstCore(InnerFlowGraph ifg, string curNodeID, List<InnerFlowEdge> pathRecvL)
                    {
                        if(!ifg.GetInnerFlowNode(curNodeID).Explored)
                        {
                            ifg.GetInnerFlowNode(curNodeID).Explored = true;
                        }

                        if(curNodeID == "_aggregateSink")
                        {
                            return true;
                        }

                        foreach(InnerFlowEdge outEdge in ifg.GetInnerFlowNode(curNodeID).OutEdges)
                        {
                            Debug.Log("      = Exploring edge " + curNodeID + " - " + outEdge.DestNode.NodeID);
                            if(!outEdge.DestNode.Explored)
                            {
                                Debug.Log("        Flow cap: " + outEdge.FlowCapacity.ToString() + " U/s, borne: " + outEdge.FlowBorne.ToString() + " U/s");
                                if(outEdge.FlowCapacity - outEdge.FlowBorne > 0.0)
                                {
                                    var pathFound = DepthFirstCore(ifg, outEdge.DestNode.NodeID, pathRecvL);
                                    if(pathFound)
                                    {
                                        pathRecvL.Add(outEdge);
                                        return true;
                                    }
                                }
                            }
                        }

                        return false;
                    }

                    foreach(string innerFlowNodeID in innerFlowGraph.GetInnerFlowNodeIDs())
                    {
                        innerFlowGraph.GetInnerFlowNode(innerFlowNodeID).Explored = false;
                    }
                    
                    pathRecv.Clear();
                    bool didFindPath = DepthFirstCore(innerFlowGraph, "_aggregateSource", pathRecv);
                    pathRecv.Reverse();

                    return didFindPath;
                }
                
                private void SolveSingleResource(AggregateFlowGraph flowGraph, string resourceName)
                {
                    InnerFlowGraph innerFlowGraph = InternalizeFlowGraph(flowGraph, resourceName);

                    // The heart of the F-F algorithm itself.
                    Debug.Log("Performing algorithm core");
                    List<InnerFlowEdge> flowPath = new List<InnerFlowEdge>();
                    while(FindPathWithPositiveCapacity(innerFlowGraph, flowPath))
                    {
                        Debug.Log("Path with positive cap found: ");
                        Debug.Log(" - Node: _aggregateSource (implied)");
                        double pathBottleneck = double.PositiveInfinity;
                        foreach(InnerFlowEdge edge in flowPath)
                        {
                            Debug.Log(" - Node: " + edge.DestNode.NodeID);
                            if(edge.FlowCapacity - edge.FlowBorne < pathBottleneck)
                            {
                                pathBottleneck = edge.FlowCapacity - edge.FlowBorne;
                            }
                        }

                        InnerFlowNode curNode = innerFlowGraph.GetInnerFlowNode("_aggregateSource");
                        foreach(InnerFlowEdge edge in flowPath)
                        {
                            edge.FlowBorne += pathBottleneck;
                            
                            // Check for a reverse edge back to the current node
                            foreach(InnerFlowEdge revEdge in edge.DestNode.OutEdges)
                            {
                                if(revEdge.DestNode == curNode)
                                {
                                    revEdge.FlowBorne -= pathBottleneck;
                                }
                            }
                        }
                    }
                    Debug.Log("Solved inner graph:");
                    innerFlowGraph.DebugDump();

                    BackcopyFlows(flowGraph, resourceName, innerFlowGraph);
                }

                public void SolveFlowGraph(AggregateFlowGraph flowGraph)
                {
                    HashSet<string> resources = new HashSet<string>();
                    
                    foreach(string flowNodeID in flowGraph.GetFlowNodeIDs())
                    {
                        FlowNode flowNode = flowGraph.GetFlowNode(flowNodeID);
                        if(!resources.Contains(flowNode.ResourceName))
                        {
                            resources.Add(flowNode.ResourceName);
                        }

                        if(flowNode is IOutFlowNode)
                        {
                            foreach(OutFlowEdge outEdge in (flowNode as IOutFlowNode).OutEdges)
                            {
                                outEdge.FlowBorne = 0.0;
                            }
                        }

                        if(flowNode is IInFlowNode)
                        {
                            foreach(InFlowEdge inEdge in (flowNode as IInFlowNode).InEdges)
                            {
                                inEdge.FlowBorne = 0.0;
                            }
                        }
                    }

                    foreach(string resource in resources)
                    {
                        SolveSingleResource(flowGraph, resource);
                    }
                }
            }
        }
    }
}