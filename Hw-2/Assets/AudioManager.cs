using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    public Button toggleButton; // Assign in inspector
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleAudio);
    }

    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            ToggleAudio();
        }
    }

    void ToggleAudio()
    {
        if (audioSource.isPlaying)
            audioSource.Pause();
        else
            audioSource.Play();
    }
}