// Author: Shimrra Shai
// Date Created: UE+1655.6178 Ms (2022-06-18)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines a flow node which consumes resource.
            public class FlowConsumer : FlowNode, IInFlowNode
            {
                public double ConsumptionRate { get; set; }
                
                public List<InFlowEdge> InEdges { get; }

                public FlowConsumer(string nodeID, string resourceName) : base(nodeID, resourceName)
                {
                    ConsumptionRate = 0.0;
                    InEdges = new List<InFlowEdge>();
                }

                public override void BakeToInnerFlowGraph(InnerFlowGraph innerFlowGraph)
                {
                    innerFlowGraph.AddInnerFlowNode(NodeID);
                    innerFlowGraph.JoinInnerFlowNodes(NodeID, "_aggregateSink");
                    
                    InnerFlowEdge outEdge = innerFlowGraph.GetInnerFlowNode(NodeID).OutEdges[0];
                    outEdge.FlowCapacity = ConsumptionRate;
                }
            }
        }
    }
}