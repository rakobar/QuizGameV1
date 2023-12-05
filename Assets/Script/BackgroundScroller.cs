using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private RawImage _img;
    private float _speed;
    private float _x, _y;

    private void Update()
    {
        BGAnimatedIsActive(true, 5, 0.05f);
    }

    public void BGAnimatedIsActive(bool anim, int pos, float speed)
    {
         //directions
        _speed = speed;

        if (anim == true)
        {
            if (pos == 0) //non directional
            {
                _x = 0f;
                _y = 0f;

            }
            else if (pos == 1) 
            {
                _x = 1f;
                _y = 0f;
            }
            else if (pos == 2)
            {
                _x = 0f;
                _y = 1f;
            }
            else if (pos == 3)
            {
                _x = -1f;
                _y = 0f;
            }
            else if (pos == 4)
            {
                _x = 0f;
                _y = -1f;
            }
            else if(pos == 5)
            {
                _x = 1f;
                _y = 1f;
            }
            else if (pos == 6)
            {
                _x = -1f;
                _y = 1f;
            }
            else if (pos == 7)
            {
                _x = 1f;
                _y = -1f;
            }
            else if (pos == 8)
            {
                _x = -1f;
                _y = -1f;
            }

            _img.uvRect = new Rect(_img.uvRect.position + (new Vector2(_x, _y) * _speed) * Time.deltaTime, _img.uvRect.size);
        }
        else
        {
            Debug.Log("BG Scroller is inactive!");
        }
    }
}
