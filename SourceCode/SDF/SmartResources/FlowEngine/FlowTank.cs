// Author: Shimrra Shai
// Date Created: UE+1655.6179 Ms (2022-06-18)
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Defines a flow node which acts as a tank.
            public abstract class FlowTank : FlowNode, IBidirectionalFlowNode
            {
                public abstract double Capacity { get; }
                public abstract double Amount { get; }

                public List<OutFlowEdge> OutEdges { get; }
                public List<InFlowEdge> InEdges { get; }

                public FlowTank(string nodeID, string resourceName) : base(nodeID, resourceName)
                {
                    OutEdges = new List<OutFlowEdge>();
                    InEdges = new List<InFlowEdge>();
                }

                public abstract void WithdrawResource(double amount);
                public abstract void ContributeResource(double amount);

                public override void BakeToInnerFlowGraph(InnerFlowGraph innerFlowGraph)
                {
                    string sourceNodeID = NodeID + ".Source";
                    string sinkNodeID = NodeID + ".Sink";
                    string junctionNodeID = NodeID;

                    if(Amount == 0.0)
                    {
                        // Can act as sink only
                        innerFlowGraph.AddInnerFlowNode(sinkNodeID);
                        innerFlowGraph.AddInnerFlowNode(junctionNodeID);

                        innerFlowGraph.JoinInnerFlowNodes(sinkNodeID, "_aggregateSink");
                        innerFlowGraph.JoinInnerFlowNodes(junctionNodeID, sinkNodeID);
                    }
                    else if(Amount == Capacity)
                    {
                        // Can act as source only
                        innerFlowGraph.AddInnerFlowNode(sourceNodeID);
                        innerFlowGraph.AddInnerFlowNode(junctionNodeID);

                        innerFlowGraph.JoinInnerFlowNodes("_aggregateSource", sourceNodeID);
                        innerFlowGraph.JoinInnerFlowNodes(sourceNodeID, junctionNodeID);
                    }
                    else
                    {
                        // Can act as both
                        innerFlowGraph.AddInnerFlowNode(sinkNodeID);
                        innerFlowGraph.AddInnerFlowNode(sourceNodeID);
                        innerFlowGraph.AddInnerFlowNode(junctionNodeID);

                        innerFlowGraph.JoinInnerFlowNodes("_aggregateSource", sourceNodeID);
                        innerFlowGraph.JoinInnerFlowNodes(sinkNodeID, "_aggregateSink");
                        innerFlowGraph.JoinInnerFlowNodes(sourceNodeID, junctionNodeID);
                        innerFlowGraph.JoinInnerFlowNodes(junctionNodeID, sinkNodeID);
                    }
                }
            }
        }
    }
}