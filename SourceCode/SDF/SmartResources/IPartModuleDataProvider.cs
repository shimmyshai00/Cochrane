// Author: Shimrra Shai
// Date Created: UE+1665.1141 Ms (2022-10-06)
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
        // Defines an interface for accessing the field data for a part module so we can smooth over whether that data
        // comes from the foreground or the background.
        public interface IPartModuleDataProvider
        {
            BaseEventList Events { get; }
            BaseFieldList Fields { get; }

            void SetFieldValue<T>(string fieldName, T newFieldValue);
            T GetFieldValue<T>(string fieldName);
        }
    }
}