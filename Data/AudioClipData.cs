using BobbysMusicPlayer.Utils;
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
        clipBytes = AudioCache.AudioClipToBytes(originalClip);
        clipChannels = originalClip.channels;
        clipFrequency = originalClip.frequency;
    }

    public AudioClip Get()
    {
        return AudioCache.BytesToAudioClip(clipBytes, clipName, clipChannels, clipFrequency);
    }
}