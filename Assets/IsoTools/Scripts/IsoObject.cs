using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IsoTools {
	[ExecuteInEditMode, DisallowMultipleComponent]
	public class IsoObject : MonoBehaviour {

		// ------------------------------------------------------------------------
		//
		// size
		//
		// ------------------------------------------------------------------------

		[SerializeField]
		Vector3 _size = Vector3.one;

		public Vector3 size {
			get { return _size; }
			set {
				_size = IsoUtils.Vec3Max(value, Vector3.zero);
				FixTransform();
			}
		}

		public float sizeX {
			get { return size.x; }
			set { size = IsoUtils.Vec3ChangeX(size, value); }
		}

		public float sizeY {
			get { return size.y; }
			set { size = IsoUtils.Vec3ChangeY(size, value); }
		}

		public float sizeZ {
			get { return size.z; }
			set { size = IsoUtils.Vec3ChangeZ(size, value); }
		}

		public Vector2 sizeXY {
			get { return new Vector2(sizeX, sizeY); }
		}

		public Vector2 sizeYZ {
			get { return new Vector2(sizeY, sizeZ); }
		}

		public Vector2 sizeXZ {
			get { return new Vector2(sizeX, sizeZ); }
		}

		// ------------------------------------------------------------------------
		//
		// position
		//
		// ------------------------------------------------------------------------

		[SerializeField]
		Vector3 _position = Vector3.zero;

		public Vector3 position {
			get { return _position; }
			set {
				_position = value;
				FixTransform();
			}
		}

		public float positionX {
			get { return position.x; }
			set { position = IsoUtils.Vec3ChangeX(position, value); }
		}

		public float positionY {
			get { return position.y; }
			set { position = IsoUtils.Vec3ChangeY(position, value); }
		}

		public float positionZ {
			get { return position.z; }
			set { position = IsoUtils.Vec3ChangeZ(position, value); }
		}

		public Vector2 positionXY {
			get { return new Vector2(positionX, positionY); }
		}

		public Vector2 positionYZ {
			get { return new Vector2(positionY, positionZ); }
		}

		public Vector2 positionXZ {
			get { return new Vector2(positionX, positionZ); }
		}

		// ------------------------------------------------------------------------
		//
		// tilePosition
		//
		// ------------------------------------------------------------------------

		public Vector3 tilePosition {
			get { return IsoUtils.Vec3Round(position); }
			set { position = value; }
		}

		public float tilePositionX {
			get { return tilePosition.x; }
			set { tilePosition = IsoUtils.Vec3ChangeX(tilePosition, value); }
		}

		public float tilePositionY {
			get { return tilePosition.y; }
			set { tilePosition = IsoUtils.Vec3ChangeY(tilePosition, value); }
		}

		public float tilePositionZ {
			get { return tilePosition.z; }
			set { tilePosition = IsoUtils.Vec3ChangeZ(tilePosition, value); }
		}

		public Vector2 tilePositionXY {
			get { return new Vector2(tilePositionX, tilePositionY); }
		}

		public Vector2 tilePositionYZ {
			get { return new Vector2(tilePositionY, tilePositionZ); }
		}

		public Vector2 tilePositionXZ {
			get { return new Vector2(tilePositionX, tilePositionZ); }
		}

		// ------------------------------------------------------------------------
		//
		// For editor
		//
		// ------------------------------------------------------------------------

		#if UNITY_EDITOR
		Vector3 _lastSize     = Vector3.zero;
		Vector3 _lastPosition = Vector3.zero;
		Vector2 _lastTransPos = Vector2.zero;

		[SerializeField] bool _isAlignment  = true;
		[SerializeField] bool _isShowBounds = false;

		public bool isAlignment {
			get { return _isAlignment; }
		}

		public bool isShowBounds {
			get { return _isShowBounds; }
		}
		#endif

		// ------------------------------------------------------------------------
		//
		// Functions
		//
		// ------------------------------------------------------------------------

		IsoWorld _isoWorld = null;
		public IsoWorld isoWorld {
			get {
				if ( (object)_isoWorld == null ) {
					_isoWorld = GameObject.FindObjectOfType<IsoWorld>();
				}
				if ( (object)_isoWorld == null ) {
					throw new UnityException("IsoObject. IsoWorld not found!");
				}
				return _isoWorld;
			}
		}

		public void ResetIsoWorld() {
			_isoWorld = null;
		}

		public void FixTransform() {
		#if UNITY_EDITOR
			if ( !Application.isPlaying ) {
				if ( isAlignment ) {
					_position = tilePosition;
				} else if ( Selection.gameObjects.Length == 1 ) {
					SnappingProcess();
				}
			}
		#endif
			transform.position = IsoUtils.Vec3ChangeZ(
				isoWorld.IsoToScreen(position),
				transform.position.z);
			FixLastProperties();
			MartDirtyIsoWorld();
			MarkEditorObjectDirty();
		}

		public void FixIsoPosition() {
			position = isoWorld.ScreenToIso(
				transform.position,
				positionZ);
		}

		void SnappingProcess() {
			var pos_a = position;
			var size_a = size;
			var iso_objects = GameObject.FindObjectsOfType<IsoObject>();
			foreach ( var iso_object_b in iso_objects ) {
				if ( this != iso_object_b ) {
					var delta = 0.2f;
					var pos_b = iso_object_b.position;
					var size_b = iso_object_b.size;
					for ( var i = 0; i < 3; ++i ) {
						var d0 = Mathf.Abs(pos_a[i] - pos_b[i]);
						var d1 = Mathf.Abs(pos_a[i] + size_a[i] - pos_b[i]);
						var d2 = Mathf.Abs(pos_a[i] - pos_b[i] - size_b[i]);
						var d3 = Mathf.Abs(pos_a[i] + size_a[i] - pos_b[i] - size_b[i]);
						if ( d0 > Mathf.Epsilon && d0 < delta ) _position = IsoUtils.Vec3ChangeI(position, i, pos_b[i]);
						if ( d1 > Mathf.Epsilon && d1 < delta ) _position = IsoUtils.Vec3ChangeI(position, i, pos_b[i] - size_a[i]);
						if ( d2 > Mathf.Epsilon && d2 < delta ) _position = IsoUtils.Vec3ChangeI(position, i, pos_b[i] + size_b[i]);
						if ( d3 > Mathf.Epsilon && d3 < delta ) _position = IsoUtils.Vec3ChangeI(position, i, pos_b[i] + size_b[i] - size_a[i]);
					}
				}
			}
		}

		void FixLastProperties() {
		#if UNITY_EDITOR
			_lastSize     = size;
			_lastPosition = position;
			_lastTransPos = transform.position;
		#endif
		}

		void MartDirtyIsoWorld() {
			isoWorld.MarkDirty(this);
		}

		void MarkEditorObjectDirty() {
		#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
		#endif
		}

		// ------------------------------------------------------------------------
		//
		// Messages
		//
		// ------------------------------------------------------------------------

		void Awake() {
			FixLastProperties();
			FixIsoPosition();
		}

		void OnEnable() {
			MartDirtyIsoWorld();
		}

		#if UNITY_EDITOR
		void Reset() {
			size     = Vector3.one;
			position = Vector3.zero;
		}

		void OnValidate() {
			size     = _size;
			position = _position;
		}

		void OnDrawGizmos() {
			if ( isShowBounds ) {
				IsoUtils.DrawCube(isoWorld, position + size * 0.5f, size, Color.red);
			}
		}

		void Update() {
			if ( !IsoUtils.Vec3Approximately(_lastSize, _size) ) {
				size = _size;
			}
			if ( !IsoUtils.Vec3Approximately(_lastPosition, _position) ) {
				position = _position;
			}
			if ( !IsoUtils.Vec2Approximately(_lastTransPos, transform.position) ) {
				FixIsoPosition();
			}
		}
		#endif
	}
} // namespace IsoTools