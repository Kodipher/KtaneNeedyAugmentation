using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using Rephidock.GeneralUtilities.Collections;
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

		bool isButtonHeld = false;
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

		const int DisplayHeightCharacters = 9;
		internal VerticalDisplay verticalDisplay; // created
		
		// Misc. Created
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
			var displayText = transform.Find("objectScaler/display/text").GetComponent<TextMesh>();

			var lightsTransform = transform.Find("objectScaler/display/lights");
			var letterLights = Enumerable
							.Range(0, lightsTransform.childCount)
							.Select(i => lightsTransform.GetChild(i).GetComponent<Light>())
							.ToArray();

			foreach (var letterLight in letterLights) {
				letterLight.range *= transform.lossyScale.x;
			}

			verticalDisplay = new VerticalDisplay(DisplayHeightCharacters, displayText, letterLights, logger);
			
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

			if (isButtonHeld) return;
			isButtonHeld = true;
			
			// Cue
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, acknowledgeButton);
			acknowledgeButtonSelectable.AddInteractionPunch(0.5f);
			animationRunner.Run(CreateButtonPressMovement());
			
			// Logic
			logger.LogString("Holding...");
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		void OnButtonReleased() {
			
			if (!isButtonHeld) return;
			isButtonHeld = false;
			
			// Cue
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, acknowledgeButton);
			acknowledgeButtonSelectable.AddInteractionPunch(0.5f);
			animationRunner.Run(CreateButtonReleaseMovement());
			
			// Logic
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
		
		#region /--- Run Button Animation ---/
		
		const float ButtonHeldOffsetY = -0.05f;
		static readonly TimeSpan buttonPressAnimationDuration = TimeSpan.FromSeconds(0.075);

		Shift1D CreateButtonPressMovement() {
			return new Shift1D(
				ButtonHeldOffsetY,
				buttonPressAnimationDuration,
				Easing.Linear,
				(yy) => {
					var position = acknowledgeButton.localPosition;
					position.y += yy;
					acknowledgeButton.localPosition = position;
				}
			);
		}

		Shift1D CreateButtonReleaseMovement() {
			return new Shift1D(
				-ButtonHeldOffsetY,
				buttonPressAnimationDuration,
				Easing.Linear,
				(yy) => {
					var position = acknowledgeButton.localPosition;
					position.y += yy;
					acknowledgeButton.localPosition = position;
				}
			);
		}
		
		#endregion
		
		#region /--- Display Texts ---/

		const string AugmentedText = "AUGMETNED";
		const string UnchangedText = "UNCHANGED";
		
		static readonly Pair<Color, Color> introColors = Pair.New(
			new Color(0.8f, 0.8f, 0.8f, 0.8f), 
			new Color(0.9f, 0.9f, 0.9f)
		);
		
		static readonly Pair<Color, Color> augmentedColors = Pair.New(
			new Color(0.8f, 0.8f, 0.0f, 0.8f), 
			new Color(0.9f, 0.9f, 0.4f)
		);
		
		static readonly Pair<Color, Color> unchangedColors = Pair.New(
			new Color(0.8f, 0.5f, 0.0f, 0.8f), 
			new Color(0.9f, 0.6f, 0.4f)
		);
		
		static readonly Pair<Color, Color> errorColors = Pair.New(
			new Color(1.0f, 0.2f, 0.0f, 0.8f), 
			new Color(1.0f, 0.5f, 0.5f)
		);

		static readonly Pair<Color, Color> solvedColors = Pair.New(
			new Color(0.2f, 1.0f, 0.2f), 
			new Color(0.6f, 0.9f, 0.4f)
		);
		
		#endregion
		
		#region /--- Routines ---/

		IEnumerable<CoroutineYield> TemplateRoutine() {
			yield break;
		}

		#endregion

	}

}
