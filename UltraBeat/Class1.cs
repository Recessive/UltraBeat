using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;
using ULTRAKILL;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UltraBeat
{
    public enum PauseLength
    {
        None,
        Mini,
        Short,
        Long
    }


    [BepInPlugin("UltraBeat.recessive.ultrakill", "UltraBeat", "0.0.1")]
    public class Class1 : BaseUnityPlugin
    {
        public static Class1 Instance { get; private set; }
        public ConductorScript conductor;

        public PauseLength timePause = PauseLength.None;
        private AudioMixer[] audmix;
        

        private void Awake()
        {
            Instance = this;

            Harmony harmony = new Harmony("UltraBeat.recessive.ultrakill");
            harmony.PatchAll();

            SceneManager.sceneLoaded += SceneLoaded;
            string mapDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "config", "UltraBeat");

            if (!Directory.Exists(mapDirectory))
            {
                DownloadMaps(mapDirectory, Logger, InitConductor);
            }
            else
            {
                InitConductor(mapDirectory);
            }
        }

        void InitConductor(string mapDirectory)
        {
            conductor = new ConductorScript(mapDirectory, Logger);
            conductor.onBeat += beat;
        }

        static async Task DownloadMaps(string mapDirectory, ManualLogSource Logger, Action<string> callback)
        {
            Logger.LogWarning("Expected map directory not found!");
            Logger.LogMessage($"Creating map directory: {mapDirectory}");
            Directory.CreateDirectory(mapDirectory);


            string mapUrl = "https://api.github.com/repos/Recessive/UltraBeat/contents/maps";
            Logger.LogMessage("Downloading beatmaps from https://github.com/Recessive/UltraBeat/tree/master/maps");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "C# App");

            try
            {
                // Get repository contents
                var response = await client.GetStringAsync(mapUrl);
                var files = JArray.Parse(response);

                // Download each file
                foreach (var file in files)
                {
                    if (file["type"].ToString() == "file")
                    {
                        string fileName = file["name"].ToString();
                        string downloadUrl = file["download_url"].ToString();
                        string outputPath = Path.Combine(mapDirectory, fileName);

                        byte[] fileData = await client.GetByteArrayAsync(downloadUrl);
                        await File.WriteAllBytesAsync(outputPath, fileData);
                        Logger.LogMessage($"Downloaded: {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}");
            }
            Logger.LogMessage("Successfully downloaded all maps!");
            callback(mapDirectory);
        }


        AudioSource source;
        bool lastCheck = true;
        void Update()
        {
            if (conductor == null) return;
            bool check = CheckSource();
            if(check && !lastCheck) // New source
            {
                conductor.NewSource(source);
            }
            if(!check && lastCheck) // Lost source
            {
                conductor.Pause();
            }
            else
            {
                conductor.Update();
            }

            lastCheck = check;
            
        }

        void beat(int beat, Dictionary<string, bool> enabled)
        {
            /*if (enabled[1])
            {
                Revolver rev = (Revolver)FindObjectOfType(typeof(Revolver));

                if (rev != null)
                {
                    Instantiate(rev.superGunSound);
                }
            }*/
            // Logger.LogInfo(beat);

            if (enabled["fast"])
            {
                Revolver rev = (Revolver)FindObjectOfType(typeof(Revolver));
                if(rev != null && !rev.altVersion)
                {
                    FieldInfo shootReadyInfo = AccessTools.Field(typeof(Revolver), "shootReady");
                    FieldInfo gunReadyInfo = AccessTools.Field(typeof(Revolver), "gunReady");
                    shootReadyInfo.SetValue(rev, true);
                    gunReadyInfo.SetValue(rev, true);
                }
            }

            if (enabled["fast"] && timePause == PauseLength.Mini)
            {
                Unfreeze();
                timePause = PauseLength.None;
            }

            if (enabled["freeze_short"] && timePause == PauseLength.Short)
            {
                BigUnfreeze();
                timePause = PauseLength.None;
            }

            if (enabled["freeze_long"] && timePause == PauseLength.Long)
            {
                BigUnfreeze();
                timePause = PauseLength.None;
            }


        }

        void BigUnfreeze()
        {
            AudioMixer[] audmix = new AudioMixer[4]
            {
                    MonoSingleton<AudioMixerController>.Instance.allSound,
                    MonoSingleton<AudioMixerController>.Instance.goreSound,
                    MonoSingleton<AudioMixerController>.Instance.musicSound,
                    MonoSingleton<AudioMixerController>.Instance.doorSound
            };


            for (int i = 0; i < audmix.Length; i++)
            {
                audmix[i].SetFloat("lowPassVolume", -80f);
            }

            Unfreeze();
        }

        void Unfreeze()
        {
            Time.timeScale = TimeController.Instance.timeScale * TimeController.Instance.timeScaleModifier;
        }

        bool CheckSource()
        {
            // Because I cbf patching events into CustomMusicPlayer and MusicManager I'm just gonna check here to see if either of them has a valid clip

            MusicManager musman = MonoSingleton<MusicManager>.Instance;
            if (musman != null && musman.targetTheme.clip != null)
            {
                source = musman.targetTheme;
                return true;
            }

            CustomMusicPlayer cusplay = (CustomMusicPlayer)FindObjectOfType(typeof(CustomMusicPlayer));
            if(cusplay != null && cusplay.source.clip != null)
            {
                source = cusplay.source;
                return true;
            }
            source = null;
            return false;
        }


        void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {

        }

        [HarmonyPatch]
        public class Patch
        {
            // Hit stop length values:
            // 0.005
            // 0.05
            // 0.1
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TimeController), "HitStop")]
            public static bool HitStopPrefix(float length)
            {
                return true;
                // Class1.Instance.Logger.LogInfo("HitStop length: " + length);
                PauseLength currentPause = Class1.Instance.timePause;
                if (currentPause == PauseLength.Short || currentPause == PauseLength.Long)
                {
                    return false;
                }
                Time.timeScale = 0f;
                Class1.Instance.timePause = PauseLength.Mini;

                /*if (length > currentStop)
                {
                    currentStop = length;
                    Time.timeScale = 0f;
                    StartCoroutine(TimeIsStopped(length, trueStop: false));
                }*/
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(TimeController), "TrueStop")]
            public static bool Prefix(float length, TimeController __instance, float ___currentStop, AudioMixer[] ___audmix)
            {
                if (!Class1.Instance.conductor.active) return true;

                if (!(length > ___currentStop))
                {
                    return false;
                }

                ___currentStop = length;
                if (__instance.controlPitch)
                {
                    AudioMixer[] array = ___audmix;
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].SetFloat("lowPassVolume", 0f);
                    }
                }

                Time.timeScale = 0f;


                // __instance.StartCoroutine(TimeIsStopped(length, trueStop: true, __instance, ___currentStop, ___audmix));

                if(length > 0.25f)
                {
                    Class1.Instance.timePause = PauseLength.Long;
                }
                else
                {
                    Class1.Instance.timePause = PauseLength.Short;
                }
                

                return false;
            }


            private static IEnumerator TimeIsStopped(float length, bool trueStop, TimeController __instance, float ___currentStop, AudioMixer[] ___audmix)
            {
                yield return new WaitForSecondsRealtime(length);
                ContinueTime(length, trueStop, __instance, ___currentStop, ___audmix);
            }

            private static void ContinueTime(float length, bool trueStop, TimeController __instance, float ___currentStop, AudioMixer[] ___audmix)
            {
                if (!(length >= ___currentStop))
                {
                    return;
                }

                Time.timeScale = __instance.timeScale * __instance.timeScaleModifier;
                if (trueStop && __instance.controlPitch)
                {

                    AudioMixer[] array = ___audmix;
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].SetFloat("lowPassVolume", -80f);
                    }
                }

                ___currentStop = 0f;
            }
        }
    }
}