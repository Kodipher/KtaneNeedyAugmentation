using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using Rephidock.AtomicAnimations;
using Rephidock.AtomicAnimations.Coroutines;

using SharedAssets.Utils;

using TimeSpan = System.TimeSpan;


namespace NeedyAugmentationMod {

	[RequireComponent(typeof(KMBombModule))]
	[RequireComponent(typeof(KMBombInfo))]
	[RequireComponent(typeof(KMAudio))]
	public class NeedyAugmentationModule : MonoBehaviour {

		#region /--- State ---/

		private enum State {
			Initiating,
			Solved
		}

		State state = State.Initiating;

		// Expose log id for LFA at module instance (required by Tweaks)
		public int LogFileAnalyzerId => this.logger.tagId ?? 0;

		#endregion

		#region /--- Components and parts ---/

		// KM
		internal KMBombModule kmModule;
		internal KMBombInfo kmBomb;
		internal KMAudio kmAudio;
		
		// Module parts
		internal Transform acknowledgeButton;
		internal KMSelectable acknowledgeButtonSelectable;
		internal TextMesh displayText;
		internal Light[] letterLights;
		
		// Created 
		internal System.Random rng;
		internal ModuleLogger logger;
		internal AnimationRunner animationRunner;

		private void PrepareComponents() {

			// KM
			kmModule = GetComponent<KMBombModule>();
			kmBomb = GetComponent<KMBombInfo>();
			kmAudio = GetComponent<KMAudio>();

			kmModule.OnActivate += OnActivate;
			
			// Button
			acknowledgeButton = transform.Find("objectScaler/button");
			
			acknowledgeButtonSelectable = acknowledgeButton.GetComponent<KMSelectable>();
			acknowledgeButtonSelectable.OnInteract += () => { OnButtonHold(); return false; };
			acknowledgeButtonSelectable.OnInteractEnded += OnButtonReleased;
			
			// Display
			displayText = transform.Find("objectScaler/display/text").GetComponent<TextMesh>();

			var lightsTransform = transform.Find("objectScaler/display/lights");
			letterLights = Enumerable
							.Range(0, lightsTransform.childCount)
							.Select(i => lightsTransform.GetChild(i).GetComponent<Light>())
							.ToArray();

			foreach (var letterLight in letterLights) {
				letterLight.range *= transform.lossyScale.x;
			}
			
			// Misc. common
			logger = new ModuleLogger(kmModule);
			rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
			animationRunner = new AnimationRunner();
		}

		#endregion

		#region /--- Events ---/

		void Start() {
			PrepareComponents();
		}

		// ReSharper disable Unity.PerformanceAnalysis
		void OnActivate() {

		}

		void Update() {
			animationRunner?.Update(TimeSpan.FromSeconds(Time.deltaTime));
		}

		void OnDestroy() {
			animationRunner?.Dispose();
		}

		// ReSharper disable Unity.PerformanceAnalysis
		void OnButtonHold() {
			logger.LogString("Holding...");
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		void OnButtonReleased() {
			logger.LogString("Released.");
		}
		
		public void TwitchHandleForcedSolve() {
			logger.LogString("Forced solve command received. Ending routines.");
			animationRunner.Clear();
			
			state = State.Solved;
			kmModule.HandlePass();
			logger.LogString("Module solved.");
		}

		#endregion

		#region /--- Routines ---/

		IEnumerable<CoroutineYield> TemplateRoutine() {
			yield break;
		}

		#endregion

	}

}
