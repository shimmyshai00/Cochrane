// Author: Shimrra Shai
// Date Created: UE+1655.6386 Ms (2022-06-19)
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
            // Defines a flow graph containing flow elements.
            public class AggregateFlowGraph : IFlowGraph
            {
                private Dictionary<string, FlowElement> mFlowElements = new Dictionary<string, FlowElement>();
                private Dictionary<string, FlowNode> mAllFlowNodes = new Dictionary<string, FlowNode>();

                public bool HasFlowNode(string flowNodeID)
                {
                    return mAllFlowNodes.ContainsKey(flowNodeID);
                }

                public List<string> GetFlowElementIDs()
                {
                    return mFlowElements.Keys.ToList();
                }

                public FlowElement GetFlowElement(string flowElementID)
                {
                    return mFlowElements[flowElementID];
                }

                public void AddFlowElement(FlowElement flowElement)
                {
                    mFlowElements.Add(flowElement.ElementID, flowElement);

                    foreach(string flowNodeID in flowElement.GetFlowNodeIDs())
                    {
                        if(!mAllFlowNodes.ContainsKey(flowNodeID))
                        {
                            mAllFlowNodes.Add(flowNodeID, flowElement.GetFlowNode(flowNodeID));
                        }
                    }
                }

                public List<string> GetFlowNodeIDs()
                {
                    return mAllFlowNodes.Keys.ToList();
                }

                public FlowNode GetFlowNode(string flowNodeID)
                {
                    return mAllFlowNodes[flowNodeID];
                }
                
                public void AddFlowNode(FlowNode flowNode)
                {
                    mAllFlowNodes.Add(flowNode.NodeID, flowNode);
                }
                
                public void ConnectFlowNodes(string flowNodeID1, string flowNodeID2)
                {
                    FlowNode srcNode = mAllFlowNodes[flowNodeID1];
                    FlowNode dstNode = mAllFlowNodes[flowNodeID2];

                    if(srcNode.ResourceName != dstNode.ResourceName)
                    {
                        throw new ArgumentException("Cannot connect two flow nodes of disparate resource type");
                    }

                    if((srcNode is IOutFlowNode) && (dstNode is IInFlowNode))
                    {
                        IOutFlowNode outNode = srcNode as IOutFlowNode;
                        IInFlowNode inNode = dstNode as IInFlowNode;

                        outNode.OutEdges.Add(new OutFlowEdge(inNode));
                        inNode.InEdges.Add(new InFlowEdge(outNode));
                    }
                    else
                    {
                        throw new ArgumentException("ConnectFlowNodes requires node 1 to be an out node and node 2 to be an in node");
                    }
                }

                public void Clear()
                {
                    mFlowElements.Clear();
                    mAllFlowNodes.Clear();
                }

                public void DebugDump()
                {
                    Debug.Log("SmartResources Flow Graph Summary:");
                    foreach(FlowNode flowNode in mAllFlowNodes.Values)
                    {
                        Debug.Log("   Node: " + flowNode.NodeID);
                        Debug.Log("      Resource type: " + flowNode.ResourceName);

                        if(flowNode is FlowConsumer)
                        {
                            var fc = flowNode as FlowConsumer;
                            Debug.Log("      Node type: Consumer");
                            Debug.Log("      Consumption rate: " + fc.ConsumptionRate.ToString() + " U/s");
                        }
                        else if(flowNode is FlowProducer)
                        {
                            var fp = flowNode as FlowProducer;
                            Debug.Log("      Node type: Producer");
                            Debug.Log("      Production rate: " + fp.ProductionRate.ToString() + " U/s");
                        }
                        else if(flowNode is FlowTank)
                        {
                            var ft = flowNode as FlowTank;
                            Debug.Log("      Node type: Tank");
                            Debug.Log("      Capacity: " + ft.Capacity.ToString() + " U");
                            Debug.Log("      Retaining: " + ft.Amount.ToString() + " U");
                        }

                        if(flowNode is IInFlowNode)
                        {
                            var ifn = flowNode as IInFlowNode;
                            Debug.Log("      In edges:");
                            foreach(InFlowEdge inEdge in ifn.InEdges)
                            {
                                Debug.Log("       - From " + inEdge.SourceNode.NodeID + ", flow rate: " + inEdge.FlowBorne.ToString() + " U/s, cap: " + inEdge.FlowCapacity.ToString() + " U/s");
                            }
                        }
                        
                        if(flowNode is IOutFlowNode)
                        {
                            var ofn = flowNode as IOutFlowNode;
                            Debug.Log("      Out edges:");
                            foreach(OutFlowEdge outEdge in ofn.OutEdges)
                            {
                                Debug.Log("       - To " + outEdge.DestinationNode.NodeID + ", flow rate: " + outEdge.FlowBorne.ToString() + " U/s, cap: " + outEdge.FlowCapacity.ToString() + " U/s");
                            }
                        }
                    }
                }
            }
        }
    }
}