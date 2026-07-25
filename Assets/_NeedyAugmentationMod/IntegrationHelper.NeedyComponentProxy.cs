using UnityEngine;
using System;
using System.Reflection;
using System.Linq.Expressions;
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
			[CanBeNull] Action<bool> turnOffMethod = null;
			[CanBeNull] Func<int> secondsBeforeForcedActivationFieldGetter = null;
			[CanBeNull] Func<int> bombGetSolvedComponentCountMethod = null;
			
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

			public enum NeedyState {
				InitialSetup = 0,
				AwaitingActivation = 1,
				Running = 2,
				Cooldown = 3,
				Terminated = 4,
				BombComplete = 5
			}
			
			public void TurnOff(bool bombSolved = true /*Parameter is unused anyway*/) {
				if (turnOffMethod == null) {
					turnOffMethod = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), NeedyComponent, "TurnOff");
				}

				turnOffMethod(bombSolved);
			}

			public int GetSecondsBeforeForcedActivation() {

				if (secondsBeforeForcedActivationFieldGetter == null) {
					secondsBeforeForcedActivationFieldGetter = () => 90; // prevent throwing exceptions every frame
					
					FieldInfo fieldInfo = NeedyComponent.GetType().GetField("SecondsBeforeForcedActivation");
					
					var instanceExp = Expression.Constant(NeedyComponent);
					var fieldGetExp = Expression.Field(instanceExp, fieldInfo);
					secondsBeforeForcedActivationFieldGetter = Expression.Lambda<Func<int>>(fieldGetExp).Compile();
				}
				
				return secondsBeforeForcedActivationFieldGetter();
			}

			public int BombCountSolvedComponents() {

				if (bombGetSolvedComponentCountMethod == null) {
					bombGetSolvedComponentCountMethod = () => 0; // prevent throwing exceptions every frame
					
					var bomb = bombComponentType.GetValue<object>("Bomb", NeedyComponent);
					bombGetSolvedComponentCountMethod = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), bomb, "GetSolvedComponentCount");
				}

				return bombGetSolvedComponentCountMethod();
			}
			
		}
	}

}