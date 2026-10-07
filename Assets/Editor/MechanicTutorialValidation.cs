#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MechanicTutorialValidation
{
    [MenuItem("RealBuca/Toy Box 3D/Validate Narrated Mechanic Tutorials")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Validate outside Play Mode.");
        var keys = BuildToyBoxMainMenu.TutorialKeys;
        var saved = new Dictionary<string, int>();
        GameObject test = null;
        int lessonCount = 0;
        try
        {
            foreach (string key in keys)
            {
                string pref = ObstacleIntroController.SeenPreferencePrefix + key +
                    ((key == "MUD" || key == "PEGS") ? "_V3" : "");
                saved[pref] = PlayerPrefs.GetInt(pref, -1);
                PlayerPrefs.DeleteKey(pref);
                string clipName = key == "+PTS" ? "POINTS" : key == "!" ? "HAZARD" : key;
                var clip = Resources.Load<AudioClip>("Audio/TutorialVoice/" + clipName);
                if (clip == null || clip.length < 1 || clip.length > 13.5f)
                    throw new Exception("Missing or invalid narration duration: " + key);
            }
            test = new GameObject("TemporaryTutorialValidation");
            test.hideFlags = HideFlags.HideAndDontSave;
            var intro = test.AddComponent<ObstacleIntroController>();
            var taught = new HashSet<string>();
            bool testedMultiple = false;
            for (int level = 1; level <= 30; level++)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ToyBoxMenu/Levels/Level_{level:00}.prefab");
                if (root == null) throw new Exception("Missing level " + level);
                var lessons = intro.SelectLessonsForLevel(root, level);
                if (lessons.Length > 1) testedMultiple = true;
                foreach (string key in lessons)
                {
                    taught.Add(key);
                    string pref = ObstacleIntroController.SeenPreferencePrefix + key +
                        ((key == "MUD" || key == "PEGS") ? "_V3" : "");
                    PlayerPrefs.SetInt(pref, 1);
                    lessonCount++;
                }
                if (!System.Linq.Enumerable.SequenceEqual(intro.SelectLessonsForLevel(root, level), lessons))
                    throw new Exception("Restart suppressed or changed the tutorial queue.");
            }
            if (!testedMultiple) throw new Exception("Multiple-mechanic path was not tested.");
            // Direct entry must also show the level tutorial.
            foreach (var pref in saved.Keys) PlayerPrefs.DeleteKey(pref);
            var late = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_20.prefab");
            if (intro.SelectLessonsForLevel(late,20).Length == 0) throw new Exception("Direct level entry skipped unseen mechanics.");
            // Existing watched preferences must never suppress any of the lessons.
            var before = intro.SelectLessonsForLevel(late,20);
            string first = before[0];
            PlayerPrefs.SetInt(ObstacleIntroController.SeenPreferencePrefix + first +
                ((first == "MUD" || first == "PEGS") ? "_V3" : ""), 1);
            if (!System.Linq.Enumerable.SequenceEqual(intro.SelectLessonsForLevel(late,20), before))
                throw new Exception("A watched preference suppressed a lesson.");
        }
        finally
        {
            if (test != null) UnityEngine.Object.DestroyImmediate(test);
            foreach (var entry in saved)
                if (entry.Value < 0) PlayerPrefs.DeleteKey(entry.Key); else PlayerPrefs.SetInt(entry.Key, entry.Value);
            PlayerPrefs.Save();
        }
        Debug.Log("NARRATED_TUTORIALS_VALIDATED: 19 voice clips, 30 levels, " + lessonCount +
            " queued lessons, multiple mechanics, repeat entry, direct entry and watched-preference independence. Preferences restored.");
    }
}
#endif
