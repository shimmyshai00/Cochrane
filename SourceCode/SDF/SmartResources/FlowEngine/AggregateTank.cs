// Author: Shimrra Shai
// Date Created: UE+1655.6181 Ms (2022-06-18)
using System;
using System.Collections.Generic;
using System.Linq;

using KSP.IO;
using UnityEngine;

namespace SDF
{
    namespace SmartResources
    {
        namespace FlowEngine
        {
            // Aggregates a number of vessel tanks into a single common pool, so that the tanks can be drained 
            // simultaneously and proportionally.
            public class AggregateTank : FlowTank
            {
                private Guid mVesselGUID;
                private HashSet<uint> mPartPersistentIDs = new HashSet<uint>();

                public override double Capacity
                {
                    get
                    {
                        double rv = 0.0;
                        Vessel v = FlightGlobals.FindVessel(mVesselGUID);
                        if (v.loaded)
                        {
                            foreach (uint partPersistentID in mPartPersistentIDs)
                            {
                                Part part = v.PartsContain(partPersistentID);
                                if (part != null)
                                {
                                    if (part.Resources.Contains(ResourceName))
                                    {
                                        rv += part.Resources.Get(ResourceName).maxAmount;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // NB: inefficient
                            foreach (ProtoPartSnapshot snap in v.protoVessel.protoPartSnapshots)
                            {
                                if (mPartPersistentIDs.Contains(snap.persistentId))
                                {
                                    foreach (ProtoPartResourceSnapshot rsnap in snap.resources)
                                    {
                                        if (rsnap.resourceName == ResourceName)
                                        {
                                            rv += rsnap.maxAmount;
                                        }
                                    }
                                }
                            }
                        }

                        return rv;
                    }
                }

                public override double Amount
                {
                    get
                    {
                        double rv = 0.0;
                        Vessel v = FlightGlobals.FindVessel(mVesselGUID);
                        if (v.loaded)
                        {
                            foreach (uint partPersistentID in mPartPersistentIDs)
                            {
                                Part part = v.PartsContain(partPersistentID);
                                if (part != null)
                                {
                                    if (part.Resources.Contains(ResourceName))
                                    {
                                        rv += part.Resources.Get(ResourceName).amount;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // NB: inefficient
                            foreach (ProtoPartSnapshot snap in v.protoVessel.protoPartSnapshots)
                            {
                                if (mPartPersistentIDs.Contains(snap.persistentId))
                                {
                                    foreach (ProtoPartResourceSnapshot rsnap in snap.resources)
                                    {
                                        if (rsnap.resourceName == ResourceName)
                                        {
                                            rv += rsnap.amount;
                                        }
                                    }
                                }
                            }
                        }

                        return rv;
                    }
                }

                public AggregateTank(string nodeID, Guid vesselGUID, string resourceName) : base(nodeID, resourceName)
                {
                    mVesselGUID = vesselGUID;
                }

                public void AddPart(uint partPersistentID)
                {
                    mPartPersistentIDs.Add(partPersistentID);
                }

                public void RemovePart(uint partPersistentID)
                {
                    mPartPersistentIDs.Remove(partPersistentID);
                }

                public override void WithdrawResource(double amount)
                {
                    // Figure out the *percentage* of the entire tank to consume. From each sub-tank, we will consume equal
                    // percentages, thus mimicking the usual behavior in the KSP game's stock resource system.
                    double percentageToDraw = amount / Capacity;
                    double maxAmount = Capacity;

                    double amountProcessed = 0.0;
                    double amountToProcess = Math.Abs(amount);

                    Vessel v = FlightGlobals.FindVessel(mVesselGUID);
                    HashSet<uint> usableTanks = new HashSet<uint>(mPartPersistentIDs);
                    if (v.loaded)
                    {
                        // Check for which tank in the aggregate will be depleted (or filled) first, if any. Drain it 
                        // first, then redistribute the remaining demand over the other tanks.
                        //int giveUpCounter = 0;
                        while(usableTanks.Count > 0)
                        {
                            //++giveUpCounter;
                            //if(giveUpCounter > 500) break;
                            
                            bool foundInsufficientTank = false;
                            uint leastSufficientTankID = 0;
                            double leastSufficientTankExcursion = 0.0;
                            foreach (uint partPersistentID in usableTanks)
                            {
                                Part part = v.PartsContain(partPersistentID);
    
                                if (part != null)
                                {
                                    if (part.Resources.Contains(ResourceName))
                                    {
                                        double partDrawAmount = part.Resources.Get(ResourceName).maxAmount * 
                                            percentageToDraw;
                                        double partAmount = part.Resources.Get(ResourceName).amount;
                                        double partMaxAmount = part.Resources.Get(ResourceName).maxAmount;

                                        if(partDrawAmount >= 0.0)
                                        {
                                            if(partAmount < partDrawAmount)
                                            {
                                                foundInsufficientTank = true;
                                                double excursion = partDrawAmount - partAmount;
                                                if(excursion >= leastSufficientTankExcursion)
                                                {
                                                    leastSufficientTankExcursion = excursion;
                                                    leastSufficientTankID = partPersistentID;
                                                }
                                            }
                                        }
                                        else if (partDrawAmount < 0.0)
                                        {
                                            if(partAmount + partDrawAmount > partMaxAmount)
                                            {
                                                foundInsufficientTank = true;
                                                double excursion = partAmount + partDrawAmount - partMaxAmount;
                                                if(excursion >= leastSufficientTankExcursion)
                                                {
                                                    leastSufficientTankExcursion = excursion;
                                                    leastSufficientTankID = partPersistentID;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Debug.Log("Found bad tank in set!");
                                        return;
                                    }
                                }
                            }

                            if(foundInsufficientTank)
                            {
                                Part part = v.PartsContain(leastSufficientTankID);
                                
                                // Use KSP's drain system here to ensure the flow gets reported to the GUI
                                double partDrawAmount = part.Resources.Get(ResourceName).maxAmount * percentageToDraw;
                                double actualPartDraw = part.RequestResource(ResourceName, partDrawAmount,
                                    ResourceFlowMode.NO_FLOW);
                                
                                amountProcessed += Math.Abs(actualPartDraw);
                                if(amountProcessed > amountToProcess)
                                {
                                    amountProcessed = amountToProcess;
                                }

                                usableTanks.Remove(leastSufficientTankID);
                            }
                            else
                            {
                                // Drain from all tanks uniformly
                                foreach (uint partPersistentID in usableTanks)
                                {
                                    Part part = v.PartsContain(partPersistentID);
    
                                    double partDrawAmount = part.Resources.Get(ResourceName).maxAmount * 
                                        percentageToDraw;

                                    // Use KSP's drain system here to ensure the flow gets reported to the GUI
                                    double actualPartDraw = part.RequestResource(ResourceName, partDrawAmount,
                                        ResourceFlowMode.NO_FLOW);
                                    
                                    amountProcessed += Math.Abs(actualPartDraw);
                                    if(amountProcessed > amountToProcess)
                                    {
                                        amountProcessed = amountToProcess;
                                    }
                                }

                                usableTanks.Clear();
                            }
                        }
                    }
                    else
                    {
                        // We have to drain manually. Same logic as above.
                        while(usableTanks.Count > 0)
                        {
                            bool foundInsufficientTank = false;
                            uint leastSufficientTankID = 0;
                            ProtoPartResourceSnapshot leastSufficientTankSnapshot = null;
                            double leastSufficientTankExcursion = 0.0;

                            foreach (ProtoPartSnapshot snap in v.protoVessel.protoPartSnapshots)
                            {
                                if (usableTanks.Contains(snap.persistentId))
                                {
                                    bool foundResource = false;
                                    foreach (ProtoPartResourceSnapshot res in snap.resources)
                                    {
                                        if (res.resourceName == ResourceName)
                                        {
                                            double partDrawAmount = res.maxAmount * percentageToDraw;
                                            double partAmount = res.amount;
                                            double partMaxAmount = res.maxAmount;

                                            if(partDrawAmount >= 0.0)
                                            {
                                                if(partAmount < partDrawAmount)
                                                {
                                                    foundInsufficientTank = true;
                                                    double excursion = partDrawAmount - partAmount;
                                                    if(excursion >= leastSufficientTankExcursion)
                                                    {
                                                        leastSufficientTankExcursion = excursion;
                                                        leastSufficientTankID = snap.persistentId;
                                                        leastSufficientTankSnapshot = res;
                                                    }
                                                }
                                            }
                                            else if (partDrawAmount < 0.0)
                                            {
                                                if(partAmount + partDrawAmount > partMaxAmount)
                                                {
                                                    foundInsufficientTank = true;
                                                    double excursion = partAmount + partDrawAmount - partMaxAmount;
                                                    if(excursion >= leastSufficientTankExcursion)
                                                    {
                                                        leastSufficientTankExcursion = excursion;
                                                        leastSufficientTankID = snap.persistentId;
                                                        leastSufficientTankSnapshot = res;
                                                    }
                                                }
                                            }

                                            foundResource = true;
                                            break;
                                        }
                                    }

                                    if(!foundResource)
                                    {
                                        Debug.Log("Found bad tank in set!");
                                        return;
                                    }
                                }
                            }

                            if(foundInsufficientTank)
                            {
                                // Perform the manual drain ourselves.
                                // Use KSP's drain system here to ensure the flow gets reported to the GUI
                                double partDrawAmount = leastSufficientTankSnapshot.maxAmount * percentageToDraw;
                                double actualPartDraw = partDrawAmount;
                                if(partDrawAmount >= 0.0)
                                {
                                    if(leastSufficientTankSnapshot.amount < partDrawAmount)
                                    {
                                        actualPartDraw = leastSufficientTankSnapshot.amount;
                                        leastSufficientTankSnapshot.amount = 0.0;
                                    }
                                    else
                                    {
                                        leastSufficientTankSnapshot.amount -= partDrawAmount;
                                    }
                                }
                                else if (partDrawAmount < 0.0)
                                {
                                    double headroom = leastSufficientTankSnapshot.maxAmount - 
                                        leastSufficientTankSnapshot.amount;
                                    if (headroom < -partDrawAmount)
                                    {
                                        actualPartDraw = -headroom;
                                        leastSufficientTankSnapshot.amount = leastSufficientTankSnapshot.maxAmount;
                                    }
                                    else
                                    {
                                        leastSufficientTankSnapshot.amount -= partDrawAmount;
                                    }
                                }
                                
                                amountProcessed += Math.Abs(actualPartDraw);
                                if(amountProcessed > amountToProcess)
                                {
                                    amountProcessed = amountToProcess;
                                }

                                usableTanks.Remove(leastSufficientTankID);
                            }
                            else
                            {
                                // Drain from all tanks uniformly
                                foreach (ProtoPartSnapshot snap in v.protoVessel.protoPartSnapshots)
                                {
                                    if (usableTanks.Contains(snap.persistentId))
                                    {
                                        bool foundResource = false;
                                        foreach (ProtoPartResourceSnapshot res in snap.resources)
                                        {
                                            if (res.resourceName == ResourceName)
                                            {
                                                double partDrawAmount = res.maxAmount * percentageToDraw;
                                                res.amount -= partDrawAmount;

                                                if(res.amount < 0.0)
                                                {
                                                    res.amount = 0.0;
                                                    Debug.Log("SmartResources: ZOUNDS! Tank " + snap.persistentId.ToString() + " (vessel: " + v.id.ToString() + " ) depleted when not expecting depletion");
                                                }
                                                else if(res.amount > res.maxAmount)
                                                {
                                                    res.amount = res.maxAmount;
                                                    Debug.Log("SmartResources: ZOUNDS! Tank " + snap.persistentId.ToString() + " (vessel: " + v.id.ToString() + " ) overfull when not expecting overfill");
                                                }

                                                foundResource = true;
                                                break;
                                            }
                                        }

                                        if(!foundResource)
                                        {
                                            Debug.Log("Found bad tank in set!");
                                            return;
                                        }
                                    }
                                }

                                usableTanks.Clear();
                            }
                        }
                    }
                }

                public override void ContributeResource(double amount)
                {
                    WithdrawResource(-amount);
                }
            }
        }
    }
}
