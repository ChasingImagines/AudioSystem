using System;
using UnityEngine;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Links string or AudioKey fields to valid AudioCatalog entries with a searchable Inspector dropdown
    /// and optional prefix filtering.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class AudioIdAttribute : PropertyAttribute
    {
        public string FilterPrefix { get; }

        public AudioIdAttribute(string filterPrefix = null)
        {
            FilterPrefix = filterPrefix;
        }
    }
}
