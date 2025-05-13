using System;
using UnityEngine;
using Rephidock.AtomicAnimations;
using Rephidock.AtomicAnimations.Base;


namespace SharedAssets.Utils.AtomicAnimations {

	public class ColorFade : Ease {

		readonly Color oldColor;
		readonly Color newColor;
		readonly Action<Color> setter;

		public ColorFade(
			Color oldColor,
			Color newColor,
			TimeSpan duration,
			EasingCurve easing,
			Action<Color> setter
		) : base(duration, easing) {
			this.oldColor = oldColor;
			this.newColor = newColor;
			this.setter = setter;
		}

		protected override void EaseUpdateImpl(float valueProgressNew) {
			Color lerpedColor = Color.Lerp(oldColor, newColor, valueProgressNew);
			setter(lerpedColor);
		}

	}

}
