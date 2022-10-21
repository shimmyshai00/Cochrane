// Author: Shimrra Shai
// Date Created: UE+1653.3327 Ms (2022-05-23)
using System;

namespace SDF
{
    // Defines an attribute for specifying flow parameters.
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class FlowParamsAttribute : System.Attribute
    {
        public string flowGroupName;
    }
}