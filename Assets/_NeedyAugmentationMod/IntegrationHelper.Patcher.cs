using System;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

using System.Diagnostics.CodeAnalysis;


namespace NeedyAugmentationMod {

	public static partial class IntegrationHelper {
		
		[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Haromy Injections")]
		public static class Patcher {

			public const string HarmonyId = "kodipher.NeedyAugmentation";
			
			static readonly Harmony harmony = new Harmony(HarmonyId);
			static bool isPatched = false;
			
			/// <remarks>May throw.</remarks>
			public static void EnsurePatched() {
				
				if (isPatched) return;
				
				// Prevents dupe patches in case of an error
				if (Harmony.HasAnyPatches(HarmonyId)) harmony.UnpatchAll(HarmonyId);
				
				Patch();
			}
			
			/*
			 * Flow of logic, with `!!!` marking patches.
			 * 
			 * OnBombTimerTick -> !!! -> [hasStarted guard] -> [elapsed guard] -> StartRunning();
			 * ComponentStrikeEvent,ComponentPassEvent -> [state guard] -> PlayerChangedBomb(tinyDelay);
			 * PlayerChangedBomb -> !!! -> [coroutine] -> [state guard] -> [rng/dupe guard] -> StartRunning();
			 * StartRunning -> [hasStarted guard] -> !!! ResetAndStart();
			 * WaitAndResetRoutine -> [coroutine] -> ChangeState(Cooldown) -> ResetAndStart()
			 * ResetAndStart -> ChangeState(Running)
			 * OnSolved,OnTimerExpired -> [shouldReset check] -> [if true] WaitAndResetRoutine() / [if false] ChangeState(Terminated)
			 * TurnOff -> !!! -> ChangeState(BombComplete)
			 *
			 */
			
			public const BindingFlags AllMethodFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			
			/// <remarks>May throw.</remarks>
			static void Patch() {

				MethodInfo original;
				MethodInfo prefix;
				MethodInfo transpiler;
				
				// -----
				
				// OnBombTimerTick
				// patch: if AugmentedActivatorComponent exists, let it do the waiting instead
				original = needyComponentType.GetMethod("OnBombTimerTick", AllMethodFlags);
				prefix = typeof(Patcher).GetMethod(nameof(OnBombTimerTickPrefix), AllMethodFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix));
				
				// PlayerChangedBomb
				// patch: if AugmentedActivatorComponent exists, add its state guard
				// This patch is a bit dirty, but this way you do not need a transpiler
				original = needyComponentType.GetMethod("PlayerChangedBomb", AllMethodFlags);
				prefix = typeof(Patcher).GetMethod(nameof(PlayerChangedBombPrefix), AllMethodFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix) { priority = Priority.VeryLow });
				
				// StartRunning
				// patch: wrap ResetAndStart in ResetAndStartGuardInfix
				// The infix checks if AugmentedActivatorComponent exists and, if it does,
				// notifies it that a needy wants to activate (through a guard call) 
				original = needyComponentType.GetMethod("StartRunning", AllMethodFlags);
				transpiler = typeof(Patcher).GetMethod(nameof(GuardResetAndStartTranspiler), AllMethodFlags);
				harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
				
				
				// TurnOff
				// This method is called by this mod and when the bomb is solved or explodes
				// patch: prevent emitting the deactivation event twice for modded modules
				original = needyComponentType.GetMethod("TurnOff", AllMethodFlags);
				prefix = typeof(Patcher).GetMethod(nameof(TurnOffPrefix), AllMethodFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix));

				isPatched = true;
			}

			#region /--- Patches ---/

			static bool OnBombTimerTickPrefix(Component __instance) {

				AugmentedActivatorComponent activator;
				if (AugmentedActivatorComponent.TryGetCached(__instance.GetInstanceID(), out activator)) {
					// Let the activator start the module
					return false;
				}

				return true;
			}
			
			static bool TurnOffPrefix(Component __instance) {
				int currentState = __instance.GetValue<int>("State");
				if (currentState == (int)NeedyComponentProxy.NeedyState.BombComplete) return false; // skip double trigger
				return true;
			}
			
			static bool PlayerChangedBombPrefix(Component __instance, ref IEnumerator __result) {

				AugmentedActivatorComponent activator;
				if (!AugmentedActivatorComponent.TryGetCached(__instance.GetInstanceID(), out activator)) {
					// No component
					return true;
				}

				bool isActivating;
				activator.GuardInterruption(out isActivating);

				if (!isActivating) {
					// Cannot interrupt
					__result = Enumerable.Empty<object>().GetEnumerator();
					return false;
				}
				
				// Interrupting...
				return true;
			}
			
			static IEnumerable<CodeInstruction> GuardResetAndStartTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator) {
				
				bool patched = false;
				
				MethodInfo resetAndStartMethod = needyComponentType.GetMethod("ResetAndStart", AllMethodFlags);
				
				List<CodeInstruction> instructionList = instructions.ToList();
				for (int i = 0; i < instructionList.Count; i++) {
					
					// Unless `this.ResetAndStart()` ...
					if (  
					    !(
						    instructionList[i].IsLdarg(0) &&
							i + 1 < instructionList.Count &&
							instructionList[i + 1].Is(OpCodes.Callvirt, resetAndStartMethod)
					    )
					) {
						// ... leave as is
						yield return instructionList[i];
						continue;
					}

					// Wrap `this.ResetAndStart()` in a ResetAndStartGuardInfix check and notifier
					//
					// ```
					// .maxstack 2
					//	.locals init (
					//	[0] bool V_0
					// )
					//
					// IL_0000: nop
					// IL_0001: ldarg.0      // this
					// IL_0002: ldloca.s     V_0
					// IL_0004: call         void NeedyAugmentationMod.IntegrationHelper/Patcher::Infix(object, bool&)
					// IL_0009: ldloc.0      // V_0
					// IL_000a: brfalse      IL_0017
					// IL_000f: nop
					// IL_0010: ldarg.0      // this
					// IL_0011: call         instance void NeedyComponent::ResetAndStart()
					// IL_0016: nop
					// IL_0017: nop
					// ``

					// bool isActivating;
					var isActivatingLocal = ilGenerator.DeclareLocal(typeof(bool));
						
					// ResetAndStartGuardInfix(this, ref isActivating);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldloca_S, isActivatingLocal.LocalIndex);
					yield return CodeInstruction.Call(typeof(Patcher), nameof(ResetAndStartGuardInfix));
						
					// if (!isActivatingLocal)
					var skipCallLabel = ilGenerator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Brfalse, skipCallLabel);
					
					// {
					// this.ResetAndStart();
					yield return instructionList[i];
					yield return instructionList[i + 1];
						
					// }
					yield return new CodeInstruction(OpCodes.Nop).WithLabels(skipCallLabel);

					patched = true;

					i++; // consume ldarg.0
					continue; // consume call and continue
				}

				if (!patched) throw new ArgumentException("Could not find the original method call to wrap.");
			}
			
			static void ResetAndStartGuardInfix(Component __instance, out bool isActivating) {
				
				AugmentedActivatorComponent activator;
				if (!AugmentedActivatorComponent.TryGetCached(__instance.GetInstanceID(), out activator)) {
					// No component
					isActivating = true;
					return;
				}

				if (activator.IsInStoppedState()) {
					isActivating = false;
					return;
				}
				
				activator.GuardActivationBySolves(out isActivating);
			}

			#endregion
			
		}
	}

}