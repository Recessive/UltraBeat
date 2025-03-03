using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;
using ULTRAKILL;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

namespace UltraBeat
{
    [BepInPlugin("UltraBeat.recessive.ultrakill", "UltraBeat", "0.0.1")]
    public class Class1 : BaseUnityPlugin
    {
        public static Class1 Instance { get; private set; }
        ConductorScript conductor;

        private void Awake()
        {
            Instance = this;

            Harmony harmony = new Harmony("UltraBeat.recessive.ultrakill");
            harmony.PatchAll();

            SceneManager.sceneLoaded += SceneLoaded;

            conductor = new ConductorScript("", Logger);
        }

        AudioSource source;
        void Update()
        {
            bool sourceNull = (source == null);
            CheckSource();
            if(sourceNull && source != null) // New source
            {
                conductor.NewSource(source);
            }
        }

        void CheckSource()
        {
            // Because I cbf patching events into CustomMusicPlayer and MusicManager I'm just gonna check here to see if either of them has a valid clip
            MusicManager musman = MonoSingleton<MusicManager>.Instance;
            if (musman != null && musman.targetTheme.clip != null)
            {
                source = musman.targetTheme;
                return;
            }

            CustomMusicPlayer cusplay = (CustomMusicPlayer)FindObjectOfType(typeof(CustomMusicPlayer));
            if(cusplay != null && cusplay.source.clip != null)
            {
                source = cusplay.source;
                return;
            }
            source = null;
        }


        void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {

        }

        [HarmonyPatch]
        public class Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TimeController), "TrueStop")]
            public static bool Prefix(float length, TimeController __instance, float ___currentStop, AudioMixer[] ___audmix)
            {

                Class1.Instance.Logger.LogInfo("Audio Clip: " + Class1.Instance.source.clip.name);

                

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
                        array[i].SetFloat("lowPassVolume", 0f);
                    }
                }

                Time.timeScale = 0f;

                __instance.StartCoroutine(TimeIsStopped(length, trueStop: true, __instance, ___currentStop, ___audmix));


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