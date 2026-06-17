using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AnimatedMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float clickBounceScale = 1.12f;
    [SerializeField] private float hoverDuration = 0.12f;
    [SerializeField] private float clickStepDuration = 0.08f;
    [SerializeField] private bool useUnscaledTime = true;

    private Button button;
    private Vector3 baseScale;
    private Coroutine animationRoutine;
    private bool isHovered;

    public static AnimatedMenuButton Ensure(Button sourceButton)
    {
        if (sourceButton == null)
        {
            return null;
        }

        AnimatedMenuButton animator = sourceButton.GetComponent<AnimatedMenuButton>();
        if (animator == null)
        {
            animator = sourceButton.gameObject.AddComponent<AnimatedMenuButton>();
        }

        return animator;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        if (target == null)
        {
            target = transform as RectTransform;
        }

        CacheBaseScale();
    }

    private void OnEnable()
    {
        CacheBaseScale();
        if (target != null)
        {
            target.localScale = baseScale;
        }

        isHovered = false;
    }

    private void OnDisable()
    {
        StopAnimation();
        if (target != null)
        {
            target.localScale = baseScale;
        }

        isHovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanAnimate())
        {
            return;
        }

        isHovered = true;
        AnimateTo(baseScale * hoverScale, hoverDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanAnimate())
        {
            return;
        }

        isHovered = false;
        AnimateTo(baseScale, hoverDuration);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate())
        {
            return;
        }

        AnimateTo(baseScale * pressedScale, clickStepDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PlayClickBounce();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickBounce();
    }

    private void PlayClickBounce()
    {
        if (!CanAnimate())
        {
            return;
        }

        StopAnimation();
        animationRoutine = StartCoroutine(ClickBounceRoutine());
    }

    private IEnumerator ClickBounceRoutine()
    {
        yield return ScaleTo(baseScale * pressedScale, clickStepDuration);
        yield return ScaleTo(baseScale * clickBounceScale, clickStepDuration);
        yield return ScaleTo(isHovered ? baseScale * hoverScale : baseScale, hoverDuration);
        animationRoutine = null;
    }

    private void AnimateTo(Vector3 targetScale, float duration)
    {
        StopAnimation();
        animationRoutine = StartCoroutine(ScaleTo(targetScale, duration));
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 startScale = target.localScale;
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            target.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        target.localScale = targetScale;
    }

    private bool CanAnimate()
    {
        return target != null && (button == null || button.IsInteractable());
    }

    private void CacheBaseScale()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        if (target != null)
        {
            baseScale = target.localScale;
        }
    }

    private void StopAnimation()
    {
        if (animationRoutine == null)
        {
            return;
        }

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }
}
