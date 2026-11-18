using System;
using UnityEngine;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// String alanları AudioCatalog içindeki geçerli ses kimliklerine (ID) bağlayan ve
    /// Inspector'da fuzzy search destekli bir açılır menü (dropdown) sunan öznitelik (attribute).
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

