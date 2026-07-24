using UnityEngine;
using System;
using Rephidock.GeneralUtilities.Reflection;

using JetBrains.Annotations;


namespace NeedyAugmentationMod {

	public static partial class IntegrationHelper {
		
		public class NeedyComponentProxy {
			
			public MonoBehaviour NeedyComponent { get; }
			[CanBeNull] public KMNeedyModule KmNeedy { get; }
			public string ModuleId { get; }

			#region /--- Creation ---/
			
			public NeedyComponentProxy(MonoBehaviour needyComponent, KMNeedyModule kmNeedy) {
				ModuleId = kmNeedy.ModuleType;
				KmNeedy = kmNeedy;
				NeedyComponent = needyComponent;
			}
			
			public NeedyComponentProxy(MonoBehaviour needyComponent, string id) {
				ModuleId = id;
				KmNeedy = null;
				NeedyComponent = needyComponent;
			}

			public static NeedyComponentProxy CreateFromNeedyComponent(MonoBehaviour needyComponent) {

				// Mod needies
				var maybeKmNeedy = needyComponent.GetComponent<KMNeedyModule>();

				if (maybeKmNeedy != null) {
					return new NeedyComponentProxy(needyComponent, maybeKmNeedy);
				}
				
				// Vanilla needies
				var type = needyComponent.GetType();

				if (type.IsSubclassOrSelfOf(needyCapacitorType)) {
					return new NeedyComponentProxy(needyComponent, "NeedyCapacitor");
				}
				
				if (type.IsSubclassOrSelfOf(needyVentType)) {
					return new NeedyComponentProxy(needyComponent, "NeedyVentGas");
				}
				
				if (type.IsSubclassOrSelfOf(needyKnobType)) {
					return new NeedyComponentProxy(needyComponent, "NeedyKnob");
				}

				throw new ArgumentException("Given component is not a needy.", nameof(needyComponent));
			}
			
			#endregion
			
			[CanBeNull] Action resetAndStartMethod = null;
			[CanBeNull] Action startRunningMethod = null;
			
			public void ResetAndStart() {

				if (resetAndStartMethod == null) {
					resetAndStartMethod = (Action)Delegate.CreateDelegate(typeof(Action), NeedyComponent, "ResetAndStart");
				}

				resetAndStartMethod();
			}
		
			public void StartRunning() {
				
				if (startRunningMethod == null) {
					startRunningMethod = (Action)Delegate.CreateDelegate(typeof(Action), NeedyComponent, "StartRunning");
				}
				
				startRunningMethod();
			}
			
		}
	}

}