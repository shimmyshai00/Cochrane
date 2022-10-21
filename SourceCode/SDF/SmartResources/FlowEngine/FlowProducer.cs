// Author: Shimrra Shai
// Date Created: UE+1655.6177 Ms (2022-06-18)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines a flow node which produces resource.
            public class FlowProducer : FlowNode, IOutFlowNode
            {
                public double ProductionRate { get; set; }
                public List<OutFlowEdge> OutEdges { get; }

                public FlowProducer(string nodeID, string resourceName) : base(nodeID, resourceName)
                {
                    ProductionRate = 0.0;
                    OutEdges = new List<OutFlowEdge>();
                }

                public override void BakeToInnerFlowGraph(InnerFlowGraph innerFlowGraph)
                {
                    innerFlowGraph.AddInnerFlowNode(NodeID);
                    innerFlowGraph.JoinInnerFlowNodes("_aggregateSource", NodeID);
                    
                    foreach(InnerFlowEdge outEdge in innerFlowGraph.GetInnerFlowNode("_aggregateSource").OutEdges)
                    {
                        if(outEdge.DestNode.NodeID == NodeID)
                        {
                            outEdge.FlowCapacity = ProductionRate;
                            break;
                        }
                    }
                }
            }
        }
    }
}