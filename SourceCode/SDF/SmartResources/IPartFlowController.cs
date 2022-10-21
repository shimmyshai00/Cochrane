// Author: Shimrra Shai
// Date Created: UE+1665.1929 Ms (2022-10-07)
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using SDF.SmartResources.FlowEngine;
using SDF.SmartResources.FlowGraphManagerDetail;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        // Defines an interface for controlling resource flows.
        public interface IPartFlowController
        {
            void SetResourceProduction(string resourceName, double newProductionRate);
            void SetResourceConsumption(string resourceName, double newConsumptionRate);
        }
    }
}