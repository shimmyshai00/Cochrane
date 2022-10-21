// Author: Shimrra Shai
// Date Created: UE+1666.0716 Ms (2022-10-17)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace DataBinding
        {
            // Defines an interface for a KSP field binding that elides the type-specific information.
            public interface IKSPFieldBinding
            {
                bool guiActive { get; set; }
                bool guiActiveEditor { get; set; }

                void Bind(PartModule bindingLigand);
                void Release();
            }
        }
    }
}