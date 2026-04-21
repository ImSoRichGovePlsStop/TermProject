using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip swordAttackClip;
    [SerializeField] private AudioClip wandAttackClip;
    [SerializeField] private AudioClip enemyHitClip;

    [Header("Basic Scene Music")]
    [SerializeField] private AudioClip scene0Track;
    [SerializeField] private AudioClip scene1Track;

    [Header("Scene 2 Playlist")]
    [SerializeField] private List<AudioClip> scene2Playlist;
    private int currentPlaylistIndex = 0;
    private bool isCycling = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); return; }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int index = scene.buildIndex;
        isCycling = (index == 2); // Only enable cycling logic for Scene 2

        if (index == 0) PlayMusic(scene0Track);
        else if (index == 1) PlayMusic(scene1Track);
        else if (index == 2 && scene2Playlist.Count > 0)
        {
            // Start the first song of the playlist
            PlayMusic(scene2Playlist[currentPlaylistIndex]);
        }
    }

    private void Update()
    {
        // If we are in Scene 2 and the current song has finished playing...
        if (isCycling && !musicSource.isPlaying)
        {
            PlayNextInPlaylist();
        }
    }

    private void PlayNextInPlaylist()
    {
        currentPlaylistIndex++;
        // Loop back to the start of the list if we reach the end
        if (currentPlaylistIndex >= scene2Playlist.Count) currentPlaylistIndex = 0;

        musicSource.clip = scene2Playlist[currentPlaylistIndex];
        musicSource.Play();
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlaySwordAttack()
    {
        if (sfxSource != null && swordAttackClip != null)
            sfxSource.PlayOneShot(swordAttackClip);
    }

    public void PlayWandAttack()
    {
        if (sfxSource != null && wandAttackClip != null)
            sfxSource.PlayOneShot(wandAttackClip);
    }

    public void PlayEnemyHit()
    {
        if (sfxSource != null && enemyHitClip != null)
            sfxSource.PlayOneShot(enemyHitClip);
    }
}