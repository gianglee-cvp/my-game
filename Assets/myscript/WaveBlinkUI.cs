using UnityEngine;
using TMPro;
using System.Collections;

public class WaveBlinkUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text waveText;

    [Header("Settings")]
    public float blinkDuration = 3f;
    public float blinkInterval = 0.3f;

    private void Awake()
    {
        if (waveText != null) waveText.enabled = false;
    }

    public void StartBlink(int waveNumber)
    {
        if (waveText == null) return;
        
        StopAllCoroutines();
        StartCoroutine(BlinkRoutine(waveNumber));
    }

    private IEnumerator BlinkRoutine(int waveNumber)
    {
        waveText.text = "WAVE " + waveNumber;
        waveText.enabled = true;
        
        float elapsed = 0f;
        while (elapsed < blinkDuration)
        {
            waveText.enabled = !waveText.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }
        
        waveText.enabled = false;
    }
}
