﻿using BepInEx;
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

    public bool active = false;

    Dictionary<string, bool> enabledBeats = new Dictionary<string, bool>();

    public event System.Action<int, Dictionary<string, bool>> onBeat;


    public ConductorScript(string mapDirectory, BepInEx.Logging.ManualLogSource l)
    {
        this.l = l;

        // Read in songs (ie, iterate over mapping directory
        string[] files = Directory.GetFiles(mapDirectory, "*.json");
        foreach (string file in files)
        {
            string jsonString = File.ReadAllText(file);

            JObject o1 = JObject.Parse(jsonString);

            string songName = Path.GetFileNameWithoutExtension(file);

            JObject mapsObject = (JObject)o1["maps"];
            Dictionary<string, bool[]> mapsDict = mapsObject
                .Properties()
                .ToDictionary(
                prop => prop.Name,
                prop => prop.Value
                .Select(value => value.Value<int>() == 1)  // Convert 1 to true, 0 to false
                .ToArray()
            );




            Songs[songName] = new SongMap(songName, mapsDict, (float)o1["offset"], (float)o1["bpm"]);

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
        active = false;
    }


    float lastTime = -1f;
    // Must be called from within a script with access to Unity Update
    public void Update()
    {

        if (music == null)
        {
            return;
        }

        if (music.time < lastTime && music.time < 1f) // Song has reset/changed, reload beats | ONLY DO SO IF SONG IS AT BEGINNING
            // This is because of bugs involved with Ultrakill switching AudioSources and the song briefly losing time
        {
            Reset();
        }
        lastTime = music.time;

        if (song == null)
        {
            return;
        }


        songPosition = music.time - song.offset;

        if (beatNumber > song.beats.Values.First().Length - 1)
        {
            l.LogError("Beats exceeded mapping length!");
        }

        if (songPosition > lastBeat)
        {
            foreach (KeyValuePair<string, bool[]> entry in song.beats)
            {
                enabledBeats[entry.Key] = entry.Value[beatNumber];
            }


            onBeat?.Invoke(beatNumber, enabledBeats);
            beatNumber++;

            lastBeat += song.Crotchet;
        }
    }

    public float percentageEnabled(string mapKey)
    {
        float last = song.leftEnabled[mapKey][Mathf.Max(0, beatNumber - 1)] * song.Crotchet;
        float next = song.rightEnabled[mapKey][Mathf.Min(song.rightEnabled[mapKey].Length - 1, beatNumber)] * song.Crotchet;

        return (last >= next) ? 1 : (songPosition - last) / (next - last);
    }

    public float percentageBeat(float nBeats)
    {
        return (songPosition - lastBeat) / (song.Crotchet * nBeats);
    }


    private void Reset()
    {
        l.LogInfo("Clip name: " + music.clip.name);
        if (Songs.ContainsKey(music.clip.name))
        {
            lastBeat = -1;
            beatNumber = 0;
            song = Songs[music.clip.name];
            enabledBeats.Clear();
            l.LogInfo("Resetting with song: " + music.clip.name + ", offset: " + song.offset + ", bpm: " + song.bpm);
            active = true;
        }
        else { 
            song = null;
            active = false;
        }
        
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

    public Dictionary<string, bool[]> beats;

    public Dictionary<string, int[]> leftEnabled;
    public Dictionary<string, int[]> rightEnabled;

    public SongMap(string clipName, Dictionary<string, bool[]> beats, float offset, float bpm)
    {
        this.clipName = clipName;
        this.beats = beats;
        this.offset = offset;
        this.bpm = bpm;

        if (!beatLengthsMatch(beats))
        {
            Debug.LogWarning("Beat maps on " + clipName + " mapping are different lengths! This may cause unexpected results");
        }

        leftEnabled = getLeftEnabled();
        rightEnabled = getRightEnabled();
    }

    bool beatLengthsMatch(Dictionary<string, bool[]> beats)
    {
        int prevLength = beats.Values.First().Length;
        foreach (KeyValuePair<string, bool[]> entry in this.beats)
        {
            if(entry.Value.Length != prevLength)
            {
                return false;
            }
        }
        return true;

    }


    Dictionary<string, int[]> getLeftEnabled()
    {

        // Create a dictionary that has the left most enabled beat at each position in all maps
        Dictionary<string, int[]> res = new Dictionary<string, int[]>();
        foreach (KeyValuePair<string, bool[]> entry in this.beats)
        {
            List<int> _ = new List<int>();
            // Record the last enabled beat (left most)
            int lastEnabled = 0;
            for (int j = 0; j < entry.Value.Length; j++)
            {
                if (entry.Value[j]) lastEnabled = j;
                _.Add(lastEnabled);
            }
            res[entry.Key] = _.ToArray();
        }

        return res;
    }

    Dictionary<string, int[]> getRightEnabled()
    {

        // Create a dictionary that has the right most enabled beat at each position in all maps
        Dictionary<string, int[]> res = new Dictionary<string, int[]>();

        foreach (KeyValuePair<string, bool[]> entry in this.beats)
        {
            List<int> _ = new List<int>();
            int lastEnabled = entry.Value.Length;

            // Iterate through the list backwards to get the right most
            for (int j = entry.Value.Length - 1; j >= 0; j--)
            {
                if (entry.Value[j]) lastEnabled = j;
                _.Add(lastEnabled);
            }
            _.Reverse();
            res[entry.Key] = _.ToArray();
        }

        return res;
    }

}