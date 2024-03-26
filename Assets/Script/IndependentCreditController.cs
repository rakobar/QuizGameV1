using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IndependentCreditController : MonoBehaviour
{
    [SerializeField] private GameObject CreditsUI;
    [Tooltip("0:Trigger Credit UI Btn, 1: OK Button")]
    [SerializeField] private Button[] triggerBtn;
    [SerializeField] private float animationTime = 0.35f;
    Vector3 defaScale;

    private void Start()
    {
        CreditsUI.SetActive(false);

        //set y to 0;
        defaScale = CreditsUI.transform.localScale;
        defaScale.y = 0;
        CreditsUI.transform.localScale = defaScale;

        triggerBtn[0].onClick.AddListener(ShowUI);
        triggerBtn[1].onClick.AddListener(DisabledUI);
    }

    void ShowUI()
    {
        CreditsUI.SetActive(true);
        CreditsUI.transform.LeanScaleY(1f, animationTime);
    }
    void DisabledUI()
    {
        StartCoroutine(AnimatedDisableUI());
    }
    private IEnumerator AnimatedDisableUI()
    {
        CreditsUI.transform.LeanScaleY(0f, animationTime);
        yield return new WaitUntil(()=> CreditsUI.transform.localScale == defaScale);
        CreditsUI.SetActive(false);
    }

}
