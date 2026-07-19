using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using Rephidock.GeneralUtilities.Reflection;

using UnityEngine;


namespace NeedyAugmentationMod {

	public static partial class GameIntegrationHelper {

		static readonly Type sceneManagerType = ReflectionHelper.FindGameType("SceneManager");
		static readonly Type localizationType = ReflectionHelper.FindGameType("Localization");
		
		static readonly Type bombComponentType = ReflectionHelper.FindGameType("BombComponent");
		static readonly Type needyComponentType = ReflectionHelper.FindGameType("NeedyComponent");
		static readonly Type needyCapacitorType = ReflectionHelper.FindGameType("NeedyDischargeComponent");
		static readonly Type needyKnobType = ReflectionHelper.FindGameType("NeedyKnobComponent");
		static readonly Type needyVentType = ReflectionHelper.FindGameType("NeedyVentComponent");
		
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
		
		public static NeedyComponentInfo[] GetAllNeedyComponentsOnTheSameBomb(GameObject bombComponentObject) {
			
			if (Application.isEditor) throw new NotSupportedException();
			
			var bombComponentObjectComponent = bombComponentObject.GetComponent(bombComponentType);
			var bomb = bombComponentType.GetValue<object>("Bomb", bombComponentObjectComponent);
			var componentList = bomb.GetType().GetValue<IList>("BombComponents", bomb);

			return componentList
						.Cast<MonoBehaviour>()
						.Where(obj => obj.GetType().IsSubclassOrSelfOf(needyComponentType))
						.Select(NeedyComponentInfo.CreateFromNeedyComponent)
						.ToArray();
		}
		
	}

}