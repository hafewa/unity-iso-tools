﻿using UnityEngine;
using HutongGames.PlayMaker;

namespace IsoTools.PlayMaker.Actions {
	[ActionCategory("IsoTools")]
	[HutongGames.PlayMaker.Tooltip("Sets the Position of a IsoObject. To leave any axis unchanged, set variable to 'None'.")]
	public class IsoSetPosition : IsoComponentAction<IsoObject> {
		[RequiredField]
		[CheckForComponent(typeof(IsoObject))]
		public FsmOwnerDefault gameObject;

		[UIHint(UIHint.Variable)]
		public FsmVector3 vector;

		public FsmFloat x;
		public FsmFloat y;
		public FsmFloat z;

		public bool everyFrame;
		public bool lateUpdate;
		public bool fixedUpdate;

		public override void Reset() {
			gameObject  = null;
			vector      = null;
			x           = new FsmFloat{UseVariable = true};
			y           = new FsmFloat{UseVariable = true};
			z           = new FsmFloat{UseVariable = true};
			everyFrame  = false;
			lateUpdate  = false;
			fixedUpdate = false;
		}

		public override void OnPreprocess() {
			Fsm.HandleFixedUpdate = true;
		}

		public override void OnEnter() {
			if ( !everyFrame && !lateUpdate && !fixedUpdate ) {
				DoSetPosition();
				Finish();
			}
		}

		public override void OnUpdate() {
			if ( !lateUpdate && !fixedUpdate ) {
				DoSetPosition();
			}
		}

		public override void OnLateUpdate() {
			if ( lateUpdate ) {
				DoSetPosition();
			}
			if ( !everyFrame ) {
				Finish();
			}
		}

		public override void OnFixedUpdate() {
			if ( fixedUpdate ) {
				DoSetPosition();
			}
			if ( !everyFrame ) {
				Finish();
			}
		}

		void DoSetPosition() {
			var go = Fsm.GetOwnerDefaultTarget(gameObject);
			if ( UpdateCache(go) ) {
				var value = vector.IsNone
					? isoObject.position
					: vector.Value;

				if ( !x.IsNone ) {
					value.x = x.Value;
				}
				if ( !y.IsNone ) {
					value.y = y.Value;
				}
				if ( !z.IsNone ) {
					value.z = z.Value;
				}

				isoObject.position = value;
			}
		}
	}
} // IsoTools.PlayMaker.Actions