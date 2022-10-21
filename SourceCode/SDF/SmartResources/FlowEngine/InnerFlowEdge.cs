// Author: Shimrra Shai
// Date Created: UE+1655.8483 Ms (2022-06-21)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines an edge of an inner graph.
            public class InnerFlowEdge
            {
                public InnerFlowNode SrcNode { get; }
                public InnerFlowNode DestNode { get; }
                public double FlowCapacity { get; set; }
                public double FlowBorne { get; set; }
                public OutFlowEdge MainGraphOutFlowEdgeRef { get; set; }
                public InFlowEdge MainGraphInFlowEdgeRef { get; set; }

                public InnerFlowEdge(InnerFlowNode srcNode, InnerFlowNode destNode)
                {
                    SrcNode = srcNode;
                    DestNode = destNode;
                    FlowCapacity = double.PositiveInfinity;
                    FlowBorne = 0.0;
                    MainGraphOutFlowEdgeRef = null;
                    MainGraphInFlowEdgeRef = null;
                }
            }
        }
    }
}