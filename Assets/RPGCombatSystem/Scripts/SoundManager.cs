using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	[System.Serializable]
	public class SoundAudioClip
	{
		public string sound;
		public AudioClip audioClip;
	}

	public SoundAudioClip[] soundAudioClips;

	[Range(0f, 1f)]
	public float volume = 0.5f; // Inspector에서 전체 볼륨 조절

	public void PlaySound(string sound)
	{
		AudioClip clip = GetAudioClip(sound);
		if (clip == null) return;

		GameObject soundGameObject = new GameObject("Sound");
		AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();
		audioSource.clip = clip;
		audioSource.volume = volume;
		audioSource.Play();
		Object.Destroy(soundGameObject, clip.length);
	}

	private AudioClip GetAudioClip(string sound)
	{
		if (soundAudioClips == null) return null;
		foreach (SoundAudioClip soundAudioClip in soundAudioClips)
		{
			if (soundAudioClip.sound == sound)
				return soundAudioClip.audioClip;
		}
		return null;
	}
}
