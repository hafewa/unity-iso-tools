﻿using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IsoTools {
	[ExecuteInEditMode, DisallowMultipleComponent]
	public class IsoWorld : MonoBehaviour {

		bool               _dirty              = true;
		HashSet<IsoObject> _objects            = new HashSet<IsoObject>();
		HashSet<IsoObject> _visibles           = new HashSet<IsoObject>();
		HashSet<IsoObject> _oldVisibles        = new HashSet<IsoObject>();

		class SectorInfo {
			public List<IsoObject> objects     = new List<IsoObject>();
		}

		List<SectorInfo>   _sectors            = new List<SectorInfo>();
		Stack<SectorInfo>  _sectorsPool        = new Stack<SectorInfo>();
		float              _sectorsSize        = 0.0f;
		Vector3            _sectorsMinNumPos   = Vector3.zero;
		Vector3            _sectorsMaxNumPos   = Vector3.zero;
		Vector3            _sectorsNumPosCount = Vector3.zero;
		
		// ------------------------------------------------------------------------
		//
		// Public
		//
		// ------------------------------------------------------------------------

		[SerializeField]
		public float _tileSize = 32.0f;
		public float tileSize {
			get { return _tileSize; }
			set {
				_tileSize = Mathf.Max(value, Mathf.Epsilon);
				ChangeSortingProperty();
			}
		}

		[SerializeField]
		public float _minDepth = 1.0f;
		public float minDepth {
			get { return _minDepth; }
			set {
				_minDepth = value;
				ChangeSortingProperty();
			}
		}
		
		[SerializeField]
		public float _maxDepth = 100.0f;
		public float maxDepth {
			get { return _maxDepth; }
			set {
				_maxDepth = value;
				ChangeSortingProperty();
			}
		}

		public void MarkDirty() {
			_dirty = true;
			MarkEditorWorldDirty();
		}

		public void MarkDirty(IsoObject iso_object) {
			if ( iso_object && _visibles.Contains(iso_object) ) {
				iso_object.Internal.Moved = true;
				MarkDirty();
			}
		}

		public Vector2 IsoToScreen(Vector3 pos) {
			return new Vector2(
				(pos.x - pos.y),
				(pos.x + pos.y) * 0.5f + pos.z) * tileSize;
		}

		public Vector3 ScreenToIso(Vector2 pos) {
			return new Vector3(
				(pos.x * 0.5f + pos.y),
				(pos.y - pos.x * 0.5f),
				0.0f) / tileSize;
		}

		public Vector3 ScreenToIso(Vector2 pos, float iso_z) {
			return IsoUtils.Vec3ChangeZ(
				ScreenToIso(new Vector2(pos.x, pos.y - iso_z * tileSize)),
				iso_z);
		}

		public void AddIsoObject(IsoObject iso_object) {
			_objects.Add(iso_object);
		}

		public void RemoveIsoObject(IsoObject iso_object) {
			_objects.Remove(iso_object);
		}

		// ------------------------------------------------------------------------
		//
		// Private
		//
		// ------------------------------------------------------------------------

		void MarkEditorWorldDirty() {
		#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
		#endif
		}

		void FixAllTransforms() {
			var objects_iter = _objects.GetEnumerator();
			while ( objects_iter.MoveNext() ) {
				objects_iter.Current.FixTransform();
			}
		}

		void ChangeSortingProperty() {
			MarkDirty();
			FixAllTransforms();
		}

		bool IsIsoObjectVisible(IsoObject iso_object, Plane[] planes) {
			return planes != null && planes.Length > 0
				? GeometryUtility.TestPlanesAABB(planes, iso_object.bounds)
				: false;
		}

		bool IsIsoObjectDepends(Vector3 a_min, Vector3 a_size, Vector3 b_min, Vector3 b_size) {
			var a_max = a_min + a_size;
			var b_max = b_min + b_size;
			var a_yesno = a_max.x > b_min.x && a_max.y > b_min.y && b_max.z > a_min.z;
			var b_yesno = b_max.x > a_min.x && b_max.y > a_min.y && a_max.z > b_min.z;
			if ( a_yesno && b_yesno ) {
				var da_p = new Vector3(a_max.x - b_min.x, a_max.y - b_min.y, b_max.z - a_min.z);
				var db_p = new Vector3(b_max.x - a_min.x, b_max.y - a_min.y, a_max.z - b_min.z);
				var dp_p = a_size + b_size - IsoUtils.Vec3Abs(da_p - db_p);
				if ( dp_p.x <= dp_p.y && dp_p.x <= dp_p.z ) {
					return da_p.x > db_p.x;
				} else if ( dp_p.y <= dp_p.x && dp_p.y <= dp_p.z ) {
					return da_p.y > db_p.y;
				} else {
					return da_p.z > db_p.z;
				}
			}
			return a_yesno;
		}

		bool IsIsoObjectDepends(IsoObject a, IsoObject b) {
			return IsIsoObjectDepends(a.position, a.size, b.position, b.size);
		}

		void PushSectorPool(SectorInfo sector) {
			sector.objects.Clear();
			_sectorsPool.Push(sector);
		}

		SectorInfo PopSectorPool() {
			return _sectorsPool.Count > 0
				? _sectorsPool.Pop()
				: new SectorInfo();
		}

		int SectorIndex(Vector3 num_pos) {
			return Mathf.FloorToInt(
				num_pos.x + _sectorsNumPosCount.x * (num_pos.y + num_pos.z * _sectorsNumPosCount.y));
		}
		
		Vector3 SectorNumPos(int index) {
			var mz = _sectorsNumPosCount.x * _sectorsNumPosCount.y;
			var my = _sectorsNumPosCount.x;
			var vz = Mathf.FloorToInt(index / mz);
			var vy = Mathf.FloorToInt((index - vz * mz) / my);
			var vx = Mathf.FloorToInt(index - vz * mz - vy * my);
			return new Vector3(vx, vy, vz);
		}
		
		SectorInfo FindSector(Vector3 num_pos) {
			if ( num_pos.x < 0 || num_pos.y < 0 || num_pos.z < 0 ) {
				return null;
			}
			if ( num_pos.x >= _sectorsNumPosCount.x || num_pos.y >= _sectorsNumPosCount.y || num_pos.z >= _sectorsNumPosCount.z ) {
				return null;
			}
			return _sectors[SectorIndex(num_pos)];
		}
		
		void LookUpSectorDepends(Vector3 num_pos, System.Action<SectorInfo> act) {
			var ms = FindSector(num_pos);
			if ( ms != null ) {
				act(ms);
				var s1 = FindSector(num_pos + new Vector3(-1,  0, 0));
				var s2 = FindSector(num_pos + new Vector3( 0, -1, 0));
				var s3 = FindSector(num_pos + new Vector3(-1, -1, 0));
				if ( s1 != null ) act(s1);
				if ( s2 != null ) act(s2);
				if ( s3 != null ) act(s3);
				for ( var i = 0; i <= _sectorsNumPosCount.z; ++i ) {
					var ss1 = FindSector(num_pos + new Vector3( 0 - i,  0 - i, i + 1));
					var ss2 = FindSector(num_pos + new Vector3(-1 - i,  0 - i, i + 1));
					var ss3 = FindSector(num_pos + new Vector3( 0 - i, -1 - i, i + 1));
					var ss4 = FindSector(num_pos + new Vector3(-1 - i, -1 - i, i + 1));
					var ss5 = FindSector(num_pos + new Vector3(-2 - i, -1 - i, i + 1));
					var ss6 = FindSector(num_pos + new Vector3(-1 - i, -2 - i, i + 1));
					var ss7 = FindSector(num_pos + new Vector3(-2 - i, -2 - i, i + 1));
					if ( ss1 != null ) act(ss1);
					if ( ss2 != null ) act(ss2);
					if ( ss3 != null ) act(ss3);
					if ( ss4 != null ) act(ss4);
					if ( ss5 != null ) act(ss5);
					if ( ss6 != null ) act(ss6);
					if ( ss7 != null ) act(ss7);
				}
			}
		}

		void LookUpSectorRDepends(Vector3 num_pos, System.Action<SectorInfo> act) {
			var ms = FindSector(num_pos);
			if ( ms != null ) {
				act(ms);
				var s1 = FindSector(num_pos + new Vector3( 1,  0, 0));
				var s2 = FindSector(num_pos + new Vector3( 0,  1, 0));
				var s3 = FindSector(num_pos + new Vector3( 1,  1, 0));
				if ( s1 != null ) act(s1);
				if ( s2 != null ) act(s2);
				if ( s3 != null ) act(s3);
				for ( var i = 0; i <= _sectorsNumPosCount.z; ++i ) {
					var ss1 = FindSector(num_pos + new Vector3( 0 + i,  0 + i, -i - 1));
					var ss2 = FindSector(num_pos + new Vector3( 1 + i,  0 + i, -i - 1));
					var ss3 = FindSector(num_pos + new Vector3( 0 + i,  1 + i, -i - 1));
					var ss4 = FindSector(num_pos + new Vector3( 1 + i,  1 + i, -i - 1));
					var ss5 = FindSector(num_pos + new Vector3( 2 + i,  1 + i, -i - 1));
					var ss6 = FindSector(num_pos + new Vector3( 1 + i,  2 + i, -i - 1));
					var ss7 = FindSector(num_pos + new Vector3( 2 + i,  2 + i, -i - 1));
					if ( ss1 != null ) act(ss1);
					if ( ss2 != null ) act(ss2);
					if ( ss3 != null ) act(ss3);
					if ( ss4 != null ) act(ss4);
					if ( ss5 != null ) act(ss5);
					if ( ss6 != null ) act(ss6);
					if ( ss7 != null ) act(ss7);
				}
			}
		}

		void SetupSectorSize() {
			_sectorsSize = 0.0f;
			var visibles_iter = _visibles.GetEnumerator();
			while ( visibles_iter.MoveNext() ) {
				_sectorsSize += IsoUtils.Vec3MaxF(visibles_iter.Current.size);
			}
			_sectorsSize = Mathf.Round(Mathf.Max(3.0f, _sectorsSize / _visibles.Count));
		}

		void SetupObjectsSectors() {
			_sectorsMinNumPos = Vector3.zero;
			_sectorsMaxNumPos = Vector3.one;
			var visibles_iter = _visibles.GetEnumerator();
			while ( visibles_iter.MoveNext() ) {
				var iso_object = visibles_iter.Current;
				var max_size = IsoUtils.Vec3Max(Vector3.one, iso_object.size);
				var min_npos = IsoUtils.Vec3DivFloor(iso_object.position, _sectorsSize);
				var max_npos = IsoUtils.Vec3DivCeil(iso_object.position + max_size, _sectorsSize);
				_sectorsMinNumPos = IsoUtils.Vec3Min(_sectorsMinNumPos, min_npos);
				_sectorsMaxNumPos = IsoUtils.Vec3Max(_sectorsMaxNumPos, max_npos);
				iso_object.Internal.MinSector = min_npos;
				iso_object.Internal.MaxSector = max_npos;
			}
			_sectorsNumPosCount = _sectorsMaxNumPos - _sectorsMinNumPos;
		}

		void ClearSectors() {
			var sectors_iter = _sectors.GetEnumerator();
			while ( sectors_iter.MoveNext() ) {
				PushSectorPool(sectors_iter.Current);
			}
			_sectors.Clear();
		}

		void ResizeSectors(int count) {
			_sectors.Capacity = count;
			while ( _sectors.Count < _sectors.Capacity ) {
				var sector = PopSectorPool();
				_sectors.Add(sector);
			}
		}

		void TuneSectors() {
			var visibles_iter = _visibles.GetEnumerator();
			while ( visibles_iter.MoveNext() ) {
				var iso_object = visibles_iter.Current;
				iso_object.Internal.MinSector -= _sectorsMinNumPos;
				iso_object.Internal.MaxSector -= _sectorsMinNumPos;

				/*
				IsoUtils.LookUpCube(iso_object.Internal.MinSector, iso_object.Internal.MaxSector, p => {
					var sector = FindSector(p);
					if ( sector != null ) {
						sector.objects.Add(iso_object);
					}
				});*/

				var min = iso_object.Internal.MinSector;
				var max = iso_object.Internal.MaxSector;
				for ( var z = min.z; z < max.z; ++z ) {
				for ( var y = min.y; y < max.y; ++y ) {
				for ( var x = min.x; x < max.x; ++x ) {
					var sector = FindSector(new Vector3(x, y, z));
					if ( sector != null ) {
						sector.objects.Add(iso_object);
					}
				}}}
			}
		}
		
		void SetupSectors() {
			ClearSectors();
			ResizeSectors(Mathf.FloorToInt(_sectorsNumPosCount.x * _sectorsNumPosCount.y * _sectorsNumPosCount.z));
			TuneSectors();
		}

		void StepSort() {
			UpdateVisibles();
			if ( _dirty ) {
				PlaceAllVisibles();
				_dirty = false;
			}
		}

		void UpdateVisibles() {
			CalculateNewVisibles();

			SetupSectorSize();
			SetupObjectsSectors();
			SetupSectors();

			var new_count = 0;
			var visibles_iter = _visibles.GetEnumerator();
			while ( visibles_iter.MoveNext() ) {
				var iso_object = visibles_iter.Current;
				if ( iso_object.Internal.Moved || !_oldVisibles.Contains(iso_object) ) {
					MarkDirty();
					SetupIsoObjectDepends(iso_object);
					iso_object.Internal.Moved = false;
					++new_count;
				}
			}

			var old_count = 0;
			var old_visibles_iter = _oldVisibles.GetEnumerator();
			while ( old_visibles_iter.MoveNext() ) {
				var iso_object = old_visibles_iter.Current;
				if ( !_visibles.Contains(iso_object) ) {
					MarkDirty();
					ClearIsoObjectDepends(iso_object);
					++old_count;
				}
			}

			if ( new_count > 0 || old_count > 0 ) {
				/*Debug.LogFormat(
					"New or moved: {0}, Missings: {1}, Visibles: {2}, All: {3}",
					new_count, old_count, _visibles.Count, _objects.Count);*/
			}
		}
		
		void CalculateNewVisibles() {
			var planes = Camera.current ? GeometryUtility.CalculateFrustumPlanes(Camera.current) : null;
			_oldVisibles.Clear();
			var objects_iter = _objects.GetEnumerator();
			while ( objects_iter.MoveNext() ) {
				var iso_object = objects_iter.Current;
				if ( IsIsoObjectVisible(iso_object, planes) ) {
					iso_object.Internal.Visited = false;
					_oldVisibles.Add(iso_object);
				}
			}
			var old_visibles = _visibles;
			_visibles = _oldVisibles;
			_oldVisibles = old_visibles;
		}

		void ClearIsoObjectDepends(IsoObject iso_object) {
			var their_depends_iter = iso_object.Internal.TheirDepends.GetEnumerator();
			while ( their_depends_iter.MoveNext() ) {
				their_depends_iter.Current.Internal.SelfDepends.Remove(iso_object);
			}
			iso_object.Internal.SelfDepends.Clear();
			iso_object.Internal.TheirDepends.Clear();
		}

		void SetupIsoObjectDepends(IsoObject obj_a) {
			ClearIsoObjectDepends(obj_a);
			var min = obj_a.Internal.MinSector;
			var max = obj_a.Internal.MaxSector;
			for ( var z = min.z; z < max.z; ++z ) {
			for ( var y = min.y; y < max.y; ++y ) {
			for ( var x = min.x; x < max.x; ++x ) {
				LookUpSectorDepends(new Vector3(x, y, z), sec => {
					var sec_objects_iter = sec.objects.GetEnumerator();
					while ( sec_objects_iter.MoveNext() ) {
						var obj_b = sec_objects_iter.Current;
						if ( obj_a != obj_b && IsIsoObjectDepends(obj_a, obj_b) ) {
							obj_a.Internal.SelfDepends.Add(obj_b);
							obj_b.Internal.TheirDepends.Add(obj_a);
						}
					}
				});
			}}}
			for ( var z = min.z; z < max.z; ++z ) {
			for ( var y = min.y; y < max.y; ++y ) {
			for ( var x = min.x; x < max.x; ++x ) {
				LookUpSectorRDepends(new Vector3(x, y, z), sec => {
					var sec_objects_iter = sec.objects.GetEnumerator();
					while ( sec_objects_iter.MoveNext() ) {
						var obj_b = sec_objects_iter.Current;
						if ( obj_a != obj_b && IsIsoObjectDepends(obj_b, obj_a) ) {
							obj_b.Internal.SelfDepends.Add(obj_a);
							obj_a.Internal.TheirDepends.Add(obj_b);
						}
					}
				});
			}}}
		}

		void PlaceAllVisibles() {
			var depth = minDepth;
			var visibles_iter = _visibles.GetEnumerator();
			while ( visibles_iter.MoveNext() ) {
				depth = RecursivePlaceIsoObject(visibles_iter.Current, depth);
			}
		}

		float RecursivePlaceIsoObject(IsoObject iso_object, float depth) {
			if ( iso_object.Internal.Visited ) {
				return depth;
			}
			iso_object.Internal.Visited = true;
			var self_depends_iter = iso_object.Internal.SelfDepends.GetEnumerator();
			while ( self_depends_iter.MoveNext() ) {
				depth = RecursivePlaceIsoObject(self_depends_iter.Current, depth);
			}
			PlaceIsoObject(iso_object, depth);
			return depth + (maxDepth - minDepth) / _visibles.Count;
		}

		void PlaceIsoObject(IsoObject iso_object, float depth) {
			var trans = iso_object.transform;
			trans.position = IsoUtils.Vec3ChangeZ(trans.position, depth);
		}

		// ------------------------------------------------------------------------
		//
		// Messages
		//
		// ------------------------------------------------------------------------

		void Start() {
			ChangeSortingProperty();
			StepSort();
		}

		void LateUpdate() {
			StepSort();
		}

		void OnEnable() {
			_objects = new HashSet<IsoObject>(FindObjectsOfType<IsoObject>());
			_visibles.Clear();
			_sectors.Clear();
			MarkDirty();
		}

		void OnDisable() {
			_objects.Clear();
			_visibles.Clear();
			_sectors.Clear();
		}

		#if UNITY_EDITOR
		void Reset() {
			tileSize = 32.0f;
			minDepth = 1.0f;
			maxDepth = 100.0f;
		}
		
		void OnValidate() {
			tileSize = _tileSize;
			minDepth = _minDepth;
			maxDepth = _maxDepth;
		}

		void OnRenderObject() {
			if ( Camera.current && Camera.current.name == "SceneCamera" ) {
				StepSort();
			}
		}
		#endif
	}
} // namespace IsoTools