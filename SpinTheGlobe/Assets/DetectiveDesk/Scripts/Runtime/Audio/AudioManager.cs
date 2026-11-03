using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public sealed class AudioManager : MonoBehaviour
    {
        public AudioSource source;
        [Range(0, 1)] public float volume = .18f;
        AudioClip click, launch, payout;
        float nextPayout;
        void Awake()
        {
            if (!source) source = GetComponent<AudioSource>();
            click = Tone("Pin click", 900, .045f, false); launch = Tone("Spring launch", 170, .12f, true); payout = Tone("Payout chime", 680, .16f, false);
        }
        AudioClip Tone(string name, float frequency, float seconds, bool falling)
        {
            int rate = 22050, count = Mathf.CeilToInt(rate * seconds); float[] data = new float[count];
            for (int i = 0; i < count; i++) { float t = i / (float)rate, envelope = Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-t * 20); data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t * (falling ? 1 - t * 2 : 1)) * envelope * .35f; }
            var clip = AudioClip.Create(name, count, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        float Vol => volume * GameSettings.Effects;   // master volume is applied by the AudioListener
        public void Click() { if (source && click) source.PlayOneShot(click, Vol); }
        public void Launch() { if (source && launch) source.PlayOneShot(launch, Vol); }
        public void Payout() { if (Time.unscaledTime >= nextPayout && source && payout) { nextPayout = Time.unscaledTime + .08f; source.PlayOneShot(payout, Vol); } }
        void OnDestroy() { if (click) Destroy(click); if (launch) Destroy(launch); if (payout) Destroy(payout); }
    }
}
