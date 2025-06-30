using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class UI_ElementAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scaleMultiplier = 1.1f;
    public Color changingColor = Color.white;
    public float changingTime = 0.1f;

    private Vector3 startScale = Vector3.one;
    private Color startColor;
    private Image image;

    private void Awake()
    {
        startScale = transform.localScale;
    }
    private void Start()
    {
        startScale = transform.localScale;
        image = GetComponent<Image>();
        startColor = image.color;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(startScale * scaleMultiplier, changingColor, GetMultiplierDeltaBetweenScales() * changingTime));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(startScale, startColor, (1 - GetMultiplierDeltaBetweenScales()) * changingTime));
    }
    private IEnumerator ScaleTo(Vector3 targetScale, Color targetColor, float time)
    {
        float startTime = Time.time;
        while(Time.time < startTime + time)
        {
            float step = 1 - ((startTime + time - Time.time) / changingTime);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, step);
            image.color = Color.Lerp(image.color, targetColor, step);
            yield return null;
        }
        yield break;
    }
    private float GetMultiplierDeltaBetweenScales()
    {
        float delta = Mathf.Abs(transform.localScale.magnitude - startScale.magnitude);
        float maxDelta = Mathf.Abs((startScale * scaleMultiplier).magnitude - startScale.magnitude);

        return Mathf.Abs(1 - (delta / maxDelta));
    }
    private void OnDisable()
    {
        transform.localScale = startScale;
        if(image != null)
            image.color = startColor;
    }
}
