using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;
    private float animatedTime = 0.25f;
    private float defVolume = 1f;
    private bool defToggle = false;
    [SerializeField] GameObject SettingUI;
    [SerializeField] Slider bgmSlider, sfxSlider;
    [SerializeField] Button bgmButton, sfxButton;
    [SerializeField] Sprite[] SpeakerSprite;
    public AudioData[] bgmAudio, sfxAudio;
    public AudioSource bgmSource, sfxSource;
    
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        //else
        //{
        //    Destroy(gameObject);
        //}
        bgmSlider.value = PlayerPrefs.GetFloat("bgmVolume", defVolume);
        bgmSource.volume = bgmSlider.value;
        bgmSource.mute = PlayerPrefs.GetInt("bgmSetMute", defToggle ? 1 : 0) == 1;
        muteSwitchImage(bgmButton, PlayerPrefs.GetInt("bgmSetMute", defToggle ? 1 : 0) == 0);

        sfxSlider.value = PlayerPrefs.GetFloat("sfxVolume", defVolume);
        sfxSource.volume = sfxSlider.value;
        sfxSource.mute = PlayerPrefs.GetInt("sfxSetMute", defToggle ? 1 : 0) == 1;
        muteSwitchImage(sfxButton, PlayerPrefs.GetInt("sfxSetMute", defToggle ? 1 : 0) == 0);
    }

    private void Start()
    {
        SettingUI.SetActive(false);
        SettingUI.transform.localScale = Vector3.zero;

        bgmButton.onClick.AddListener(ToggleBGM);
        sfxButton.onClick.AddListener(ToggleSFX);
        SettingUI.transform.GetChild(1).transform.GetComponentInChildren<Button>().onClick.AddListener(closeAudioSetting);
    }

    //public void bgmAudioFadeIn(float time)
    //{
    //    StartCoroutine(AudioFadeIn(time));
    //}
    public void bgmAudioFadeOut(float time)
    {
        StartCoroutine(AudioFadeOut(time));
    }

    public void ShowAudioSetting()
    {
        PlayAudioSFX("ButtonClick");
        StartCoroutine(UIAnimated(true));
    }

    private void closeAudioSetting()
    {
        PlayAudioSFX("ButtonClick");
        StartCoroutine(UIAnimated(false));
    }

    IEnumerator UIAnimated(bool active)
    {
        if (active)
        {
            if (!SettingUI.activeSelf)
            {
                SettingUI.SetActive(true);
                SettingUI.transform.LeanScale(Vector3.one, animatedTime).setEaseOutBack();
            }
        }
        else
        {
            if (SettingUI.activeSelf)
            {
                SettingUI.transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
                yield return new WaitUntil(()=> SettingUI.transform.localScale == Vector3.zero);
                SettingUI.SetActive(false);
            }
        }
    }

    IEnumerator AudioFadeIn(float duration)
    {
        float startTime = Time.time;
        float startVolume = 0f;
        var currentVolume = bgmSource.volume;

        bgmSource.volume = 0;
        bgmSource.Play();

        while (bgmSource.volume < currentVolume)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, currentVolume, (Time.time - startTime) / duration);
            yield return null;
        }
    }

    IEnumerator AudioFadeOut(float duration)
    {
        float startTime = Time.time;
        var currentVolume = bgmSource.volume;

        while (bgmSource.volume > 0f)
        {
            bgmSource.volume = Mathf.Lerp(currentVolume, 0f, (Time.time - startTime) / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = currentVolume;
    }

    public void RandomPlayAudioBGM()
    {
        if(bgmAudio.Length == 0)
        {
            Debug.LogError("No BGM Data Available");
        }
        else
        {
            int randomInt = UnityEngine.Random.Range(1, bgmAudio.Length);
            AudioData randomBGM = bgmAudio[randomInt];

            if (bgmSource.clip == bgmAudio[0].AudioClip)
            {
                bgmSource.clip = randomBGM.AudioClip;
                StartCoroutine(AudioFadeIn(0.2f));
            }
            else
            {
                if(bgmSource.time >= bgmSource.clip.length)
                {
                    bgmSource.clip = randomBGM.AudioClip;
                    StartCoroutine(AudioFadeIn(0.2f));
                }
            }
        }
    }

    public void PlayAudioBGM(string name)
    {
        AudioData AD = Array.Find(bgmAudio, x => x.AudioName == name);

        if (AD != null)
        {
            bgmSource.clip = AD.AudioClip;
            StartCoroutine(AudioFadeIn(0.3f));
            //bgmSource.Play();
        }
    }
    public void PlayAudioSFX(string name)
    {
        AudioData AD = Array.Find(sfxAudio, x => x.AudioName == name);

        if (AD != null)
        {
            sfxSource.PlayOneShot(AD.AudioClip);
        }
    }

    public void ToggleBGM()
    {
        bgmSource.mute = !bgmSource.mute;
        PlayAudioSFX("ButtonClick");
        PlayerPrefs.SetInt("bgmSetMute", bgmSource.mute ? 1 : 0);
        muteSwitchImage(bgmButton, !bgmSource.mute);
    }
    public void ToggleSFX()
    {
        sfxSource.mute = !sfxSource.mute;
        PlayAudioSFX("ButtonClick");
        PlayerPrefs.SetInt("sfxSetMute", sfxSource.mute ? 1 : 0);
        muteSwitchImage(sfxButton, !sfxSource.mute);
    }
    public void VolumeBGM()
    {
        bgmSource.volume = bgmSlider.value;
        PlayerPrefs.SetFloat("bgmVolume", bgmSource.volume);
    }
    public void VolumeSFX()
    {
        sfxSource.volume = sfxSlider.value;
        PlayerPrefs.SetFloat("sfxVolume", sfxSource.volume);
    }

    public void muteSwitchImage(Button btn, bool muteStatus)
    {
        Sprite loaderSpite;
        var renderSprite = btn.gameObject.GetComponent<Image>();

        if (muteStatus)
        {
            loaderSpite = SpeakerSprite[1];
        }
        else
        {
            loaderSpite = SpeakerSprite[0];
        }

        if(renderSprite != null)
        {
            renderSprite.sprite = loaderSpite;
        }
    }
}
