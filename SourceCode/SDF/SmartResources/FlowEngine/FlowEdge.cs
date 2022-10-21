// Author: Shimrra Shai
// Date Created: UE+1655.6176 Ms (2022-06-18)
using System.Collections.Generic;
using System.Linq;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines an edge in a flow graph. This can be thought of as like a pipe.
            public abstract class FlowEdge
            {
                public double FlowCapacity { get; set; }
                public double FlowBorne { get; set; }

                public FlowEdge()
                {
                    FlowCapacity = double.PositiveInfinity;
                    FlowBorne = 0.0;
                }
            }

            public class OutFlowEdge : FlowEdge
            {
                public IInFlowNode DestinationNode { get; }

                public OutFlowEdge(IInFlowNode destinationNode)
                {
                    DestinationNode = destinationNode;
                }
            }

            public class InFlowEdge : FlowEdge
            {
                public IOutFlowNode SourceNode { get; }

                public InFlowEdge(IOutFlowNode sourceNode)
                {
                    SourceNode = sourceNode;
                }
            }
        }
    }
}