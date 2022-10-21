// Author: Shimrra Shai
// Date Created: UE+1665.1959 Ms (2022-10-07)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using KSP.Localization;
using UnityEngine;

using SDF.SmartResources;
using SDF.SmartResources.DataBinding;

namespace Cochrane
{
    // Defines a controller interface for the FSM part module.
    public interface IFsmPartModuleController<TState> where TState : FsmState<TState>
    {
        Dictionary<string, KSPEventBinding> Events { get; }
        Dictionary<string, IKSPFieldBinding> Fields { get; }

        Part ForegroundPart { get; }

        void SetFieldValue<T>(string fieldName, T newFieldValue);
        T GetFieldValue<T>(string fieldName);

        List<string> GetInterestInputResources();
        List<string> GetInterestOutputResources();
        List<string> GetInterestInternalResources();

        void SetResourceProduction(string resourceName, double productionRate);
        void SetResourceConsumption(string resourceName, double consumptionRate);

        void ChangeState(TState newState);
    }
}