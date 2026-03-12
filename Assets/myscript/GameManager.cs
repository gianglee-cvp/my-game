using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Wave UI Settings")]
    public TMP_Text waveText;
    public float blinkDuration = 3f;
    public float blinkInterval = 0.3f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (waveText != null) waveText.enabled = false;
    }

    public void ShowWaveAnnouncement(int waveNumber)
    {
        if (waveText == null) return;

        StopAllCoroutines();
        StartCoroutine(BlinkRoutine(waveNumber));
    }

    private IEnumerator BlinkRoutine(int waveNumber)
    {
        waveText.text = "WAVE " + waveNumber;
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
