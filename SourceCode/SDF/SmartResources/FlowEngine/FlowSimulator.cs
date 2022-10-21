// Author: Shimrra Shai
// Date Created: UE+1655.6748 Ms (2022-06-19)
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Simulates the flow through a flow graph, updating tanks and returning the amount of flows delivered to 
            // each consumer and producer.
            public class SimResult
            {
                // note that these are total amounts of resource (U) not rates (U/s)
                public Dictionary<string, double> ConsumerFlowTotals { get; }
                public Dictionary<string, double> ProducerFlowTotals { get; }
                
                // this can be used to get average rates
                public double DeltaTime { get; }

                public SimResult(double deltaTime)
                {
                    ConsumerFlowTotals = new Dictionary<string, double>();
                    ProducerFlowTotals = new Dictionary<string, double>();
                    DeltaTime = deltaTime;
                }
            }

            public class FlowSimulator
            {
                private FlowSolver mSolver = new FlowSolver();

                public SimResult SimulateFlows(AggregateFlowGraph flowGraph, double curTime, double deltaTime)
                {
                    SimResult rv = new SimResult(deltaTime);

                    if(curTime == 0.0)
                    {
                        // Do initial solution
                        mSolver.SolveFlowGraph(flowGraph);
                    }
                    
                    double curDeltaTime = 0.0;
                    while(curDeltaTime < deltaTime)
                    {
                        // Find which tanks will deplete first, then sim up to that point (their advertised flow rates will
                        // change to 0 once depleted).
                        double earliestDepletionTimeFromNow = double.PositiveInfinity;
                        foreach(string flowNodeID in flowGraph.GetFlowNodeIDs())
                        {
                            FlowNode flowNode = flowGraph.GetFlowNode(flowNodeID);
                            if(flowNode is FlowTank)
                            {
                                FlowTank tank = flowNode as FlowTank;

                                double totalInFlow = 0.0;
                                foreach(InFlowEdge inEdge in tank.InEdges)
                                {
                                    totalInFlow += inEdge.FlowBorne;
                                }

                                double totalOutFlow = 0.0;
                                foreach(OutFlowEdge outEdge in tank.OutEdges)
                                {
                                    totalOutFlow += outEdge.FlowBorne;
                                }

                                double netFlow = totalOutFlow - totalInFlow;
                                double netAmountRemoved = netFlow * deltaTime;
                                double depletionTimeFromNow = double.PositiveInfinity;
                                if(netFlow > 0.0)
                                {
                                    depletionTimeFromNow = tank.Amount / netFlow;
                                }

                                if(depletionTimeFromNow < earliestDepletionTimeFromNow)
                                {
                                    earliestDepletionTimeFromNow = depletionTimeFromNow;
                                }
                            }
                        }

                        double nextSimInterval = Math.Min(earliestDepletionTimeFromNow, deltaTime - curDeltaTime);

                        // Add up the flows and draw on the tanks.
                        foreach(string flowNodeID in flowGraph.GetFlowNodeIDs())
                        {
                            FlowNode flowNode = flowGraph.GetFlowNode(flowNodeID);
                            if(flowNode is FlowConsumer)
                            {
                                FlowConsumer consumer = flowNode as FlowConsumer;
                                if(!rv.ConsumerFlowTotals.ContainsKey(consumer.NodeID))
                                {
                                    rv.ConsumerFlowTotals.Add(consumer.NodeID, 0.0);
                                }

                                foreach(InFlowEdge inEdge in consumer.InEdges)
                                {
                                    rv.ConsumerFlowTotals[consumer.NodeID] += inEdge.FlowBorne * nextSimInterval;
                                }
                            }
                            else if(flowNode is FlowProducer)
                            {
                                FlowProducer producer = flowNode as FlowProducer;
                                if(!rv.ProducerFlowTotals.ContainsKey(producer.NodeID))
                                {
                                    rv.ProducerFlowTotals.Add(producer.NodeID, 0.0);
                                }

                                foreach(OutFlowEdge outEdge in producer.OutEdges)
                                {
                                    rv.ProducerFlowTotals[producer.NodeID] += outEdge.FlowBorne * nextSimInterval;
                                }
                            }
                            else if(flowNode is FlowTank)
                            {
                                FlowTank tank = flowNode as FlowTank;
                                
                                // aaaand this is how we avoid the infamous "time warp phantom drain" issue - we now have
                                // simultaneous availability of all supplies and demands on the tank!
                                double totalOutFlow = 0.0;
                                double totalInFlow = 0.0;

                                foreach(OutFlowEdge outEdge in tank.OutEdges)
                                {
                                    totalOutFlow += outEdge.FlowBorne;
                                }

                                foreach(InFlowEdge inEdge in tank.InEdges)
                                {
                                    totalInFlow += inEdge.FlowBorne;
                                }

                                double netDrawRate = totalOutFlow - totalInFlow;
                                tank.WithdrawResource(netDrawRate * nextSimInterval);
                            }
                        }

                        // Recalculate the flow graph and move on to the next step.
                        curDeltaTime += nextSimInterval;
                        if(curDeltaTime < deltaTime)
                        {
                            Debug.Log("Recalculating flow graph - curDeltaTime = " + curDeltaTime.ToString() + ", deltaTime = " + deltaTime.ToString());
                            mSolver.SolveFlowGraph(flowGraph);
                            //Debug.Log("--- Fully solved flow graph ---");
                            //flowGraph.DebugDump();
                            //Debug.Log("-------------------------------");
                        }
                    }

                    return rv;
                }
            }
        }
    }
}