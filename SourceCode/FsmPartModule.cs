// Author: Shimrra Shai
// Date Created: UE+1665.1953 Ms (2022-10-07)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using KSP.Localization;
using UnityEngine;

using SDF.SmartResources;

namespace Cochrane
{
    // Defines a base for a state machine-based part module.
    public abstract class FsmPartModule<TState> : PartModuleProcessor, IFsmPartModuleController<TState> 
        where TState : FsmState<TState>
    {
        private TState mInitialState;
        private TState mLoadState = null;
        private ConfigNode mLoadNode = null;
        private TState mCurState = null;

        protected TState CurState
        {
            get
            {
                return mCurState;
            }
        }

        public FsmPartModule(TState initialState)
        {
            mInitialState = initialState;
            mLoadState = null;
        }

        public void ChangeState(TState newState)
        {
            if(mCurState != null)
            {
                mCurState.OnExit();
            }

            mCurState = newState;
            mCurState.SetPartModuleController(this);
            mCurState.OnEnter();
        }

        public bool IsInState<TWhich>() where TWhich : TState
        {
            return (mCurState is TWhich);
        }

        public override void OnLoad(ConfigNode node) 
        { 
            string stateTypeName = "";

            TState loadedState = null;
            if(node.TryGetValue("CurStateType", ref stateTypeName))
            {
                // Reflectively construct this state type
                Type stateType = Type.GetType(stateTypeName);
                var stateObj = Activator.CreateInstance(stateType);
                loadedState = (TState)(object)stateObj;
            }

            mLoadState = loadedState;
            mLoadNode = node;
        }

        public override void OnReady()
        {
            base.OnReady();

            if(mCurState == null)
            {
                if(mLoadState != null)
                {
                    ChangeState(mLoadState);

                    mLoadState.OnLoad(mLoadNode);

                    mLoadState = null;
                    mLoadNode = null;
                }
                else
                {
                    ChangeState(mInitialState);
                }
            }
        }

        public override void OnSave(ConfigNode node) 
        { 
            var stateTypeName = mCurState.GetType().AssemblyQualifiedName;
            node.AddValue("CurStateType", stateTypeName);
            
            mCurState.OnSave(node);
        }

        public override void OnVesselWasModified()
        {
            mCurState.OnVesselWasModified();
        }

        public override void OnFlow(Dictionary<string, ResourceFlowStat> flowStats, double deltaTime) 
        { 
            mCurState.OnFlow(flowStats, deltaTime);
        }

        public override void OnFixedUpdate() 
        { 
            mCurState.OnFixedUpdate();
        }
    }
}