using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour
{
    float AnimatedSpeed = 0.3f;
    [Tooltip("Loading Icon Image, Loading Text TextMeshPro")]
    [SerializeField] GameObject LoadingUI;
    [SerializeField] Sprite[] UILoadingIcon;

    private Quaternion intialRotation;
    // Start is called before the first frame update
    void Start()
    {
        LoadingUI.SetActive(false);
        ImgAlphaLoadingUISet(0f);
        intialRotation = LoadingUI.transform.GetChild(0).gameObject.transform.rotation;
    }

    private void Update()
    {
        if (LoadingUI.activeSelf)
        {
            if(LoadingUI.transform.GetChild(0).gameObject.transform.localScale == Vector3.zero)
            {
                ChangeIconLoading();
            }
        }
    }

    public void LoadingDisplay(bool Active, string LoadingText = "Loading..")
    {
        if (Active)
        {
            LoadingUI.transform.GetChild(1).gameObject.transform.GetComponent<TMP_Text>().text = LoadingText;
            StartCoroutine(AnimatedIn());   
        }
        else
        {
            StartCoroutine(AnimatedOut());
        }
    }

    private IEnumerator AnimatedIn()
    {
        if (!LoadingUI.activeSelf)
        {
            LoadingUI.SetActive(true);
            LeanTween.value(LoadingUI, ImgAlphaLoadingUISet, 0f, 0.66f, AnimatedSpeed);
            LoadingUI.transform.GetChild(0).gameObject.transform.LeanScale(Vector3.one, AnimatedSpeed);
            LoadingUI.transform.GetChild(1).gameObject.transform.LeanScale(Vector3.one, AnimatedSpeed);
            yield return new WaitUntil(()=> Mathf.Approximately(LoadingUI.GetComponent<Image>().color.a, 0.66f));
            LoadingUI.transform.GetChild(0).gameObject.LeanRotateZ(180f, AnimatedSpeed * 2).setEaseInOutQuad().setLoopPingPong();
            LoadingUI.transform.GetChild(0).gameObject.LeanScale(Vector3.zero, AnimatedSpeed).setLoopPingPong();
            LeanTween.value(LoadingUI.transform.GetChild(1).gameObject, ImgAlphaAnimatedUISet, 0.66f, 0f, AnimatedSpeed).setLoopPingPong();
        }
    }
    private IEnumerator AnimatedOut()
    {
        if (LoadingUI.activeSelf)
        {
            LeanTween.value(LoadingUI, ImgAlphaLoadingUISet, 0.66f, 0f, AnimatedSpeed);
            LoadingUI.transform.GetChild(0).gameObject.transform.LeanScale(Vector3.zero, AnimatedSpeed);
            LoadingUI.transform.GetChild(1).gameObject.transform.LeanScale(Vector3.zero, AnimatedSpeed);
            yield return new WaitUntil(() => Mathf.Approximately(LoadingUI.GetComponent<Image>().color.a, 0f));
            LeanTween.cancel(LoadingUI.transform.GetChild(0).gameObject);
            LeanTween.cancel(LoadingUI.transform.GetChild(1).gameObject);
            LoadingUI.transform.GetChild(0).gameObject.transform.rotation = intialRotation;
            ImgAlphaAnimatedUISet(0.66f);
            LoadingUI.SetActive(false);
        }
    }

    void ImgAlphaLoadingUISet(float ColorAlpha)
    {
        var ColorA = LoadingUI.GetComponent<Image>().color;
        ColorA.a = ColorAlpha;
        LoadingUI.GetComponent<Image>().color = ColorA;
    }
    void ImgAlphaAnimatedUISet(float ColorAlpha)
    {
        var ColorA = LoadingUI.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().color;
        ColorA.a = ColorAlpha;
        LoadingUI.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().color = ColorA;
    }

    void ChangeIconLoading()
    {
        Sprite randomSprite = UILoadingIcon[Random.Range(0, UILoadingIcon.Length)];
        var renderer = LoadingUI.transform.GetChild(0).gameObject.GetComponent<Image>();
        if(renderer != null)
        {
            renderer.sprite = randomSprite;
        }
        else
        {
            Debug.Log("Error");
        }

    }
}
