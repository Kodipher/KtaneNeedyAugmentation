using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Rephidock.GeneralUtilities.Collections;
using Rephidock.GeneralUtilities.Reflection;

using UnityEngine;


namespace NeedyAugmentationMod {

	public static partial class IntegrationHelper {
		
		//
		// I did not want to import the game's assembly, 
		// because of potential issues with Rewritten later.
		//
		// Rewritten compatibility will have to be added either way,
		// but at least I am not relying on the original game's dll being loaded.
		//

		static readonly Type sceneManagerType = ReflectionHelper.FindGameType("SceneManager");
		static readonly Type localizationType = ReflectionHelper.FindGameType("Localization");
		
		static readonly Type bombComponentType = ReflectionHelper.FindGameType("BombComponent");
		static readonly Type needyComponentType = ReflectionHelper.FindGameType("NeedyComponent");
		static readonly Type needyCapacitorType = ReflectionHelper.FindGameType("NeedyDischargeComponent");
		static readonly Type needyKnobType = ReflectionHelper.FindGameType("NeedyKnobComponent");
		static readonly Type needyVentType = ReflectionHelper.FindGameType("NeedyVentComponent");
		
		static readonly Lazy<IDictionary<string, object>> moddedApi = 
								new Lazy<IDictionary<string, object>>(
									() => GameObject.Find("ModdedAPI_Info").GetComponent<IDictionary<string, object>>()
								);
		
		public static string GetCurrentMissionDescription() {

			if (Application.isEditor) throw new NotSupportedException();

			// ReflectionHelper caches MemberInfo queries
			object sceneManager = sceneManagerType.GetValue<object>("Instance", null);
			object gameplayState = sceneManager.GetType().GetValue<object>("GameplayState", sceneManager);
			object mission = gameplayState.GetType().GetValue<object>("Mission", gameplayState);
			string descriptionTerm = mission.GetType().GetValue<string>("DescriptionTerm", mission);

			const bool defaultFixForRtl = true;
			string description = localizationType.CallMethod<string>("GetLocalizedString", null, descriptionTerm, defaultFixForRtl);

			return description ?? "";
		}

		public static NeedyComponentProxy[] GetAllNeedyComponentsOnTheSameBomb(GameObject bombComponentObject) {
			
			if (Application.isEditor) throw new NotSupportedException();
			
			var bombComponentObjectComponent = bombComponentObject.GetComponent(bombComponentType);
			var bomb = bombComponentType.GetValue<object>("Bomb", bombComponentObjectComponent);
			var componentList = bomb.GetType().GetValue<IList>("BombComponents", bomb);

			return componentList
						.Cast<MonoBehaviour>()
						.Where(obj => obj.GetType().IsSubclassOrSelfOf(needyComponentType))
						.Select(NeedyComponentProxy.CreateFromNeedyComponent)
						.ToArray();
		}

		// Has to be done through the api object,
		// because the check is not done in a module component
		public static bool IsZenMode() {
			if (moddedApi.Value == null) return false;

			object isZenMode;
			if (moddedApi.Value.TryGetValue("ZenMode", out isZenMode)) {
				if (isZenMode is bool) return (bool)isZenMode;
				return false;
			}

			return false;
		}
		
		public static bool IsTimeMode() {
			if (moddedApi.Value == null) return false;

			object isTimeMode;
			if (moddedApi.Value.TryGetValue("TimeMode", out isTimeMode)) {
				if (isTimeMode is bool) return (bool)isTimeMode;
				return false;
			}

			return false;
		}
		
	}

}