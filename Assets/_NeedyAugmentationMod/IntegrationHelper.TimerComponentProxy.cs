using UnityEngine;
using System;
using System.Reflection;
using System.Linq.Expressions;

using JetBrains.Annotations;


namespace NeedyAugmentationMod {

	public static partial class IntegrationHelper {
		
		/// <summary>Holds some proxy methods and getters for a TimerComponent.</summary>
		public class TimerComponentProxy {
			
			public MonoBehaviour TimerComponent { get; }

			private TimerComponentProxy(MonoBehaviour component) {
				TimerComponent = component;
			}
			
			public static TimerComponentProxy CreateFromComponentOnTheSameBomb(GameObject bombComponentObject) {
				var bombComponentObjectComponent = bombComponentObject.GetComponent(bombComponentType);
				var bomb = bombComponentType.GetValue<object>("Bomb", bombComponentObjectComponent);
				var timer = bomb.CallMethod<MonoBehaviour>("GetTimer");
				return new TimerComponentProxy(timer);
			}
			
			
			[CanBeNull] Func<float> getRateMethod = null;
			[CanBeNull] Func<bool> isUpdatingPropertyGetter = null;
			[CanBeNull] Func<float> timeRemainingFieldGetter = null;
			
			public float GetRate() {

				if (getRateMethod == null) {
					getRateMethod = () => 1f; // prevent throwing exceptions every frame
					getRateMethod = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), TimerComponent, "GetRate");
				}

				return getRateMethod();
			}
			
			public bool GetIsUpdating() {
				
				if (isUpdatingPropertyGetter == null) {
					isUpdatingPropertyGetter = () => false; // prevent throwing exceptions every frame
					
					var methodInfo = TimerComponent.GetType().GetProperty("IsUpdating")?.GetGetMethod();
					if (methodInfo == null) throw new NullReferenceException("Cannot find IsUpdating property getter.");
					
					isUpdatingPropertyGetter = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), TimerComponent, methodInfo);
				}

				return isUpdatingPropertyGetter();
			}

			public float GetTimeRemaining() {

				if (timeRemainingFieldGetter == null) {
					timeRemainingFieldGetter = () => 0f; // prevent throwing exceptions every frame

					FieldInfo fieldInfo = TimerComponent.GetType().GetField("TimeRemaining");
					
					var instanceExp = Expression.Constant(TimerComponent);
					var fieldGetExp = Expression.Field(instanceExp, fieldInfo);
					timeRemainingFieldGetter = Expression.Lambda<Func<float>>(fieldGetExp).Compile();
				}

				return timeRemainingFieldGetter();
			}
			
		}
	}

}