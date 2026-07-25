using UnityEngine;
using System;

using JetBrains.Annotations;


namespace NeedyAugmentationMod {

	public static partial class IntegrationHelper {
		
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
			
		}
	}

}