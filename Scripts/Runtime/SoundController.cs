using UnityEngine;

namespace WizardsCode.Kalmeer.Wildlife
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundController : MonoBehaviour
    {
        [SerializeField, Tooltip("The wingflap sounda, typically a random one will be played on the standard wing downstroke.")]
        AudioClip[] m_WingFlapClip;

        AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void PlaySound()
        {
            if (m_WingFlapClip.Length == 0) return;

            audioSource.clip = m_WingFlapClip[Random.Range(0, m_WingFlapClip.Length)];
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.Play();
        }
    }
}
