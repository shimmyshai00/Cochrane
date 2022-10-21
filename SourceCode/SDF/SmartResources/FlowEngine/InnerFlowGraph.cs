// Author: Shimrra Shai
// Date Created: UE+1655.8482 Ms (2022-06-21)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines a simplified, single-resource flow graph suitable for easy solving.
            public class InnerFlowGraph
            {
                private Dictionary<string, InnerFlowNode> mInnerFlowNodes = new Dictionary<string, InnerFlowNode>();

                public InnerFlowGraph()
                {
                    // An inner flow graph always comes with two special nodes by default.
                    mInnerFlowNodes.Add("_aggregateSource", new InnerFlowNode("_aggregateSource"));
                    mInnerFlowNodes.Add("_aggregateSink", new InnerFlowNode("_aggregateSink"));
                }

                public List<string> GetInnerFlowNodeIDs()
                {
                    return mInnerFlowNodes.Keys.ToList();
                }

                public bool HasInnerFlowNode(string flowNodeID)
                {
                    return mInnerFlowNodes.ContainsKey(flowNodeID);
                }

                public InnerFlowNode GetInnerFlowNode(string flowNodeID)
                {
                    return mInnerFlowNodes[flowNodeID];
                }

                public void AddInnerFlowNode(string flowNodeID)
                {
                    mInnerFlowNodes.Add(flowNodeID, new InnerFlowNode(flowNodeID));
                }

                public bool AreInnerFlowNodesJoined(string fromID, string toID)
                {
                    foreach(InnerFlowEdge outEdge in mInnerFlowNodes[fromID].OutEdges)
                    {
                        if(outEdge.DestNode.NodeID == toID)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                public InnerFlowEdge JoinInnerFlowNodes(string fromID, string toID)
                {
                    InnerFlowEdge edge = new InnerFlowEdge(mInnerFlowNodes[fromID], mInnerFlowNodes[toID]);
                    mInnerFlowNodes[fromID].OutEdges.Add(edge);
                    mInnerFlowNodes[toID].InEdges.Add(edge);

                    return edge;
                }

                public void DebugDump()
                {
                    Debug.Log("Inner flow graph:");
                    foreach(InnerFlowNode node in mInnerFlowNodes.Values)
                    {
                        Debug.Log("   Node: " + node.NodeID);
                        Debug.Log("      In edges: ");
                        foreach(InnerFlowEdge inEdge in node.InEdges)
                        {
                            Debug.Log("       - Source node: " + inEdge.SrcNode.NodeID + ", Flow: " + inEdge.FlowBorne.ToString() + " U/s, Cap: " + inEdge.FlowCapacity.ToString() + " U/s");
                        }
                        Debug.Log("      Out edges: ");
                        foreach(InnerFlowEdge outEdge in node.OutEdges)
                        {
                            Debug.Log("       - Destination node: " + outEdge.DestNode.NodeID + ", Flow: " + outEdge.FlowBorne.ToString() + " U/s, Cap: " + outEdge.FlowCapacity.ToString() + " U/s");
                        }
                    }
                }
            }
        }
    }
}