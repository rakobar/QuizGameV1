using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IndependentCreditController : MonoBehaviour
{
    [SerializeField] private GameObject CreditsUI;
    [SerializeField] private Sprite[] DevLogo;
    [Tooltip("0:Trigger Credit UI Btn, 1: OK Button")]
    [SerializeField] private Button[] triggerBtn;
    [SerializeField] private float animationTime = 0.25f;
    //Vector3 defaScale;
    bool logosetIn = true;
    public int indexImage = 0;

    private void Start()
    {
        CreditsUI.SetActive(false);

        //set y to 0;
        //defaScale = CreditsUI.transform.localScale;
        //defaScale.y = 0;
        CreditsUI.transform.localScale = Vector3.zero;

        triggerBtn[0].onClick.AddListener(ShowUI);
        triggerBtn[1].onClick.AddListener(DisabledUI);
    }

    private void Update()
    {
        if (CreditsUI.activeSelf)
        {
            if (CreditsUI.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.localScale == Vector3.zero)
            {
                if (!logosetIn)
                {
                    indexImage = (indexImage + 1) % DevLogo.Length;

                    Sprite randomSprite = DevLogo[indexImage];
                    var renderer = CreditsUI.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).GetComponent<Image>();
                    if (renderer != null)
                    {
                        renderer.sprite = randomSprite;
                    }

                    StartCoroutine(LogoSpriteAnimation(false));
                    logosetIn = true;
                }
            }
            
            if (CreditsUI.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.localScale == Vector3.one)
            {
                if (logosetIn)
                {
                    StartCoroutine(LogoSpriteAnimation(true));
                    logosetIn = false;
                }
                
            }
        }
        else
        {
            CreditsUI.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.localScale = Vector3.one;
            CreditsUI.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.localRotation = new Quaternion(0, 0, 0, 0);
            LeanTween.cancel(CreditsUI.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).gameObject);
            logosetIn = true;
        }
    }

    void ShowUI()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        triggerBtn[0].interactable = false;
        CreditsUI.SetActive(true);
        CreditsUI.transform.LeanScale(Vector3.one, animationTime).setEaseOutBack();
    }
    void DisabledUI()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        StartCoroutine(AnimatedDisableUI());
    }
    private IEnumerator AnimatedDisableUI()
    {
        CreditsUI.transform.LeanScale(Vector3.zero, animationTime).setEaseInBack();
        yield return new WaitUntil(()=> CreditsUI.transform.localScale == Vector3.zero);
        CreditsUI.SetActive(false);
        yield return new WaitForSeconds(animationTime);
        triggerBtn[0].interactable = true;
    }

    private IEnumerator LogoSpriteAnimation(bool In)
    {
        var Logo = CreditsUI.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0);

        if (In)
        {
            yield return new WaitForSeconds(animationTime * 4);
            Logo.transform.LeanRotateZ(180f, animationTime).setEaseInOutQuad();
            Logo.transform.LeanScale(Vector3.zero, animationTime);
        }
        else
        {
            Logo.transform.LeanRotateZ(0f, animationTime * 2).setEaseInOutQuad();
            Logo.transform.LeanScale(Vector3.one, animationTime);
        }

    }

}
