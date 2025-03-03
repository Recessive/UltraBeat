using BepInEx;
using UnityEngine;
using System.IO;
using BepInEx.Logging;
using System.Collections.Generic;

public class ConductorScript
{
    ManualLogSource l;

    public AudioSource music;

    private Dictionary<string, SongMap> Songs;
    SongMap song;

    private float songPosition;
    private float start_dspTime;
    public int beatNumber = 0;
    private float lastBeat;
    private int startBeat;

    bool[] enabledBeats;

    public static event System.Action<int, bool[]> onBeat;


    public ConductorScript(string mapDirectory, BepInEx.Logging.ManualLogSource l)
    {
        this.l = l;

        // Read in songs (ie, iterate over mapping directory
    }

    public void NewSource(AudioSource music)
    {
        this.music = music;
        l.LogInfo("New music: " + music.clip);
        Reset();
    }
    
    // Must be called from within a script with access to Unity Update
    public void Update()
    {
        if (song == null)
        {
            return;
        }

        if(music.time < songPosition) // Song has reset/changed, reload beats
        {
            Reset();
        }

        songPosition = music.time - song.offset;

        if (beatNumber > song.beats[0].Length - 1)
        {
            l.LogError("Beats exceeded mapping length!");
        }

        if (songPosition > lastBeat)
        {
            for (int i = 0; i < song.beats.Length; i++)
            {
                enabledBeats[i] = song.beats[i][beatNumber];
            }


            onBeat?.Invoke(beatNumber, enabledBeats);
            beatNumber++;

            lastBeat += song.Crotchet;
        }
    }

    public float percentageEnabled(int mapIndex)
    {
        float last = song.leftEnabled[mapIndex][Mathf.Max(0, beatNumber - 1)] * song.Crotchet;
        float next = song.rightEnabled[mapIndex][Mathf.Min(song.rightEnabled[mapIndex].Length - 1, beatNumber)] * song.Crotchet;

        return (last >= next) ? 1 : (songPosition - last) / (next - last);
    }

    public float percentageBeat(float nBeats)
    {
        return (songPosition - lastBeat) / (song.Crotchet * nBeats);
    }


    private void Reset()
    {
        l.LogInfo("NEW SONG: " + music.clip.name);
        return;
        if (Songs.ContainsKey(music.clip.name))
        {
            song = Songs[music.clip.name];
        }
        else { song = null; }
        
    }
}


class SongMap
{
    public string clipName;

    public float offset;
    public float bpm;
    static readonly int SECONDS_PER_MIN = 60;
    public float Crotchet
    {
        get
        {
            return SECONDS_PER_MIN / bpm;
        }
    }

    public bool[][] beats;

    public int[][] leftEnabled;
    public int[][] rightEnabled;

    public SongMap(string clipName)
    {
        this.clipName = clipName;
        // Read in song mapping
    }


    int[][] getLeftEnabled()
    {

        // Create a list that has the left most enabled beat at each position in all maps

        List<int[]> res = new List<int[]>();
        for (int i = 0; i < beats.Length; i++)
        {
            List<int> _ = new List<int>();
            // Record the last enabled beat (left most)
            int lastEnabled = 0;
            for (int j = 0; j < beats[i].Length; j++)
            {
                if (beats[i][j]) lastEnabled = j;
                _.Add(lastEnabled);
            }
            res.Add(_.ToArray());
        }

        return res.ToArray();
    }

    int[][] getRightEnabled()
    {

        // Create a list that has the right most enabled beat at each position in all maps

        List<int[]> res = new List<int[]>();

        for (int i = 0; i < beats.Length; i++)
        {
            List<int> _ = new List<int>();
            int lastEnabled = beats[i].Length;

            // Iterate through the list backwards to get the right most
            for (int j = beats[i].Length - 1; j >= 0; j--)
            {
                if (beats[i][j]) lastEnabled = j;
                _.Add(lastEnabled);
            }
            _.Reverse();
            res.Add(_.ToArray());
        }

        return res.ToArray();
    }

}