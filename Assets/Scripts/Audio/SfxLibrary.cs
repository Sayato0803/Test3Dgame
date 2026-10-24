using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Bảng gán âm thanh: mỗi SfxId 1 ô AudioClip. Kéo file (wav/mp3/ogg) vào ô để thay âm tổng hợp sẵn; để trống = giữ âm cũ.
    /// Asset nằm ở Assets/Resources/Audio/SfxLibrary.asset (Resources để luôn vào bản build).
    /// </summary>
    [CreateAssetMenu(menuName = "King Hunt/Sfx Library", fileName = "SfxLibrary")]
    public class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [HideInInspector] public string Name;   // để Inspector hiện tên thay vì "Element 0"
            public SfxId Id;
            [Tooltip("Để trống = dùng âm tổng hợp sẵn.")]
            public AudioClip Clip;
            [Range(0f, 2f)] public float Volume = 1f;
        }

        public List<Entry> Sounds = new();

        private static SfxLibrary _instance;
        private static bool _loaded;

        public static SfxLibrary Instance
        {
            get
            {
                if (!_loaded) { _instance = Resources.Load<SfxLibrary>("Audio/SfxLibrary"); _loaded = true; }
                return _instance;
            }
        }

        public Entry Find(SfxId id) => Sounds.Find(e => e.Id == id);

        // Luôn đủ 1 ô cho mọi SfxId (thêm SfxId mới trong code thì ô tự hiện ra).
        private void OnValidate()
        {
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
                if (Find(id) == null) Sounds.Add(new Entry { Id = id });
            foreach (Entry e in Sounds) e.Name = e.Id.ToString();
        }
    }
}
