﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AlphaGenes
{
    //**Idea writing down to consider more later
    //Gizmo can also set preffered metal better the metal, gives mood buff akin to Normal/Fine/Lavish. 
    
    public class Gene_Resource_Metal : Gene_Resource, IGeneResourceDrain
    {
        public Gene_Resource Resource => this;

        //Caravan is going to be a massive fucking headache so I'm going to handwave it to say its easy enough to forage metal to sustain themselves while traveling XD
        public bool CanOffset => pawn.Spawned;

        public float ResourceLossPerDay => def.resourceLossPerDay;

        public Pawn Pawn => pawn;

        public string DisplayLabel => def.resourceLabel;
        private int lastConsumed;
        public override float InitialResourceMax => 1f;

        public override float MinLevelForAlert => 0.15f;
        
        public override float MaxLevelOffset => base.MaxLevelOffset;
        public bool ShouldConsumeNow()
        {
            if(Value <= 0.15f && Value<= targetValue )
            {
                return true;
            }
            else if (Value<= targetValue && Find.TickManager.TicksGame - lastConsumed >= 30000) 
            {
                return true;
            }
            return false;
        }
        //I dont like this because mass is granular enough that they will eat pretty much exactly enough to get to targetvalue which would make them immediatly to eat again
        //Last consumed helps a bit but its still not ideal I made it so they wont try to eat more then once a day but that could sort've still be awkward. Though I do want to make it possible to be able to tell a pawn to eat X so that might resolve issues
        public float MassDesired
        {
            get
            {
                float diff  = targetValue - Value;
                return diff * 10f; //*10 for mass
            }
        }

        public override void Tick()
        {
            base.Tick();
            //Dont use utility resource offset because it will add the hemogencraving hediff even if the calling resource is not hemogen
            //This is basically just the utility done right. Might move to own utility class if doing multiple
            if (CanOffset)
            {
                float before = Value;
                float loss = (-ResourceLossPerDay / 60000f);
                Value += loss;
                if(before>0f && Value <= 0f)
                {
                    //Add hediff here
                    if (!pawn.health.hediffSet.HasHediff(InternalDefOf.AG_MineralCraving))
                    {
                        pawn.health.AddHediff(InternalDefOf.AG_MineralCraving);
                    }
                }
            }
        }

        public static float GetResourceRestore(Thing thing)
        {
            float mass = 0f;
            //whacky idea but it would be amusing if they could eat forged/crafted metal objects as well
            if (!thing.def.IsStuff)
            {
                if (thing.Stuff?.IsMetal ?? false)
                {
                    mass += thing.Stuff.statBases.First(x => x.stat == StatDefOf.Mass).value * thing.def.CostStuffCount;
                }
                else if(thing.def.CostList != null)
                {
                    foreach (var resource in thing.def.CostList)
                    {
                        if (resource.thingDef.IsMetal)
                        {         
                            mass += resource.thingDef.statBases.First(x => x.stat == StatDefOf.Mass).value * resource.count;
                        }
                    }
                }
                else
                {
                    return 0f;
                }
            }
            else if (thing.def.IsMetal)
            {
                mass = thing.def.statBases.First(x => x.stat == StatDefOf.Mass).value;
            }
            else { return 0; }
            float resourceRestore = mass / 10;//dividing by 10 so 1 steel is 0.05 "nutrition"
            return resourceRestore;
        }
        //Dont know what color codes to use
        protected override Color BarColor => Color.grey;

        protected override Color BarHighlightColor => Color.white;

        //This would only work with things that are already ingestible so for something like metal that is not ingestible it has to be called via its own Jobdrivers//have the resource offset be done directly in that jobgiver
        //putting it here as it can be call by every giver
        //For now making the amount restored based on mass. Will 100% need to be tweaks and might need to be a different stat
        public override void Notify_IngestedThing(Thing thing, int numTaken)
        {
            
            float resourceRestore = GetResourceRestore(thing) * numTaken;
            Value += resourceRestore;
            if(Value > 0f)
            {
                lastConsumed = Find.TickManager.TicksGame;
            }            

        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) //Base adds the actual gizmo, would want to not use base if new resource is more complicated then hemogen bar
            {
                yield return gizmo;
            }
            foreach(Gizmo gizmo in GeneResourceDrainUtility.GetResourceDrainGizmos(this)) //This utility is to add Dev gizmos only
            {
                yield return gizmo;
            }
            yield break;
        }
        public override void PostAdd()
        {
            base.PostAdd();
            pawn.health.AddHediff(InternalDefOf.AG_MineralFueled);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastConsumed, "lastConsumed");
        }
    }
}
