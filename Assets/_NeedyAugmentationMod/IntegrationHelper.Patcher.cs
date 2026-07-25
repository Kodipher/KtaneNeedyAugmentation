using UnityEngine;
using System;
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
			
			
			public const BindingFlags AllMethodFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			
			/// <remarks>May throw.</remarks>
			static void Patch() {

				MethodInfo original;
				MethodInfo prefix;
				
				// -----
				
				// Turn off
				// This method is called by this mod and when the bomb is solved or explodes
				// patch: prevent emitting the deactivation event twice
				original = needyComponentType.GetMethod("TurnOff", AllMethodFlags);
				prefix = typeof(Patcher).GetMethod(nameof(TurnOffPrefix), AllMethodFlags);
				harmony.Patch(original, prefix: new HarmonyMethod(prefix));
			}

			#region /--- Patches ---/
			
			static bool TurnOffPrefix(Component __instance) {
				int currentState = __instance.GetValue<int>("State");
				if (currentState == (int)NeedyComponentProxy.NeedyState.BombComplete) return false; // skip double trigger
				return true;
			}

			#endregion
			
		}
	}

}