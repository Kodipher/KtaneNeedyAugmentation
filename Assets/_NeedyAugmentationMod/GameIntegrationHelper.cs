using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using Rephidock.GeneralUtilities.Reflection;

using JetBrains.Annotations;

using UnityEngine;


namespace NeedyAugmentationMod {

	public static class GameIntegrationHelper {

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

		
		static readonly Type bombComponentType = ReflectionHelper.FindGameType("BombComponent");
		static readonly Type needyComponentType = ReflectionHelper.FindGameType("NeedyComponent");
		static readonly Type needyCapacitorType = ReflectionHelper.FindGameType("NeedyDischargeComponent");
		static readonly Type needyKnobType = ReflectionHelper.FindGameType("NeedyKnobComponent");
		static readonly Type needyVentType = ReflectionHelper.FindGameType("NeedyVentComponent");
		
		public /*record*/ class NeedyComponentInfo {
			
			public MonoBehaviour NeedyComponent { get; }
			[CanBeNull] public KMNeedyModule KmNeedy { get; }
			public string ModuleId { get; }

			public NeedyComponentInfo(MonoBehaviour needyComponent, KMNeedyModule kmNeedy) {
				ModuleId = kmNeedy.ModuleType;
				KmNeedy = kmNeedy;
				NeedyComponent = needyComponent;
			}
			
			public NeedyComponentInfo(MonoBehaviour needyComponent, string id) {
				ModuleId = id;
				KmNeedy = null;
				NeedyComponent = needyComponent;
			}

			public static NeedyComponentInfo CreateFromNeedyComponent(MonoBehaviour needyComponent) {

				// Mod needies
				var maybeKmNeedy = needyComponent.GetComponent<KMNeedyModule>();

				if (maybeKmNeedy != null) {
					return new NeedyComponentInfo(needyComponent, maybeKmNeedy);
				}
				
				// Vanilla needies
				var type = needyComponent.GetType();

				if (type.IsSubclassOrSelfOf(needyCapacitorType)) {
					return new NeedyComponentInfo(needyComponent, "NeedyCapacitor");
				}
				
				if (type.IsSubclassOrSelfOf(needyVentType)) {
					return new NeedyComponentInfo(needyComponent, "NeedyVentGas");
				}
				
				if (type.IsSubclassOrSelfOf(needyKnobType)) {
					return new NeedyComponentInfo(needyComponent, "NeedyKnob");
				}

				throw new ArgumentException("Given component is not a needy.", nameof(needyComponent));
			}
			
		}
		
		/// <returns>IList&lt;NeedyComponent&gt;</returns>
		public static NeedyComponentInfo[] GetAllNeedyComponentsOnTheSameBomb(GameObject bombComponentObject) {
			
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