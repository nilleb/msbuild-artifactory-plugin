using System.Collections.Generic;

namespace JFrog.Artifactory.Utils
{
	public static class DictionaryExtension
	{
		public static void AddOrSet<K, V>(this Dictionary<K, List<V>> @this, K key, V value)
		{
			if (@this.ContainsKey(key))
				@this[key].Add(value);
			else
				@this[key] = new List<V> { value };
		}
	}
}
