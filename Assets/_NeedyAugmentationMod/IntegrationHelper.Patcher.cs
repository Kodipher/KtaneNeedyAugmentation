using UnityEngine;
using System.Collections;
using System.Linq;

using System.Reflection;
using HarmonyLib;

using System.Diagnostics.CodeAnalysis;


namespace NeedyAugmentationMod {

	public static partial class IntegrationHelper {
		
		[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Haromy Injections")]
		public static class Patcher {

			public const string HarmonyId = "kodipher.NeedyAugmentation";
			
			static readonly Harmony harmony = new Harmony(HarmonyId);
			
			/// <remarks>May throw.</remarks>
			public static void EnsurePatched() {
				if (Harmony.HasAnyPatches(HarmonyId)) return;
				Patch();
			}
			
			/*
			 * Original flow:
			 * 
			 * OnBombTimerTick -> [hasStarted guard] -> [elapsed guard] -> StartRunning;
			 * ComponentStrikeEvent,ComponentPassEvent -> [state guard] -> PlayerChangedBomb
			 * PlayerChangedBomb -> [coroutine] -> [state guard] -> [rng/dupe guard] -> StartRunning;
			 * StartRunning -> [hasStarted guard] -> ResetAndStart;
			 * WaitAndResetRoutine -> [coroutine] -> ChangeState(Cooldown) -> ResetAndStart
			 * ResetAndStart -> ChangeState(Running)
			 * OnSolved,OnTimerExpired -> [shouldReset check] -> [if true] WaitAndResetRoutine / [if false] ChangeState(Terminated)
			 * TurnOff -> ChangeState(BombComplete)
			 *
			 */
			
			public const BindingFlags AllMethodFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			
			/// <remarks>May throw.</remarks>
			static void Patch() {

				MethodInfo original;
				MethodInfo prefix;
				
				// -----
				
				// OnBombTimerTick
				// patch: if AugmentedActivatorComponent exists, let it do the waiting instead
				original = needyComponentType.GetMethod("OnBombTimerTick", AllMethodFlags);
				prefix = typeof(Patcher).GetMethod(nameof(OnBombTimerTickPrefix), AllMethodFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix));
				
				// PlayerChangedBomb
				// patch: if AugmentedActivatorComponent exists, add its state guard
				// This patch is a bit dirty, but this way the patch is only called for correct instances
				// without the use of a transpiler
				original = needyComponentType.GetMethod("PlayerChangedBomb", AllMethodFlags);
				prefix = typeof(Patcher).GetMethod(nameof(PlayerChangedBombPrefix), AllMethodFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix) { priority = Priority.VeryLow });
				
				
				// TurnOff
				// This method is called by this mod and when the bomb is solved or explodes
				// patch: prevent emitting the deactivation event twice for modded modules
				original = needyComponentType.GetMethod("TurnOff", AllMethodFlags);
				prefix = typeof(Patcher).GetMethod(nameof(TurnOffPrefix), AllMethodFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix));
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

			#endregion
			
		}
	}

}