// Author: Shimrra Shai
// Date Created: UE+1665.1144 Ms (2022-10-06)
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
        // Defines the foreground part module component that integrates with the KSP game. Overrides should ONLY define
        // KSPFields and KSPEvents - the logic should go in the corresponding PartModuleProcessor instead.
        public abstract class SmartPartModuleBase : PartModule, IPartModuleDataProvider
        {
            private PartModuleProcessor mBackgroundProcessor = null;

            protected abstract PartModuleProcessor CreateBackgroundProcessorObject();

            public void SetFieldValue<T>(string fieldName, T newFieldValue)
            {
                // Reflectively get the appropriate KSPField
                var spmType = GetType();
                var field = spmType.GetField(fieldName);
                if(field != null)
                {
                    var kspAttributes = field.GetCustomAttributes(false);
                    foreach(var attr in kspAttributes)
                    {
                        if(attr is KSPField)
                        {
                            field.SetValue(this, newFieldValue);
                            break;
                        }
                    }
                }
            }

            public T GetFieldValue<T>(string fieldName)
            {
                // Reflectively get the appropriate KSPField
                var spmType = GetType();
                var field = spmType.GetField(fieldName);
                if(field != null)
                {
                    var kspAttributes = field.GetCustomAttributes(false);
                    foreach(var attr in kspAttributes)
                    {
                        if(attr is KSPField)
                        {
                            return (T)field.GetValue(this);
                        }
                    }
                }

                throw new ArgumentException("Unable to find KSPField named '" + fieldName + 
                    "', or the field has the wrong type");
            }

            public PartModuleProcessor SpawnSeparateBackgroundProcessor()
            {
                var backgroundProcessor = CreateBackgroundProcessorObject();
                backgroundProcessor.AddBindingsFrom(this);
                
                /*
                foreach(string key in Events.Keys)
                {*/
                    /*
                    foreach(System.Attribute attr in fieldInfo.GetCustomAttributes(mBindingLigand))
                    {
                        if(attr is KSPField)
                        {
                            mBindingLigand = bindingLigand;
                            mBindingFieldInfo = fieldInfo;
                            mBindingKspField = bindingLigand.Fields[mBoundFieldName];
                            return;
                        }
                    }
                    */
                /*}*/

                return backgroundProcessor;
            }

            private void SpawnBackgroundProcessorOnboard(Guid spawnGUID)
            {
                if(mBackgroundProcessor == null)
                {
                    mBackgroundProcessor = SpawnSeparateBackgroundProcessor();
                    mBackgroundProcessor.AssociateGUID(spawnGUID);
                    mBackgroundProcessor.BindForegroundModule(this);
                }
            }

            public void SpawnBackgroundProcessorOnboard()
            {
                SpawnBackgroundProcessorOnboard(Guid.NewGuid());
            }

            public PartModuleProcessor GetBackgroundProcessor()
            {
                return mBackgroundProcessor;
            }
            
            public override bool IsStageable()
            {
                if(mBackgroundProcessor != null)
                {
                    return mBackgroundProcessor.IsStageable();
                }
                else
                {
                    return false;
                }
            }

            public override string GetModuleDisplayName()
            {
                if(mBackgroundProcessor != null)
                {
                    return mBackgroundProcessor.GetModuleDisplayName();
                }
                else
                {
                    return "*UNDEFINED*";
                }
            }

            public override void OnLoad(ConfigNode configNode)
            {
                Debug.Log("SmartResources: SmartPartModuleBase OnLoad");

                base.OnLoad(configNode);

                string guidStr = "";
                Debug.Log(" - Checking if data is available...");
                if(configNode.TryGetValue("BackgroundProcessorGUID", ref guidStr))
                {
                    Debug.Log(" - Data is available");
                    Guid backgroundProcessorGUID = Guid.Parse(guidStr);
                    if((backgroundProcessorGUID != Guid.Empty) && (mBackgroundProcessor == null))
                    {
                        SpawnBackgroundProcessorOnboard(backgroundProcessorGUID);

                        if(configNode.HasNode("BackgroundProcessorExtras"))
                        {
                            mBackgroundProcessor.OnLoad(configNode.GetNode("BackgroundProcessorExtras"));
                        }
                    }
                }
            }

            public override void OnSave(ConfigNode configNode)
            {
                Debug.Log("SmartResources: SmartPartModuleBase OnSave");

                base.OnSave(configNode);

                if(mBackgroundProcessor != null)
                {
                    configNode.AddValue("BackgroundProcessorGUID", mBackgroundProcessor.GUID.ToString());
                    
                    ConfigNode extrasNode = null;
                    if(!configNode.HasNode("BackgroundProcessorExtras"))
                    {
                        extrasNode = configNode.AddNode("BackgroundProcessorExtras");
                    }
                    else
                    {
                        extrasNode = configNode.GetNode("BackgroundProcessorExtras");
                    }

                    mBackgroundProcessor.OnSave(extrasNode);
                }
            }
        }

        public abstract class SmartPartModule<TProcessor> : SmartPartModuleBase where TProcessor : PartModuleProcessor, 
            new()
        {
            protected override PartModuleProcessor CreateBackgroundProcessorObject()
            {
                return new TProcessor();
            }

            protected TProcessor GetTypedBackgroundProcessor()
            {
                return GetBackgroundProcessor() as TProcessor;
            }
        }
    }
}
