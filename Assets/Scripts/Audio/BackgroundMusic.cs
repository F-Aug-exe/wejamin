using UnityEngine;

namespace Replica.Audio
{
    public class BackgroundMusic : MonoBehaviour
    {
        [Header("Configuracion de Musica de Fondo")]
        [SerializeField] private AudioClip backgroundTrack;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.025f;
        [SerializeField] private bool loop = true;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = loop;
            audioSource.volume = volume;
        }

        private void Start()
        {
            if (backgroundTrack != null && !audioSource.isPlaying)
            {
                audioSource.clip = backgroundTrack;
                audioSource.volume = volume;
                audioSource.Play();
            }
        }

        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }
    }
}
