using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicit batch-only regression: press Play from an Untitled scene, wait for
/// the real menu to tick, and use its Play action to enter the existing game.
/// SessionState keeps the test alive across Unity's normal domain reloads.
/// </summary>
public static class ToyBoxStartupValidation
{
    const string PhaseKey = "ToyBox.StartupTest.Phase";
    const string StartKey = "ToyBox.StartupTest.Start";
    const string ErrorKey = "ToyBox.StartupTest.Error";
    static readonly string[] ProgressKeys = { "BucaCurrentLevel", LevelSelectController.PendingLevelKey, LevelSelectController.HighestUnlockedKey, BucaSkinBinding.PreferenceKey };

    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this regression in a disposable batch-mode project copy.");
        VerifyLuxoddArcadeInput.Run();
        BootFromMainMenu.SetEnabled(true);
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (!BootFromMainMenu.TryOpenMainMenuIfEmpty() || SceneManager.GetActiveScene().path != "Assets/Scenes/MainMenu.unity")
            throw new Exception("Untouched empty editor scene did not recover to MainMenu.");
        if (BootFromMainMenu.TryOpenMainMenuIfEmpty()) throw new Exception("Saved scene was replaced.");
        var unsaved = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        new GameObject("UnsavedUserWork"); EditorSceneManager.MarkSceneDirty(unsaved);
        if (BootFromMainMenu.TryOpenMainMenuIfEmpty() || GameObject.Find("UnsavedUserWork") == null)
            throw new Exception("Unsaved editor work was not preserved.");
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BootFromMainMenu.SetEnabled(false);
        if (BootFromMainMenu.TryOpenMainMenuIfEmpty()) throw new Exception("Explicit boot opt-out ignored.");
        BootFromMainMenu.SetEnabled(true);
        Debug.Log("BUCA_EMPTY_EDITOR_RECOVERY_PASSED: opens menu; preserves saved scenes, unsaved work and opt-out.");
        RebalanceLearningCampaign.Validate();
        ToyBoxSkinValidation.CheckCampaign();
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BootFromMainMenu.SetEnabled(true);
        if (EditorSceneManager.playModeStartScene == null ||
            AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) != "Assets/Scenes/MainMenu.unity")
            throw new Exception("Play is not configured to start MainMenu.");
        foreach (string key in ProgressKeys)
        {
            SessionState.SetBool(PhaseKey + key + ".exists", PlayerPrefs.HasKey(key));
            SessionState.SetInt(PhaseKey + key, PlayerPrefs.GetInt(key));
        }
        SessionState.SetString(ErrorKey, "");
        SessionState.SetString(PhaseKey, "menu");
        SessionState.SetFloat(StartKey, (float)EditorApplication.timeSinceStartup);
        ResumeAfterReload();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!Application.isBatchMode || string.IsNullOrEmpty(SessionState.GetString(PhaseKey, ""))) return;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= RecordException;
        Application.logMessageReceived += RecordException;
    }

    static void RecordException(string message, string stack, LogType type)
    {
        if (type != LogType.Exception) return;
        // Unity 6.5 can throw from its delayed Search index initialization in
        // batch mode. Keep that editor diagnostic separate from game failures.
        if (stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("Assets/"))
        {
            Debug.LogWarning("TOYBOX_STARTUP_EDITOR_DIAGNOSTIC: Unity Search indexing failed; continuing the gameplay regression.");
            return;
        }
        SessionState.SetString(ErrorKey, message + "\n" + stack);
    }

    static void RestoreProgress()
    {
        foreach (string key in ProgressKeys)
        {
            if (SessionState.GetBool(PhaseKey + key + ".exists", false))
                PlayerPrefs.SetInt(key, SessionState.GetInt(PhaseKey + key, 0));
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
    }

    static void ClickButton(ToyBoxMenuController menu, ToyBoxMenuController.PuckButton button)
    {
        Physics.SyncTransforms();
        Vector3 pointer = menu.ActiveCamera.WorldToScreenPoint(button.hit.bounds.center);
        Ray ray = menu.ActiveCamera.ScreenPointToRay(pointer);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f) || hit.collider != button.hit)
            throw new Exception("Button ray is blocked: " + button.cap.parent.name + "; hit=" + (hit.collider == null ? "nothing" : hit.collider.name));
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = typeof(ToyBoxMenuController);
        bool clicked = (bool)type.GetMethod("HandlePointer", flags).Invoke(menu, new object[] { pointer, true });
        if (!clicked) throw new Exception("Pointer click was not accepted: " + button.cap.parent.name);
    }

    static void CheckLevels(ToyBoxMenuController menu)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = typeof(ToyBoxMenuController);
        type.GetMethod("Navigate", flags).Invoke(menu, new object[] { Vector2.right });
        if (!menu.homeButtons[1].selectedRing.activeInHierarchy || menu.homeButtons[0].selectedRing.activeInHierarchy)
            throw new Exception("LEVELS selector did not follow keyboard navigation immediately.");
        ClickButton(menu, menu.homeButtons[1]);
        if (!menu.levelsRoot.activeInHierarchy || menu.homeRoot.activeInHierarchy)
            throw new Exception("Clicking LEVELS did not display the saved level tray.");
        if (!menu.levelButtons[0].selectedRing.activeInHierarchy)
            throw new Exception("Level tray opened without a visible initial selector.");
        if(menu.mapCamera==null || !menu.mapCamera.gameObject.activeInHierarchy || menu.menuCamera.gameObject.activeInHierarchy)
            throw new Exception("Chapter map camera is not isolated.");
        float mapCountdown=(float)type.GetField("_remaining",flags).GetValue(menu);
        type.GetMethod("Update",flags).Invoke(menu,null);
        if((float)type.GetField("_remaining",flags).GetValue(menu)!=mapCountdown) throw new Exception("Map browsing countdown did not pause.");
        if(menu.mapClockSegments != null && menu.mapClockSegments.Length == 14)
        {
            var clock=type.GetMethod("SetMapClock",flags);
            int[] masks={63,6,91,79,102,109,125,7,127,111};
            for(int second=0;second<=30;second++)
            {
                clock.Invoke(menu,new object[]{second});
                for(int digit=0;digit<2;digit++) for(int segment=0;segment<7;segment++)
                    if(menu.mapClockSegments[digit*7+segment].activeSelf != ((masks[digit==0?second/10:second%10] & (1<<segment))!=0))
                        throw new Exception("Train timer digit incorrect at "+second);
            }
            type.GetField("_mapRemaining",flags).SetValue(menu,30f);
            type.GetMethod("TickMapCountdown",flags).Invoke(menu,new object[]{1f});
            if((float)type.GetField("_mapRemaining",flags).GetValue(menu)!=29f) throw new Exception("Train countdown did not advance.");
            var oldAspect=menu.mapCamera.aspect;var oldPosition=menu.mapCamera.transform.position;
            menu.mapCamera.aspect=16f/9f;menu.FitMapCamera(16f/9f);
            var timerCenter=menu.mapCamera.WorldToViewportPoint(menu.levelsRoot.transform.Find("SelectionTimer").position);
            if(Mathf.Abs(timerCenter.x-.5f)>.01f || Mathf.Abs(timerCenter.y-.5f)>.01f)
                throw new Exception("Train timer is not centered in the viewport.");
            ToyBoxMenuValidation.RenderCamera(menu.mapCamera,"Train-Level-Selector",1920,1080);
            menu.mapCamera.aspect=oldAspect;menu.mapCamera.transform.position=oldPosition;
            Debug.Log("TRAIN_TIMER_PASSED: all thirty-one digit states and countdown progression.");
        }
        ToyBoxMenuValidation.RenderCamera(menu.mapCamera,"Chapter-Islands",1600,1000);
        foreach (var button in menu.levelButtons)
        {
            if (!button.hit.enabled || !button.hit.gameObject.activeInHierarchy || !button.face.enabled)
                throw new Exception("Level button is inactive: " + button.cap.parent.name);
            Vector3 pointer = menu.ActiveCamera.WorldToScreenPoint(button.hit.bounds.center);
            type.GetMethod("HandlePointer", flags).Invoke(menu, new object[] { pointer, false });
            if (!button.selectedRing.activeInHierarchy)
                throw new Exception("Hover selector failed: " + button.cap.parent.name);
            if (button.selectedRing.transform.Find("SelectionDiamond") == null)
                throw new Exception("The baked selection marker is missing: " + button.cap.parent.name);
        }
        // Every island and BACK must be reachable with directional input.
        var visited=new System.Collections.Generic.HashSet<int>{0};
        var queue=new System.Collections.Generic.Queue<int>(); queue.Enqueue(0);
        while(queue.Count>0)
        {
            int current=queue.Dequeue();
            foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
            {
                type.GetMethod("SetFocus",flags).Invoke(menu,new object[]{current});
                type.GetMethod("Navigate",flags).Invoke(menu,new object[]{direction});
                int next=(int)type.GetField("_focus",flags).GetValue(menu);
                if(visited.Add(next)) queue.Enqueue(next);
            }
        }
        if(visited.Count!=31) throw new Exception("Unreachable chapter map controls: "+(31-visited.Count));
        for(int i=Mathf.Clamp(PlayerPrefs.GetInt(LevelSelectController.HighestUnlockedKey,0),0,29)+1;i<30;i++)
        {
            ClickButton(menu,menu.levelButtons[i]);
            if((bool)type.GetField("_loading",flags).GetValue(menu)) throw new Exception("Locked island level started.");
        }
        menu.mapCamera.aspect=.75f; menu.FitMapCamera(.75f);
        foreach(var button in menu.levelButtons)
        {
            Vector3 p=menu.mapCamera.WorldToViewportPoint(button.hit.bounds.center);
            if(p.x<=0 || p.x>=1 || p.y<=0 || p.y>=1) throw new Exception("Portrait map button clipped.");
        }
        ToyBoxMenuValidation.RenderCamera(menu.mapCamera,"Chapter-Islands-Portrait",900,1200);
        menu.mapCamera.aspect=1.6f; menu.FitMapCamera(1.6f);
        ClickButton(menu, menu.backButton);
        if (!menu.homeRoot.activeInHierarchy || menu.levelsRoot.activeInHierarchy)
            throw new Exception("Level tray BACK did not return to home.");
        Debug.Log("TOYBOX_STARTUP_LEVELS_PASSED: five 3D islands, 30 visible controls, all 31 controls keyboard-reachable, locked clicks rejected, portrait framing, hover selectors and BACK.");
    }

    static void CheckSkins(ToyBoxMenuController menu)
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var type=typeof(ToyBoxMenuController);
        ClickButton(menu,menu.homeButtons[2]);
        if(!menu.skinsRoot.activeInHierarchy) throw new Exception("SKINS did not open.");
        if(menu.paintWorkshop!=null)
        {
            var w=menu.paintWorkshop;int equipped=BucaSkinBinding.Selected;
            for(int i=0;i<5;i++){w.Focus(i);foreach(var binding in new[]{w.board,w.puck})foreach(var surface in binding.surfaces)if(surface.target.sharedMaterial!=surface.variants[i])throw new Exception("Workshop variant mismatch");if(BucaSkinBinding.Selected!=equipped)throw new Exception("Preview saved too early");}
            w.Focus(0);for(int i=1;i<5;i++){w.Move(Vector2.right);if(w.FocusIndex!=i)throw new Exception("Workshop joystick selection failed");}
            w.Move(Vector2.down);if(w.FocusIndex!=6)throw new Exception("Use Skin unreachable");w.Activate();if(BucaSkinBinding.Selected!=4||!menu.homeRoot.activeInHierarchy)throw new Exception("Equip failed");
            ClickButton(menu,menu.homeButtons[2]);if(w.PreviewIndex!=4)throw new Exception("Equipped preview not restored");
            w.Focus(0);w.Move(Vector2.down);if(w.FocusIndex!=5)throw new Exception("Back unreachable");w.Activate();if(BucaSkinBinding.Selected!=4)throw new Exception("Cancel changed equipped skin");
            Debug.Log("TOYBOX_SKINS_SELECTOR_PASSED: workshop five variants, preview without saving, joystick navigation, Use Skin persistence and Back cancellation.");return;
        }
        for(int i=0;i<5;i++)
        {
            ClickButton(menu,menu.skinButtons[i]);
            if(BucaSkinBinding.Selected!=i || !menu.previewRoot.activeInHierarchy || !menu.previewTitles[i].activeInHierarchy)
                throw new Exception("Large preview did not open: "+i);
            foreach(var binding in new[]{menu.previewBoard,menu.previewPuck})
                foreach(var surface in binding.surfaces)
                    if(surface.target.sharedMaterial!=surface.variants[i]) throw new Exception("Preview material mismatch.");
            if(menu.menuCamera.gameObject.activeInHierarchy) throw new Exception("Two cameras active.");
            ToyBoxMenuValidation.RenderCamera(menu.previewCamera,"Skin-Preview-"+(i+1),1600,1000);
            float remaining=(float)type.GetField("_remaining",flags).GetValue(menu);
            type.GetMethod("Update",flags).Invoke(menu,null);
            if((float)type.GetField("_remaining",flags).GetValue(menu)!=remaining) throw new Exception("Preview countdown running.");
            ClickButton(menu,menu.previewButtons[1]);
            if(BucaSkinBinding.Selected!=(i+1)%5) throw new Exception("Next preview failed.");
            ClickButton(menu,menu.previewButtons[0]);
            if(BucaSkinBinding.Selected!=i) throw new Exception("Previous preview failed.");
            ClickButton(menu,menu.previewButtons[2]);
            if(!menu.skinsRoot.activeInHierarchy || !menu.equippedMarkers[i].activeInHierarchy)
                throw new Exception("Preview BACK failed.");
        }
        type.GetMethod("Navigate",flags).Invoke(menu,new object[]{Vector2.right});
        if(!menu.skinsBackButton.selectedRing.activeInHierarchy) throw new Exception("Skin keyboard BACK selector failed.");
        ClickButton(menu,menu.skinsBackButton);
        ClickButton(menu,menu.homeButtons[2]);
        if(!menu.skinButtons[4].selectedRing.activeInHierarchy || !menu.equippedMarkers[4].activeInHierarchy)
            throw new Exception("Saved skin not restored on reopen.");
        float before=(float)type.GetField("_remaining",flags).GetValue(menu);
        type.GetMethod("Update",flags).Invoke(menu,null);
        if((float)type.GetField("_remaining",flags).GetValue(menu)!=before) throw new Exception("Countdown runs in Skins.");
        ClickButton(menu,menu.skinsBackButton);
        Debug.Log("TOYBOX_SKINS_SELECTOR_PASSED: five full-size 3D previews, material matches, Next/Previous wrap, Back, paused preview timer, equipped state, keyboard, reopen and paused countdown.");
    }

    static void CheckSkinBinding(BucaSkinBinding binding)
    {
        if(binding==null || binding.surfaces.Length==0) throw new Exception("Gameplay skin binding missing.");
        foreach(var surface in binding.surfaces)
            if(surface.target.sharedMaterial!=surface.variants[4]) throw new Exception("Gameplay skin did not persist: "+surface.target.name);
    }

    static void Tick()
    {
        string phase = SessionState.GetString(PhaseKey, "");
        if (phase == "") return;
        try
        {
            string error = SessionState.GetString(ErrorKey, "");
            if (error != "") throw new Exception(error);
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartKey, 0) > 90)
                throw new Exception("Timed out during " + phase + "; active scene=" + SceneManager.GetActiveScene().name);

            if (phase == "menu" && EditorApplication.isPlaying)
            {
                if (SceneManager.GetActiveScene().name != "MainMenu") return;
                var menu = UnityEngine.Object.FindAnyObjectByType<ToyBoxMenuController>();
                if (menu == null) throw new Exception("MainMenu loaded without the Toy Box prefab.");
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(ToyBoxMenuController);
                float remaining = (float)type.GetField("_remaining", flags).GetValue(menu);
                if (remaining > 29.5f) return;
                if (!menu.menuCamera.isActiveAndEnabled || !menu.homeRoot.activeInHierarchy)
                    throw new Exception("Menu has no visible camera or home controls.");
                if (UnityEngine.Object.FindAnyObjectByType<LuxoddGameBridge>() == null || AudioManager.Instance == null)
                    throw new Exception("Original menu services did not survive installation.");
                Debug.Log("TOYBOX_STARTUP_MENU_PASSED: Untitled -> MainMenu; visible 3D menu, running countdown, audio and Luxodd services.");
                CheckLevels(menu);
                CheckSkins(menu);
                int savedLevel=PlayerPrefs.GetInt("BucaCurrentLevel",0);
                PlayerPrefs.SetInt("BucaCurrentLevel",0);
                ClickButton(menu,menu.homeButtons[0]);
                if(!menu.resumeRoot.activeInHierarchy || menu.resumeButtons.Length!=3) throw new Exception("New player did not receive Level 1 / Continue / Back.");
                ClickButton(menu,menu.resumeButtons[2]);
                PlayerPrefs.SetInt("BucaCurrentLevel",6);
                ClickButton(menu,menu.homeButtons[0]);
                if(!menu.resumeRoot.activeInHierarchy || menu.resumeButtons.Length!=3) throw new Exception("Returning player did not receive Resume / Start 1 / Back.");
                ClickButton(menu,menu.resumeButtons[2]);
                PlayerPrefs.SetInt("BucaCurrentLevel",savedLevel);
                Debug.Log("FEEDBACK_RESUME_PASSED: saved progress prompts Continue or Start 1.");
                SessionState.SetString(PhaseKey, "game");
                ClickButton(menu,menu.homeButtons[1]);
                if(menu.mapClockSegments != null && menu.mapClockSegments.Length == 14)
                {
                    if((float)type.GetField("_mapRemaining",flags).GetValue(menu)!=menu.mapSelectionSeconds)
                        throw new Exception("Train timer did not reset when reopened.");
                    type.GetMethod("SetFocus",flags).Invoke(menu,new object[]{0});
                    type.GetMethod("TickMapCountdown",flags).Invoke(menu,new object[]{menu.mapSelectionSeconds});
                    if(!(bool)type.GetField("_loading",flags).GetValue(menu)) throw new Exception("Train timer did not start selected level.");
                    Debug.Log("TRAIN_AUTO_START_PASSED: timer resets on reopen and expiry starts highlighted unlocked level.");
                }
                else ClickButton(menu,menu.levelButtons[0]);
            }
            else if (phase == "game" && EditorApplication.isPlaying && SceneManager.GetActiveScene().name == "Game")
            {
                var level = LevelManager.Instance;
                if (level == null || level.Puck == null) return;
                if (level.levelPrefabs.Length != 30 || !level.Puck.activeInHierarchy)
                    throw new Exception("Gameplay did not initialize the expected campaign/puck.");
                if(level.CurrentLevelRoot==null) return;
                WatchCopyTutorialValidation.Check(level);
                ToyBoxWinValidation.Check(level);
                ToyBoxResultsValidation.Check(level);
                var timeMethod=typeof(LevelManager).GetMethod("GetTimeLimitForLevel",BindingFlags.Instance|BindingFlags.NonPublic);
                for(int i=0;i<30;i++)
                {
                    float expected=RebalanceLearningCampaign.Seconds[i];
                    float actual=(float)timeMethod.Invoke(level,new object[]{i});
                    if(actual!=expected) throw new Exception("Runtime timer ignores level budget: "+(i+1));
                }
                Debug.Log("LEARNING_RUNTIME_TIMERS_PASSED: all 30 authored budgets.");
                CheckSkinBinding(level.Puck.GetComponent<BucaSkinBinding>());
                CheckSkinBinding(level.CurrentLevelRoot.GetComponent<BucaSkinBinding>());
                var solidHud=UnityEngine.Object.FindAnyObjectByType<ToyBoxGameplayHud>();
                if(solidHud==null || solidHud.manager!=level || solidHud.digitMeshes.Length!=10)
                    throw new Exception("Solid gameplay HUD not wired.");
                if(level.shotCounter.enabled || level.levelLabel.enabled || level.timerDisplay.timerText.enabled)
                    throw new Exception("Legacy flat gameplay stats are still visible.");
                float savedTime=level.CurrentTimeRemaining;
                typeof(LevelManager).GetField("_timeRemaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(level,4.2f);
                solidHud.SendMessage("LateUpdate");
                if(solidHud.timeDigits[1].sharedMesh!=solidHud.digitMeshes[5] || solidHud.timeFaces[0].sharedMaterial!=solidHud.urgentTime)
                    throw new Exception("Solid countdown does not match game timer/critical state.");
                typeof(LevelManager).GetField("_timeRemaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(level,savedTime);
                solidHud.SendMessage("LateUpdate");
                Debug.Log("TOYBOX_SOLID_HUD_PASSED: saved 3D digits update from the live timer; critical state and legacy HUD suppression verified.");
                if(level.comboSign==null || level.comboSign.messages.Length!=4)
                    throw new Exception("Wooden combo sign not assigned.");
                ToyBoxMenuValidation.RenderCamera(Camera.main,"Gameplay-Clear-Hud",1600,1000);
                for(int multiplier=2;multiplier<=5;multiplier++)
                {
                    level.comboSign.Show(multiplier); level.comboSign.Sample(.35f);
                    for(int m=0;m<4;m++)
                        if(level.comboSign.messages[m].activeSelf!=(m==multiplier-2))
                            throw new Exception("Wrong combo sign selected.");
                    if(multiplier==2) ToyBoxMenuValidation.RenderCamera(Camera.main,"Gameplay-Combo-2",1600,1000);
                }
                level.comboSign.Sample(1.31f);
                if(level.comboSign.presentation.gameObject.activeSelf) throw new Exception("Combo sign did not exit.");
                level.comboSign.ShowHoleInOne(); level.comboSign.Sample(2);
                if(!level.comboSign.holeInOne.activeSelf || !level.comboSign.presentation.gameObject.activeSelf) throw new Exception("One-shot award disappeared early.");
                ToyBoxMenuValidation.RenderCamera(Camera.main,"Gameplay-Hole-In-One",1600,1000);
                level.comboSign.Sample(2.41f);
                if(level.comboSign.presentation.gameObject.activeSelf) throw new Exception("One-shot award did not exit.");
                if(!level.useShotLives) throw new Exception("Hearts disabled in the saved game.");
                var budget = typeof(LevelManager).GetMethod("GetMaxLivesForLevel",BindingFlags.Instance|BindingFlags.NonPublic);
                for(int i=0;i<30;i++)
                    if((int)budget.Invoke(level,new object[]{i}) != Mathf.Max(5,i+1)) throw new Exception("Wrong move budget at level "+(i+1));
                int lives=level.CurrentLives;
                typeof(LevelManager).GetMethod("OnMissedShot",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(level,null);
                if(level.CurrentLives!=lives-1) throw new Exception("Miss did not consume exactly one heart.");
                Debug.Log("FEEDBACK_RULES_PASSED: all 30 heart budgets and missed-shot decrement.");
                Debug.Log("WOODEN_COMBO_SIGNS_PASSED: four multipliers, exclusive selection and animation exit.");
                for(int skin=0;skin<5;skin++)
                {
                    level.Puck.GetComponent<BucaSkinBinding>().Apply(skin);
                    level.CurrentLevelRoot.GetComponent<BucaSkinBinding>().Apply(skin);
                    ToyBoxMenuValidation.RenderCamera(Camera.main,"Gameplay-Skin-"+(skin+1),1600,1000);
                }
                Debug.Log("TOYBOX_SKINS_GAME_PASSED: selected Ocean board and puck applied after scene load.");
                Debug.Log("TOYBOX_STARTUP_GAME_PASSED: chapter map level click -> Game; 30-level campaign and active puck.");
                FeedbackGameplayValidation.Check(level);
                SessionState.SetString(PhaseKey, "finish");
                EditorApplication.ExitPlaymode();
            }
            else if (phase == "finish" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                BootFromMainMenu.RestoreStartupScene();
                if (EditorSceneManager.playModeStartScene == null) throw new Exception("Startup scene lost after leaving Play Mode.");
                RestoreProgress();
                SessionState.SetString(PhaseKey, "");
                Debug.Log("TOYBOX_STARTUP_VALIDATION_PASSED");
                EditorApplication.Exit(0);
            }
        }
        catch (Exception error)
        {
            RestoreProgress();
            SessionState.SetString(PhaseKey, "");
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
