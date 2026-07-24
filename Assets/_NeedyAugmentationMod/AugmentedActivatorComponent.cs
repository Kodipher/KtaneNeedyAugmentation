using UnityEngine;
using System;
using System.Collections.Generic;

using JetBrains.Annotations;


namespace NeedyAugmentationMod {

	public class AugmentedActivatorComponent : MonoBehaviour {

		#region /--- Global Instance Cache ---/

		static readonly object cacheDictLock = new object();

		static readonly Dictionary<int, WeakReference> instanceCache = new Dictionary<int, WeakReference>();

		public static void ClearDeadCacheReferences() {
			lock (cacheDictLock) {

				List<int> keysToRemove = new List<int>(); // cannot mutate during iteration
				
				foreach (var pair in instanceCache) {
					
					var maybeComponent = pair.Value.Target as AugmentedActivatorComponent;
					if (maybeComponent == null) {
						keysToRemove.Add(pair.Key);
					}
				}

				foreach (var key in keysToRemove) {
					instanceCache.Remove(key);
				}
				
			}
		}
		
		[ContractAnnotation("=> true, component: notnull; => false, component: null")]
		public static bool TryGetCached(int targetObjectId, out AugmentedActivatorComponent component) {
			lock (cacheDictLock) {
				
				WeakReference weakRef;
				if (instanceCache.TryGetValue(targetObjectId, out weakRef)) {
					
					var maybeComponent = weakRef.Target as AugmentedActivatorComponent;
					if (maybeComponent == null) {
						// Garbage collected
						instanceCache.Remove(targetObjectId);
						component = null;
						return false;
					}

					// Exists
					component = maybeComponent;
					return true;
				}
				
				// does not exist
				component = null;
				return false;
			}
		}

		public static void AddExistingToCache(int targetObjectId, AugmentedActivatorComponent component) {
			lock (cacheDictLock) {
				instanceCache[targetObjectId] = new WeakReference(component);
			}
		}
		
		public static AugmentedActivatorComponent CreateForAndCache(GameObject gameObject, AugmentationPropertySet augmentation) {
			lock (cacheDictLock) {
				
				var component = gameObject.AddComponent<AugmentedActivatorComponent>();
				component.Settings = augmentation;
				
				int id = gameObject.GetInstanceID();
				instanceCache[id] = new WeakReference(component);
				
				return component;
			}
		}
		
		#endregion
		
		public AugmentationPropertySet Settings { get; private set; }

		public int TimesActivated { get; private set; } = 0;
		
		int? GetActivationLimitForSolves(int solves) {

			int? settingBase = Settings.ActivationLimit;
			int? settingPer = Settings.ActivationLimitAddendPerSolves;
			int? settingSolves = Settings.ActivationLimitSolves;
			
			if (!settingBase.HasValue) return null; // inf
			if (!settingPer.HasValue) return settingBase; // base
			if (!settingSolves.HasValue) return settingBase.Value + solves * settingPer.Value; // base + perSolve

			if (settingSolves.Value == 0) {
				if (settingPer.Value == 0) return settingBase; // base+0 (assumes 0/0 is 0)
				if (settingPer.Value < 0) return 0; // base-inf (assumes -x/0 is -inf)
				return null; // base+inf (assumes x/0 is inf)
			}
			
			return settingBase.Value + (solves / settingSolves.Value) * settingPer.Value; // base + per/solves
		}

		/// <returns>seconds</returns>
		float ModifyCooldown(float cooldownSeconds) {
			float result = cooldownSeconds;
			if (Settings.CooldownMultiplier.HasValue) result *= Settings.CooldownMultiplier.Value;
			if (Settings.CooldownAddend.HasValue) result += Settings.CooldownAddend.Value;
			if (result <= 0) return 0;
			return result;
		}

	}
	
}