using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BlackUI : MonoBehaviour
{
    public static BlackUI instance { get; private set; }

    private Image img;

    private void Awake()
    {
        instance = this;
        img = GetComponent<Image>();
        gameObject.SetActive(false);
        img.color = new Color(0, 0, 0, 0);
    }

    public void DoFade(float duration, float wait, Action callback)
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        img.DOFade(1, duration)
            .OnComplete(() =>
            {
                callback.Invoke();

                img.DOFade(0, duration)
                    .SetDelay(wait)
                    .OnComplete(() => { gameObject.SetActive(false); });
            });
    }
}