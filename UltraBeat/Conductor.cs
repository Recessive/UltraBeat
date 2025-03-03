using BepInEx;
using UnityEngine;
using System.IO;
using BepInEx.Logging;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

public class ConductorScript
{
    ManualLogSource l;

    public AudioSource music;

    private Dictionary<string, SongMap> Songs = new Dictionary<string, SongMap>();
    SongMap song;

    private float songPosition;
    private float start_dspTime;
    public int beatNumber = 0;
    private float lastBeat;
    private int startBeat;

    bool[] enabledBeats;

    public event System.Action<int, bool[]> onBeat;


    public ConductorScript(string mapDirectory, BepInEx.Logging.ManualLogSource l)
    {
        this.l = l;

        // Read in songs (ie, iterate over mapping directory
        if (!Directory.Exists(mapDirectory))
        {
            l.LogWarning($"Creating map directory: {mapDirectory}");
            Directory.CreateDirectory(mapDirectory);
        }
        string[] files = Directory.GetFiles(mapDirectory, "*.json");
        foreach (string file in files)
        {
            string jsonString = File.ReadAllText(file);

            JObject o1 = JObject.Parse(jsonString);

            string songName = Path.GetFileNameWithoutExtension(file);

            JArray mapsArray = (JArray)o1["maps"];
            bool[][] beats = mapsArray
                .Select(row => row
                .Select(value => value.Value<int>() == 1)  // Convert 1 to true, 0 to false
                .ToArray())
                .ToArray();

            Songs[songName] = new SongMap(songName, beats, (float)o1["offset"], (float)o1["bpm"]);

        }

        l.LogInfo("Successfully loaded " + files.Length + " beat maps.");


    }

    public void NewSource(AudioSource music)
    {
        this.music = music;
        Reset();
    }

    public void Pause()
    {
        l.LogInfo("Paused conductor.");
        this.music = null;
        this.song = null;
    }


    float lastTime = -1f;
    // Must be called from within a script with access to Unity Update
    public void Update()
    {
        if (song == null)
        {
            return;
        }

        if (music.time < lastTime) // Song has reset/changed, reload beats
        {
            Reset();
        }
        lastTime = music.time;


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
        
        if (Songs.ContainsKey(music.clip.name))
        {
            lastBeat = -1;
            beatNumber = 0;
            song = Songs[music.clip.name];
            enabledBeats = new bool[song.beats.Length];
            l.LogInfo("Resetting with song: " + music.clip.name + ", offset: " + song.offset + ", bpm: " + song.bpm);
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

    public SongMap(string clipName, bool[][] beats, float offset, float bpm)
    {
        this.clipName = clipName;
        this.beats = beats;
        this.offset = offset;
        this.bpm = bpm;

        leftEnabled = getLeftEnabled();
        rightEnabled = getRightEnabled();
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