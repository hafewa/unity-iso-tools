﻿using UnityEngine;
using HutongGames.PlayMaker;

namespace IsoTools.PlayMaker.Actions {
	[ActionCategory("IsoTools")]
	[HutongGames.PlayMaker.Tooltip("Gets the TilePosition of a IsoObject and stores it in a Vector3 Variable or each Axis in a Float Variable")]
	public class IsoGetTilePosition : FsmStateAction {
		[RequiredField]
		public FsmOwnerDefault gameObject;

		[UIHint(UIHint.Variable)]
		public FsmVector3 vector;

		[UIHint(UIHint.Variable)]
		public FsmFloat x;

		[UIHint(UIHint.Variable)]
		public FsmFloat y;

		[UIHint(UIHint.Variable)]
		public FsmFloat z;

		public bool everyFrame;

		public override void Reset() {
			gameObject = null;
			vector     = null;
			x          = null;
			y          = null;
			z          = null;
			everyFrame = false;
		}

		public override void OnEnter() {
			DoGetPosition();
			if ( !everyFrame ) {
				Finish();
			}
		}

		public override void OnUpdate() {
			DoGetPosition();
		}

		void DoGetPosition() {
			var go = Fsm.GetOwnerDefaultTarget(gameObject);
			var iso_object = go ? go.GetComponent<IsoObject>() : null;
			if ( iso_object ) {
				var position = iso_object.tilePosition;
				vector.Value = position;
				x.Value      = position.x;
				y.Value      = position.y;
				z.Value      = position.z;
			}
		}
	}
} // IsoTools.PlayMaker.Actions