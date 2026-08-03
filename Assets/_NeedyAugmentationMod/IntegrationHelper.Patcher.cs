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
			 * WaitAndResetRoutine -> !!! -> [coroutine] -> ChangeState(Cooldown) -> ResetAndStart()
			 * ResetAndStart -> ChangeState(Running)
			 * OnSolved,OnTimerExpired -> [shouldReset check] -> [if true] WaitAndResetRoutine() / [if false] ChangeState(Terminated)
			 * TurnOff -> !!! -> ChangeState(BombComplete)
			 *
			 */
			
			/// <remarks>May throw.</remarks>
			static void Patch() {

				MethodInfo original;
				MethodInfo prefix;
				MethodInfo transpiler;
				
				// -----
				
				// OnBombTimerTick
				// patch: if AugmentedActivatorComponent exists, let it do the waiting instead
				original = needyComponentType.GetMethod("OnBombTimerTick", ReflectionHelper.AllFlags);
				prefix = typeof(Patcher).GetMethod(nameof(OnBombTimerTickPrefix), ReflectionHelper.AllFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix));
				
				// PlayerChangedBomb
				// patch: if AugmentedActivatorComponent exists, add its state guard
				// This patch is a bit dirty, but this way you do not need a transpiler
				original = needyComponentType.GetMethod("PlayerChangedBomb", ReflectionHelper.AllFlags);
				prefix = typeof(Patcher).GetMethod(nameof(PlayerChangedBombPrefix), ReflectionHelper.AllFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix));
				
				// StartRunning
				// patch: wrap ResetAndStart in ResetAndStartGuardInfix
				// The infix checks if AugmentedActivatorComponent exists and, if it does,
				// notifies it that a needy wants to activate (through a guard call) 
				original = needyComponentType.GetMethod("StartRunning", ReflectionHelper.AllFlags);
				transpiler = typeof(Patcher).GetMethod(nameof(StartRunningResetAndStartGuardTranspiler), ReflectionHelper.AllFlags);
				harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
				
				// WaitAndResetRoutine
				// patch: if AugmentedActivatorComponent exists, replace iterator
				// with one that has an activation guard and cooldown modification
				original = needyComponentType.GetMethod("WaitAndResetRoutine", ReflectionHelper.AllFlags);
				prefix = typeof(Patcher).GetMethod(nameof(WaitAndResetRoutinePrefix), ReflectionHelper.AllFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix));
				
				// TurnOff
				// This method is called by this mod and when the bomb is solved or explodes
				// patch: prevent emitting the deactivation event twice for modded modules
				original = needyComponentType.GetMethod("TurnOff", ReflectionHelper.AllFlags);
				prefix = typeof(Patcher).GetMethod(nameof(TurnOffPrefix), ReflectionHelper.AllFlags);
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

				if (!activator.IsInInterruptableWaiting()) {
					// Cannot interrupt
					__result = Enumerable.Empty<object>().GetEnumerator();
					return false;
				}
				
				// Interrupting...
				return true;
			}

			static IEnumerable<CodeInstruction> StartRunningResetAndStartGuardTranspiler(
				IEnumerable<CodeInstruction> instructions,
				ILGenerator ilGenerator
			) {
				MethodInfo resetAndStartMethod = needyComponentType.GetMethod("ResetAndStart", ReflectionHelper.AllFlags);
				
				// Wrap `this.ResetAndStart()` in a ResetAndStartGuardInfix check and notifier
				//
				// ```
				//	.locals init (
				//	[0] bool V_0
				// )
				//
				// IL_0000: nop
				// IL_0001: ldarg.0      // this
				// IL_0002: ldloca.s     V_0
				// IL_0004: call         void NeedyAugmentationMod.IntegrationHelper/Patcher::Infix(object, bool&)
				// IL_0009: ldloc.0      // V_0
				// IL_000a: brfalse      IL_0016
				// IL_000f: nop
				//
				// IL_0010: ldarg.0      // this
				// IL_0011: call         instance void NeedyComponent::ResetAndStart()
				//
				// IL_0016: nop
				// ```
				
				CodeMatcher editor = new CodeMatcher(instructions, ilGenerator)
										.MatchStartForward(
											new CodeMatch(OpCodes.Ldarg_0),
											new CodeMatch(OpCodes.Callvirt, resetAndStartMethod)
										)
										.ThrowIfInvalid("Could not find the original method call to wrap");
				
				var isActivatingLocal = ilGenerator.DeclareLocal(typeof(bool)); // bool isActivating;
				var skipCallLabel = ilGenerator.DefineLabel(); // (label skipCall)
				
				editor.InsertAndAdvance(
					// ResetAndStartGuardInfix(this, &isActivating);
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldloca_S, isActivatingLocal.LocalIndex),
					CodeInstruction.Call(typeof(Patcher), nameof(ResetAndStartGuardInfix)),
					
					// if (isActivating) {
					new CodeInstruction(OpCodes.Ldloc_0),
					new CodeInstruction(OpCodes.Brfalse, skipCallLabel)
				);
				
				editor.Advance(offset: 2); // this.ResetAndStart();
				
				// }
				editor.InsertAndAdvance(
					new CodeInstruction(OpCodes.Nop).WithLabels(skipCallLabel)
				);

				return editor.Instructions();
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
			
			static bool WaitAndResetRoutinePrefix(Component __instance, ref IEnumerator __result) {

				AugmentedActivatorComponent activator;
				if (!AugmentedActivatorComponent.TryGetCached(__instance.GetInstanceID(), out activator)) {
					// No component -- continue like normal
					return true;
				}

				// Replace coroutine with the patched one
				__result = WaitAndResetReplacementCoroutine(__instance, activator);
				return false;
			}

			static IEnumerator WaitAndResetReplacementCoroutine(Component __instance, AugmentedActivatorComponent activator) {

				var proxy = activator.NeedyComponent;
				
				// Mimic existing code
				proxy.ChangeState(NeedyComponentProxy.NeedyState.Cooldown);
				float baseDelay = UnityEngine.Random.Range(proxy.GetResetDelayMin(), proxy.GetResetDelayMax());
				
				// Modify the cooldown
				float newDelay = activator.ModifyCooldown(baseDelay);
				yield return new WaitForSeconds(newDelay);
				
				// Activate but with a guard
				bool isActivating;
				ResetAndStartGuardInfix(__instance, out isActivating);
				
				if (isActivating) proxy.ResetAndStart();
			}
			
			#endregion
			
		}
	}

}