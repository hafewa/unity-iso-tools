﻿using UnityEngine;
using IsoTools.Internal;
using System.Collections.Generic;

#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace IsoTools {
	public class IsoSortingSolver {
		public float stepDepth  = IsoWorld.DefStepDepth;
		public float startDepth = IsoWorld.DefStartDepth;

		List<Renderer> _tmpRenderers = new List<Renderer>();

		// ---------------------------------------------------------------------
		//
		// Callbacks
		//
		// ---------------------------------------------------------------------

		public void OnAddInstance(IsoObject iso_object) {
			if ( iso_object.cacheRenderers ) {
				iso_object.UpdateCachedRenderers();
			}
		}

		public void OnRemoveInstance(IsoObject iso_object) {
			if ( iso_object.cacheRenderers ) {
				iso_object.ClearCachedRenderers();
			}
		}

		public bool OnMarkDirtyInstance(IsoObject iso_object) {
			return false;
		}

		// ---------------------------------------------------------------------
		//
		// Functions
		//
		// ---------------------------------------------------------------------

		public bool StepSortingAction(IsoScreenSolver screen_solver){
			Profiler.BeginSample("CalculateSectors");
			var dirty = ResolveVisibles(screen_solver);
			Profiler.EndSample();
			if ( dirty ) {
				Profiler.BeginSample("PlaceAllVisibles");
				PlaceAllVisibles(screen_solver.curVisibles);
				Profiler.EndSample();
			}
			return dirty;
		}

		public void PostStepSortingAction() {
			_tmpRenderers.Clear();
		}

		// ---------------------------------------------------------------------
		//
		// ResolveVisibles
		//
		// ---------------------------------------------------------------------

		bool ResolveVisibles(IsoScreenSolver screen_solver){
			var old_visibles = screen_solver.oldVisibles;
			var cur_visibles = screen_solver.curVisibles;

			var mark_dirty = false;
			for ( int i = 0, e = cur_visibles.Count; i < e; ++i ) {
				var iso_object = cur_visibles[i];
				if ( iso_object.Internal.Dirty || !old_visibles.Contains(iso_object) ) {
					mark_dirty = true;
					screen_solver.SetupIsoObjectDepends(iso_object);
					iso_object.Internal.Dirty = false;
				}
				if ( UpdateIsoObjectBounds3d(iso_object) ) {
					mark_dirty = true;
				}
			}
			for ( int i = 0, e = old_visibles.Count; i < e; ++i ) {
				var iso_object = old_visibles[i];
				if ( !cur_visibles.Contains(iso_object) ) {
					mark_dirty = true;
					screen_solver.ClearIsoObjectDepends(iso_object);
				}
			}
			_tmpRenderers.Clear();
			return mark_dirty;
		}

		bool UpdateIsoObjectBounds3d(IsoObject iso_object) {
			if ( iso_object.mode == IsoObject.Mode.Mode3d ) {
				var minmax3d = IsoObjectMinMax3D(iso_object);
				var offset3d = iso_object.Internal.Transform.position.z - minmax3d.center;
				if ( iso_object.Internal.MinMax3d.Approximately(minmax3d) ||
					 !Mathf.Approximately(iso_object.Internal.Offset3d, offset3d) )
				{
					iso_object.Internal.MinMax3d = minmax3d;
					iso_object.Internal.Offset3d = offset3d;
					return true;
				}
			}
			return false;
		}

		IsoMinMax IsoObjectMinMax3D(IsoObject iso_object) {
			bool inited    = false;
			var  result    = IsoMinMax.zero;
			var  renderers = GetIsoObjectRenderers(iso_object);
			for ( int i = 0, e = renderers.Count; i < e; ++i ) {
				var bounds = renderers[i].bounds;
				var extents = bounds.extents;
				if ( extents.x > 0.0f || extents.y > 0.0f || extents.z > 0.0f ) {
					var center    = bounds.center.z;
					var minbounds = center - extents.z;
					var maxbounds = center + extents.z;
					if ( inited ) {
						if ( minbounds < result.min ) {
							result.min = minbounds;
						}
						if ( maxbounds > result.max ) {
							result.max = maxbounds;
						}
					} else {
						inited = true;
						result = new IsoMinMax(minbounds, maxbounds);
					}
				}
			}
			return inited ? result : IsoMinMax.zero;
		}

		List<Renderer> GetIsoObjectRenderers(IsoObject iso_object) {
			if ( iso_object.cacheRenderers ) {
				return iso_object.Internal.Renderers;
			} else {
				iso_object.GetComponentsInChildren<Renderer>(_tmpRenderers);
				return _tmpRenderers;
			}
		}

		// ---------------------------------------------------------------------
		//
		// PlaceAllVisibles
		//
		// ---------------------------------------------------------------------

		void PlaceAllVisibles(IsoAssocList<IsoObject> cur_visibles) {
			var depth = startDepth;
			for ( int i = 0, e = cur_visibles.Count; i < e; ++i ) {
				depth = RecursivePlaceIsoObject(cur_visibles[i], depth);
			}
		}

		float RecursivePlaceIsoObject(IsoObject iso_object, float depth) {
			if ( iso_object.Internal.Placed ) {
				return depth;
			}
			iso_object.Internal.Placed = true;
			var self_depends = iso_object.Internal.SelfDepends;
			for ( int i = 0, e = self_depends.Count; i < e; ++i ) {
				depth = RecursivePlaceIsoObject(self_depends[i], depth);
			}
			if ( iso_object.mode == IsoObject.Mode.Mode3d ) {
				var zoffset = iso_object.Internal.Offset3d;
				var extents = iso_object.Internal.MinMax3d.size;
				PlaceIsoObject(iso_object, depth + extents * 0.5f + zoffset);
				return depth + extents + stepDepth;
			} else {
				PlaceIsoObject(iso_object, depth);
				return depth + stepDepth;
			}
		}

		void PlaceIsoObject(IsoObject iso_object, float depth) {
			var iso_internal = iso_object.Internal;
			var old_position = iso_internal.LastTrans;
			iso_internal.Transform.position =
				IsoUtils.Vec3FromVec2(old_position, depth);
		}
	}
}