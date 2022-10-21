// Author: Shimrra Shai
// Date Created: UE+1665.1139 Ms (2022-10-06)
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using SDF.SmartResources.FlowEngine;
using SDF.SmartResources.FlowGraphManagerDetail;
using SDF.SmartResources.DataBinding;

using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        // Defines the portion of a part module containing the processing logic. The user should derive their part
        // module logic from this class instead of from PartModule.
        public abstract class PartModuleProcessor
        {
            public class ResourceFlowStat
            {
                public double InFlowAmount { get; set; }
                public double OutFlowAmount { get; set; }

                public ResourceFlowStat(double inFlowAmount, double outFlowAmount)
                {
                    InFlowAmount = inFlowAmount;
                    OutFlowAmount = outFlowAmount;
                }
            }
            
            private IPartFlowController mPartFlowController = null;

            private Guid mPartModuleProcessorGUID = Guid.Empty;

            private List<string> mInputInterestResources;
            private List<string> mOutputInterestResources;
            private List<string> mInternalInterestResources;

            private SmartPartModuleBase mForegroundModule = null;
            private Dictionary<string, KSPEventBinding> mEventBindings = new Dictionary<string, KSPEventBinding>();
            private Dictionary<string, IKSPFieldBinding> mFieldBindings = new Dictionary<string, IKSPFieldBinding>();

            public Dictionary<string, KSPEventBinding> Events
            {
                get
                {
                    return mEventBindings;
                }
            }

            public Dictionary<string, IKSPFieldBinding> Fields
            {
                get
                {
                    return mFieldBindings;
                }
            }

            public Part ForegroundPart
            {
                get
                {
                    if(mForegroundModule != null)
                    {
                        return mForegroundModule.part;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public Guid GUID
            {
                get
                {
                    return mPartModuleProcessorGUID;
                }
            }

            public PartModuleProcessor()
            { 
                mInputInterestResources = new List<string>();
                mOutputInterestResources = new List<string>();
                mInternalInterestResources = new List<string>();

                Type pmpType = this.GetType();

                System.Attribute[] attrs = System.Attribute.GetCustomAttributes(pmpType);
                foreach(System.Attribute attr in attrs)
                {
                    if(attr is ResourcesAttribute)
                    {
                        ResourcesAttribute resources = attr as ResourcesAttribute;
                        if(resources.inputs != null)
                        {
                            foreach(string inResourceName in resources.inputs)
                            {
                                if(!mInputInterestResources.Contains(inResourceName))
                                {
                                    mInputInterestResources.Add(inResourceName);
                                }
                            }
                        }

                        if(resources.outputs != null)
                        {
                            foreach(string outResourceName in resources.outputs)
                            {
                                if(!mOutputInterestResources.Contains(outResourceName))
                                {
                                    mOutputInterestResources.Add(outResourceName);
                                }
                            }
                        }

                        if(resources.internals != null)
                        {
                            foreach(string internalResourceName in resources.internals)
                            {
                                if(!mInternalInterestResources.Contains(internalResourceName))
                                {
                                    mInternalInterestResources.Add(internalResourceName);
                                }
                            }
                        }
                    }
                }
            }

            public void AddEventBinding(string bindingName)
            {
                mEventBindings.Add(bindingName, new KSPEventBinding(bindingName));
            }

            public void AddFieldBinding<T>(string bindingName, T defaultValue)
            {
                mFieldBindings.Add(bindingName, new KSPFieldBinding<T>(bindingName, defaultValue));
            }

            public void AddBindingsFrom(SmartPartModuleBase partModule)
            {
                Type selfType = typeof(PartModuleProcessor);
                Type objType = partModule.GetType();

                foreach(var mthdInfo in objType.GetMethods())
                {
                    foreach(System.Attribute attr in mthdInfo.GetCustomAttributes(false))
                    {
                        if(attr is KSPEvent)
                        {
                            Debug.Log(" - Found KSPEvent: " + mthdInfo.Name);
                            AddEventBinding(mthdInfo.Name);
                        }
                    }
                }

                foreach(var fieldInfo in objType.GetFields())
                {
                    foreach(System.Attribute attr in fieldInfo.GetCustomAttributes(false))
                    {
                        if(attr is KSPField)
                        {
                            Debug.Log(" - Found KSPField: " + fieldInfo.Name);
                            MethodInfo selfAddMethod = selfType.GetMethod("AddFieldBinding");
                            MethodInfo selfSpecifAddMethod = selfAddMethod.MakeGenericMethod(fieldInfo.FieldType);
                            selfSpecifAddMethod.Invoke(this, new object[] { fieldInfo.Name, 
                                fieldInfo.GetValue(partModule) });
                        }
                    }
                }
            }

            public void BindForegroundModule(SmartPartModuleBase foregroundModule)
            {
                if(mForegroundModule == null)
                {
                    foreach(var eventBinding in mEventBindings.Values)
                    {
                        eventBinding.Bind(foregroundModule);
                    }

                    foreach(var fieldBinding in mFieldBindings.Values)
                    {
                        fieldBinding.Bind(foregroundModule);
                    }

                    mForegroundModule = foregroundModule;
                }
            }

            public void ReleaseForegroundModule()
            {
                if(mForegroundModule != null)
                {
                    foreach(var eventBinding in mEventBindings.Values)
                    {
                        eventBinding.Release();
                    }

                    foreach(var fieldBinding in mFieldBindings.Values)
                    {
                        fieldBinding.Release();
                    }

                    mForegroundModule = null;
                }
            }

            public void SetFieldValue<T>(string fieldName, T newFieldValue)
            {
                (mFieldBindings[fieldName] as KSPFieldBinding<T>).Value = newFieldValue;
            }

            public T GetFieldValue<T>(string fieldName)
            {
                return (mFieldBindings[fieldName] as KSPFieldBinding<T>).Value;
            }

            public void AssociateGUID(Guid guid)
            {
                if(mPartModuleProcessorGUID == Guid.Empty)
                {
                    mPartModuleProcessorGUID = guid;
                }
                else
                {
                    Debug.Log("SmartResources PartModuleProcessor: Cannot alter part module processor GUID!");
                }
            }

            public List<string> GetInterestInputResources()
            {
                return new List<string>(mInputInterestResources);
            }

            public List<string> GetInterestOutputResources()
            {
                return new List<string>(mOutputInterestResources);
            }

            public List<string> GetInterestInternalResources()
            {
                return new List<string>(mInternalInterestResources);
            }

            public void SetResourceProduction(string resourceName, double productionRate)
            {
                if(mPartFlowController != null)
                {
                    mPartFlowController.SetResourceProduction(resourceName, productionRate);
                }
            }

            public void SetResourceConsumption(string resourceName, double consumptionRate)
            {
                if(mPartFlowController != null)
                {
                    mPartFlowController.SetResourceConsumption(resourceName, consumptionRate);
                }
            }

            public void SetPartModuleFlowController(IPartFlowController flowController)
            {
                mPartFlowController = flowController;
            }

            public virtual bool IsStageable()
            {
                return false;
            }

            public virtual string GetModuleDisplayName()
            {
                return "*UNDEFINED*";
            }

            public virtual void OnCreate() { }
            public virtual void OnReady() { }

            public virtual void OnLoad(ConfigNode configNode) { }
            public virtual void OnSave(ConfigNode configNode) { }

            public virtual void OnVesselWasModified() { }

            public virtual void OnFlow(Dictionary<string, ResourceFlowStat> flowStats, double deltaTime) { }

            public virtual void OnFixedUpdate() { }
        }
    }
}