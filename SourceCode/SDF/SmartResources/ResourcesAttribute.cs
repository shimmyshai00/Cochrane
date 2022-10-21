// Author: Shimrra Shai
// Date Created: UE+1653.1728 Ms (2022-05-21)
using System;

namespace SDF
{
    // Defines an attribute for specifying resources required and produced by a SmartModule.
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ResourcesAttribute : System.Attribute
    {
        public string[] inputs;
        public string[] outputs;
        public string[] internals;
    }
}