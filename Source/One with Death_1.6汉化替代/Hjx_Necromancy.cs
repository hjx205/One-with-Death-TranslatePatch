using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection.Emit;
using System.Reflection;
using Verse;
using OneWithDeath;
using RimWorld;
using HarmonyLib;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;


namespace Hjx_OneWithDeath_HarmonyTest
{
	//v2.0:2025.11.17
	[StaticConstructorOnStartup]
	public class StartUp
    {
        static StartUp()
        {
            Log.Message("OWD_zh 版本：v3 2026.9.30");
            var harmony = new Harmony("Hjx.onewithdeath_zh");
            harmony.PatchAll();
        }
    }

	public class Hjx_translate_OWD
    {
		public CodeMatcher Translate_Assingment(CodeMatcher matcher, string replacetarget, string replace ,bool debug = false)
        {
			var translateMethod = AccessTools.Method(typeof(Verse.Translator), "Translate", new Type[] { typeof(string) });
			var implicitConversion = AccessTools.Method(typeof(Verse.TaggedString), "op_Implicit", new Type[] { typeof(Verse.TaggedString) });
			List<CodeInstruction> translate = new List<CodeInstruction>();
			translate.Add(new CodeInstruction(OpCodes.Call, translateMethod));
			translate.Add(new CodeInstruction(OpCodes.Call, implicitConversion));
			matcher.Start();
			matcher.MatchStartForward(
				new CodeMatch(OpCodes.Ldstr, replacetarget));
            if (matcher.IsValid)
            {
				matcher.SetOperandAndAdvance(replace);
				matcher.Insert(translate);
				if (debug)
					Log.Message("Success");
            }
            else
            {
				if (debug)
					Log.Message("Replace Error: " + replacetarget);
            }
			return matcher;
        }

		public CodeMatcher Translate_Direct(CodeMatcher matcher, string replacetarget, bool debug = false)
        {
			var translateMethod = AccessTools.Method(typeof(Verse.Translator), "Translate", new Type[] { typeof(string) });
			var implicitConversion = AccessTools.Method(typeof(Verse.TaggedString), "op_Implicit", new Type[] { typeof(Verse.TaggedString) });
			List<CodeInstruction> translate = new List<CodeInstruction>();
			translate.Add(new CodeInstruction(OpCodes.Call, translateMethod));
			translate.Add(new CodeInstruction(OpCodes.Call, implicitConversion));
			matcher.Start();
			matcher.MatchStartForward(
				new CodeMatch(OpCodes.Ldstr, replacetarget));
			if (matcher.IsValid)
			{
				matcher.InsertAfter(translate);
				if (debug)
					Log.Message("Success");
			}
			else
			{
				if (debug)
					Log.Message("Replace Error: " + replacetarget);
			}
			return matcher;
		}
    }


	public static class OWD_TranslateUtility
	{
		public static float ParseExtractValue(string phrase, int pos)
		{
			var parts = phrase.Split(' ');
			if (string.IsNullOrEmpty(phrase))
			{
				Log.Error($"ParseExtractValue: phrase is null or empty");
				return -1.0f;
			}else if (pos < 0 || pos > parts.Count())
            {
				Log.Message("Extract Value Error: " + phrase);
				return -1.0f;
            }

			if (parts[pos - 1].Contains(","))
			{
				var parts_next = parts[pos - 1].Split(',');
				return float.Parse(parts_next[0]);
			}
			else if (parts[pos - 1].Contains(")"))
			{
				var parts_next = parts[pos - 1].Split(')');
				return float.Parse(parts_next[0]);
			}
			else
				return float.Parse(parts[pos - 1]);
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(Ability_SelectNecromancer))]
	public class Patch_Ability_SelectNecromancer
	{
		[HarmonyPatch(nameof(Ability_SelectNecromancer.GetGizmos))]
		[HarmonyPrefix]
		public static bool Prefix_GetGizmos(Ability_SelectNecromancer __instance, ref IEnumerable<Command> __result)
		{
			var commands = new List<Command>();
			__result = commands;
			Pawn necromancer = __instance.pawn.relations?.GetFirstDirectRelationPawn(MyModDefs.NecromancerControl);
			if (necromancer == null)
			{
				return false;
			}
			List<Pawn> selectedPawns = Find.Selector.SelectedPawns.Where(delegate (Pawn p)
			{
				Pawn_AbilityTracker abilities = p.abilities;
				return abilities != null && abilities.abilities.Any((Ability a) => a.def == __instance.def) && p.relations?.GetFirstDirectRelationPawn(MyModDefs.NecromancerControl) == necromancer;
			}).ToList();
			if (!selectedPawns.Any() || selectedPawns.OrderBy((Pawn p) => p.thingIDNumber).First() != __instance.pawn)
			{
				return false;
			}
			Command_Action gizmo = new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("UI/Commands/SelectOverseer"),
				Order = -200f
			};
			Hediff weakLink = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(MyModDefs.OWD_WeakLink);
			if (weakLink != null && weakLink.Severity >= 0.4f)
			{
				gizmo.defaultLabel = "Select_Necromancer_unknown".Translate();
				gizmo.defaultDesc = "Select_Necromancer_unknown_text".Translate();
				gizmo.action = delegate
				{
					Find.WindowStack.Add(new Dialog_MessageBox("Select_Necromancer_unknown_detail".Translate()));
				};
			}
			else
			{
				gizmo.defaultLabel = "Select_Necromancer".Translate(necromancer.LabelShort);
				gizmo.defaultDesc = "Select_Necromancer_text".Translate();
				gizmo.action = delegate
				{
					__instance.Activate(LocalTargetInfo.Invalid, LocalTargetInfo.Invalid);
				};
			}
			commands.Add(gizmo);
			__result = commands;
			return false;
		}
	}

	//校对完成 v2.1
	[HarmonyPatch]
	public class MannulPatch_Ability_UndeadSuicide_GetGizmos_0
	{
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Ability_UndeadSuicide/<GetGizmos>d__5:MoveNext",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			string replaceTarget = "Destroy Undead";
			string replace = "Destroy_Undead";
			var matcher = new CodeMatcher(instructions);
			matcher = translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "This undead's link to you has alreadyt been severed. Something else is keeping it alive.";
			replace = "Necrotic_link_servered".Translate();
			matcher = translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "Order all selected undead to destroy themselves.";
			replace = "Destroy_Undead_Detail";
			matcher = translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);

			return matcher.Instructions();
		}
    }

	//校对完成 v2.1
	[HarmonyPatch]
	public class MannulPatch_Ability_UndeadSuicide_GetGizmos_1
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Ability_UndeadSuicide/<>c:<GetGizmos>b__5_3",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Your link to this undead has already been severed, something else is keeping it anchored to reality";
			string replace = "Necrotic_link_servered_detail".Translate();
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.2
	[HarmonyPatch(typeof(Building_ControlServitor))]
	public class Patch_Building_ControlServitor
    {
		[HarmonyPatch(nameof(Building_ControlServitor.GetInspectString), new Type[0])]
		[HarmonyPostfix]
		public static void Postfix(ref string __result)
        {
			if (__result == null) return;

			__result = __result
			.Replace("Occupant: ", "OWD_BCS_0".Translate())
			.Replace("Intelligence: ", "OWD_BCS_1".Translate())
			.Replace("Control Provided: ", "OWD_BCS_2".Translate())
			.Replace(" (Base: ", "OWD_BCS_3".Translate())
			.Replace(" - Diminished)", "OWD_BCS_4".Translate())
			.Replace("Potential Control: ", "OWD_BCS_5".Translate())
			.Replace("Linked Necromancer: ", "OWD_BCS_6".Translate())
			.Replace("Auxiliary Servitors: ", "OWD_BCS_7".Translate());
		}
    }

	//校对完成 v2.3
	[HarmonyPatch(typeof(Building_DimensionalCore))]
	public class Patch_Building_DimensionalCore
    {
		[HarmonyPatch("get_EnterString")]
		[HarmonyPostfix]
		public static void Postfix_get_EnterString(ref string __result)
        {
			__result = "OWD_BD_EnterD".Translate();
		}

		[HarmonyPatch("CollapseDimension")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Cannot collapse the rift: No safe anchor point exists.";
			string replace = "OWD_BD_CD_1";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "The dimensional fabric rejects the paradox. The anchor has been displaced to a stable reality.";
			replace = "OWD_BD_CD_2";
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "The dimensional rift collapses, sealing the space within.";
			replace = "OWD_BD_CD_3";
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
	}

	//校对完成 v2.3
	[HarmonyPatch(typeof(Building_DimensionalCoreInner))]
	public class Patch_Building_DimensionalCoreInner
    {
		[HarmonyPatch("get_EnterString")]
		[HarmonyPostfix]
		public static void Postfix_get_EnterString(ref string __result)
        {
			__result = "OWD_BD_ExitD".Translate();
		}
	}

	//校对完成 v2.3
	[HarmonyPatch]
	public class MannulPatch_Building_DimensionalCore_GetGizmo
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_DimensionalCore/<GetGizmos>d__21:MoveNext",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replaceTarget = "Auto-Siphon";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			translate_OWD.Translate_Direct(matcher, replaceTarget);
			replaceTarget = "Collapse dimension";
			string replace = "OWD_CollapseDimension";
			translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "Safely store the dimension. If the owner is inside, they will be teleported out.";
			replace = "OWD_CollapseDimension_desc";
			translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "Destroy dimension";
			replace = "OWD_DestroyDimension";
			translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "Permanently destroy the pocket dimension and everything inside it. This resets the layout but your dimension level and points are kept.";
			replace = "OWD_DestroyDimension_desc";
			translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "Remove Abductor";
			replace = "OWD_RemoveAbductor";
			translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "Delete the active Abductor Core.";
			replace = "OWD_RemoveAbductor_desc";
			translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "Open dimension";
			replace = "OWD_OpenDimension";
			translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			replaceTarget = "Open the portal to your pocket dimension.";
			replace = "OWD_OpenDimension_desc";
			translate_OWD.Translate_Assingment(matcher, replaceTarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.3
	[HarmonyPatch]
	public class MannulPatch_Building_DimensionalCore_GetGizmo_1
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_DimensionalCore/<>c__DisplayClass21_0:<GetGizmos>b__9",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "The tear in reality stabilizes.";
			string replace = "OWD_OpenDimensionMsg";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.3
	[HarmonyPatch(typeof(Building_DimensionalCoreInner))]
	public class MannulPatch_Building_DimensionalCoreInner_GetGizmo
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_DimensionalCoreInner/<GetGizmos>d__22:MoveNext",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instruction)
        {
			var matcher = new CodeMatcher(instruction);
			string replacetarget = "Auto-Siphon";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "Dimension Status";
			string replace = "OWD_DS";
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "View statistics, expand size, and manage floors/roofs.";
			replace = "OWD_DS_desc";
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Remove Abductor Output";
			replace = "OWD_RAO";
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Delete the active Abductor Output.";
			replace = "OWD_RAO_desc";
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.2
	[HarmonyPatch]
	public class MannulPatch_Building_ControlServitor_GetGizmo_0
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_ControlServitor/<>c__DisplayClass50_0:<GetGizmos>b__5",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "The pawn will forever be trapped and will die when removed, are you sure?";
			string replace = "OWD_InsertPawn_text_0";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.2
	[HarmonyPatch]
	public class MannulPatch_Building_ControlServitor_GetGizmo_1
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_ControlServitor:<GetGizmos>b__50_8",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Removing this pawn from the building will kill it, are you sure?";
			string replace = "OWD_BCS_DOB";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.2
	[HarmonyPatch]
	public class MannulPatch_Building_ControlServitor_GetGizmo_2
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_ControlServitor/<GetGizmos>d__50:MoveNext",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Auto-transfer";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "Toggle automatic energy transfer.";
			string replace = "OWD_At_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Transfer excess energy from the necromancer to the servitor";
			replace = "OWD_BCS_N2S";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Transfer energy from the servitor to the necromancer, when above the threshold";
			replace = "OWD_BCS_S2N";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Insert pawn...";
			replace = "OWD_BCS_Ip";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Select a pawn to enter the Control Servitor";
			replace = "OWD_BCS_Ip_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Cancel";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "Cancel the order for the pawn to enter.";
			replace = "OWD_BCS_C_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Eject pawn";
			replace = "OWD_BCS_Ep";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Eject the pawn from the Control Servitor";
			replace = "OWD_BCS_Ep_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Scan Corpses...";
			replace = "OWD_BCS_SC";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Scan all corpses on the map for specific traits or xenotypes and teleport them to a designated area. Costs 10 energy.";
			replace = "OWD_BCS_SC_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Link Necromancer...";
			replace = "OWD_BCS_LN";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Link this servitor to a Necromancer to provide a control bonus. Linked to: ";
			replace = "OWD_BCS_LN_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.2
	[HarmonyPatch]
	public class MannulPatch_Building_ObeliskEntrance_GetGizmo_0
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_ObeliskEntrance/<GetGizmos>d__25:MoveNext",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Collapse Link";
			string replace = "OWD_CollapseLink";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Start the self-destruct sequence for the linked pocket dimension. This cannot be stopped.";
			replace = "OWD_CollapseLink_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.2
	[HarmonyPatch]
	public class MannulPatch_Building_ObeliskEntrance_GetGizmo_1
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_ObeliskEntrance:<GetGizmos>b__25_0",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Are you sure you want to collapse the link to the pocket dimension? Anything and anyone inside will be lost forever.";
			string replace = "OWD_CollapseLink_detail";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();

		}
    }

	//校对完成 v2.3
	[HarmonyPatch]
	public class MannulPatch_Building_SoulBinder_GetGizmo_0
    {
		public static MethodInfo TargetMethod()
        {
			return AccessTools.Method("OneWithDeath.Building_SoulBinder/<>c__DisplayClass8_0:<GetGizmos>b__7",
				new Type[0],
				null);
        }

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = " skipped to Soul Binder.";
			string replace = "OWD_SB_SkipMsg";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(Command_BookOfTheDead))]
	public class Patch_Command_BookOfTheDead
	{
		[HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Pawn) })]
		[HarmonyPrefix]
		public static bool Prefix_Command_BookOfTheDead(Command_BookOfTheDead __instance, Pawn pawn)
		{
			// 1. 强制调用基类无参构造
			var baseCtor = typeof(Command_Action).GetConstructor(
				BindingFlags.Instance | BindingFlags.Public,
				null,
				Type.EmptyTypes,
				null
			);
			baseCtor?.Invoke(__instance, null);
			__instance.pawn = pawn;
			__instance.defaultLabel = "Book_of_Death".Translate();
			__instance.defaultDesc = "Book_of_Death_default".Translate();
			__instance.icon = ContentFinder<Texture2D>.Get("UI/Commands/BookOfTheDeadIcon");
			__instance.Order = -100f;
			__instance.action = delegate
			{
				Find.WindowStack.Add(new Dialog_BookOfTheDead(pawn));
			};
			return false;
		}
	}

	//校对完成 v2.1
	[HarmonyPatch]
	public class MannulPatch_Comp_RitualTarget_Enduvia
	{
		public static MethodInfo TargetMethod()
		{
			return AccessTools.Method("OneWithDeath.Comp_RitualTarget_Enduvia/<>c__DisplayClass0_0:<CompGetGizmosExtra>b__0",
				new Type[0],
				null);
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "There's no one to call forth. Proceeding might be dangerous, as we don't know what we might awaken.";
			string replace = "ritual_enduvial_target".Translate();
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Proceed Anyway?";
			replace = "ritual_enduvial_title".Translate();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompAbilityEffect_ConsumeCorpses))]
	public class Patch_CompAbilityEffect_ConsumeCorpses
	{
		[HarmonyPatch(nameof(CompAbilityEffect_ConsumeCorpses.Apply), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
		[HarmonyPrefix]
		public static bool Prefix_Apply(CompAbilityEffect_ConsumeCorpses __instance, LocalTargetInfo target, LocalTargetInfo dest)
		{
			Traverse trav = Traverse.Create(__instance);
			Pawn pawn = __instance.parent.pawn;
			if (pawn == null || pawn.Map == null)
			{
				return false;
			}
			PawnComp_Necromancer comp = pawn.GetComp<PawnComp_Necromancer>();
			if (comp == null || comp.tracker == null)
			{
				return false;
			}
			float num = comp.tracker.EffectiveMaxMana - comp.tracker.currentMana;
			if (num <= 0f)
			{
				Messages.Message("Necrotic_energy_already_full".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			IntVec3 cell = target.Cell;
			Map map = pawn.Map;
			IntVec3[] array = trav.Method("CalculateConsumeCells", new Type[] { typeof(IntVec3) }).GetValue<IntVec3[]>(cell);
			List<Corpse> list = new List<Corpse>();
			IntVec3[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				IntVec3 intVec = array2[i];
				if (!intVec.IsValid || !intVec.InBounds(map))
				{
					continue;
				}
				List<Thing> list2 = map.thingGrid.ThingsListAt(intVec);
				for (int j = 0; j < list2.Count; j++)
				{
					if (list2[j] is Corpse corpse && corpse.Position == intVec && !corpse.Destroyed && !trav.Field("excludedCorpseDefNames").GetValue<HashSet<string>>().Contains(corpse.def.defName))
					{
						Pawn innerPawn = corpse.InnerPawn;
						if (innerPawn != null && !innerPawn.RaceProps.IsDrone && (innerPawn.RaceProps.Humanlike || innerPawn.RaceProps.Animal || trav.Field("allowedEntityCorpseDefNames").GetValue<HashSet<string>>().Contains(innerPawn.def.defName)))
						{
							list.Add(corpse);
						}
					}
				}
			}
			if (list.Count == 0)
			{
				Messages.Message("No_consumed_target".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			List<Corpse> list3 = new List<Corpse>();
			List<Corpse> list4 = new List<Corpse>();
			foreach (Corpse item in list)
			{
				if (item.GetRotStage() == RotStage.Dessicated)
				{
					list4.Add(item);
				}
				else
				{
					list3.Add(item);
				}
			}
			List<Corpse> list5 = new List<Corpse>();
			bool flag = false;
			if (list3.Count > 0)
			{
				list5 = list3;
				flag = true;
			}
			else
			{
				list5 = list4;
				flag = false;
			}
			list5 = list5.OrderBy((Corpse c) => trav.Method("GetManaFromAnimalOrEntityCorpse", new Type[] { typeof(Pawn) }).GetValue<float>(c.InnerPawn)).ToList();
			float num2 = 0f;
			int num3 = 0;
			foreach (Corpse item2 in list5)
			{
				if (num <= 0f)
				{
					break;
				}
				float manaFromAnimalOrEntityCorpse = trav.Method("GetManaFromAnimalOrEntityCorpse", new Type[] { typeof(Pawn) }).GetValue<float>(item2.InnerPawn);
				float num4 = 0f;
				if (flag)
				{
					float num5 = ((item2.GetRotStage() == RotStage.Rotting) ? 0.5f : 0.75f);
					num4 = manaFromAnimalOrEntityCorpse * num5;
					CompRottable comp2 = item2.GetComp<CompRottable>();
					if (comp2 != null)
					{
						comp2.RotProgress = 1000000f;
					}
				}
				else
				{
					num4 = manaFromAnimalOrEntityCorpse * 0.25f;
					item2.Destroy();
				}
				MyModDefs.OWD_ConsumeCorpseEffect.Spawn(item2.Position, map);
				num2 += num4;
				num -= num4;
				num3++;
			}
			if (num3 == 0)
			{
				Messages.Message("No_consumed_target".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			comp.tracker.AddMana(num2);
			string text = "Consume_corpse".Translate(list3.Count, num2);
			Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent);

			return false;
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompAbilityEffect_ExpandMana))]
	public class Patch_CompAbilityEffect_ExpandMana
	{
		[HarmonyPatch(nameof(CompAbilityEffect_ExpandMana.Apply), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
		[HarmonyPrefix]
		public static bool Prefix_Apply(CompAbilityEffect_ExpandMana __instance, LocalTargetInfo target, LocalTargetInfo dest)
		{
			Pawn pawn = __instance.parent.pawn;
			if (pawn == null)
			{
				return false;
			}
			PawnComp_Necromancer comp = pawn.GetComp<PawnComp_Necromancer>();
			if (comp != null && comp.tracker != null)
			{
				if (comp.tracker.currentMana < comp.tracker.EffectiveMaxMana)
				{
					Messages.Message("Necrotic_energy_lack".Translate(), pawn, MessageTypeDefOf.RejectInput);
					return false;
				}
				comp.tracker.currentMana = 0f;
				comp.tracker.maxMana += 10;
				Messages.Message("Necrotic_energy_capacity_expand".Translate(comp.tracker.EffectiveMaxMana), pawn, MessageTypeDefOf.PositiveEvent);
				MyModDefs.OWD_ExpandManaCompleteEffect.Spawn(pawn, pawn.Map);
			}

			return false;
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompAbilityEffect_NecroticHealing))]
	public class Patch_CompAbilityEffect_NecroticHealing
	{
		[HarmonyPatch(nameof(CompAbilityEffect_NecroticHealing.DoEffect), new Type[] { typeof(LocalTargetInfo)})]
		[HarmonyPrefix]
		public static bool Prefix_DoEffect(CompAbilityEffect_NecroticHealing __instance, LocalTargetInfo target)
        {
			Traverse trav = Traverse.Create(__instance);
			OneWithDeathMod.LogDebug("[NecroticHealing.DoEffect] - Warmup finished, applying effect.");
			Pawn pawn = __instance.parent.pawn;
			if (pawn == null || pawn.Map == null)
			{
				return false;
			}
			IntVec3 cell = target.Cell;
			Map map = pawn.Map;
			List<IntVec3> affectedCells = trav.Method("GetAffectedCells", new Type[] { typeof(IntVec3)}).GetValue<List<IntVec3>>(cell);
			List<Pawn> list = new List<Pawn>();
			HashSet<Pawn> hashSet = new HashSet<Pawn>();
			foreach (IntVec3 item in affectedCells)
			{
				if (!item.IsValid || !item.InBounds(map))
				{
					continue;
				}
				foreach (Thing thing in item.GetThingList(map))
				{
					if (thing is Pawn pawn2 && !pawn2.Dead && pawn2.Spawned && hashSet.Add(pawn2) && pawn2.health.hediffSet.hediffs.Any((Hediff h) => trav.Field("ValidHediffs").GetValue<List<HediffDef>>().Contains(h.def)))
					{
						list.Add(pawn2);
					}
				}
			}
			if (list.Count == 0)
			{
				Messages.Message("No_valid_target".Translate(), pawn, MessageTypeDefOf.RejectInput);
				return false;
			}
			foreach (Pawn item2 in list)
			{
				trav.Method("ApplyMending", new Type[] { typeof(Pawn)}).GetValue(item2);
			}
			Messages.Message("Necrotic_Healing_text".Translate(pawn.Name.ToStringShort, list.Count), pawn, MessageTypeDefOf.PositiveEvent);

			return false;
        }
	}

    //校对完成 v2.1
    [HarmonyPatch(typeof(CompAbilityEffect_RaiseCreature))]
    public class Patch_CompAbilityEffect_RaiseCreature
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(CompAbilityEffect), "CanApplyOn", new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
        public static bool BaseCanApplyOn(object instance, LocalTargetInfo target, LocalTargetInfo dest)
        {
            throw new NotImplementedException("Patch_CompAbilityEffect_RaiseCreature: It's a stub");
        }

        [HarmonyPatch(nameof(CompAbilityEffect_RaiseCreature.CanApplyOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
        [HarmonyPrefix]
        public static bool Prefix_CanApplyOn(CompAbilityEffect_RaiseCreature __instance, ref bool __result, LocalTargetInfo target, LocalTargetInfo dest)
        {
            __result = false;
            if (!BaseCanApplyOn(__instance, target, dest))
            {
                return false;
            }
            Pawn pawn = __instance.parent.pawn;
            if (pawn == null || pawn.Map == null)
            {
                return false;
            }
            Pawn pawn2;
            if (pawn.HasComp<PawnComp_Necromancer>())
            {
                pawn2 = pawn;
            }
            else
            {
                pawn2 = pawn.relations?.GetFirstDirectRelationPawn(MyModDefs.NecromancerControl);
                if (pawn2 == null || pawn2.Dead || !pawn2.Spawned)
                {
                    Messages.Message("Cannot_raise_creature".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                    return false;
                }
            }
            if (!pawn2.health.hediffSet.HasHediff(MyModDefs.OWD_NecroticFlow))
            {
                Messages.Message("Cannot_raise_creature_missflow".Translate(pawn2.LabelShort), pawn2, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            PawnComp_Necromancer comp = pawn2.GetComp<PawnComp_Necromancer>();
            if (comp == null || comp.tracker == null)
            {
                Messages.Message("Necromancer_comp_mis".Translate(), pawn2, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            Map map = __instance.parent.pawn.Map;
            IntVec3 cell = target.Cell;
            IntVec3[] array = new IntVec3[13]
            {
            cell + new IntVec3(-1, 0, 1),
            cell + new IntVec3(0, 0, 1),
            cell + new IntVec3(1, 0, 1),
            cell + new IntVec3(-1, 0, 0),
            cell,
            cell + new IntVec3(1, 0, 0),
            cell + new IntVec3(-1, 0, -1),
            cell + new IntVec3(0, 0, -1),
            cell + new IntVec3(1, 0, -1),
            cell + new IntVec3(0, 0, 2),
            cell + new IntVec3(2, 0, 0),
            cell + new IntVec3(0, 0, -2),
            cell + new IntVec3(-2, 0, 0)
            };
            bool flag = false;
            IntVec3[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                IntVec3 intVec = array2[i];
                if (intVec.IsValid && intVec.InBounds(map))
                {
                    List<Thing> list = map.thingGrid.ThingsListAt(intVec);
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j] is Corpse corpse && corpse.Position == intVec && __instance.IsValidCreatureCorpse(corpse))
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (flag)
                {
                    break;
                }
            }
            if (!flag)
            {
                return false;
            }
            int num = HediffComp_NecroticFlowManager.CalculateUsedControlPoints(pawn2, comp);
            int num2 = HediffComp_NecroticFlowManager.CalculateMaxControlPoints(comp.tracker);
            if (num2 - num <= 0)
            {
                Messages.Message("Cannot_raise_more_creature".Translate(num, num2), pawn2, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            if (!Fleshtomana.CanPayManaCost(pawn, 25f, out var _))
            {
                Messages.Message("not_enough_necrotic_energy".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            __result = true;

            return false;
        }

        [HarmonyPatch(nameof(CompAbilityEffect_RaiseCreature.Apply), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
        [HarmonyPrefix]
        public static bool Prefix_Apply(CompAbilityEffect_RaiseCreature __instance, LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = __instance.parent.pawn;
            Map map = pawn?.Map;
            Pawn pawn2;
            PawnComp_Necromancer comp;
            if (pawn.HasComp<PawnComp_Necromancer>())
            {
                pawn2 = pawn;
                comp = pawn.GetComp<PawnComp_Necromancer>();
            }
            else
            {
                pawn2 = pawn.relations?.GetFirstDirectRelationPawn(MyModDefs.NecromancerControl);
                comp = pawn2.GetComp<PawnComp_Necromancer>();
            }
            float requiredMana = 25f;
            if (!Fleshtomana.HandleManaCost(pawn, requiredMana))
            {
                return false;
            }
            IntVec3 cell = target.Cell;
            IntVec3[] array = new IntVec3[13]
            {
                cell + new IntVec3(-1, 0, 1),
                cell + new IntVec3(0, 0, 1),
                cell + new IntVec3(1, 0, 1),
                cell + new IntVec3(-1, 0, 0),
                cell,
                cell + new IntVec3(1, 0, 0),
                cell + new IntVec3(-1, 0, -1),
                cell + new IntVec3(0, 0, -1),
                cell + new IntVec3(1, 0, -1),
                cell + new IntVec3(0, 0, 2),
                cell + new IntVec3(2, 0, 0),
                cell + new IntVec3(0, 0, -2),
                cell + new IntVec3(-2, 0, 0)
            };
            List<Corpse> list = new List<Corpse>();
            IntVec3[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                IntVec3 intVec = array2[i];
                if (!intVec.IsValid || !intVec.InBounds(map))
                {
                    continue;
                }
                List<Thing> list2 = map.thingGrid.ThingsListAt(intVec);
                for (int j = 0; j < list2.Count; j++)
                {
                    if (list2[j] is Corpse corpse && corpse.Position == intVec && __instance.IsValidCreatureCorpse(corpse))
                    {
                        list.Add(corpse);
                    }
                }
            }
            if (comp.tracker.linkedDrainMinions == null)
            {
                comp.tracker.linkedDrainMinions = new List<Pawn>();
            }
            int num = HediffComp_NecroticFlowManager.CalculateUsedControlPoints(pawn2, comp);
            int num2 = HediffComp_NecroticFlowManager.CalculateMaxControlPoints(comp.tracker);
            int num3 = num2 - num;
            List<Corpse> list3 = new List<Corpse>();
            int num4 = 0;
            foreach (Corpse item in list.OrderBy((Corpse x) => Rand.Value))
            {
                if (item?.InnerPawn != null)
                {
                    int num5 = HediffComp_NecroticFlowManager.CalculateControlPointCost(item.InnerPawn);
                    if (num4 + num5 <= num3)
                    {
                        list3.Add(item);
                        num4 += num5;
                    }
                }
            }
            if (list3.Count == 0)
            {
                int num6 = (list.Where((Corpse c) => c?.InnerPawn != null).Any() ? list.Where((Corpse c) => c?.InnerPawn != null).Min((Corpse c) => HediffComp_NecroticFlowManager.CalculateControlPointCost(c.InnerPawn)) : 0);
                Messages.Message("Cannot_raise_creature_misspoint".Translate(num6, num3, num2), pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            MapComponent_RaiseCreature mapComponent_RaiseCreature = map.GetComponent<MapComponent_RaiseCreature>();
            if (mapComponent_RaiseCreature == null)
            {
                mapComponent_RaiseCreature = new MapComponent_RaiseCreature(map);
                map.components.Add(mapComponent_RaiseCreature);
            }
            int num7 = 0;
            foreach (Corpse item2 in list3)
            {
                Pawn innerPawn = item2.InnerPawn;
                innerPawn.Drawer.renderer.SetAnimation(AnimationDefOf.DeathRefusalTwitches);
                mapComponent_RaiseCreature.AddRaisingAction(new DelayedCreatureRaisingAction(item2, pawn2, comp, __instance, __instance.Props.delayTicks));
                num7++;
            }
            if (num7 > 0)
            {
                Messages.Message("beging_raise_creature".Translate(pawn.LabelShort, num7) + "cost_control_point".Translate(num4, num2), pawn, MessageTypeDefOf.PositiveEvent);
            }
            return false;
        }
    }

    //校对完成 v2.1
    [HarmonyPatch(typeof(CompAbilityEffect_RaiseUndead))]
	public class Patch_CompAbilityEffect_RaiseUndead
	{
		[HarmonyPatch(nameof(CompAbilityEffect_RaiseUndead.CanApplyOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Cannot raise undead: controlling necromancer is missing, dead, or not on the map.";
			string replace = "Cannot_raise_Undead";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Necromancer component or tracker missing.";
			replace = "Necromancer_comp_mis";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "No valid corpses to raise.";
			replace = "No_vaild_corpse";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Insufficient bandwidth to raise any undead.";
			replace = "Insufficient_bandwith";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Cannot raise undead: ";
			replace = "Cannot_raise_Undead_sample";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}

		[HarmonyPatch(nameof(CompAbilityEffect_RaiseUndead.Apply), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
		[HarmonyPrefix]
		public static bool Prefix_Apply(CompAbilityEffect_RaiseUndead __instance, LocalTargetInfo target, LocalTargetInfo dest)
		{
			Pawn pawn = __instance.parent.pawn;
			Map map = pawn.Map;
			Pawn pawn2;
			PawnComp_Necromancer comp;
			if (pawn.HasComp<PawnComp_Necromancer>())
			{
				pawn2 = pawn;
				comp = pawn.GetComp<PawnComp_Necromancer>();
			}
			else
			{
				pawn2 = pawn.relations?.GetFirstDirectRelationPawn(MyModDefs.NecromancerControl);
				comp = pawn2.GetComp<PawnComp_Necromancer>();
			}
			IntVec3 cell = target.Cell;
			IntVec3[] array = new IntVec3[13]
			{
				cell + new IntVec3(-1, 0, 1),
				cell + new IntVec3(0, 0, 1),
				cell + new IntVec3(1, 0, 1),
				cell + new IntVec3(-1, 0, 0),
				cell,
				cell + new IntVec3(1, 0, 0),
				cell + new IntVec3(-1, 0, -1),
				cell + new IntVec3(0, 0, -1),
				cell + new IntVec3(1, 0, -1),
				cell + new IntVec3(0, 0, 2),
				cell + new IntVec3(2, 0, 0),
				cell + new IntVec3(0, 0, -2),
				cell + new IntVec3(-2, 0, 0)
			};
			List<Corpse> list = new List<Corpse>();
			IntVec3[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				IntVec3 c = array2[i];
				if (c.IsValid && c.InBounds(map))
				{
					Corpse firstThing = c.GetFirstThing<Corpse>(map);
					if (firstThing != null && !firstThing.Destroyed && firstThing.InnerPawn.RaceProps.Humanlike && (!firstThing.InnerPawn.IsMutant || (firstThing.InnerPawn.mutant != null && firstThing.InnerPawn.mutant.Def == MyModDefs.OWD_Undead_Base)))
					{
						list.Add(firstThing);
					}
				}
			}
			int num = 10;
			int num2 = comp.tracker.TotalBandwidth - comp.tracker.UsedBandwidth;
			List<Corpse> list2 = new List<Corpse>();
			foreach (Corpse item in list.OrderBy((Corpse x) => Rand.Value))
			{
				if (list2.Count >= num2 || !Fleshtomana.HandleManaCost(pawn, num))
				{
					break;
				}
				list2.Add(item);
			}
			if (list2.Count == 0)
			{
				Messages.Message("Not_resource_raise_Undead".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			MapComponent_RaiseUndead mapComponent_RaiseUndead = map.GetComponent<MapComponent_RaiseUndead>();
			if (mapComponent_RaiseUndead == null)
			{
				mapComponent_RaiseUndead = new MapComponent_RaiseUndead(map);
				map.components.Add(mapComponent_RaiseUndead);
			}
			foreach (Corpse item2 in list2)
			{
				Pawn innerPawn = item2.InnerPawn;
				innerPawn.Drawer.renderer.SetAnimation(AnimationDefOf.DeathRefusalTwitches);
				mapComponent_RaiseUndead.AddRaisingAction(new DelayedRaisingAction(item2, pawn2, comp, __instance, 180));
			}
			string text = (pawn == pawn2) ? "OWD_Raise_Undead".Translate(pawn.LabelShort, list2.Count) : "OWD_Raise_Undead_Other".Translate(pawn2.LabelShort, pawn.LabelShort, list2.Count);
			Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent);

			return false;
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompAbilityEffect_RaiseUndeadChampion))]
	public class Patch_CompAbilityEffect_RaiseUndeadChampion
    {
		[HarmonyPatch(nameof(CompAbilityEffect_RaiseUndeadChampion.CanApplyOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
		[HarmonyPrefix]
		public static bool Prefix_CanApplyOn(CompAbilityEffect_RaiseUndeadChampion __instance, ref bool __result, LocalTargetInfo target, LocalTargetInfo dest)
        {
			__result = false;
			if (!target.HasThing || !(target.Thing is Corpse corpse))
			{
				return false;
			}
			if (corpse.Destroyed || corpse.InnerPawn == null || !corpse.InnerPawn.RaceProps.Humanlike || (corpse.InnerPawn.IsMutant && corpse.InnerPawn.mutant.Def != MyModDefs.OWD_Undead_Base))
			{
				return false;
			}
			Pawn pawn = __instance.parent.pawn;
			PawnComp_Necromancer comp = pawn.GetComp<PawnComp_Necromancer>();
			if (comp == null || comp.tracker == null)
			{
				Messages.Message("Necromancer_comp_mis".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			int num = 2;
			if (comp.tracker.TotalBandwidth - comp.tracker.UsedBandwidth < num)
			{
				Messages.Message("Champion_Insufficient_bandwidth".Translate(num), pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			int num2 = 50;
			if (!Fleshtomana.CanPayManaCost(pawn, num2, out var failReason))
			{
				Messages.Message("Cannot_raise_Champion_sample".Translate() + failReason, pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			__result = true;
			return false;
		}

		[HarmonyPatch(nameof(CompAbilityEffect_RaiseUndeadChampion.Apply), new Type[] {typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
		[HarmonyPrefix]
		public static bool Prefix_Apply(CompAbilityEffect_RaiseUndeadChampion __instance, LocalTargetInfo target, LocalTargetInfo dest)
        {
			Pawn pawn = __instance.parent.pawn;
			Map map = pawn.Map;
			Corpse corpse = (Corpse)target.Thing;
			PawnComp_Necromancer comp = pawn.GetComp<PawnComp_Necromancer>();
			int num = 50;
			if (Fleshtomana.HandleManaCost(pawn, num))
			{
				MapComponent_RaiseUndeadChampion mapComponent_RaiseUndeadChampion = map.GetComponent<MapComponent_RaiseUndeadChampion>();
				if (mapComponent_RaiseUndeadChampion == null)
				{
					mapComponent_RaiseUndeadChampion = new MapComponent_RaiseUndeadChampion(map);
					map.components.Add(mapComponent_RaiseUndeadChampion);
				}
				Pawn innerPawn = corpse.InnerPawn;
				innerPawn.Drawer.renderer.SetAnimation(AnimationDefOf.DeathRefusalTwitches);
				mapComponent_RaiseUndeadChampion.AddRaisingAction(new DelayedRaisingActionChampion(corpse, pawn, comp, __instance, 180));
				Messages.Message("Raising_undead_champion".Translate(), pawn, MessageTypeDefOf.PositiveEvent);
			}
			return false;
        }
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompAbilityEffect_RaiseUndeadOverseer))]
	public class Patch_CompAbilityEffect_RaiseUndeadOverseer
    {
		[HarmonyPatch(nameof(CompAbilityEffect_RaiseUndeadOverseer.CanApplyOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
		[HarmonyPrefix]
		public static bool Prefix_CanApplyOn(CompAbilityEffect_RaiseUndeadOverseer __instance, ref bool __result, LocalTargetInfo target, LocalTargetInfo dest)
        {
			__result = false;
			if (!target.HasThing || !(target.Thing is Corpse corpse))
			{
				return false;
			}
			if (corpse.Destroyed || corpse.InnerPawn == null || !corpse.InnerPawn.RaceProps.Humanlike || (corpse.InnerPawn.IsMutant && corpse.InnerPawn.mutant.Def != MyModDefs.OWD_Undead_Base && corpse.InnerPawn.mutant.Def != MyModDefs.OWD_Lich))
			{
				return false;
			}
			Pawn pawn = __instance.parent.pawn;
			PawnComp_Necromancer comp = pawn.GetComp<PawnComp_Necromancer>();
			if (comp == null || comp.tracker == null)
			{
				Messages.Message("Necromancer_comp_mis".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			int num = 3;
			if (comp.tracker.TotalBandwidth - comp.tracker.UsedBandwidth < num)
			{
				Messages.Message("Champion_Insufficient_bandwidth".Translate(num), pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			int num2 = 75;
			if (!Fleshtomana.CanPayManaCost(pawn, num2, out var failReason))
			{
				Messages.Message("Cannot_raise_Overseer_sample".Translate() + failReason, pawn, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			__result = true;
			return false;
		}

		[HarmonyPatch(nameof(CompAbilityEffect_RaiseUndeadOverseer.Apply), new Type[] {typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
		[HarmonyPrefix]
		public static bool Prefis_Apply(CompAbilityEffect_RaiseUndeadOverseer __instance, LocalTargetInfo target, LocalTargetInfo dest)
        {
			Pawn pawn = __instance.parent.pawn;
			Map map = pawn.Map;
			Corpse corpse = (Corpse)target.Thing;
			PawnComp_Necromancer comp = pawn.GetComp<PawnComp_Necromancer>();
			int num = 75;
			if (Fleshtomana.HandleManaCost(pawn, num))
			{
				MapComponent_RaiseUndeadOverseer mapComponent_RaiseUndeadOverseer = map.GetComponent<MapComponent_RaiseUndeadOverseer>();
				if (mapComponent_RaiseUndeadOverseer == null)
				{
					mapComponent_RaiseUndeadOverseer = new MapComponent_RaiseUndeadOverseer(map);
					map.components.Add(mapComponent_RaiseUndeadOverseer);
				}
				Pawn innerPawn = corpse.InnerPawn;
				innerPawn.Drawer.renderer.SetAnimation(AnimationDefOf.DeathRefusalTwitches);
				mapComponent_RaiseUndeadOverseer.AddRaisingAction(new DelayedRaisingActionOverseer(corpse, pawn, comp, __instance, 180));
				Messages.Message("Raising_undead_overseer".Translate(), pawn, MessageTypeDefOf.PositiveEvent);
			}
			return false;
        }
    }

	//校对完成 v2.2
	[HarmonyPatch(typeof(CompFacility_AuxServitor))]
	public class Patch_CompFacility_AuxServitor
    {
		[HarmonyPatch(nameof(CompFacility_AuxServitor.CompInspectStringExtra), new Type[0])]
		[HarmonyPostfix]
		public static void Postfix_CompInspectStringExtra(ref string __result)
        {
			__result = "OWD_AuxSeveritorExtra".Translate();

		}

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompUsable_InstallNecroticOrb))]
	public class Patch_CompUsable_InstallNecroticOrb
    {
		[HarmonyPatch("CanBeUsedBy")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Can only be installed on an Undead or a Lich.";
			string replace = "OWD_installNecroticOrb_fail_0".Translate();
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Already installed";
			replace = "OWD_installNecroticOrb_fail_1".Translate();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Another pawn of your faction already has a Necrotic Orb installed.";
			replace = "OWD_installNecroticOrb_fail_2".Translate();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "A corpse on the map already has a Necrotic Orb installed.";
			replace = "OWD_installNecroticOrb_fail_3".Translate();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.1
	[HarmonyPatch]
	public class MannulPatch_CompUsableNecroticCore_CompFloatMenuOptions
	{
		public static MethodInfo TargetMethod()
		{
			return AccessTools.Method("OneWithDeath.CompUsableNecroticCore/<CompFloatMenuOptions>d__2:MoveNext",
				new Type[0],
				null);
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Cannot install ";
			string replace = "OWD_Cannot_install";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompUsableNecroticCore))]
	public class Patch_CompUsableNecroticCore_TryStartUseJob
	{
		[HarmonyPatch(nameof(CompUsableNecroticCore.TryStartUseJob), new Type[] { typeof(Pawn), typeof(LocalTargetInfo), typeof(bool) })]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Cannot install ";
			string replace = "OWD_Cannot_install";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
	}
	
	//校对完成 v2.1
	[HarmonyPatch(typeof(CompUsableNecroticCore))]
    public class Patch_CompUsableNecroticCore_CanBeUsedBy
    {
        [HarmonyPatch("CanBeUsedBy")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            string replacetarget = "Pawn is dead or null";
            string replace = "OWD_Pawn_is_dead_or_null";
            Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
            matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
            replacetarget = "Pawn already has this implant installed";
            replace = "OWD_Pawn_already_has_this_implant_installed";
            matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
            replacetarget = "Pawn has no heart to install implant";
            replace = "OWD_Pawn_has_no_heart_to_install_implant";
            matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
            replacetarget = "Pawn must be an undead";
            replace = "OWD_Pawn_must_be_an_undead";
            matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
            return matcher.Instructions();
        }
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompUseEffect_ApplyHediff))]
	public class Patch_CompUseEffect_ApplyHediff
    {
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(CompUseEffect), "DoEffect")]
		public static void BaseDoEffect(object instance, Pawn usedBy)
        {

        }

		[HarmonyPatch(nameof(CompUseEffect_ApplyHediff.DoEffect), new Type[] { typeof(Pawn) })]
        [HarmonyPrefix]
        public static bool Prefix_DoEffect(CompUseEffect_ApplyHediff __instance, Pawn usedBy)
        {
            var Props = (CompProperties_UseEffect_ApplyHediff)AccessTools.Property(typeof(CompUseEffect_ApplyHediff), "Props").GetValue(__instance);
            var applyHediffAndEffect = AccessTools.Method(typeof(CompUseEffect_ApplyHediff), "ApplyHediffAndEffects", new Type[] { typeof(Pawn) });
			BaseDoEffect(__instance, usedBy);
            if (Props.requiredHediff != null && !usedBy.health.hediffSet.HasHediff(Props.requiredHediff))
            {
                Messages.Message("OWD_experiment_result_lack".Translate(usedBy, Props.requiredHediff.label, __instance.parent.Label), usedBy, MessageTypeDefOf.RejectInput);
            }
            else if (Props.hediffDef != null && Props.hediffDef.defName == "OneWithDeath")
            {
                string text = "OWD_the_last_experiment".Translate();
                Find.WindowStack.Add(new Dialog_MessageBox(text, "Confirm", delegate
                {
                    applyHediffAndEffect.Invoke(__instance, new object[] { usedBy });
					__instance.parent.Destroy();
                }, "Cancel", delegate
                {
                }, "OWD_warning".Translate()));
            }
            else
            {
                applyHediffAndEffect.Invoke(__instance, new object[] { usedBy });
				__instance.parent.Destroy();
            }
            return false;
        }

        [HarmonyPatch("ApplyHediffAndEffects", new Type[] { typeof(Pawn) })]
        [HarmonyPrefix]
        public static bool Prefix_ApplyHediffAndEffect(CompUseEffect_ApplyHediff __instance, Pawn usedBy)
        {
            var Props = (CompProperties_UseEffect_ApplyHediff)AccessTools.Property(typeof(CompUseEffect_ApplyHediff), "Props").GetValue(__instance);
			if(Props.hediffToRemove != null && usedBy.health.hediffSet.HasHediff(Props.hediffToRemove))
            {
				Hediff firstHediffOfDef = usedBy.health.hediffSet.GetFirstHediffOfDef(Props.hediffToRemove);
				usedBy.health.RemoveHediff(firstHediffOfDef);
			}
            if (Props.hediffDef != null)
            {
				if (Props.hediffDef == MyModDefs.OneWithDeath)
				{
					OneWithDeathUtility.ApplyOneWithDeathEffects(usedBy);
				}
				else
				{
					usedBy.health.AddHediff(Props.hediffDef);
				}
				Messages.Message("OWD_already_have_experiment".Translate(usedBy, __instance.parent.Label), usedBy, MessageTypeDefOf.PositiveEvent);
			}

            return false;
        }

    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(CompUseEffect_NecroBloomTonic))]
	public class Patch_CompUseEffect_NecroBloomTonic
    {
		[HarmonyPatch(nameof(CompUseEffect_NecroBloomTonic.DoEffect), new Type[] { typeof(Pawn)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "NecroBloomTonic_Necromancer";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "NecroBloomTonic_Undead";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "NecroBloomTonic_Humanlike";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.3
	[HarmonyPatch(typeof(Designator_PlaceRiftIntake))]
	public class Patch_Designator_PlaceRiftIntake
    {
		[HarmonyPatch(MethodType.Constructor, new Type[] { typeof(string)})]
		[HarmonyPostfix]
		public static void Postfix_Designator_PlaceRiftIntake(Designator_PlaceRiftIntake __instance)
        {
			__instance.defaultLabel = "OWD_PRI".Translate();
			__instance.defaultDesc = "OWD_PRI_desc".Translate();
		}

	}

	//校对完成 v2.3
	[HarmonyPatch(typeof(Designator_PlaceRiftEmitter))]
	public class Patch_Designator_PlaceRiftEmitter
    {
		[HarmonyPatch(MethodType.Constructor, new Type[] { typeof(string)})]
		[HarmonyPostfix]
		public static void Postfix_Designator_PlaceRiftEmitter(Designator_PlaceRiftEmitter __instance)
        {
			__instance.defaultLabel = "OWD_PRE".Translate();
			__instance.defaultDesc = "OWD_PRE_desc".Translate();
		}

	}

	//校对完成 v2.3
	[HarmonyPatch(typeof(Designator_PlaceAbductorCore))]
	public class Patch_Designator_PlaceAbductorCore
    {
		[HarmonyPatch(MethodType.Constructor, new Type[] {typeof(Building_DimensionalCore)})]
		[HarmonyPostfix]
		public static void Postfix_Designator_PlaceAbductorCore(Designator_PlaceAbductorCore __instance)
        {
			__instance.defaultLabel = "OWD_PA".Translate();
			__instance.defaultDesc = "OWD_PA_desc".Translate();
		}

	}

	[HarmonyPatch(typeof(Designator_PlaceAbductorOutput))]
	public class Patch_Designator_PlaceAbductorOutput
    {
		[HarmonyPatch(MethodType.Constructor, new Type[] {typeof(Building_DimensionalCoreInner)})]
		[HarmonyPostfix]
		public static void Postfix_Designator_PlaceAbductorOutput(Designator_PlaceAbductorOutput __instance)
        {
			__instance.defaultLabel = "OWD_PAO".Translate();
			__instance.defaultDesc = "OWD_PAO_desc".Translate();
		}

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(Dialog_BookOfTheDead))]
	public class Patch_Dialog_BookOfTheDead
	{
		[HarmonyPatch(nameof(Dialog_BookOfTheDead.DoWindowContents), new Type[] { typeof(Rect) })]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Book of Death";
			string replace = "Book_of_Death";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);

			replacetarget = "Show in ability bar";
			replace = "Show_in_ability_bar";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);

			replacetarget = " added to ability bar.";
			replace = "add_to_ability_bar";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);

			replacetarget = " hidden in the book.";
			replace = "hidden_in_the_book";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);

			return matcher.Instructions();
		}


	}

	//校对完成 v2.2
	[HarmonyPatch(typeof(Dialog_CorpseScanner))]
	public class Patch_Dialog_CorpseScanner
    {
		[HarmonyPatch(nameof(Dialog_CorpseScanner.DoWindowContents), new Type[] { typeof(Rect)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Search Corpses";
			string replace = "OWD_CS_SC";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Traits";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "Xenotypes";
			replace = "OWD_CS_Xenotypes";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "No player-created areas found";
			replace = "OWD_CS_NAF";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Select area to move corpses to:";
			replace = "OWD_CS_SAM";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Select an Area...";
			replace = "OWD_CS_SA";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "You must select an area.";
			replace = "OWD_CS_MSA";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.3
	[HarmonyPatch(typeof(Dialog_DimensionEffects))]
	public class Patch_Dialog_DimensionEffects
    {
		[HarmonyPatch(nameof(Dialog_DimensionEffects.DoWindowContents))]
		[HarmonyPrefix]
		public static bool Prefix_DoWindowContents(Dialog_DimensionEffects __instance, Rect inRect)
        {
			Traverse trav = Traverse.Create(__instance);
			Map dimMap = trav.Field("dimMap").GetValue<Map>();
			Vector2 scrollPosition = trav.Field("scrollPosition").GetValue<Vector2>();
			List<WeatherDef> allowedWeathers = trav.Field("allowedWeathers").GetValue<List<WeatherDef>>();
			MapComponent_PersonalDimension comp = MapComponent_PersonalDimension.GetOrCreate(dimMap);
			int num = comp.unlockedGrids.Count((IntVec3 g) => !comp.gridRoofs.ContainsKey(g) || !comp.gridRoofs[g]);
			if (comp.dimensionEntropy > 0f)
			{
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				GUI.color = Color.red;
				Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "OWD_DimensionUnstable".Translate().ToString());
				Widgets.Label(new Rect(inRect.x, inRect.y + 25f, inRect.width, 30f), "OWD_CannotChangeUntilStabilized".Translate().ToString());
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			else
			{
				Text.Font = GameFont.Medium;
				Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "OWD_DimensionEffects".Translate().ToString());
			}
			Text.Font = GameFont.Small;
			Rect outRect = new Rect(inRect.x, inRect.y + 60f, inRect.width, inRect.height - 110f);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, (float)allowedWeathers.Count * 35f);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			float num2 = 0f;
			foreach (WeatherDef weather in allowedWeathers)
			{
				Rect rect = new Rect(0f, num2, viewRect.width, 30f);
				string arg = "OWD_Weather_Default".Translate();
				string text = "OWD_Tooltip_Default".Translate();
				float num3 = -0.5f;
				if (weather == WeatherDefOf.Clear)
				{
					arg = "OWD_Weather_Clear".Translate();
					text = "OWD_Tooltip_Clear".Translate();
					num3 = 0f;
				}
				else if (weather == MyModDefs.Rain)
				{
					arg = "OWD_Weather_Rain".Translate();
					text = "OWD_Tooltip_Rain".Translate();
					num3 = 0.2f + (float)num * 0.1f;
				}
				bool flag = dimMap.weatherManager.curWeather == weather;
				bool flag2 = comp.dimensionEntropy > 0f;
				bool flag3 = comp.dimensionEnergy > 0f || num3 <= 0f;
				bool flag4 = flag || flag2 || !flag3;
				if (flag4)
				{
					GUI.color = Color.grey;
				}
				TooltipHandler.TipRegion(rect, text);
				string arg2 = ((num3 > 0f) ? "-" : "+");
				if (Widgets.ButtonText(rect, $"{arg} ({arg2}{Mathf.Abs(num3):F1}/h)") && !flag4)
				{
					Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("OWD_DE_Change".Translate(arg, arg2, $"{Mathf.Abs(num3):F1}"), delegate
					{
						__instance.ExecuteChangeWeather(weather);
					}));
				}
				GUI.color = Color.white;
				num2 += 35f;
			}
			Widgets.EndScrollView();
			if (Widgets.ButtonText(new Rect(inRect.width / 2f - 60f, inRect.height - 40f, 120f, 35f), "OWD_Close".Translate()))
			{
				__instance.Close();
			}

			return false;
        }

	}

	//校对完成 v2.3
	[HarmonyPatch(typeof(Dialog_DimensionStatus))]
	public class Patch_Dialog_DimensionStatus
    {
		[HarmonyPatch(nameof(Dialog_DimensionStatus.DoWindowContents))]
		[HarmonyPrefix]
		public static bool Prefix_DoWindowContents(Dialog_DimensionStatus __instance, Rect inRect)
        {
			Traverse trav = Traverse.Create(__instance);
			Pawn_NecromancerTracker necroTracker = trav.Field("necroTracker").GetValue<Pawn_NecromancerTracker>();
			MapComponent_PersonalDimension comp = trav.Field("comp").GetValue<MapComponent_PersonalDimension>();
			Map dimMap = trav.Field("dimMap").GetValue<Map>();
			if (Prefs.DevMode && DebugSettings.godMode)
			{
				Text.Anchor = TextAnchor.MiddleCenter;
				if (Widgets.ButtonText(new Rect(inRect.x, inRect.y, 24f, 24f), "-") && necroTracker != null)
				{
					necroTracker.dimensionLevel = Mathf.Max(1, necroTracker.dimensionLevel - 1);
				}
				if (Widgets.ButtonText(new Rect(inRect.x + 30f, inRect.y, 24f, 24f), "+") && necroTracker != null)
				{
					necroTracker.dimensionLevel++;
				}
				Text.Anchor = TextAnchor.UpperLeft;
			}
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(inRect);
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			listing_Standard.Label("OWD_DimensionStatus".Translate().ToString());
			Text.Font = GameFont.Small;
			listing_Standard.GapLine();
			int num = comp.pendingChanges.Count((PendingDimChange p) => p.type == DimChangeType.Expand);
			int num2 = comp.unlockedGrids.Count - 1 + num;
			int num3 = ((necroTracker != null) ? Mathf.Max(0, necroTracker.dimensionLevel - num2 - comp.unlockedStabilizerLimit) : 0);
			if (necroTracker != null)
			{
				necroTracker.dimensionPoints = num3;
			}
			int num4 = necroTracker?.dimensionLevel ?? 1;
			int num5 = necroTracker?.dimensionXP ?? 0;
			int requiredXP = PortalVisuals.GetRequiredXP(num4);
			Text.Anchor = TextAnchor.UpperLeft;
			Rect rect = listing_Standard.GetRect(Text.LineHeight);
			Widgets.Label(rect, "OWD_DS_DL".Translate(num4, num5, requiredXP));
			TooltipHandler.TipRegion(rect, "OWD_DS_earnDL".Translate());
			Rect rect2 = listing_Standard.GetRect(30f);
			string text = "OWD_DS_ADP".Translate();
			float x = Text.CalcSize(text).x;
			float x2 = Text.CalcSize(num3.ToString()).x;
			float num6 = rect2.x + (rect2.width - (x + x2)) / 2f;
			Widgets.Label(new Rect(num6, rect2.y, x, rect2.height), text);
			GUI.color = Color.green;
			Widgets.Label(new Rect(num6 + x, rect2.y, x2, rect2.height), num3.ToString());
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			listing_Standard.GapLine();
			Rect rect3 = listing_Standard.GetRect(30f);
			Widgets.Label(new Rect(rect3.x, rect3.y, rect3.width - 40f, rect3.height), "OWD_StabilizerLimit".Translate(comp.unlockedStabilizerLimit));
			if (Widgets.ButtonText(new Rect(rect3.xMax - 30f, rect3.y, 24f, 24f), "+"))
			{
				if (num3 > 0)
				{
					Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("OWD_StabilizerUpgradeConfirm".Translate(), delegate
					{
						comp.unlockedStabilizerLimit++;
					}));
				}
				else
				{
					Messages.Message("OWD_DimensionPointsMissing".Translate(), MessageTypeDefOf.RejectInput, historical: false);
				}
			}
			listing_Standard.GapLine();
			int num7 = dimMap.mapPawns.AllPawns.Count((Pawn p) => !p.Dead && p.RaceProps.Humanlike);
			string text2 = "OWD_Weather_Default".Translate();
			if (dimMap.weatherManager.curWeather == WeatherDefOf.Clear)
			{
				text2 = "OWD_Weather_Clear".Translate();
			}
			else if (dimMap.weatherManager.curWeather == MyModDefs.Rain)
			{
				text2 = "OWD_Weather_Rain".Translate();
			}
			listing_Standard.Label("OWD_DS_PDN".Translate(num7));
			listing_Standard.Label("OWD_DS_CE".Translate() + text2);
			listing_Standard.Gap(15f);
			if (listing_Standard.ButtonText("OWD_ExpandDimension".Translate().ToString()))
			{
				Find.WindowStack.Add(new Dialog_EnlargeDimension(dimMap, necroTracker));
				__instance.Close();
			}
			if (listing_Standard.ButtonText("OWD_DimensionEffects".Translate().ToString()))
			{
				Find.WindowStack.Add(new Dialog_DimensionEffects(dimMap));
				__instance.Close();
			}
			Rect rect4 = listing_Standard.GetRect(30f);
			if (Widgets.ButtonText(new Rect(rect4.x, rect4.y, rect4.width / 2f - 5f, rect4.height), "OWD_ManageRoofs".Translate().ToString()))
			{
				Find.WindowStack.Add(new Dialog_ManageDimensionGrid(dimMap, isRoofMode: true));
				__instance.Close();
			}
			if (Widgets.ButtonText(new Rect(rect4.x + rect4.width / 2f + 5f, rect4.y, rect4.width / 2f - 5f, rect4.height), "OWD_ManageFloors".Translate().ToString()))
			{
				Find.WindowStack.Add(new Dialog_ManageDimensionGrid(dimMap, isRoofMode: false));
				__instance.Close();
			}
			listing_Standard.End();

			return false;
        }

	}

	//校对完成 v2.3
	[HarmonyPatch(typeof(Dialog_EnlargeDimension))]
	public class Patch_Dialog_EnlargeDimension
    {
		[HarmonyPatch(nameof(Dialog_EnlargeDimension.DoWindowContents))]
		[HarmonyPrefix]
		public static bool Prefix_DoWindowContents(Dialog_EnlargeDimension __instance, Rect inRect)
        {
			Traverse trav = Traverse.Create(__instance);
			Pawn_NecromancerTracker necroTracker = trav.Field("necroTracker").GetValue<Pawn_NecromancerTracker>();
			MapComponent_PersonalDimension comp = trav.Field("comp").GetValue<MapComponent_PersonalDimension>();
			Map dimMap = trav.Field("dimMap").GetValue<Map>();
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "OWD_ExpandDimension".Translate().ToString());
			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(inRect.x, inRect.y + 35f, inRect.width, 40f), "OWD_ED_ExpandD".Translate());
			float num = 50f;
			float num2 = 5f;
			float num3 = 5f * num + 4f * num2;
			float num4 = inRect.x + (inRect.width - num3) / 2f;
			float num5 = inRect.y + 80f;
			float energyCost = 50f;
			int num6 = 1;
			int num7 = comp.pendingChanges.Count((PendingDimChange p) => p.type == DimChangeType.Expand);
			int num8 = comp.unlockedGrids.Count - 1 + num7;
			int num9 = ((necroTracker != null) ? Mathf.Max(0, necroTracker.dimensionLevel - num8) : 0);
			bool flag = necroTracker != null && num9 >= num6 && comp.dimensionEnergy >= energyCost;
			for (int num10 = 0; num10 >= -4; num10--)
			{
				for (int num11 = -2; num11 <= 2; num11++)
				{
					IntVec3 gridPos = new IntVec3(num11, 0, num10);
					Rect rect = new Rect(num4 + (float)(num11 + 2) * (num + num2), num5 + (float)(-num10) * (num + num2), num, num);
					bool flag2 = num11 == 0 && num10 == 0;
					bool flag3 = comp.unlockedGrids.Contains(gridPos);
					bool flag4 = comp.pendingChanges.Any((PendingDimChange p) => p.gridPos == gridPos);
					bool flag5 = !flag3 && (comp.unlockedGrids.Contains(new IntVec3(num11 + 1, 0, num10)) || comp.unlockedGrids.Contains(new IntVec3(num11 - 1, 0, num10)) || comp.unlockedGrids.Contains(new IntVec3(num11, 0, num10 + 1)) || comp.unlockedGrids.Contains(new IntVec3(num11, 0, num10 - 1)));
					if (flag2)
					{
						Widgets.DrawBoxSolid(rect, Color.yellow);
					}
					else if (flag4)
					{
						Widgets.DrawBoxSolid(rect, new Color(0.8f, 0.5f, 0.2f));
					}
					else if (flag3)
					{
						Widgets.DrawBoxSolid(rect, new Color(0.5f, 0.8f, 0.5f));
					}
					else if (flag5)
					{
						if (!flag)
						{
							GUI.color = Color.grey;
						}
						Widgets.DrawBoxSolid(rect, new Color(0.7f, 0.7f, 0.7f));
						if (Mouse.IsOver(rect))
						{
							Widgets.DrawHighlight(rect);
						}
						if (!flag4 && Widgets.ButtonInvisible(rect))
						{
							if (flag)
							{
								Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("OWD_ED_ExpandRequire".Translate(energyCost, num6), delegate
								{
									__instance.ExecuteExpand(gridPos, energyCost);
								}));
							}
							else
							{
								Messages.Message("OWD_ED_ExpandNo".Translate(energyCost, num6), MessageTypeDefOf.RejectInput, historical: false);
							}
						}
						GUI.color = Color.white;
					}
					else
					{
						Widgets.DrawBoxSolid(rect, new Color(0.3f, 0.3f, 0.3f));
					}
					Widgets.DrawBox(rect);
				}
			}
			if (Widgets.ButtonText(new Rect(inRect.width / 2f - 60f, inRect.height - 40f, 120f, 35f), "OWD_Close".Translate()))
			{
				__instance.Close();
			}

			return false;
        }

	}

	//校对完成 v2.2
	[HarmonyPatch(typeof(Dialog_LesserUndeadSettings))]
	public class Patch_Dialog_LesserUndeadSettings
    {
		[HarmonyPatch(nameof(Dialog_LesserUndeadSettings.DoWindowContents), new Type[] { typeof(Rect)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Auto-Spawn Limit";
			string replace = "OWD_LUS_ASL";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Target: ";
			replace = "OWD_LUS_T";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Area Restriction";
			replace = "OWD_LUS_AR";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Lesser Undead";
			replace = "OWD_LUS_LU";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Gnawer";
			replace = "OWD_LUS_G";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Hunger Settings";
			replace = "OWD_LUS_HS";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Meat";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "Bones";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "Only Corpses";
			replace = "OWD_LUS_OC";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Humanlikes";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "Entities";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "Animals";
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.3
	[HarmonyPatch(typeof(Dialog_ManageDimensionGrid))]
	public class Patch_Dialog_ManageDimensionGrid
    {
		[HarmonyPatch("ConfirmAndApplyFloor")]
		[HarmonyPrefix]
		public static bool Prefix_ConfirmAndApplyFloor(IntVec3 gridPos, string terrainDefName, ref string label)
        {
			Log.Message("Test");
            switch (label)
            {
				case "Default":
					label = "OWD_Default".Translate();
					break;
				case "Soil":
					label = "OWD_Soil".Translate();
					break;
				case "rough Granite":
					label = "OWD_Granite_Rough".Translate();
					break;
				case "rough Slate":
					label = "OWD_Slate_Rough".Translate();
					break;
				case "rough Sandstone":
					label = "OWD_Sandstone_Rough".Translate();
					break;
				case "rough Marble":
					label = "OWD_Marble_Rough".Translate();
					break;
			}

			return true;
        }

		[HarmonyPatch(nameof(Dialog_ManageDimensionGrid.DoWindowContents))]
		[HarmonyPrefix]
		public static bool Prefix_DoWindowContents(Dialog_ManageDimensionGrid __instance, Rect inRect)
        {
			Traverse trav = Traverse.Create(__instance);
			MapComponent_PersonalDimension comp = trav.Field("comp").GetValue<MapComponent_PersonalDimension>();
			Map dimMap = trav.Field("dimMap").GetValue<Map>();
			bool isRoofMode = trav.Field("isRoofMode").GetValue<bool>();
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), isRoofMode ? "OWD_ManageRoofs".Translate().ToString() : "OWD_ManageFloors".Translate().ToString());
			Text.Font = GameFont.Small;
			string label = (isRoofMode ? "OWD_MDG_TitleRoof".Translate() : "OWD_MDG_TitleFloor".Translate());
			Widgets.Label(new Rect(inRect.x, inRect.y + 35f, inRect.width, 40f), label);
			float num = 50f;
			float num2 = 5f;
			float num3 = 5f * num + 4f * num2;
			float num4 = inRect.x + (inRect.width - num3) / 2f;
			float num5 = inRect.y + 80f;
			for (int num6 = 0; num6 >= -4; num6--)
			{
				for (int i = -2; i <= 2; i++)
				{
					IntVec3 gridPos = new IntVec3(i, 0, num6);
					Rect rect = new Rect(num4 + (float)(i + 2) * (num + num2), num5 + (float)(-num6) * (num + num2), num, num);
					bool flag = comp.unlockedGrids.Contains(gridPos);
					if (comp.pendingChanges.Any((PendingDimChange p) => p.gridPos == gridPos))
					{
						Widgets.DrawBoxSolid(rect, new Color(0.8f, 0.5f, 0.2f));
						Text.Font = GameFont.Tiny;
						Text.Anchor = TextAnchor.MiddleCenter;
						Widgets.Label(rect, "Working...");
						Text.Font = GameFont.Small;
						Text.Anchor = TextAnchor.UpperLeft;
					}
					else if (flag)
					{
						if (isRoofMode)
						{
							bool flag2 = !comp.gridRoofs.ContainsKey(gridPos) || comp.gridRoofs[gridPos];
							Widgets.DrawBoxSolid(rect, flag2 ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.7f, 0.7f, 0.7f));
						}
						else
						{
							Widgets.DrawBoxSolid(rect, new Color(0.7f, 0.7f, 0.7f));
							Text.Font = GameFont.Tiny;
							Text.Anchor = TextAnchor.MiddleCenter;
							string text = (comp.gridFloors.ContainsKey(gridPos) ? comp.gridFloors[gridPos].Translate() : "OWD_GraySurface".Translate());
							string label2 = ((text == "OWD_Soil".Translate()) ? "OWD_Soil".Translate() : (text.Contains("OWD_Granite".Translate()) ? "OWD_Granite".Translate() : (text.Contains("OWD_Slate".Translate()) ? "OWD_Slate".Translate() : (text.Contains("OWD_Sandstone".Translate()) ? "OWD_Sandstone".Translate() : (text.Contains("OWD_Marble".Translate()) ? "OWD_Marble".Translate() : "OWD_Default".Translate())))));
							Widgets.Label(rect, label2);
							Text.Font = GameFont.Small;
							Text.Anchor = TextAnchor.UpperLeft;
						}
						if (Mouse.IsOver(rect))
						{
							Widgets.DrawHighlight(rect);
						}
						if (Widgets.ButtonInvisible(rect))
						{
							if (isRoofMode)
							{
								float cost = 10f;
								Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("OWD_RoofToggleConfirm".Translate(cost.ToString("F0")), delegate
								{
									__instance.ExecuteToggleRoof(gridPos, cost);
								}));
							}
							else
							{
								List<FloatMenuOption> list = new List<FloatMenuOption>
							{
								new FloatMenuOption("OWD_Default".Translate() + " (" + "OWD_GraySurface".Translate() + ")", delegate
								{
									trav.Method("ConfirmAndApplyFloor", new Type[]{ typeof(IntVec3), typeof(string), typeof(string)}).GetValue(gridPos, "GraySurface", "Default");
								}),
								new FloatMenuOption("OWD_Soil".Translate(), delegate
								{
									trav.Method("ConfirmAndApplyFloor", new Type[]{ typeof(IntVec3), typeof(string), typeof(string)}).GetValue(gridPos, "Soil", "Soil");
								})
							};
								FloatMenuOption item = new FloatMenuOption("OWD_Rocks".Translate(), delegate
								{
									List<FloatMenuOption> options = new List<FloatMenuOption>
								{
									new FloatMenuOption("OWD_Granite".Translate(), delegate
									{
										trav.Method("ConfirmAndApplyFloor", new Type[]{ typeof(IntVec3), typeof(string), typeof(string)}).GetValue(gridPos, "Granite_Rough", "rough Granite");
									}),
									new FloatMenuOption("OWD_Slate".Translate(), delegate
									{
										trav.Method("ConfirmAndApplyFloor", new Type[]{ typeof(IntVec3), typeof(string), typeof(string)}).GetValue(gridPos, "Slate_Rough", "rough Slate");
									}),
									new FloatMenuOption("OWD_Sandstone".Translate(), delegate
									{
										trav.Method("ConfirmAndApplyFloor", new Type[]{ typeof(IntVec3), typeof(string), typeof(string)}).GetValue(gridPos, "Sandstone_Rough", "rough Sandstone");
									}),
									new FloatMenuOption("OWD_Marble".Translate(), delegate
									{
										trav.Method("ConfirmAndApplyFloor", new Type[]{ typeof(IntVec3), typeof(string), typeof(string)}).GetValue(gridPos, "Marble_Rough", "rough Marble");
									})
								};
									Find.WindowStack.Add(new FloatMenu(options));
								});
								list.Add(item);
								Find.WindowStack.Add(new FloatMenu(list));
							}
						}
					}
					else
					{
						Widgets.DrawBoxSolid(rect, new Color(0.3f, 0.3f, 0.3f));
					}
					Widgets.DrawBox(rect);
				}
			}
			if (Widgets.ButtonText(new Rect(inRect.width / 2f - 60f, inRect.height - 40f, 120f, 35f), "OWD_Close".Translate()))
			{
				__instance.Close();
			}

			return false;
        }

	}

	//校对完成 v2.2
	[HarmonyPatch(typeof(Dialog_NecromancerGroupSettings))]
	public class Patch_Dialog_NecromancerGroupSettings
    {
		[HarmonyPatch(nameof(Dialog_NecromancerGroupSettings.DoWindowContents), new Type[] {typeof(Rect)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			string replacetarget = "Settings for Group: ";
			string replace = "OWD_NGS_SG";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Colonist Bar Visibility";
			replace = "OWD_NGS_CBV";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "  Hide Basic Undead";
			replace = "OWD_NGS_HBU";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "  Hide Undead Champions";
			replace = "OWD_NGS_HUC";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Reverted max undead setting.";
			replace = "OWD_NGS_RMS";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Applied group settings.";
			replace = "OWD_NGS_AGS";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();

		}
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(Fleshtomana))]
	public class Patch_Fleshtomana
	{
		[HarmonyPatch(nameof(Fleshtomana.CanPayManaCost))]
		[HarmonyPostfix]
		public static void Postfix_CanPayManaCost(ref bool __result, Pawn caster, float cost, ref string failReason)
		{
			// 关键：添加 null 检查
			if (string.IsNullOrEmpty(failReason))
			{
				return; // 如果 failReason 是 null 或空，直接返回
			}

			if (failReason.StartsWith("not enough") && failReason.Contains("to consume."))
			{
				failReason = "not_enough_bodypart".Translate();
			}
			else if (failReason.StartsWith("not enough") && failReason.Contains("Necrotic Energy"))
			{
				failReason = "not_enough_necrotic_energy".Translate();
			}
		}

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(Gizmo_LesserUndeadControls))]
	public class Patch_Gizmo_LesserUndeadControls_GizmoOnGUI
    {
		[HarmonyPatch(nameof(Gizmo_LesserUndeadControls.GizmoOnGUI), new Type[] { typeof(Vector2), typeof(float), typeof(GizmoRenderParms)})]
		[HarmonyPrefix]
		public static bool Prefix_GizmoOnGUI(Gizmo_LesserUndeadControls __instance, ref GizmoResult __result, Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
			Traverse trav = Traverse.Create(__instance);
			HediffComp_LesserUndeadSpawner comp = trav.Field("comp").GetValue<HediffComp_LesserUndeadSpawner>();
			Texture2D SpawnIcon = trav.Field("SpawnIcon").GetValue<Texture2D>();
			Texture2D HungerIcon = trav.Field("HungerIcon").GetValue<Texture2D>();
			Texture2D AutoIcon = trav.Field("AutoIcon").GetValue<Texture2D>();
			Texture2D BuffIcon = trav.Field("BuffIcon").GetValue<Texture2D>();
			Texture2D GuardIcon = trav.Field("GuardIcon").GetValue<Texture2D>();
			Rect rect = new Rect(topLeft.x, topLeft.y, __instance.GetWidth(maxWidth), 75f);
			Widgets.DrawWindowBackground(rect);
			Rect rect2 = rect;
			float num = 75f;
			float num2 = rect2.x + num + 4f;
			float num3 = 37f;
			Rect rect3 = new Rect(rect2.x, rect2.y, num, rect2.height);
			Rect rect4 = new Rect(num2, rect2.y, num3, num3);
			Rect rect5 = new Rect(num2 + num3, rect2.y, num3, num3);
			Rect rect6 = new Rect(num2, rect2.y + num3, num3, num3);
			Rect rect7 = new Rect(num2 + num3, rect2.y + num3, num3, num3);
			Rect rect8 = new Rect(num2, rect2.y, 75f, 75f);
			float num4 = (float)comp.cooldownTicksRemaining / (float)comp.Props.cooldownTicks;
			bool flag = num4 > 0f;
			GUI.color = (flag ? Color.gray : Color.white);
			Widgets.DrawTextureFitted(rect3, SpawnIcon, 1f);
			GUI.color = Color.white;
			if (num4 > 0f)
			{
				Widgets.FillableBar(rect3.BottomPart(0.15f), num4, SolidColorMaterials.NewSolidColorTexture(Color.gray), null, doBorder: false);
			}
			if (Mouse.IsOver(rect3))
			{
				Widgets.DrawHighlight(rect3);
				string text = "SpawnLesserUndead".Translate(comp.Props.spawnCost);
				if (num4 > 0f)
				{
					text = text + "\nCooldown: " + comp.cooldownTicksRemaining.ToStringTicksToPeriod();
				}
				TooltipHandler.TipRegion(rect3, text);
			}
			Rect rect9 = new Rect(rect3.xMax - 24f, rect3.y + 2f, 22f, 22f);
			bool flag2 = Mouse.IsOver(rect9);
			if (flag2)
			{
				Widgets.DrawHighlight(rect9);
				TooltipHandler.TipRegion(rect9, "Settings");
				if (Widgets.ButtonInvisible(rect9))
				{
					Find.WindowStack.Add(new Dialog_LesserUndeadSettings(comp));
					__result =  new GizmoResult(GizmoState.Interacted);
					return false;
				}
			}
			if (Mouse.IsOver(rect3) && !flag2)
			{
				Widgets.DrawHighlight(rect3);
				string text2 = "SpawnLesserUndead".Translate(comp.Props.spawnCost);
				if (num4 > 0f)
				{
					text2 = text2 + "\nCooldown: " + comp.cooldownTicksRemaining.ToStringTicksToPeriod();
				}
				TooltipHandler.TipRegion(rect3, text2);
			}
			Rect rect10 = new Rect(rect3.x + 2f, rect3.y + 2f, rect3.width - 4f, 20f);
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperLeft;
			int count = comp.activeLesserUndead.Count;
			int maxLesserUndead = comp.MaxLesserUndead;
			GUI.color = ((count >= maxLesserUndead) ? Color.red : Color.white);
			Widgets.Label(rect10, $"{count}/{maxLesserUndead}");
			GUI.color = Color.white;
			Widgets.DrawTextureFitted(rect9, ContentFinder<Texture2D>.Get("UI/Commands/List"), 1f);
			if (Widgets.ButtonInvisible(rect3) && !flag)
			{
				Need_NecroticEnergy energyNeed = comp.Pawn.needs?.TryGetNeed<Need_NecroticEnergy>();
				comp.TrySpawn(comp.Pawn, energyNeed);
				__result =  new GizmoResult(GizmoState.Interacted);
				return false;
			}
			trav.Method("DrawIcon", new Type[] { typeof(Rect), typeof(Texture2D), typeof(bool) }).GetValue(rect4, HungerIcon, comp.hungerMode);
			trav.Method("DrawIcon", new Type[] { typeof(Rect), typeof(Texture2D), typeof(bool) }).GetValue(rect5, AutoIcon, comp.autoSpawnMode);
			trav.Method("DrawIcon", new Type[] { typeof(Rect), typeof(Texture2D), typeof(bool) }).GetValue(rect6, BuffIcon, comp.buff);
			trav.Method("DrawIcon", new Type[] { typeof(Rect), typeof(Texture2D), typeof(bool) }).GetValue(rect7, GuardIcon, comp.guard);
			Widgets.DrawBox(rect8);
			GUI.color = Color.white;
			Widgets.DrawLineVertical(num2 + num3, rect2.y, 75f);
			Widgets.DrawLineHorizontal(num2, rect2.y + num3, 75f);
			object[] args = new object[] { rect4, comp.hungerMode, "LesserUndeadControl_text1" };
			trav.Method("HandleInteraction", new Type[] { typeof(Rect), typeof(bool).MakeByRefType(), typeof(string) }).GetValue(args);
			comp.hungerMode = (bool)args[1];

			args = new object[] {rect5, comp.autoSpawnMode, "LesserUndeadControl_text2" };
			trav.Method("HandleInteraction", new Type[] { typeof(Rect), typeof(bool).MakeByRefType(), typeof(string) }).GetValue(args);
			comp.autoSpawnMode = (bool)args[1];

			args = new object[] {rect6, comp.buff, "LesserUndeadControl_text3" };
			trav.Method("HandleInteraction", new Type[] { typeof(Rect), typeof(bool).MakeByRefType(), typeof(string) }).GetValue(args);
			comp.buff = (bool)args[1];

			args = new object[] {rect7, comp.guard, "LesserUndeadControl_text4" };
			trav.Method("HandleInteraction", new Type[] { typeof(Rect), typeof(bool).MakeByRefType(), typeof(string) }).GetValue(args);
			comp.guard = (bool)args[1];

			__result = new GizmoResult(GizmoState.Clear);
			return false;
        }

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(Gizmo_LesserUndeadControls))]
	public class Patch_Gizmo_LesserUndeadControls_HandleInteraction
    {
		[HarmonyPatch("HandleInteraction")]
		[HarmonyPrefix]
		public static bool Prefix_HandleInteraction(Gizmo_LesserUndeadControls __instance, Rect rect, ref bool state, string tooltip)
        {
			Traverse trav = Traverse.Create(__instance);
			HediffComp_LesserUndeadSpawner comp = trav.Field("comp").GetValue<HediffComp_LesserUndeadSpawner>();
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
				TooltipHandler.TipRegion(rect, tooltip.Translate() + "\nStatus: " + (state ? "ON" : "OFF"));
			}
			if (!Widgets.ButtonInvisible(rect))
			{
				return false;
			}
			state = !state;
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
			Pawn pawn = comp.Pawn;
			if (pawn.Map == null)
			{
				return false;
			}
			foreach (Pawn item in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				Hediff_LesserUndead firstHediff = item.health.hediffSet.GetFirstHediff<Hediff_LesserUndead>();
				if (firstHediff != null && firstHediff.overseer == pawn)
				{
					firstHediff.UpdateShouldFeed();
					item.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
			}

			return false;
        }

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(Gizmo_NecromancerBandwidth))]
	public class Patch_Gizmo_NecromancerBandwidth
	{
		[HarmonyPatch(nameof(Gizmo_NecromancerBandwidth.GizmoOnGUI), new Type[] { typeof(Vector2), typeof(float), typeof(GizmoRenderParms) })]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Control";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);
			replacetarget = "View deceased undead";
			string replace = "OWD_View_deceased_undead";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(Gizmo_NecromancerControls))]
	public class Patch_Gizmo_NecromancerControls
	{
		[HarmonyPatch(nameof(Gizmo_NecromancerControls.GizmoOnGUI), new Type[] { typeof(Vector2), typeof(float), typeof(GizmoRenderParms) })]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "(No undead)";
			string replace = "No_undead";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);

			return matcher.Instructions();
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(Gizmo_NecroticEnergyNeed))]
	public class Patch_Gizmo_NecroticEnergyNeed
    {
		[HarmonyPatch(nameof(Gizmo_NecroticEnergyNeed.GizmoOnGUI), new Type[] { typeof(Vector2), typeof(float), typeof(GizmoRenderParms)})]
		[HarmonyPrefix]
		public static bool Prefix_GizmoOnGUI(Gizmo_NecroticEnergyNeed __instance, ref GizmoResult __result ,Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
			Traverse trav = Traverse.Create(__instance);
			Need_NecroticEnergy need = trav.Field("need").GetValue<Need_NecroticEnergy>();
			Color DarkGreen = trav.Field("DarkGreen").GetValue<Color>();
			Color BrightGreen = trav.Field("BrightGreen").GetValue<Color>();
			Color EmptyColor = trav.Field("EmptyColor").GetValue<Color>();
			Color TextColor = trav.Field("TextColor").GetValue<Color>();
			Texture2D LimitedTex = trav.Field("LimitedTex").GetValue<Texture2D>();
			Texture2D UnlimitedTex = trav.Field("UnlimitedTex").GetValue<Texture2D>();
			bool draggingBar = trav.Field("draggingBar").GetValue<bool>();
			Rect rect = new Rect(topLeft.x, topLeft.y, __instance.GetWidth(maxWidth), 75f);
			Widgets.DrawWindowBackground(rect);
			Rect rect2 = rect.ContractedBy(6f);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, 20f);
			Widgets.Label(rect3, "Necrotic_Energy".Translate());
			float num = 24f;
			float width = rect2.width - num - 4f;
			float num2 = rect3.yMax + 4f;
			float num3 = 26f;
			Rect rect4 = new Rect(rect2.x, num2, width, num3);
			Rect rect5 = new Rect(rect4.xMax + 4f, num2 + (num3 - num) / 2f, num, num);
			float maxLevel = need.MaxLevel;
			float curLevel = need.CurLevel;
			float num4 = ((maxLevel > 0f) ? (curLevel / maxLevel) : 0f);
			HediffComp_LesserUndeadSpawner hediffComp_LesserUndeadSpawner = need.Pawn.health.hediffSet.GetFirstHediffOfDef(MyModDefs.UndeadOverseer)?.TryGetComp<HediffComp_LesserUndeadSpawner>();
			Color color = Color.Lerp(DarkGreen, BrightGreen, num4);
			Texture2D texture2D = SolidColorMaterials.NewSolidColorTexture(color);
			Texture2D texture2D2 = SolidColorMaterials.NewSolidColorTexture(EmptyColor);
			if (hediffComp_LesserUndeadSpawner != null)
			{
				Widgets.DraggableBar(rect4, texture2D, texture2D, texture2D2, SolidColorMaterials.NewSolidColorTexture(Color.yellow), ref draggingBar, num4, ref hediffComp_LesserUndeadSpawner.energyThreshold, null, 16);
				if (Mouse.IsOver(rect4))
				{
					TooltipHandler.TipRegion(rect4, $"Auto-feed Threshold: {hediffComp_LesserUndeadSpawner.energyThreshold:P0}");
				}
			}
			else
			{
				Widgets.FillableBar(rect4, num4, texture2D, texture2D2, doBorder: false);
			}
			string label = $"{curLevel:F0} / {maxLevel:F0}";
			Rect rect6 = new Rect(rect4.x, rect4.y, rect4.width, rect4.height);
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = TextColor;
			Widgets.Label(rect6, label);
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			if (Find.ResearchManager.GetProgress(MyModDefs.UnsafeCasting) >= MyModDefs.UnsafeCasting.baseCost)
			{
				if (Widgets.ButtonImage(rect5, need.unsafeCastingAllowed ? UnlimitedTex : LimitedTex))
				{
					need.unsafeCastingAllowed = !need.unsafeCastingAllowed;
					SoundDefOf.Tick_High.PlayOneShotOnCamera();
				}
				TooltipHandler.TipRegion(rect5, "Unsafe Casting: " + (need.unsafeCastingAllowed ? "Allowed" : "Disabled"));
			}
			float statValue = need.Pawn.GetStatValue(MyModDefs.OWD_EnergyRegeneration);
			Pawn_NecromancerTracker pawn_NecromancerTracker = need.Pawn.GetComp<PawnComp_Necromancer>()?.tracker;
			float num5 = ((pawn_NecromancerTracker != null) ? (pawn_NecromancerTracker.bonusEnergyRegen / 300f * 2500f) : 0f);
			float num6 = statValue + num5;
			Rect rect7 = new Rect(rect2.x, rect4.yMax + 2f, rect2.width, 20f);
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperCenter;
			GUI.color = Color.white;
			Widgets.Label(rect7, $"{num6:F2} / hour");
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
			string text = "Necrotic_Energy_Need_detail".Translate(need.CurLevel.ToString("F1"), maxLevel.ToString("F1"), num6.ToString("F2"));
			TooltipHandler.TipRegion(rect, text);
			__result =  new GizmoResult(GizmoState.Clear);
			return false;
        }
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(Gizmo_ShamblerControl))]
	public class Patch_Gizmo_ShamblerControl_GizmoOnGUI
	{
		[HarmonyPatch(nameof(Gizmo_ShamblerControl.GizmoOnGUI), new Type[] { typeof(Vector2), typeof(float), typeof(GizmoRenderParms)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Shambler settings";
			string replace = "OWD_Shambler_settings";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Select all active shamblers";
			replace = "OWD_Select_all_active_shamblers";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Enrage all active shamblers (Cost: 20 Energy, Duration: 2 min)";
			replace = "OWD_Enrage_shamblers";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Necromancer not found.";
			replace = "OWD_Necromancer_not_found";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "No active shamblers found to enrage.";
			replace = "OWD_No_active_shamblers";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(Gizmo_ShamblerControl))]
	public class Patch_Gizmo_ShamblerControl_SelectAllShamblers
    {
		[HarmonyPatch("SelectAllShamblers")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "No active shamblers found on this map.";
			string replace = "OWD_No_active_shamblers_map";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();

		}
    }

	//校对完成 v2.2
	[HarmonyPatch(typeof(IncidentWorker_SpawnNecroticObelisk))]
	public class Patch_IncidentWorker_SpawnNecroticObelisk_TryExecuteWorker
    {
		[HarmonyPatch("TryExecuteWorker", new Type[] { typeof(IncidentParms)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Necrotic Obelisk Manifestation";
			string replace = "OWD_SpawnObelisk_title";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "A ritual has completed, and a humming necrotic obelisk has manifested on the map, guarded by several skeletal minions.";
			replace = "OWD_SpawnObelisk_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(NecromancerForge))]
	public class Patch_NecromanceForge_Draw
    {
		[HarmonyPatch(nameof(NecromancerForge.Draw), new Type[] {typeof(Vector2), typeof(Pawn), typeof(HediffComp_UndeadForge)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Undead";
			string replace = "OWD_Undead";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Undead Champion";
			replace = "OWD_UndeadChampion";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Undead Overseer";
			replace = "OWD_UndeadOverseer";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Shamblers";
			replace = "OWD_Shamblers";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();

		}
    }

	//校对完成v2.2
	[HarmonyPatch(typeof(NecromancerForge))]
	public class Patch_NecromancerForge_GetBonusString
    {
		[HarmonyPatch("GetBonusString", new Type[] { typeof(string), typeof(int)})]
		[HarmonyPrefix]
		public static bool Prefix_GetBonusString(ref string __result,string key, int level)
        {
			if (level == 0)
			{
				__result =  "";
				return false;
			}
			string text = "green";
			__result =  key switch
			{
				"Undead_Speed" => "[" + "OWD_Forge_MoveSpeed".Translate() + $"<color={text}>+{level * 3}%</color>]",
				"Undead_Control" => "[" + "OWD_Forge_Consciousness".Translate() + $"<color={text}>+{level * 3}%</color>]",
				"Undead_Cost" => "[" + "OWD_Forge_WorkSpeed".Translate() + $"<color={text}>+{level * 3}%</color>]",
				"Champ_Speed" => "[" + "OWD_Forge_MoveSpeed".Translate() + $"<color={text}>+{level * 3}%</color>]",
				"Champ_Resist" => "[" + "OWD_Forge_IncomingDamage".Translate() + $"<color={text}>-{level * 3}%</color>]",
				"Champ_Fight" => "[" + "OWD_Forge_MeleeDamage".Translate() + $"<color={text}>+{level * 3}%</color>, " + "OWD_Forge_HitChance".Translate() + $"<color={text}>+{(float)level * 0.5f}</color>]",
				"Over_Cap" => "[" + "OWD_Forge_LevelCap".Translate() + $"<color={text}>+{level * 15}</color>]",
				"Over_Link" => "[" + "OWD_Forge_RegenSiphon".Translate() + $"<color={text}>+{level * 5}%</color>]",
				"Over_Exp" => "[" + "OWD_Forge_Experience_Gain".Translate() + $"<color={text}>+{level * 10}%</color>]",
				"Shambler_March" => "[" + "OWD_Forge_MoveSpeed".Translate() + $"<color={text}>+{level * 3}%</color>]",
				"Shambler_Bloat" => "[" + "OWD_Forge_IncomingDamage".Translate() + $"<color={text}>-{level * 3}%</color>]",
				"Shambler_Rot" => "[" + "OWD_Forge_Lifespan".Translate() + $"<color={text}>+{level * 12}h</color>]",
				_ => "",
			};
			return false;
        }

	}

	//校对完成 v2.2
	[HarmonyPatch(typeof(OneWithDeathMod))]
	public class Patch_OneWithDeathMod
    {
		[HarmonyPatch(nameof(OneWithDeathMod.DoSettingsWindowContents), new Type[] {typeof(Rect)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Enable Debug Logging";
			string replace = "OWD_OWDM_EDL";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Toggle debug logging for One With Death mod.";
			replace = "OWD_OWDM_EDL_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Disable Custom Colonist Bar";
			replace = "OWD_OWDM_DCCB";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "If checked, undead will use the vanilla (or modded) colonist bar style instead of the custom green/grey style. Requires game restart or reload to take full effect.";
			replace = "OWD_OWDM_DCCB_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Disable Servitor Diminishing Returns";
			replace = "OWD_OWDM_DSDR";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "If checked, multiple control servitors will give their full control bonus instead of suffering a 50% penalty per additional servitor.";
			replace = "OWD_OWDM_DSDR_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Undead Appearance:";
			replace = "OWD_OWDM_UA";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Show Basic Undead as Skeletons";
			replace = "OWD_OWDM_SBUS";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "If checked, basic Undead will use desiccated graphics. If unchecked, they use fresh graphics.";
			replace = "OWD_OWDM_SBUS_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Show Champions as Skeletons";
			replace = "OWD_OWDM_SCS";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "If checked, Undead Champions will use desiccated graphics. If unchecked, they use rotting graphics.";
			replace = "OWD_OWDM_SCS_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Show Overseers as Skeletons";
			replace = "OWD_OWDM_SOS";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "If checked, Undead Overseers will use desiccated graphics. If unchecked, they use fresh graphics.";
			replace = "OWD_OWDM_SOS_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Show 'One With Death' Pawns as Skeletons";
			replace = "OWD_OWDM_SPS";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "If checked, pawns with the 'One With Death' hediff will use desiccated graphics. If unchecked, they use fresh graphics.";
			replace = "OWD_OWDM_SPS_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Cheating/Customization";
			replace = "OWD_OWDM_Cheat";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Gene Customization...";
			replace = "OWD_OWDM_GC";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Control Multiplier Cheat:";
			replace = "OWD_OWDM_CMC";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Multiplies the Necromancer's base Bandwidth and derived Control Points. Visual UI updates might require pawn re-selection or game reload.";
			replace = "OWD_OWDM_CMC_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Click to change the control multiplier.";
			replace = "OWD_OWDM_CCCM";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(Hediff_NecromancerImplant))]
	public class Pathc_Hediff_NecromancerImplant
	{
		[HarmonyPatch(nameof(Hediff_NecromancerImplant.ShowCompletionWindow), new Type[] { typeof(Pawn) })]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			string replacetarget = "No matter how many pages I turn, they remain empty—yet I cannot shake the feeling that there is something here. Something waiting. A pressure builds behind my eyes, and then—symbols. Shapes I do not recognize, yet they burn into my mind as if they have always been there. My hand trembles, then moves on its own. I take up a pen and begin to write, not with intent, but with desperation. The symbols pour forth, erratic, feverish, like something clawing its way free.\r\n\r\nThree pages. At first, meaningless scribbles. But as I focus, the disorder shifts. Patterns emerge. Structure. I feel them before I see them—microscopic constructs, archites, flowing from the pages into my flesh. The sensation is overwhelming, an intrusion at the very edge of perception. And then, for the briefest moment, I see—something. A presence. A design beyond comprehension. And in that moment, I understand.\r\n\r\nI can read what I have written.\r\n\r\nBut it is not enough. The knowledge lingers at the edge of my thoughts, incomplete, unfinished. I must press forward. There is more to uncover—so much more.";
			var matcher = new CodeMatcher(instructions);
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, "Awakening_text");

			return matcher.Instructions();
		}
	}

	//校对完成 v2.1
	[HarmonyPatch]
	public class MannulPatch_Hediff_NecromancerImplant
	{
		public static MethodInfo TargetMethod()
		{
			return AccessTools.Method("OneWithDeath.Hediff_NecromancerImplant/<>c__DisplayClass7_0:<ShowCompletionWindow>b__0",
				new Type[0],
				null);
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			string replacetarget = "Awakening";
			var matcher = new CodeMatcher(instructions);
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();

			matcher = translate_OWD.Translate_Direct(matcher, replacetarget);

			replacetarget = "Pawn Became Necromancer";
			string replace = "Awakening_after";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);

			return matcher.Instructions();
		}
	}

	//校对完成 v2.1
	[HarmonyPatch]
	public class MannulPatch_JobDriver_InstallNecromanticCore_MakeNewToils_0
	{
		public static MethodInfo TargetMethod()
		{
			return AccessTools.Method("OneWithDeath.JobDriver_InstallNecromanticCore/<>c__DisplayClass2_0:<MakeNewToils>b__1",
				new Type[0],
				null);
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Fleshbound Grimoire";
			string replace = "Fleshbound_Grimoire";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);

			replacetarget = "As you open the ancient tome, you feel as if something is calling you from those white, empty pages, a chill runs down your spine. Are you sure you want to read the book?";
			replace = "Fleshbound_Grimoire_text";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);

			return matcher.Instructions();
		}
	}

	//校对完成 v2.1
	[HarmonyPatch]
	public class MannulPatch_JobDriver_InstallNecromanticCore_MakeNewToils_1
    {
		public static MethodInfo TargetMethod()
		{
			return AccessTools.Method("OneWithDeath.JobDriver_InstallNecromanticCore/<>c__DisplayClass2_0:<MakeNewToils>b__0",
				new Type[0],
				null);
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = " is undead and cannot comprehend the Fleshbound Grimoire.";
			string replace = "Fleshbound_Grimoire_Undead";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(PawnComp_Necromancer))]
	public class Patch_PawnComp_Necromancer
	{

		[HarmonyPatch(nameof(PawnComp_Necromancer.CompGetGizmosExtra))]
		[HarmonyPrefix]
		public static bool Prefix_CompGetGizmosExtra(PawnComp_Necromancer __instance, ref IEnumerable<Gizmo> __result)
		{
			var gizmos = new List<Gizmo>();
			if (__instance.pawn == null || __instance.pawn.Faction != Faction.OfPlayer || __instance.tracker == null)
			{
				__result = gizmos;
				return false;
			}
			gizmos.Add(new Command_BookOfTheDead(__instance.pawn));
			gizmos.Add(new Gizmo_NecromancerBandwidth(__instance.tracker));
			if (__instance.pawn.health.hediffSet.HasHediff(MyModDefs.OWD_NecroticFlow))
			{
				gizmos.Add(new Gizmo_ShamblerControl(__instance.tracker));
			}
			using (List<Pawn_NecromancerTracker.UndeadControlGroup>.Enumerator enumerator = __instance.tracker.controlGroups.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					gizmos.Add(new Gizmo_NecromancerControls(group: enumerator.Current, tracker: __instance.tracker));
				}
			}
			gizmos.Add(new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("UI/Commands/SelectAllMechs"),
				defaultLabel = "Select_All_Undead".Translate(),
				action = delegate
				{
					Find.Selector.ClearSelection();
					foreach (Pawn item in __instance.tracker.linkedUndead)
					{
						if (item != null)
						{
							Find.Selector.Select(item);
						}
					}
				}
			});
			if (__instance.tracker.abilitiesInBar == null)
			{
				__instance.tracker.abilitiesInBar = new List<string>();
			}
			foreach (Ability_Necromancy ability in __instance.necromancyAbilities)
			{
				if (!__instance.tracker.abilitiesInBar.Contains(ability.def.defName))
				{
					continue;
				}
				foreach (Command gizmo in ability.GetGizmos())
				{
					gizmos.Add(gizmo);
				}
			}
			__result = gizmos;
			return false;
		}
	}

	//校对完成 v2.2
	[HarmonyPatch(typeof(PlaceWorker_ControlPylon))]
	public class Patch_PlaceWorker_ControlPylon
    {
		[HarmonyPatch(nameof(PlaceWorker_ControlPylon.AllowsPlacing), new Type[] { typeof(BuildableDef), typeof(IntVec3), typeof(Rot4), typeof(Map), typeof(Thing), typeof(Thing)})]
		[HarmonyPostfix]
		public static void Postfix_AllowsPlacing(ref AcceptanceReport __result)
        {
			if(__result.Reason == "Internal error: Servitor Def not found.")
            {
				__result = new AcceptanceReport("ControlPylon_r1".Translate());
            }else if(__result.Reason == "Internal error: Auxiliary Servitor is missing CompProperties_Facility.")
            {
				__result = new AcceptanceReport("ControlPylon_r2".Translate());
            }else if(__result.Reason.StartsWith("All nearby Control"))
            {
				__result = new AcceptanceReport("ControlPylon_r4".Translate());
            }
            else if(__result.Reason.StartsWith("Must be placed"))
            {
				float maxDist = OWD_TranslateUtility.ParseExtractValue(__result.Reason, 5);
				__result = new AcceptanceReport("ControlPylon_r3".Translate(maxDist));
            }
			
        }

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualDef_ExtractSoulShard))]
	public class Patch_PsychicRitualDef_ExtractSoulShard
    {
		[HarmonyPatch(nameof(PsychicRitualDef_ExtractSoulShard.BlockingIssues))]
		[HarmonyPostfix]
		public static IEnumerable<string> Postfix_BlockingIssues(IEnumerable<string> issues)
        {
			if(issues.Count() > 0)
            {
				foreach(var issue in issues)
                {
					if("No Control Servitor in range." == issue)
                    {
						yield return "No_Control_Servitor".Translate();
					}else if("Requires 50 stored energy in linked Control Servitor." == issue)
                    {
						var energyvalue = OWD_TranslateUtility.ParseExtractValue(issue, 2);
						yield return "require_Control_Servitor_energy".Translate(energyvalue);
                    }
                    else
                    {
						yield return issue;
                    }
                }
            }
        }
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualDef_Exuvia))]
	public class Pathc_PsychicRitualDef_Exuvia
	{
		[HarmonyPatch(nameof(PsychicRitualDef_Exuvia.BlockingIssues))]
		[HarmonyPostfix]
		public static IEnumerable<string> Postfix_BlockingIssues(IEnumerable<string> issues)
		{
			if (issues.Count() > 0)
			{
				foreach (var issue in issues)
				{
					if ("No Control Servitor in range." == issue)
					{
						yield return "No_Control_Servitor".Translate();
					}
					else if (issue.StartsWith("Requires") && issue.Contains("stored energy"))
					{
						var energevalue = OWD_TranslateUtility.ParseExtractValue(issue, 2);
						yield return "require_Control_Servitor_energy".Translate(energevalue);
					}
					else
					{
						yield return issue;
					}
				}
			}
		}

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualDef_InscribeSoul))]
	public class Patch_PsychicRitualDef_InscribeSoul
    {
		[HarmonyPatch(nameof(PsychicRitualDef_InscribeSoul.BlockingIssues))]
		[HarmonyPostfix]
		public static IEnumerable<string> Postfix_BlockingIssues(IEnumerable<string> issues)
        {
			if(issues.Count() > 0)
            {
				foreach(var issue in issues)
                {
					if("No Control Servitor in range." == issue)
                    {
						yield return "No_Control_Servitor".Translate();
					}else if("Requires 50 stored energy in linked Control Servitor." == issue)
                    {
						var energyvalue = OWD_TranslateUtility.ParseExtractValue(issue, 2);
						yield return "require_Control_Servitor_energy".Translate(energyvalue);
                    }
                    else
                    {
						yield return issue;
                    }
                }
            }
        }
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualDef_Provocation))]
	public class Patch_PsychicRitualDef_Provocation
    {
		[HarmonyPatch(nameof(PsychicRitualDef_Provocation.BlockingIssues))]
		[HarmonyPostfix]
		public static IEnumerable<string> Postfix_BlockingIssues(IEnumerable<string> issues)
        {
			if(issues.Count() > 0)
            {
				foreach(var issue in issues)
                {
					if("Ritual tracker component not found." == issue)
                    {
						yield return "ritual_trackerComponent_not_found".Translate();
					}else if("No Control Servitor in range for energy cost." == issue)
                    {
						yield return "No_Control_Servitor".Translate();
					}else if(issue.StartsWith("Requires") && issue.Contains("stored energy"))
                    {
						var energyvalue = OWD_TranslateUtility.ParseExtractValue(issue, 2);
						yield return "require_Control_Servitor_energy".Translate(energyvalue);
                    }
                    else
                    {
						yield return issue;
                    }
                }
            }
        }
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualRoleDef_CasterExuvia))]
	public class Patch_PsychicRitualRoleDef_CasterExuvia
    {
		[HarmonyPatch(nameof(PsychicRitualRoleDef_CasterExuvia.PawnCannotDoReason))]
		[HarmonyPostfix]
		public static void Postfix_PawnCannotDoReason(ref TaggedString __result)
        {
			if ("Caster must be One with Death." == __result)
				__result = "roleDef_casterExuvia".Translate();
		}

	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualRoleDef_Infused))]
	public class Patch_PsychicRitualRoleDef_Infused
    {
		[HarmonyPatch(nameof(PsychicRitualRoleDef_Infused.PawnCannotDoReason))]
		[HarmonyPostfix]
		public static void Postfix_PawnCannotDoReason(ref TaggedString __result)
        {
			if("Pawn must have an infused soul shard." == __result)
            {
				__result = "roleDef_infused".Translate();
			}
        }
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualToil_EnduviaOutcome))]
	public class Patch_PsychicRitualToil_EnduviaOutcome
    {
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(PsychicRitualToil_InvokeHorax), "End")]
		public static void BaseEnd(object instance, PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
		{

		}

		[HarmonyPatch(nameof(PsychicRitualToil_EnduviaOutcome.End), new Type[] { typeof(PsychicRitual), typeof(PsychicRitualGraph), typeof(bool) })]
		[HarmonyPrefix]
		public static bool Prefix_End(PsychicRitualToil_EnduviaOutcome __instance, PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
        {
			//var cgd = AccessTools.Method(typeof(PsychicRitualToil_EnduviaOutcome), "")
			Traverse trav = Traverse.Create(__instance);
			BaseEnd(__instance, psychicRitual, parent, success);
			if (!success)
			{
				trav.Method("ClearGlobalData").GetValue();
				return false;
			}
			Pawn pawn = psychicRitual.assignments.FirstAssignedPawn((psychicRitual.def as PsychicRitualDef_InvocationCircle).TargetRole);
			if (pawn == null)
			{
				trav.Method("ClearGlobalData").GetValue();
				return false;
			}
			if (RitualGlobalData.NoSoulRitual)
			{
				trav.Method("CatastrophicFailure", new Type[] { typeof(Pawn), typeof(string)}).GetValue(pawn, "ritual_enduvial_fail_0");
				trav.Method("ClearGlobalData").GetValue();
				return false;
			}
			string selectedSoulID = RitualGlobalData.SelectedSoulID;
			if (string.IsNullOrEmpty(selectedSoulID))
			{
				trav.Method("CatastrophicFailure", new Type[] { typeof(Pawn), typeof(string)}).GetValue(pawn, "ritual_enduvial_fail_1");
				trav.Method("ClearGlobalData").GetValue();
				return false;
			}
			SoulData soulWithActiveTraitsOnly = RitualGlobalData.SoulWithActiveTraitsOnly;
			if (soulWithActiveTraitsOnly == null)
			{
				trav.Method("CatastrophicFailure", new Type[] { typeof(Pawn), typeof(string) }).GetValue(pawn, "ritual_enduvial_fail_2");
				trav.Method("ClearGlobalData").GetValue();
				return false;
			}
			foreach (Pawn allMap in PawnsFinder.AllMaps)
			{
				if (allMap == pawn)
				{
					continue;
				}
				string pawnSoulID = trav.Method("GetPawnSoulID", new Type[] {typeof(Pawn) }).GetValue<string>(allMap);//GetPawnSoulID(allMap);
				bool flag = false;
				if (!string.IsNullOrEmpty(pawnSoulID))
				{
					if (pawnSoulID == selectedSoulID)
					{
						flag = true;
					}
				}
				else if (allMap.Name != null && allMap.Name.ToStringFull == soulWithActiveTraitsOnly.name.ToStringFull)
				{
					flag = true;
				}
				if (!flag)
				{
					continue;
				}
				trav.Method("CatastrophicFailure", new Type[] { typeof(Pawn), typeof(string) }).GetValue(pawn, "ritual_enduvial_fail_3");
				trav.Method("ClearGlobalData").GetValue();
				return false;
			}
			trav.Method("Success", new Type[] { typeof(Pawn), typeof(SoulData), typeof(string)}).GetValue(pawn, soulWithActiveTraitsOnly, selectedSoulID);
			trav.Method("ClearGlobalData").GetValue();

			return false;
        }

		[HarmonyPatch("CatastrophicFailure")]
		[HarmonyPrefix]
		public static bool Prefix_CatastrophicFailure(PsychicRitualToil_EnduviaOutcome __instance, Pawn sacrifice, string message)
        {
			Map map = sacrifice.Map;
			IntVec3 position = sacrifice.Position;
			string labelShortCap = sacrifice.LabelShortCap;
			sacrifice.Kill(null, null);
			if (ResurrectionUtility.TryResurrect(sacrifice))
			{
				if (sacrifice.Name is NameTriple nameTriple)
				{
					sacrifice.Name = new NameTriple(nameTriple.First, nameTriple.Nick + "?", nameTriple.Last);
				}
				MutantUtility.SetPawnAsMutantInstantly(sacrifice, MutantDefOf.Shambler);
				Hediff hediff = HediffMaker.MakeHediff(MyModDefs.OWD_Enrage, sacrifice);
				if (hediff != null)
				{
					hediff.Severity = 2f;
					sacrifice.health.AddHediff(hediff);
				}
				if("ritual_enduvial_fail_3" != message)
					Find.LetterStack.ReceiveLetter("OWD_Ritual_Failed".Translate(), message.Translate() + "ritual_enduvial_fail_d1".Translate(labelShortCap), LetterDefOf.ThreatBig, new LookTargets(sacrifice));
				else
					Find.LetterStack.ReceiveLetter("OWD_Ritual_Failed".Translate(), message.Translate(RitualGlobalData.SoulWithActiveTraitsOnly.name.ToString()) + "ritual_enduvial_fail_d1".Translate(labelShortCap), LetterDefOf.ThreatBig, new LookTargets(sacrifice));

			}
			else
			{
				if ("ritual_enduvial_fail_3" != message)
					Find.LetterStack.ReceiveLetter("OWD_Ritual_Failed".Translate(), message.Translate() + "ritual_enduvial_fail_d2".Translate(), LetterDefOf.NegativeEvent, new TargetInfo(position, map));
				else
					Find.LetterStack.ReceiveLetter("OWD_Ritual_Failed".Translate(), message.Translate(RitualGlobalData.SoulWithActiveTraitsOnly.name.ToString()) + "ritual_enduvial_fail_d2".Translate(), LetterDefOf.NegativeEvent, new TargetInfo(position, map));
			}
			return false;
		}

		[HarmonyPatch("Success")]
		[HarmonyPrefix]
		public static bool Prefix_Success(PsychicRitualToil_EnduviaOutcome __instance, Pawn sacrifice, SoulData soulToApply, string soulId)
        {
			Map map = sacrifice.Map;
			IntVec3 position = sacrifice.Position;
			HostData hostData = NecromancerSerializer.CreateHostDataFromPawn(sacrifice);
			sacrifice.Kill(null, null);
			Corpse firstThing = position.GetFirstThing<Corpse>(map);
			if (firstThing == null)
			{
				return false;
			}
			if (RitualGlobalData.TraitDefNamesToDiscard != null && RitualGlobalData.TraitDefNamesToDiscard.Any())
			{
				string dataString = SoulSavingUtility.LoadSoul(soulId);
				SoulData soulData = NecromancerSerializer.DeserializeFromString(dataString, firstThing.InnerPawn);
				if (soulData != null)
				{
					soulData.traits.RemoveAll((SimpleTraitData t) => RitualGlobalData.TraitDefNamesToDiscard.Contains(t.defName));
					string data = NecromancerSerializer.SerializeToString(soulData);
					SoulSavingUtility.SaveSoul(soulId, soulData.name.ToStringFull, data);
				}
			}
			MapComponent_SoulTransfer mapComponent_SoulTransfer = map.GetComponent<MapComponent_SoulTransfer>();
			if (mapComponent_SoulTransfer == null)
			{
				mapComponent_SoulTransfer = new MapComponent_SoulTransfer(map);
				map.components.Add(mapComponent_SoulTransfer);
			}
			mapComponent_SoulTransfer.AddAction(new DelayedAction_Overwrite(firstThing, soulToApply, hostData, soulId));
			Find.LetterStack.ReceiveLetter("OWD_Ritual_Complete".Translate(), "ritual_enduvial_success".Translate(soulToApply.name.ToString(), hostData.name.ToString()), LetterDefOf.PositiveEvent, new LookTargets(firstThing));

			return false;
        }
	}

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualToil_ExtractSoulShardOutcome))]
	public class Patch_PsychicRitualToil_ExtractSoulShardOutcome
    {
		[HarmonyPatch(nameof(PsychicRitualToil_ExtractSoulShardOutcome.End))]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Ritual failed: Control Servitor lost.";
			string replace = "ritual_extractsoul_fail_0";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Ritual failed: Insufficient energy in Control Servitor.";
			replace = "ritual_extractsoul_fail_1";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "The Soul Shard shattered during extraction due to insufficient focus!";
			replace = "ritual_extractsoul_shared_break";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "Successfully extracted and cleansed the soul shard from ";
			replace = "ritual_extractsoul_success";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();
		}
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualToil_ExuviaOutcome))]
	public class Patch_PsychicRitualToil_ExuviaOutcome
    {

		[HarmonyReversePatch]
		[HarmonyPatch(typeof(PsychicRitualToil_InvokeHorax), "End")]
		public static void BaseEnd(object instance, PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
		{

		}

		[HarmonyPatch(nameof(PsychicRitualToil_ExuviaOutcome.End), new Type[] {typeof(PsychicRitual), typeof(PsychicRitualGraph), typeof(bool)})]
		[HarmonyPrefix]
		public static bool Prefix_End(PsychicRitualToil_ExuviaOutcome __instance, PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
        {
			Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(MyModDefs.OWD_CasterExuvia);
			if (success && pawn != null)
			{
				Thing thing = psychicRitual.assignments.Target.Thing;
				int num = pawn.health.hediffSet.GetFirstHediff<Hediff_PawnSoul>()?.ritualCounter ?? 0;
				float num2 = 500f * (1f + 0.5f * (float)num);
				Thing thing2 = GenClosest.ClosestThingReachable(thing.Position, thing.Map, ThingRequest.ForDef(MyModDefs.OWD_CServitor), PathEndMode.InteractionCell, TraverseParms.For(TraverseMode.PassDoors), thing.def.specialDisplayRadius);
				if (thing2 != null)
				{
					ThingWithComps obj = thing2 as ThingWithComps;
					if (obj == null || !(obj.GetComp<CompEnergyTransfer>()?.StoredEnergy < num2))
					{
						goto IL_010b;
					}
				}
				success = false;
			}
			goto IL_010b;
			IL_010b:
			BaseEnd(__instance, psychicRitual, parent, success);
			if (!success)
			{
				return false;
			}
			int num3 = pawn.health.hediffSet.GetFirstHediff<Hediff_PawnSoul>()?.ritualCounter ?? 0;
			float num4 = 500f * (1f + 0.5f * (float)num3);
			Thing thing3 = psychicRitual.assignments.Target.Thing;
			Thing thing4 = GenClosest.ClosestThingReachable(thing3.Position, thing3.Map, ThingRequest.ForDef(MyModDefs.OWD_CServitor), PathEndMode.InteractionCell, TraverseParms.For(TraverseMode.PassDoors), thing3.def.specialDisplayRadius);
			(thing4 as ThingWithComps)?.GetComp<CompEnergyTransfer>()?.AddEnergy(0f - num4);
			Pawn pawn2 = psychicRitual.assignments.FirstAssignedPawn(MyModDefs.OWD_Sacrifice);
			if (pawn == null || pawn2 == null)
			{
				return false;
			}
			Hediff_PawnSoul firstHediff = pawn.health.hediffSet.GetFirstHediff<Hediff_PawnSoul>();
			SoulData soulData;
			string soulID;
			string toStringShort;
			if (firstHediff != null)
			{
				soulData = NecromancerSerializer.CreateSoulDataFromPawn(pawn, includeReferences: true);
				HostData originalHostData = firstHediff.originalHostData;
				soulID = firstHediff.soulID;
				toStringShort = pawn.Name.ToStringShort;
				HostPath.RestorePawnFromHostData(pawn, originalHostData);
			}
			else
			{
				toStringShort = pawn.Name.ToStringShort;
				HediffComp_SoulID hediffComp_SoulID = pawn.health.hediffSet.GetFirstHediffOfDef(MyModDefs.NecromancerImplant)?.TryGetComp<HediffComp_SoulID>();
				if (hediffComp_SoulID != null)
				{
					if (string.IsNullOrEmpty(hediffComp_SoulID.soulID))
					{
						hediffComp_SoulID.soulID = Guid.NewGuid().ToString();
					}
					soulID = hediffComp_SoulID.soulID;
				}
				else
				{
					soulID = Guid.NewGuid().ToString();
				}
				soulData = NecromancerSerializer.CreateSoulDataFromPawn(pawn, includeReferences: true);
				NecromancerSerializer.RemoveSerializedHediffs(pawn);
			}
			pawn.forceNoDeathNotification = true;
			pawn.Kill(null, null);
			pawn.forceNoDeathNotification = false;
			if (pawn2 != null && !pawn2.Dead)
			{
				pawn2.Kill(null, null);
			}
			Thing thing5 = ThingMaker.MakeThing(MyModDefs.OWD_SoulOrb);
			CompStoredSoul compStoredSoul = (thing5 as ThingWithComps)?.GetComp<CompStoredSoul>();
			if (compStoredSoul != null)
			{
				compStoredSoul.soulID = soulID;
				compStoredSoul.soulName = toStringShort;
				compStoredSoul.soulData = soulData;
			}
			IntVec3 cell = psychicRitual.assignments.Target.Cell;
			GenSpawn.Spawn(thing5, cell, psychicRitual.Map);
			thing5.SetForbidden(value: true);
			Find.LetterStack.ReceiveLetter("ritual_exuvia_complete".Translate(), "ritual_exuvia_success".Translate(toStringShort), LetterDefOf.PositiveEvent, thing5);

			return false;
        }
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualToil_ForbiddenKnowledgeOutcome))]
	public class Patch_PsychicRitualToil_ForbiddenKnowledgeOutcome : PsychicRitualToil_InvokeHorax
	{
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(PsychicRitualToil_InvokeHorax), "End")]
		public static void BaseEnd(object instance, PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
        {

        }

		[HarmonyPatch(nameof(PsychicRitualToil_ForbiddenKnowledgeOutcome.End), new Type[] { typeof(PsychicRitual), typeof(PsychicRitualGraph), typeof(bool)})]
		[HarmonyPrefix]
		public static bool Prefix_End(PsychicRitualToil_ForbiddenKnowledgeOutcome __instance, PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
        {
			BaseEnd(__instance, psychicRitual, parent, success);
			if (!success)
			{
				return false;
			}
			Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(__instance.targetRole);
			if (pawn != null && !pawn.Dead)
			{
				pawn.Kill(null, null);
			}
			Map map = psychicRitual.Map;
			IntVec3 cell = psychicRitual.assignments.Target.Cell;
			Thing thing = ThingMaker.MakeThing(MyModDefs.OWD_NecromanticCore);
			GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
			Messages.Message("ritual_forbiddenknowledge_outcome".Translate(), thing, MessageTypeDefOf.PositiveEvent);
			int num = PawnsFinder.AllMapsWorldAndTemporary_Alive.Count((Pawn p) => p.Faction == Faction.OfPlayer && p.health.hediffSet.HasHediff(MyModDefs.NecromancerImplant));
			int num2 = 0;
			int num3 = 0;
			string text = "";
			switch (num)
			{
				case 0:
					num2 = 1;
					num3 = 0;
					text = "ritual_forbiddenknowledge_0".Translate();
					break;
				case 1:
					num2 = 4;
					num3 = 2;
					text = "ritual_forbiddenknowledge_1".Translate();
					break;
				case 2:
					num2 = 6;
					num3 = 4;
					text = "ritual_forbiddenknowledge_2".Translate();
					break;
				default:
					num2 = 10;
					num3 = 10;
					text = "ritual_forbiddenknowledge_default".Translate();
					break;
			}
			if (num2 <= 0)
			{
				return false;
			}
			Faction faction = Find.FactionManager.FirstFactionOfDef(MyModDefs.OWD_Undeads);
			if (faction == null)
			{
				faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(MyModDefs.OWD_Undeads));
				faction.Name = "Undeed_Legion".Translate();
				Find.FactionManager.Add(faction);
			}
			List<Pawn> list = new List<Pawn>();
			for (int num4 = 0; num4 < num2; num4++)
			{
				if (!CellFinder.TryFindRandomSpawnCellForPawnNear(cell, map, out var result, 10))
				{
					result = cell;
				}
				Pawn pawn2 = LegionSpawningUtility.SpawnLegionUndead(MyModDefs.OWD_LesserUndead_Legion_Armored, result, map, faction);
				if (pawn2 != null)
				{
					Hediff hediff = HediffMaker.MakeHediff(MyModDefs.OWD_ECrescens, pawn2);
					hediff.Severity = num3;
					pawn2.health.AddHediff(hediff);
					EffecterDefOf.Skip_Entry.Spawn(pawn2.Position, map).Cleanup();
					SoundDefOf.Psycast_Skip_Entry?.PlayOneShot(new TargetInfo(pawn2.Position, map));
					list.Add(pawn2);
				}
			}
			if (list.Any())
			{
				LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, sappers: false, useAvoidGridSmart: true, canSteal: true, breachers: true, canPickUpOpportunisticWeapons: true), map, list);
			}
			if (num > 0)
			{
				string text2 = "ritual_forbiddenknowledge_other_outcome".Translate(num2);
				Find.WindowStack.Add(new Dialog_MessageBox(text2 + "\n\n" + text, "OK"));
			}

			return false;
        }
    }

	//校对完成 v2.1
	[HarmonyPatch(typeof(PsychicRitualToil_InscribeSoul))]
	public class Patch_PsychicRitualToil_InscribeSoul
    {
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(PsychicRitualToil_InvokeHorax), "End")]
		public static void BaseEnd(object instance, PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
		{

		}

		[HarmonyPatch(nameof(PsychicRitualToil_InscribeSoul.End), new Type[] { typeof(PsychicRitual), typeof(PsychicRitualGraph), typeof(bool) })]
		[HarmonyPrefix]
		public static bool Prefix_End(PsychicRitualToil_InscribeSoul __instance, PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
        {
			if (!success)
			{
				BaseEnd(__instance, psychicRitual, parent, success: false);
				return false;
			}
			Thing thing = psychicRitual.assignments.Target.Thing;
			Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(__instance.invokerRole);
			if (__instance.requiredOffering != null)
			{
				float baseCount = __instance.requiredOffering.GetBaseCount();
				if (pawn.carryTracker.CarriedThing == null || !__instance.requiredOffering.filter.Allows(pawn.carryTracker.CarriedThing) || (float)pawn.carryTracker.CarriedThing.stackCount < baseCount)
				{
					Messages.Message("ritual_inscribesoul_fail_0".Translate(), MessageTypeDefOf.NegativeEvent);
					BaseEnd(__instance, psychicRitual, parent, success: false);
					return false;
				}
			}
			if (thing != null)
			{
				Thing thing2 = GenClosest.ClosestThingReachable(thing.Position, thing.Map, ThingRequest.ForDef(MyModDefs.OWD_CServitor), PathEndMode.InteractionCell, TraverseParms.For(TraverseMode.PassDoors), thing.def.specialDisplayRadius);
				if (thing2 == null)
				{
					Messages.Message("ritual_inscribesoul_fail_1".Translate(), MessageTypeDefOf.NegativeEvent);
					BaseEnd(__instance, psychicRitual, parent, success: false);
					return false;
				}
				CompEnergyTransfer compEnergyTransfer = (thing2 as ThingWithComps)?.GetComp<CompEnergyTransfer>();
				if (compEnergyTransfer == null || !(compEnergyTransfer.StoredEnergy >= 50f))
				{
					Messages.Message("ritual_inscribesoul_fail_2".Translate(), MessageTypeDefOf.NegativeEvent);
					BaseEnd(__instance, psychicRitual, parent, success: false);
					return false;
				}
				compEnergyTransfer.AddEnergy(-50f);
			}
			BaseEnd(__instance, psychicRitual, parent, success: true);
			Pawn pawn2 = psychicRitual.assignments.FirstAssignedPawn(MyModDefs.OWD_Sacrifice);
			if (pawn2 == null)
			{
				return false;
			}
			string text = Traverse.Create(__instance).Method("SelectHediffDef", new Type[] { typeof(Pawn)}).GetValue<string>(pawn2);
			string toStringFull = pawn2.Name.ToStringFull;
			if (!pawn2.Dead)
			{
				if (pawn2.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("OWD_BrokenMind")))
				{
					pawn2.Kill(null, null);
				}
				else
				{
					foreach (SkillRecord skill in pawn2.skills.skills)
					{
						skill.Level = Mathf.FloorToInt((float)skill.Level / 2f);
					}
					Hediff hediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("OWD_BrokenMind"), pawn2);
					pawn2.health.AddHediff(hediff);
					pawn2.health.forceDowned = true;
					if (pawn2.jobs?.curJob != null)
					{
						pawn2.jobs.EndCurrentJob(JobCondition.InterruptForced);
					}
				}
			}
			ThingDef def = (text.Contains("Major") ? MyModDefs.OWD_SoulShardFilled_Major : MyModDefs.OWD_SoulShardFilled_Minor);
			Thing thing3 = ThingMaker.MakeThing(def);
			CompSoulShard compSoulShard = (thing3 as ThingWithComps)?.GetComp<CompSoulShard>();
			if (compSoulShard != null)
			{
				compSoulShard.hediffDefNameToApply = text;
				compSoulShard.qualityPercent = (int)(psychicRitual.power * 100f);
				if (!GenPlace.TryPlaceThing(thing3, thing.InteractionCell, thing.Map, ThingPlaceMode.Near))
				{
					Log.Error($"[OneWithDeath] Could not spawn filled Soul Shard near the altar at {thing.Position}.");
				}
				Messages.Message("ritual_inscribesoul_success".Translate(toStringFull), thing3, MessageTypeDefOf.PositiveEvent);
			}
			else
			{
				Log.Error("[OneWithDeath] Failed to get CompSoulShard from newly created filledShard.");
			}

			return false;
        }
	}

	//校对完成 v2.2
	[HarmonyPatch(typeof(PsychicRitualToil_ProvocationOutcome))]
	public class Patch_PsychicRitualToil_ProvocationOutcome_End
    {
		[HarmonyPatch(nameof(PsychicRitualToil_ProvocationOutcome.End), new Type[] {typeof(PsychicRitual), typeof(PsychicRitualGraph), typeof(bool)})]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var matcher = new CodeMatcher(instructions);
			string replacetarget = "Necrotic Provocation Complete";
			string replace = "OWD_ProvocationOutcome_title";
			Hjx_translate_OWD translate_OWD = new Hjx_translate_OWD();
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			replacetarget = "you can feel a presence tracking your location.";
			replace = "OWD_ProvocationOutcome_desc";
			matcher = translate_OWD.Translate_Assingment(matcher, replacetarget, replace);
			return matcher.Instructions();

		}
    }
}
