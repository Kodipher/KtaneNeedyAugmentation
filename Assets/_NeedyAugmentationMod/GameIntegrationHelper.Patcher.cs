using System;
using HarmonyLib;


namespace NeedyAugmentationMod {

	public static partial class GameIntegrationHelper {
		public static class Patcher {

			public const string HarmonyId = "kodipher.NeedyAugmentation";
			
			static Harmony harmony = new Harmony(HarmonyId);
			
			/// <remarks>May throw.</remarks>
			public static void EnsurePatched() {
				if (Harmony.HasAnyPatches(HarmonyId)) return;
				Patch();
			}
			
			/// <remarks>May throw.</remarks>
			static void Patch() {
				throw new NotImplementedException();
			}
			
		}
	}

}