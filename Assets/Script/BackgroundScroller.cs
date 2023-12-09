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
        _img.uvRect = new Rect(_img.uvRect.position + (new Vector2(_x, _y) * _speed) * Time.deltaTime, _img.uvRect.size);
    }

    public void BGAnimSet(float x, float y, float speed)
    {
        _speed = speed;
        _x = x;
        _y = y;
    }
}
