// Author: Shimrra Shai
// Date Created: UE+1666.1404 Ms (2022-10-18)
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
            // Defines a class representing an event trigger. For modifying the GUI settings only.
            public class KSPEventBinding
            {
                private PartModule mBindingLigand;
                private MethodInfo mBindingMethodInfo;
                private BaseEvent mBindingKspEvent;
                private string mBoundEventName;

                private bool mGuiActive = false;
                private bool mGuiActiveEditor = false;

                // these naming-convention ugly (not consistent capitalization with the rest of *this* program)
                // properties are for consistency with KSP itself
                public bool guiActive
                {
                    get
                    {
                        if(mBindingLigand == null)
                        {
                            return mGuiActive;
                        }
                        else
                        {
                            return mBindingKspEvent.guiActive;
                        }
                    }
                    set
                    {
                        mGuiActive = value;
                        if(mBindingLigand != null)
                        {
                            mBindingKspEvent.guiActive = mGuiActive;
                        }
                        else
                        {
                            Debug.Log("Null ligand encountered");
                        }
                    }
                }

                public bool guiActiveEditor
                {
                    get
                    {
                        if(mBindingLigand == null)
                        {
                            return mGuiActiveEditor;
                        }
                        else
                        {
                            return mBindingKspEvent.guiActiveEditor;
                        }
                    }
                    set
                    {
                        mGuiActiveEditor = value;
                        if(mBindingLigand != null)
                        {
                            mBindingKspEvent.guiActiveEditor = mGuiActiveEditor;
                        }
                    }
                }

                public KSPEventBinding(string eventName)
                {
                    mBindingLigand = null;
                    mBindingMethodInfo = null;
                    mBindingKspEvent = null;
                    mBoundEventName = eventName;
                }

                public void Bind(PartModule bindingLigand)
                {
                    // Introspect this object to obtain the information required to attach the method. Note that it
                    // *must* be a KSPEvent-annotated method.
                    var objType = bindingLigand.GetType();
                    var methodInfo = objType.GetMethod(mBoundEventName);
                    if(methodInfo != null)
                    {
                        foreach(System.Attribute attr in methodInfo.GetCustomAttributes(mBindingLigand))
                        {
                            if(attr is KSPEvent)
                            {
                                mBindingLigand = bindingLigand;
                                mBindingMethodInfo = methodInfo;
                                mBindingKspEvent = bindingLigand.Events[mBoundEventName];
                                return;
                            }
                        }

                        throw new ArgumentException("Tried to bind an event '" + mBoundEventName + "' not a KSPEvent");
                    }
                    else
                    {
                        throw new ArgumentException("Passed a ligand with no event '" + mBoundEventName + "'");
                    }
                }

                public void Release()
                {
                    if(mBindingLigand != null)
                    {
                        mGuiActive = mBindingKspEvent.guiActive;
                        mGuiActiveEditor = mBindingKspEvent.guiActiveEditor;
                    }

                    mBindingLigand = null;
                    mBindingMethodInfo = null;
                    mBindingKspEvent = null;
                }
            }
        }
    }
}