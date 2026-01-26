using UnityEngine;

public class BPMPulse : MonoBehaviour
{
    public bool isTurnOn = true;

    [Header("BPM")]
    [SerializeField] private float bpm = 120f;

    [Header("Pulse")]
    [SerializeField] private float pulseSize = 1.5f;
    [SerializeField] private float returnSpeed = 10f;

    private Vector3 startSize;
    private float beatInterval;
    private float timer;

    private void Start()
    {
        startSize = transform.localScale;
        beatInterval = 60f / bpm;
    }

    private void Update()
    {
        if (!isTurnOn) return;

        timer += Time.deltaTime;
        if (timer >= beatInterval)
        {
            timer -= beatInterval;
            Pulse();
        }
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            startSize,
            Time.deltaTime * returnSpeed
        );
    }

    private void Pulse()
    {
        transform.localScale = startSize * pulseSize;
    }
}