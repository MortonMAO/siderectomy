using System;
using System.IO;
using System.Collections.Generic;
using Siderectomy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;

public static class CanvasRendererChecks
{
    private const string PlayRequest = "Temp/CanvasRendererPlayCheck.request";

    [InitializeOnLoadMethod]
    private static void RegisterPlayCheck() { EditorApplication.update += PollPlayCheck; }

    public static void BeginPlaymodeValidation()
    {
        Directory.CreateDirectory("Temp");
        File.WriteAllText(PlayRequest, "validate");
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Siderectomy/Scenes/StarSystemTest.unity");
        EditorApplication.EnterPlaymode();
    }

    private static void PollPlayCheck()
    {
        if (!Application.isBatchMode || !File.Exists(PlayRequest) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (UnityEngine.Object.FindObjectOfType<DemoMissionView>() == null) return;
        File.Delete(PlayRequest);
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    [MenuItem("裁星计划/检查星图CanvasRenderer")]
    public static void Run()
    {
        // Editor-only regression: require a renderer before any Graphic getter can create it lazily.
        var standalone = new GameObject("StandaloneStarMapCheck", typeof(RectTransform));
        DemoMissionView view = null;
        try
        {
            standalone.AddComponent<DemoStarMap>();
            Require(standalone.GetComponent<CanvasRenderer>() != null, "脚本组件依赖未自动补齐");
            UnityEngine.Object.DestroyImmediate(standalone);
            if (Application.isBatchMode && !EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
                view = new GameObject("ValidationView").AddComponent<DemoMissionView>();
                view.Initialize(null);
            }
            else view = UnityEngine.Object.FindObjectOfType<DemoMissionView>();
            if (view == null)
            {
                Debug.Log("CanvasRenderer组件依赖验证通过；进入Play后再次运行此检查可验证星图点击。");
                return;
            }
            int[] levels = Application.isBatchMode ? new[] { 1, 2, 3, 4 } : new[] { view.Simulation.Level.number };
            foreach (int level in levels)
            {
                var map = UnityEngine.Object.FindObjectOfType<DemoStarMap>();
                Require(map != null && map.GetComponent<CanvasRenderer>() != null, "StarMap缺少CanvasRenderer");
                // Swap only simulation data outside Play; runtime level loading uses deferred Destroy.
                if (Application.isBatchMode)
                {
                    map.Simulation = new DemoSimulation(DemoLevel.Defaults()[level - 1], DemoRuleBook.Load());
                    map.Refresh();
                }
                Canvas.ForceUpdateCanvases();
                Require(map.canvasRenderer != null, "Graphic绘制组件无效");
                var rect = (RectTransform)map.transform;
                var canvas = map.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                var events = EventSystem.current != null ? EventSystem.current : UnityEngine.Object.FindObjectOfType<EventSystem>();
                Require(events != null, "鼠标事件系统缺失");
                var pointer = new PointerEventData(events)
                {
                    position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center))
                };
                var hits = new List<RaycastResult>();
                // EventSystem lifecycle does not run outside Play; test the actual failing raycaster directly.
                canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
                Require(hits.Exists(h => h.gameObject == map.gameObject), "星图未参与鼠标射线检测");
            }
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/CanvasRenderer回归验证.txt", "PASS 编辑器环境：自动补齐CanvasRenderer、星图创建组件、Graphic访问；已验证关卡 " + string.Join(",", levels) + " 的星图鼠标射线检测。\n");
            Debug.Log("CANVAS_RENDERER_CHECK_OK");
        }
        finally
        {
            if (standalone != null) UnityEngine.Object.DestroyImmediate(standalone);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("CanvasRenderer回归检查失败：" + message);
    }
}
