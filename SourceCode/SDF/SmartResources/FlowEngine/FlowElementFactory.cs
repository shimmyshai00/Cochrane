// Author: Shimrra Shai
// Date Created: UE+1655.6730 Ms (2022-06-18)
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Wraps methods to produce different kinds of flow elements.
            public class FlowElementFactory
            {
                public static FlowElement CreateManagedModuleElement(PartModuleProcessor pmp)
                {
                    string elID = "ManagedPartModule." + pmp.GUID.ToString();
                    FlowElement el = new FlowElement(elID);

                    foreach(string resource in pmp.GetInterestInputResources())
                    {
                        string consumerID = elID + ":Consumer." + resource;
                        if(!el.HasFlowNode(consumerID))
                        {
                            el.AddFlowNode(new FlowConsumer(consumerID, resource));
                        }
                    }

                    foreach(string resource in pmp.GetInterestOutputResources())
                    {
                        string producerID = elID + ":Producer." + resource;
                        if(!el.HasFlowNode(producerID))
                        {
                            el.AddFlowNode(new FlowProducer(producerID, resource));
                        }
                    }

                    // NB: still treats as possibly aggregated with outside
                    foreach(string resource in pmp.GetInterestInternalResources())
                    {
                        string producerID = elID + ":Producer." + resource;
                        string consumerID = elID + ":Consumer." + resource;

                        if(!el.HasFlowNode(producerID))
                        {
                            el.AddFlowNode(new FlowProducer(producerID, resource));
                        }
                        
                        if(!el.HasFlowNode(consumerID))
                        {
                            el.AddFlowNode(new FlowConsumer(consumerID, resource));
                        }
                    }

                    return el;
                }
            }
        }
    }
}