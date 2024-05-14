using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AlertController : MonoBehaviour
{
    [SerializeField] private GameObject AlertUI;
    [Tooltip("btnOk, btnCancel")]
    [SerializeField] private Button[] triggerBtn;
    [SerializeField] float uiAnimationTime = 0.15f;
    private UnityAction callAction;
    private float imgAlpha = 0.35f;
    private string TempAlertText;
    private Color defColor;
    private bool wAction;

    private void Start()
    {
        AlertUI.SetActive(false);
        defColor = AlertUI.gameObject.GetComponent<Image>().color;
        defColor.a = 0f;
        AlertUI.gameObject.GetComponent<Image>().color = defColor;
        //AlertUI.transform.localScale = Vector3.zero;
        AlertUI.transform.GetChild(0).gameObject.GetComponent<Transform>().localScale = Vector3.zero;
        triggerBtn[1].transform.gameObject.SetActive(false);
    }
    private void AlertUIDisabled()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        StartCoroutine(AlertAnimationOut());
    }
    private void CallFunc()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        if (wAction)
        {
            StartCoroutine(delayAction());
        }
        else
        {
            callAction.Invoke();
        }
        
    }

    //public void AlertSet(string AlertText, bool ShowImageAlert, TextAlignmentOptions textAlignment)
    //{
    //    triggerBtn[0].onClick.RemoveAllListeners();
    //    triggerBtn[0].onClick.AddListener(AlertUIDisabled);

    //    if(triggerBtn[1].transform.gameObject.activeSelf)
    //    {
    //        triggerBtn[1].transform.gameObject.SetActive(false);
    //        triggerBtn[1].onClick.RemoveAllListeners();
    //    }
    //    AlertUI.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().alignment = textAlignment;

    //    StartCoroutine(AlertAnimationIn(AlertText, ShowImageAlert));
    //    TempAlertText = AlertText;
    //}
    public void AlertSet(string AlertText, bool ShowImageAlert = false, TextAlignmentOptions textAlignment = TextAlignmentOptions.Center, bool AllowCancelBtn = false, UnityAction callFunction = null)
    {
        try
        {
            
            AlertUI.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().alignment = textAlignment;
            triggerBtn[1].transform.gameObject.SetActive(AllowCancelBtn);
            callAction = callFunction;

            if (AllowCancelBtn)
            {
                wAction = true;
                triggerBtn[1].onClick.RemoveAllListeners();
                triggerBtn[0].onClick.RemoveAllListeners();
                triggerBtn[1].onClick.AddListener(AlertUIDisabled);
                triggerBtn[0].onClick.AddListener(AlertUIDisabled);

                if(callAction != null)
                {
                    triggerBtn[0].onClick.AddListener(CallFunc);
                }

            }
            else
            {
                wAction = false;
                triggerBtn[1].onClick.RemoveAllListeners();
                triggerBtn[0].onClick.RemoveAllListeners();
                triggerBtn[0].onClick.AddListener(AlertUIDisabled);

                if (callAction != null)
                {
                    triggerBtn[0].onClick.AddListener(CallFunc);
                }
            }

            StartCoroutine(AlertAnimationIn(AlertText, ShowImageAlert));
            TempAlertText = AlertText;

        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }
        
    }

    private IEnumerator AlertAnimationIn(string AlertText, bool ShowImageAlert)
    {
        if(TempAlertText != AlertText & !AlertUI.activeSelf)
        {

            AlertUI.SetActive(true);

            LeanTween.value(AlertUI, ImgAlphaUpdate, 0f, imgAlpha, uiAnimationTime);
            yield return new WaitUntil(()=> AlertUI.gameObject.GetComponent<Image>().color == new Color(defColor.r, defColor.g, defColor.b, imgAlpha));
            AlertUI.transform.GetChild(0).gameObject.transform.LeanScaleY(0.03f, uiAnimationTime);
            yield return new WaitUntil(() => Mathf.Approximately(AlertUI.transform.GetChild(0).gameObject.transform.localScale.y, 0.03f));
            AlertUI.transform.GetChild(0).gameObject.transform.LeanScaleX(1f, uiAnimationTime);
            yield return new WaitUntil(() => Mathf.Approximately(AlertUI.transform.GetChild(0).gameObject.transform.localScale.x, 1f));

            // Mengatur teks peringatan
            AlertUI.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = AlertText;

            //Mengatur gambar pada objek Image.
            //AlertUI.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<Image>().color = ErrorAlert ? Color.red : Color.yellow;// true | false
            AlertUI.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.SetActive(ShowImageAlert);
            AlertUI.transform.GetChild(0).gameObject.transform.LeanScaleY(1f, uiAnimationTime);
            AlertUI.transform.GetChild(0).gameObject.transform.LeanScaleZ(1f, uiAnimationTime);
        }
        
    }
    private IEnumerator AlertAnimationOut()
    {
        if (AlertUI.activeSelf)
        {
            AlertUI.transform.GetChild(0).gameObject.transform.LeanScaleY(0.03f, uiAnimationTime);
            yield return new WaitUntil(() => Mathf.Approximately(AlertUI.transform.GetChild(0).gameObject.transform.localScale.y, 0.03f));
            AlertUI.transform.GetChild(0).gameObject.transform.LeanScaleX(0f, uiAnimationTime);
            yield return new WaitUntil(() => Mathf.Approximately(AlertUI.transform.GetChild(0).gameObject.transform.localScale.x, 0f));
            AlertUI.transform.GetChild(0).gameObject.transform.LeanScaleY(0f, uiAnimationTime);
            AlertUI.transform.GetChild(0).gameObject.transform.LeanScaleZ(0f, uiAnimationTime);
            //yield return new WaitUntil(() => Mathf.Approximately(AlertUI.transform.GetChild(0).gameObject.transform.localScale.z, 0f));
            triggerBtn[1].transform.gameObject.SetActive(false);
            LeanTween.value(AlertUI, ImgAlphaUpdate, imgAlpha, 0f, uiAnimationTime);
            yield return new WaitUntil(() => AlertUI.gameObject.GetComponent<Image>().color == defColor);
            AlertUI.SetActive(false);
            TempAlertText = string.Empty;
        }
    }

    private IEnumerator delayAction()
    {
        yield return new WaitForSeconds(uiAnimationTime + 0.1f);
        callAction.Invoke();
    }

    void ImgAlphaUpdate(float alpha)
    {
        var color = AlertUI.gameObject.GetComponent<Image>().color;
        color.a = alpha;
        AlertUI.gameObject.GetComponent<Image>().color = color;
    }

}
