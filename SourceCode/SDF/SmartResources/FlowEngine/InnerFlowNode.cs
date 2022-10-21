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
            // Defines a flow node of an inner graph.
            public class InnerFlowNode
            {
                public string NodeID { get; }
                public bool Explored { get; set; }

                public List<InnerFlowEdge> OutEdges { get; }
                public List<InnerFlowEdge> InEdges { get; }

                public InnerFlowNode(string nodeID)
                {
                    NodeID = nodeID;
                    Explored = false;

                    OutEdges = new List<InnerFlowEdge>();
                    InEdges = new List<InnerFlowEdge>();
                }
            }
        }
    }
}