// Author: Shimrra Shai
// Date Created: UE+1666.0695 Ms (2022-10-17)
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
            // Defines a class representing a value field that be both assigned to and bound/unbound from a KSPField-
            // containing PartModule ligand. When the field is updated here, it will update that on the ligand, and 
            // when the ligand is updated, the field here is updated as well.
            public class KSPFieldBinding<T> : IKSPFieldBinding
            {
                private T mBoundValue;
                private PartModule mBindingLigand;
                private FieldInfo mBindingFieldInfo;
                private BaseField mBindingKspField;
                private string mBoundFieldName;

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
                            return mBindingKspField.guiActive;
                        }
                    }
                    set
                    {
                        mGuiActive = value;
                        if(mBindingLigand != null)
                        {
                            mBindingKspField.guiActive = mGuiActive;
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
                            return mBindingKspField.guiActiveEditor;
                        }
                    }
                    set
                    {
                        mGuiActiveEditor = value;
                        if(mBindingLigand != null)
                        {
                            mBindingKspField.guiActiveEditor = mGuiActiveEditor;
                        }
                    }
                }

                public T Value
                {
                    get
                    {
                        if(mBindingLigand == null)
                        {
                            return mBoundValue;
                        }
                        else
                        {
                            return (T)(mBindingFieldInfo.GetValue(mBindingLigand));
                        }
                    }
                    set
                    {
                        mBoundValue = value;
                        if(mBindingLigand != null)
                        {
                            mBindingFieldInfo.SetValue(mBindingLigand, mBoundValue);
                        }
                    }
                }

                public KSPFieldBinding(string fieldName, T boundValue)
                {
                    mBoundValue = boundValue;
                    mBindingLigand = null;
                    mBindingFieldInfo = null;
                    mBindingKspField = null;
                    mBoundFieldName = fieldName;
                }

                public void Bind(PartModule bindingLigand)
                {
                    // Introspect this object to obtain the information required to attach the field. Note that it
                    // *must* be a KSPField-annotated field.
                    var objType = bindingLigand.GetType();
                    var fieldInfo = objType.GetField(mBoundFieldName);
                    if(fieldInfo != null)
                    {
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

                        throw new ArgumentException("Tried to bind a field '" + mBoundFieldName + "' not a KSPField");
                    }
                    else
                    {
                        throw new ArgumentException("Passed a ligand with no field '" + mBoundFieldName + "'");
                    }
                }

                public void Release()
                {
                    if(mBindingLigand != null)
                    {
                        mGuiActive = mBindingKspField.guiActive;
                        mGuiActiveEditor = mBindingKspField.guiActiveEditor;
                        mBoundValue = (T)(mBindingFieldInfo.GetValue(mBindingLigand));
                    }

                    mBindingLigand = null;
                    mBindingFieldInfo = null;
                    mBindingKspField = null;
                }
            }
        }
    }
}