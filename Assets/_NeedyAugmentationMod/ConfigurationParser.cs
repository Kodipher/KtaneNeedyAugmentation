using System;
using System.Reflection;

using JetBrains.Annotations;

using UnityEngine;


namespace NeedyAugmentationMod {

	public class ConfigurationParser {

		static readonly Type sceneManagerType = ReflectionHelper.FindGameType("SceneManager");
		static readonly Type localizationType = ReflectionHelper.FindGameType("Localization");
		
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
		
	}

}