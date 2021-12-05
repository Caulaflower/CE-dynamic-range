using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI;
using CombatExtended;
using System.Reflection;
using HarmonyLib;
using HarmonyMod;

namespace PistolRangeTweaken
{
    public class RangerComp : CompRangedGizmoGiver
    {
        public VerbPropertiesCE verpPropsCE
        {
            get
            {
                return (VerbPropertiesCE)this.parent.TryGetComp<CompEquippable>().PrimaryVerb.verbProps;
            }
        }
        public RangerP Props
        {
            get
            {
                return (RangerP)this.props;
            }
        }

        public bool changesApplied = false;

        public IntVec3 savedpos;

        public override void Notify_Equipped(Pawn pawn)
        {

            if (!(pawn?.kindDef?.RaceProps?.Humanlike ?? false))
            {
                Log.Message("returning".Colorize(UnityEngine.Color.green));
                return;
            }
            VerbPropertiesCE verbPropsCE2 = (VerbPropertiesCE)verpPropsCE.MemberwiseClone();
            float rangeadit = (pawn.skills.GetSkill(SkillDefOf.Shooting).Level * pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight));
            if ((pawn.skills.GetSkill(SkillDefOf.Shooting).Level <= 5f))
            {
                rangeadit = pawn.skills.GetSkill(SkillDefOf.Shooting).Level * (-1f);
            }
            verbPropsCE2.range += rangeadit;
            Log.Message(verbPropsCE2.range.ToString().Colorize(UnityEngine.Color.magenta));
            if (this.parent.TryGetComp<CompEquippable>().PrimaryVerb is Verb_ShootCE)
            {
                var verbshoots = (Verb_ShootCE)this.parent.TryGetComp<CompEquippable>().PrimaryVerb;
                verbshoots.verbProps = verbPropsCE2;
            }

            base.Notify_Equipped(pawn);
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            ResetRange();
        }

        public void CoverRangeChange(Pawn pawn)
        {
            if (Covers(pawn) != null && Covers(pawn).Count > 0 && !changesApplied && pawn.ParentHolder is Map)
            {
                VerbPropertiesCE verbPropsCE2 = (VerbPropertiesCE)verpPropsCE.MemberwiseClone();
                var adrange = ((Covers(pawn).First().def.fillPercent * 2.5f));
                Log.Message("range change is: " + adrange.ToString().Colorize(UnityEngine.Color.blue) + " source (" + (Covers(pawn).First().def.label + ")"));
                verbPropsCE2.range *= adrange;
                Log.Message("full range is: " + verbPropsCE2.range.ToString().Colorize(UnityEngine.Color.red));
                if (this.parent.TryGetComp<CompEquippable>().PrimaryVerb is Verb_ShootCE)
                {
                    var verbshoots = (Verb_ShootCE)this.parent.TryGetComp<CompEquippable>().PrimaryVerb;
                    verbshoots.verbProps = verbPropsCE2;
                    changesApplied = true;
                }

            }
        }

        public void ResetRange()
        {
            VerbPropertiesCE cereset = (VerbPropertiesCE)verpPropsCE.MemberwiseClone();

            cereset.range = this.parent.def.Verbs.Find(e => e is VerbPropertiesCE).MemberwiseClone().range;

            var verbshoots = (Verb_ShootCE)this.parent.TryGetComp<CompEquippable>().PrimaryVerb;
            verbshoots.verbProps = cereset;

            changesApplied = false;
        }

        public void RangeTick(Pawn P)
        {
            if (!P.pather.MovingNow)
            {
                ResetRange();
                CoverRangeChange(P);
            }
            else
            {
                ResetRange();
            }
        }

        public List<Thing> Covers(Pawn pwan)
        {
            List<Thing> amongUS = new List<Thing>();
            if (pwan.ParentHolder is Map)
            {
                List<IntVec3> amongus = pwan.CellsAdjacent8WayAndInside().ToList();

                /*
                if (pwan.Rotation == Rot4.East)
                {
                    amongus.RemoveAll(tt33 => tt33.x <= pwan.Position.x);
                    amongus.RemoveAll(tt33 => tt33.y != pwan.Position.y);

                }
                else if (pwan.Rotation == Rot4.West)
                {
                    amongus.RemoveAll(tt33 => tt33.x >= pwan.Position.x);
                    amongus.RemoveAll(tt33 => tt33.y != pwan.Position.y);

                }
                else if (pwan.Rotation == Rot4.South)
                {
                    amongus.Clear();
                    amongus.Add(new IntVec3 { x = pwan.Position.x, z = pwan.Position.z - 1, y = pwan.Position.y });

                }
                else if (pwan.Rotation == Rot4.North)
                {
                    amongus.Clear();
                    amongus.Add(new IntVec3 { x = pwan.Position.x, z = pwan.Position.z + 1, y = pwan.Position.y });

                }*/

                foreach (IntVec3 intvec in amongus)
                {
                    amongUS.AddRange(intvec.GetThingList(Find.CurrentMap).FindAll(tt21 => tt21.def.fillPercent >= 0.4f && tt21.def.fillPercent < 1));
                }

            }

            return amongUS;

        }

    }
    public class RangerP : CompProperties
    {
        public RangerP()
        {
            this.compClass = typeof(RangerComp);
        }
    }

    public class harmonypatchallall : Mod
    {

        public harmonypatchallall(ModContentPack content) : base(content)
        {


            var harmony = new Harmony("Caulaflower.SkillRange");
            try
            {
                Log.Message("succsefully harmony patched Caulaflower.SkillRange".Colorize(UnityEngine.Color.cyan));
                harmony.PatchAll();

            }
            catch (Exception e)
            {

                Log.Error(e.ToString());
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Tick")]
    static class PostFixTickStuffThing
    {
        public static int ticks = 0;
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null | (!__instance.def?.race?.Humanlike ?? true) | !(__instance.ParentHolder is Map))
            {
                return;
            }

            ticks++;

           



            if (ticks == 10)
            {
                

                if ((__instance.equipment?.Primary?.TryGetComp<RangerComp>() ?? null) != null)
                {
                    __instance.equipment.Primary.TryGetComp<RangerComp>().RangeTick(__instance);
                }

                ticks = 0;
            }

            if (ticks > 10)
            {
                ticks = 0;
            }
        }
    }

}

