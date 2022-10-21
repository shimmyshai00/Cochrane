// Author: Shimrra Shai
// Date Created: UE+1655.6729 Ms (2022-06-19)
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using SDF.SmartResources.FlowEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowGraphManagerDetail
        {
            // Defines an interface for objects which generate flow graphs for vessel.
            public interface IFlowGraphGenerator
            {
                void GenerateFlowGraphFor(AggregateFlowGraph flowGraph, Vessel v);
            }
        }
    }
}