using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour
{

    BackgroundScroller bgSettings = new BackgroundScroller();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bgSettings.BGAnimatedIsActive(true, 2, 0.05f);
    }

}
