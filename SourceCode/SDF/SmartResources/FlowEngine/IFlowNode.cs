// Author: Shimrra Shai
// Date Created: UE+1655.6175 Ms (2022-06-18)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines the interfaces for flow nodes.
            public interface IFlowNode
            {
                string NodeID { get; }
                string ResourceName { get; }

                void BakeToInnerFlowGraph(InnerFlowGraph innerFlowGraph);
            }

            public interface IInFlowNode : IFlowNode
            {
                List<InFlowEdge> InEdges { get; }
            }

            public interface IOutFlowNode : IFlowNode
            {
                List<OutFlowEdge> OutEdges { get; }
            }

            public interface IBidirectionalFlowNode : IInFlowNode, IOutFlowNode { }
        }
    }
}