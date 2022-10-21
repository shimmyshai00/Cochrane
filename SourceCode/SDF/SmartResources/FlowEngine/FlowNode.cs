// Author: Shimrra Shai
// Date Created: UE+1655.6167 Ms (2022-06-18)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines the base class for flow nodes.
            public abstract class FlowNode : IFlowNode
            {
                public string NodeID { get; }
                public string ResourceName { get; }

                public FlowNode(string nodeID, string resourceName)
                {
                    NodeID = nodeID;
                    ResourceName = resourceName;
                }
                
                public abstract void BakeToInnerFlowGraph(InnerFlowGraph innerFlowGraph);
            }
        }
    }
}
