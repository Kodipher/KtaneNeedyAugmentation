using System;

using JetBrains.Annotations;

using Rephidock.GeneralUtilities.Reflection;

using UnityEngine;


namespace NeedyAugmentationMod {

	public static partial class GameIntegrationHelper {
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
	}

}