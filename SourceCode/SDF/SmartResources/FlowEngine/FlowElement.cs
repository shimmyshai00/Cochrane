// Author: Shimrra Shai
// Date Created: UE+1655.6183 Ms (2022-06-18)
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines a flow element. A flow element can be considered like a small flow graph. It can be used as a 
            // "macro" to build/aggregate larger flow graphs.
            public class FlowElement : IFlowGraph
            {
                private Dictionary<string, FlowNode> mFlowNodes = new Dictionary<string, FlowNode>();

                public string ElementID { get; }

                public FlowElement(string elementID)
                {
                    ElementID = elementID;
                }
                
                public bool HasFlowNode(string flowNodeID)
                {
                    return mFlowNodes.ContainsKey(flowNodeID);
                }
                
                public List<string> GetFlowNodeIDs()
                {
                    return mFlowNodes.Keys.ToList();
                }

                public FlowNode GetFlowNode(string flowNodeID)
                {
                    return mFlowNodes[flowNodeID];
                }
                
                public void AddFlowNode(FlowNode flowNode)
                {
                    mFlowNodes.Add(flowNode.NodeID, flowNode);
                }
                
                public void ConnectFlowNodes(string flowNodeID1, string flowNodeID2)
                {
                    FlowNode srcNode = mFlowNodes[flowNodeID1];
                    FlowNode dstNode = mFlowNodes[flowNodeID2];

                    if((srcNode is IInFlowNode) && (dstNode is IOutFlowNode))
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
            }
        }
    }
}