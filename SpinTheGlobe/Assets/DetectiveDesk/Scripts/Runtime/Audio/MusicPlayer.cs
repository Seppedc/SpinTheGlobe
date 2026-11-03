using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Background music: loops the desk music (Resources "DeskMusic") at the Background music volume from the
    /// settings. Master volume is applied on top by the AudioListener. Fades in at start.
    /// To change the track, replace DetectiveDesk/Audio/Music/Resources/DeskMusic.ogg.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MusicPlayer : MonoBehaviour
    {
        public string trackName = "DeskMusic";
        public float fadeInSeconds = 3f;

        AudioSource source;
        float fade;

        public void Initialize(GameManager owner)
        {
            var clip = Resources.Load<AudioClip>(trackName);
            if (!clip) { Debug.LogWarning("MusicPlayer: no music clip '" + trackName + "' in a Resources folder."); return; }
            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.priority = 0;
            source.ignoreListenerPause = true;
            source.volume = 0;
            source.Play();
        }

        void Update()
        {
            if (!source) return;
            fade = Mathf.MoveTowards(fade, 1, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeInSeconds));
            source.volume = GameSettings.Music * fade * fade;
        }
    }
}
