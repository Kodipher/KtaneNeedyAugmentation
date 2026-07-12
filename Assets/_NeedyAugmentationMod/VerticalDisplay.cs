using System;

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rephidock.GeneralUtilities.Collections;

using Rephidock.AtomicAnimations;
using Rephidock.AtomicAnimations.Waves;
using Rephidock.GeneralUtilities.Maths;

using SharedAssets.Utils;


namespace NeedyAugmentationMod {

	public class VerticalDisplay {

		#region /--- Injected ---/

		public int Size { get; }
		
		TextMesh Text { get; }
		
		Light[] LetterLights { get; }
		readonly float maxLightIntensity;
		
		ModuleLogger Logger { get; }
		
		#endregion

		public char[] Characters { get; }
		
		public Pair<Color, Color> Colors { get; set; }

		/// <summary>
		/// A wave that controls the color "shimmer" of each letter's color.
		/// X: 1 unit = 1 character.
		/// Y: 0 - Pair.First, 1 - Pair.Second.
		/// </summary>
		/// <remarks>
		/// Unlike normally / in <see cref="WaveEase"/>, this wave is treated as looping.
		/// </remarks>
		public Wave ColorLerpWave { get; set; } = new WaveBuilder() { StartValue = 0 }
														.Add(Easing.QuadInOut).To(1).Over(2)
														.Add(Easing.QuadInOut).To(0).Over(2)
														.ToWave();

		/// <summary>In characters per second.</summary>
		public double ColorLerpWaveSpeed { get; set; } = 4;
		
		/// <summary>In characters.</summary>
		public double ColorLerpWaveDisplacement { get; set; } = 0;
		
		public VerticalDisplay(int size, TextMesh text, Light[] lights, ModuleLogger logger) {
			
			Size = size;
			Text = text;
			LetterLights = lights;
			Logger = logger;

			if (lights.Length != Size) {
				throw new ArgumentException($"Lights count mismatch. Got {lights.Length}, expected {size}.");
			}
			
			maxLightIntensity = lights[0].intensity;
			
			Characters = new char[Size];
			for (int i = 0; i < Size; i++) {
				Characters[i] = ' ';
			}
			
			Colors = Pair.New(Color.white, Color.gray);
		}
		
		public void Update(TimeSpan deltaTime) {
			
			// Update wave
			ColorLerpWaveDisplacement += ColorLerpWaveSpeed * deltaTime.TotalSeconds;
			ColorLerpWaveDisplacement = ColorLerpWaveDisplacement.PosMod(ColorLerpWave.Width);
			

			// Update display
			StringBuilder sb = new StringBuilder();
			
			for (int i = 0; i < Size; i++) {

				if (Characters[i] == ' ' || Characters[i] == '\0') {
					LetterLights[i].intensity = 0;
					
				} else {

					float lerpSamplePos = (float)(i - ColorLerpWaveDisplacement).PosMod(ColorLerpWave.Width);
					float lerpSample = ColorLerpWave.GetValueAt(lerpSamplePos);
					Color letterColor = Color.Lerp(Colors.First, Colors.Second, lerpSample);
					
					sb.Append(FormatColoredCharacter(Characters[i], letterColor));
					
					LetterLights[i].color = letterColor;
					LetterLights[i].intensity = letterColor.a * maxLightIntensity;
				}

				sb.Append('\n');
			}
			sb.Remove(sb.Length - 1, 1); // Remove last newline (end just on the displayed character)
			
			Text.text = sb.ToString();
		}

		static string FormatColoredCharacter(char chr, Color col) {
			return $"<color=#{ColorUtility.ToHtmlStringRGBA(col)}>{chr}</color>";
		}

		public void SetString(string str) {

			if (str.Length > Size) {
				Logger.LogStringError($"Trying to set display to string \"{str}\", which is too long.");
				str = str.Substring(0, Size);
			}

			for (int i = 0; i < str.Length; i++) {
				Characters[i] = str[i];
			}

			for (int i = str.Length; i < Size; i++) {
				Characters[i] = ' ';
			}
			
		}

		public void ClearString() {
			for (int i = 0; i < Size; i++) {
				Characters[i] = ' ';
			}
		}
		
	}

}