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

namespace UltraBeat
{
    [BepInPlugin("UltraBeat.recessive.ultrakill", "UltraBeat", "0.0.1")]
    public class Class1 : BaseUnityPlugin
    {
        public static Class1 Instance { get; private set; }
        public ConductorScript conductor;

        public bool timePaused = false;
        private AudioMixer[] audmix;
        

    private void Awake()
        {
            Instance = this;

            Harmony harmony = new Harmony("UltraBeat.recessive.ultrakill");
            harmony.PatchAll();

            SceneManager.sceneLoaded += SceneLoaded;
            conductor = new ConductorScript(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "config", "UltraBeat"), Logger);
            conductor.onBeat += beat;


            
        }

        AudioSource source;
        bool lastCheck = true;
        void Update()
        {
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

        void beat(int beat, bool[] enabled)
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

            if (enabled[1] && timePaused)
            {
                


                Time.timeScale = TimeController.Instance.timeScale * TimeController.Instance.timeScaleModifier;

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

                timePaused = false;
            }

            
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

            ConductorScript conductor;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(TimeController), "TrueStop")]
            public static bool Prefix(float length, TimeController __instance, float ___currentStop, AudioMixer[] ___audmix)
            {
                

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
                Class1.Instance.timePaused = true;

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