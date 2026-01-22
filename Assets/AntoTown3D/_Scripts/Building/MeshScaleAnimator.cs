using System.Collections;
using UnityEngine;

public class MeshScaleAnimator : MonoBehaviour
{
    public float duration = 0.35f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public IEnumerator Show(GameObject mesh)
    {
        if (mesh == null) yield break;

        mesh.SetActive(true);

        Transform t = mesh.transform;
        Vector3 targetScale = t.localScale;
        t.localScale = new Vector3(targetScale.x, 0f, targetScale.z);

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float v = curve.Evaluate(time / duration);
            t.localScale = new Vector3(
                targetScale.x,
                Mathf.Lerp(0f, targetScale.y, v),
                targetScale.z
            );
            yield return null;
        }

        t.localScale = targetScale;
    }

    public IEnumerator Hide(GameObject mesh)
    {
        if (mesh == null) yield break;

        Transform t = mesh.transform;
        Vector3 startScale = t.localScale;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float v = curve.Evaluate(time / duration);
            t.localScale = new Vector3(
                startScale.x,
                Mathf.Lerp(startScale.y, 0f, v),
                startScale.z
            );
            yield return null;
        }

        t.localScale = new Vector3(startScale.x, 0f, startScale.z);
        mesh.SetActive(false);
    }
}
