using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProceduralAudio : MonoBehaviour
{
    private AudioSource audioSource;
    private double sampleRate;
    private double phase;
    
    // シンプルなシーケンサー用の音階
    private float[] notes = { 261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 493.88f, 523.25f }; // C4 - C5
    private int[] melody = { 0, 2, 4, 0, 0, 2, 4, 0, 4, 5, 6, 4, 5, 6, 6, 7, 6, 5, 4, 0, 6, 5, 4, 0, 2, 6, 0, 0 };
    private float currentFreq = 0;
    
    private float noteDuration = 0.25f; // 音の長さ
    private float timer = 0f;
    private int melodyIndex = 0;
    private float volume = 0.1f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        
        // AudioSourceで擬似的に鳴らすために無音のダミークリップを設定するか、OnAudioFilterReadを使用
        sampleRate = AudioSettings.outputSampleRate;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= noteDuration)
        {
            timer = 0f;
            int noteIndex = melody[melodyIndex];
            currentFreq = notes[noteIndex % notes.Length];
            melodyIndex = (melodyIndex + 1) % melody.Length;
        }
    }

    // Unityがオーディオバッファに書き込む処理
    void OnAudioFilterRead(float[] data, int channels)
    {
        double increment = currentFreq * 2.0 * Mathf.PI / sampleRate;
        for (int i = 0; i < data.Length; i += channels)
        {
            phase += increment;
            if (phase > 2.0 * Mathf.PI)
            {
                phase -= 2.0 * Mathf.PI;
            }

            // 矩形波 (Square wave) でファミコン風のBGMにする
            float sample = (phase < Mathf.PI) ? 1.0f : -1.0f;
            
            // 音がなめらかに減衰するようにエンベロープをかける
            float t = timer / noteDuration;
            float envelope = Mathf.Max(0, 1.0f - t);
            
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample * volume * envelope;
            }
        }
    }
}
