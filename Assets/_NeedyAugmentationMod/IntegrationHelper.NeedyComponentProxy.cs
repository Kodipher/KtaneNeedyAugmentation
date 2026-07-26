using UnityEngine;
using System;
using System.Reflection;
using System.Linq.Expressions;
using Rephidock.GeneralUtilities.Reflection;

using JetBrains.Annotations;


namespace NeedyAugmentationMod {

	public static partial class IntegrationHelper {
		
		/// <summary>Holds some information and proxy methods for a NeedyComponent.</summary>
		/// <remarks>Additionally, caches some NeedyComponent.Bomb methods.</remarks>
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
			[CanBeNull] Action<int> changeStateMethod = null;
			[CanBeNull] Action<bool> turnOffMethod = null;
			[CanBeNull] Func<int> secondsBeforeForcedActivationFieldGetter = null;
			[CanBeNull] Func<float> resetDelayMinFieldGetter = null;
			[CanBeNull] Func<float> resetDelayMaxFieldGetter = null;
			[CanBeNull] Func<int> bombGetSolvableComponentCountMethod = null;
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

			/// <remarks>Might have unintended side effects, like invoking km needy events.</remarks>
			public void ChangeState(NeedyState newState) {
				if (changeStateMethod == null) {

					MethodInfo methodInfo = needyComponentType.GetMethod("ChangeState", ReflectionHelper.AllFlags);
					if (methodInfo == null) throw new NullReferenceException("Could not find the ChangeState method.");
					
					var instanceExp = Expression.Constant(NeedyComponent);
					var parameterExp = Expression.Parameter(typeof(int), "newState");
					var castExp = Expression.Convert(parameterExp, needyStateEnumType);
					var callExp = Expression.Call(instanceExp, methodInfo, castExp);
					changeStateMethod = Expression.Lambda<Action<int>>(callExp, parameterExp).Compile();
					//changeStateMethod = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), NeedyComponent, "ChangeState");
				}

				changeStateMethod((int)newState);
			}

			/// <remarks>Used seldom. Implemented via Reflection.</remarks>
			public NeedyState GetState() {
				return (NeedyState)NeedyComponent.GetValue<int>("State");
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
			
			public float GetResetDelayMin() {

				if (resetDelayMinFieldGetter == null) {
					FieldInfo fieldInfo = NeedyComponent.GetType().GetField("ResetDelayMin");
					var instanceExp = Expression.Constant(NeedyComponent);
					var fieldGetExp = Expression.Field(instanceExp, fieldInfo);
					resetDelayMinFieldGetter = Expression.Lambda<Func<float>>(fieldGetExp).Compile();
				}
				
				return resetDelayMinFieldGetter();
			}
			
			public float GetResetDelayMax() {

				if (resetDelayMaxFieldGetter == null) {
					FieldInfo fieldInfo = NeedyComponent.GetType().GetField("ResetDelayMax");
					var instanceExp = Expression.Constant(NeedyComponent);
					var fieldGetExp = Expression.Field(instanceExp, fieldInfo);
					resetDelayMaxFieldGetter = Expression.Lambda<Func<float>>(fieldGetExp).Compile();
				}
				
				return resetDelayMaxFieldGetter();
			}
			
			public int BombCountSolvableComponents() {

				if (bombGetSolvableComponentCountMethod == null) {
					bombGetSolvableComponentCountMethod = () => 0; // prevent throwing exceptions every frame
					
					var bomb = bombComponentType.GetValue<object>("Bomb", NeedyComponent);
					bombGetSolvableComponentCountMethod = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), bomb, "GetSolvableComponentCount");
				}

				return bombGetSolvableComponentCountMethod();
			}

			public int BombCountSolvedComponents() {

				if (bombGetSolvedComponentCountMethod == null) {
					bombGetSolvedComponentCountMethod = () => 0; // prevent throwing exceptions every frame
					
					var bomb = bombComponentType.GetValue<object>("Bomb", NeedyComponent);
					bombGetSolvedComponentCountMethod = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), bomb, "GetSolvedComponentCount");
				}

				return bombGetSolvedComponentCountMethod();
			}
			
			/// <remarks>Used seldom. Implemented via Reflection.</remarks>
			public void SetHasStarted(bool value) {
				NeedyComponent.SetValue("hasStarted", value);
			}
			
		}
	}

}