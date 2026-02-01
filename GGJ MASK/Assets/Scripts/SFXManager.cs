using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SfxEntry
{
    public string key;        
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitchMin = 1f;
    [Range(0.5f, 2f)] public float pitchMax = 1f;
}

public class SFXManager : MonoBehaviour
{
    public static SFXManager I { get; private set; }

    [Header("SFX Library")]
    public List<SfxEntry> entries = new();

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    public int poolSize = 8;

    private Dictionary<string, SfxEntry> map = new();
    private AudioSource[] pool;
    private int idx;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        map.Clear();
        foreach (var e in entries)
        {
            if (e == null || e.clip == null || string.IsNullOrWhiteSpace(e.key)) continue;
            map[e.key.Trim().ToLowerInvariant()] = e;
        }

        pool = new AudioSource[Mathf.Max(1, poolSize)];
        for (int i = 0; i < pool.Length; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            pool[i] = s;
        }
        idx = 0;
    }

    public void Play(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        key = key.Trim().ToLowerInvariant();

        if (!map.TryGetValue(key, out var e) || e.clip == null) return;

        var src = pool[idx];
        idx = (idx + 1) % pool.Length;

        src.pitch = UnityEngine.Random.Range(e.pitchMin, e.pitchMax);
        src.PlayOneShot(e.clip, e.volume * masterVolume);
    }
}
