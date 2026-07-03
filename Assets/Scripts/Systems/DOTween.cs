using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace DG.Tweening
{
    public class DOTween : MonoBehaviour
    {
        private static DOTween instance;
        public static DOTween Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("[DOTween]");
                    instance = go.AddComponent<DOTween>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public static void Init()
        {
            if (instance == null)
            {
                Instance.name = "[DOTween]";
            }
        }
    }

    public static class ShortcutExtensions
    {
        public static Tweener DOFade(this Graphic target, float endValue, float duration)
        {
            DOTween.Instance.StartCoroutine(DOFadeCoroutine(target, endValue, duration));
            return new Tweener();
        }

        public static Tweener DOFade(this CanvasGroup target, float endValue, float duration)
        {
            DOTween.Instance.StartCoroutine(DOFadeCoroutine(target, endValue, duration));
            return new Tweener();
        }

        private static IEnumerator DOFadeCoroutine(Graphic target, float endValue, float duration)
        {
            if (target == null) yield break;
            Color startColor = target.color;
            float startTime = Time.time;
            while (Time.time - startTime < duration)
            {
                if (target == null) yield break;
                float t = (Time.time - startTime) / duration;
                Color c = target.color;
                c.a = Mathf.Lerp(startColor.a, endValue, t);
                target.color = c;
                yield return null;
            }
            if (target != null)
            {
                Color c = target.color;
                c.a = endValue;
                target.color = c;
            }
        }

        private static IEnumerator DOFadeCoroutine(CanvasGroup target, float endValue, float duration)
        {
            if (target == null) yield break;
            float startAlpha = target.alpha;
            float startTime = Time.time;
            while (Time.time - startTime < duration)
            {
                if (target == null) yield break;
                float t = (Time.time - startTime) / duration;
                target.alpha = Mathf.Lerp(startAlpha, endValue, t);
                yield return null;
            }
            if (target != null)
            {
                target.alpha = endValue;
            }
        }
    }

    public class Tweener
    {
        // Simple dummy class for DOTween compatibility
    }
}


