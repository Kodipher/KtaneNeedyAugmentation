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
		
	}
	
}