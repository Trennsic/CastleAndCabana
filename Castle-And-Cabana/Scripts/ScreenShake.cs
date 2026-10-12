using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.1f;
    public float decreaseFactor = 1.0f;

    Vector3 originalPos;

    void Start()
    {
        originalPos = transform.position;
    }

    public void TriggerShake()
    {
        StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-shakeMagnitude, shakeMagnitude);
            float y = Random.Range(-shakeMagnitude, shakeMagnitude);

            transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;

            shakeMagnitude *= decreaseFactor;

            yield return null;
        }

        transform.position = originalPos;
    }
}