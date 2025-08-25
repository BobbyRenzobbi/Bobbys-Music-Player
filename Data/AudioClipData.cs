using System;
using UnityEngine;

namespace BobbysMusicPlayer.Data;

public class AudioClipData()
{
    public byte[] clipBytes;
    public string clipName;
    public int clipChannels;
    public int clipFrequency;

    public AudioClipData(AudioClip originalClip) : this()
    {
        clipName = originalClip.name;
        clipBytes = AudioClipToBytes(originalClip);
        clipChannels = originalClip.channels;
        clipFrequency = originalClip.frequency;
    }

    public AudioClip Get()
    {
        return BytesToAudioClip(clipBytes, clipName, clipChannels, clipFrequency);
    }
    
    private byte[] AudioClipToBytes(AudioClip clip)
    {
        if (clip == null)
            return null;
            
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
            
        byte[] bytes = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);

        return bytes;
    }
        
    private AudioClip BytesToAudioClip(byte[] bytes, string name, int channels, int frequency)
    {
        if (bytes == null || bytes.Length == 0)
            return null;
            
        float[] samples = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
            
        int sampleCount = samples.Length / channels;
        AudioClip clip = AudioClip.Create(name, sampleCount, channels, frequency, false);
        clip.SetData(samples, 0);

        return clip;
    }
}