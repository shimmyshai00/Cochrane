// Author: Shimrra Shai
// Date Created: UE+1655.6180 Ms (2022-06-18)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines a flow node which joins flows together.
            public class FlowJunction : FlowNode, IBidirectionalFlowNode
            {
                public List<OutFlowEdge> OutEdges { get; }
                public List<InFlowEdge> InEdges { get; }

                public FlowJunction(string nodeID, string resourceName) : base(nodeID, resourceName)
                {
                    OutEdges = new List<OutFlowEdge>();
                    InEdges = new List<InFlowEdge>();
                }

                public override void BakeToInnerFlowGraph(InnerFlowGraph innerFlowGraph)
                {
                    innerFlowGraph.AddInnerFlowNode(NodeID);
                }
            }
        }
    }
}