// Author: Shimrra Shai
// Date Created: UE+1655.6272 Ms (2022-06-19)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines an interface for flow graphs and flow graph-like objects.
            public interface IFlowGraph
            {
                bool HasFlowNode(string flowNodeID);
                
                List<string> GetFlowNodeIDs();
                FlowNode GetFlowNode(string flowNodeID);

                void AddFlowNode(FlowNode flowNode);
                
                void ConnectFlowNodes(string flowNodeID1, string flowNodeID2);
            }
        }
    }
}