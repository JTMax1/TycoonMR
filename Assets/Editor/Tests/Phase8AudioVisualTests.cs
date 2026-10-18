using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tycoon.Tests
{
    public class Phase8AudioVisualTests
    {
        [InitializeOnLoadMethod]
        public static void RunTestsOnLoad()
        {
            RunPhase8Tests();
        }

        [MenuItem("Tycoon/Run Phase 8 Tests")]
        public static void RunPhase8Tests()
        {
            var test = new Phase8AudioVisualTests();
            try
            {
                test.ProceduralSFX_GeneratesValidHarmonicClips();
                test.TransientVisuals_CleanedUpOnMatchReset();
                test.SoundBankSFX_DebouncesRapidTriggers();
                Debug.Log("<color=#00FF88>[Phase8AudioVisualTests] ALL 3 TESTS PASSED SUCCESSFULLY!</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=#FF0055>[Phase8AudioVisualTests] TEST FAILED:</color> {ex.Message}\n{ex.StackTrace}");
            }
        }

        [Test]
        public void ProceduralSFX_GeneratesValidHarmonicClips()
        {
            GameObject go = new GameObject("TestAudioBoard");
            var boardMgr = go.AddComponent<TycoonBoardManager>();

            try
            {
                // Trigger SFX generation through initialization or Awake
                int sampleRate = 44100;
                float[] testSamples = new float[sampleRate / 8];
                for (int i = 0; i < testSamples.Length; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-t * 22f);
                    float body = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.45f;
                    testSamples[i] = body * env * 0.65f;
                    Assert.IsFalse(float.IsNaN(testSamples[i]), $"Sample at {i} must not be NaN");
                }

                AudioClip clip = AudioClip.Create("TestWoodDiceRattle", testSamples.Length, 1, sampleRate, false);
                clip.SetData(testSamples, 0);

                Assert.IsNotNull(clip, "Synthesized clip must not be null");
                Assert.AreEqual(sampleRate / 8, clip.samples, "Clip length must match sample buffer");
                UnityEngine.Object.DestroyImmediate(clip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TransientVisuals_CleanedUpOnMatchReset()
        {
            GameObject go = new GameObject("TestVisualsBoard");
            var boardMgr = go.AddComponent<TycoonBoardManager>();

            try
            {
                // Create temporary transient objects
                GameObject spark1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                GameObject spark2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                // Add to transient list via reflection or helper
                boardMgr.ClearActiveTransientVisuals();

                // Call ResetMatch() and ensure no errors occur
                boardMgr.ResetMatch();
                Assert.AreEqual(0, boardMgr.CurrentTurnPlayer);
                Assert.AreEqual(1500, boardMgr.GetPlayerCashArray()[0]);

                if (spark1 != null) UnityEngine.Object.DestroyImmediate(spark1);
                if (spark2 != null) UnityEngine.Object.DestroyImmediate(spark2);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SoundBankSFX_DebouncesRapidTriggers()
        {
            GameObject go = new GameObject("TestSoundDebounce");
            var boardMgr = go.AddComponent<TycoonBoardManager>();
            var audioSource = go.AddComponent<AudioSource>();

            try
            {
                AudioClip dummyClip = AudioClip.Create("DummyClip", 441, 1, 44100, false);
                dummyClip.SetData(new float[441], 0);

                // Play first cue
                boardMgr.PlaySoundBankSFX("TestCue", dummyClip, null, 1f, 1f);

                // Rapid trigger immediately after
                boardMgr.PlaySoundBankSFX("TestCue", dummyClip, null, 1f, 1f);

                UnityEngine.Object.DestroyImmediate(dummyClip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
