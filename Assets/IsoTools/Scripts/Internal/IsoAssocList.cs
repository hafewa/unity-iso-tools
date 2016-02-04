﻿using System;
using System.Collections.Generic;

namespace IsoTools.Internal {
	public class IsoAssocList<T> {
		IsoList<T>           _list;
		Dictionary<T, int>   _dict;
		IEqualityComparer<T> _comparer;

		public IsoAssocList() {
			_list     = new IsoList<T>();
			_dict     = new Dictionary<T, int>();
			_comparer = EqualityComparer<T>.Default;
		}

		public IsoAssocList(int capacity) {
			_list     = new IsoList<T>(capacity);
			_dict     = new Dictionary<T, int>(capacity);
			_comparer = EqualityComparer<T>.Default;
		}

		public IsoList<T> RawList {
			get {
				return _list;
			}
		}

		public void Add(T item) {
			if ( !_dict.ContainsKey(item) ) {
				_dict.Add(item, _list.Count);
				_list.Push(item);
			}
		}

		public void Remove(T item) {
			int index;
			if ( _dict.TryGetValue(item, out index) ) {
				_dict.Remove(item);
				var reordered =_list.UnorderedRemoveAt(index);
				if ( !_comparer.Equals(reordered, item) ) {
					_dict[reordered] = index;
				}
			}
		}

		public void Clear() {
			_list.Clear();
			_dict.Clear();
		}
	}
}
