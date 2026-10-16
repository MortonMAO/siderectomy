using UnityEngine;
using UnityEngine.SceneManagement;

namespace Siderectomy
{
    public sealed class PrototypeApp : MonoBehaviour
    {
        public PrototypeAssets Art;
        public bool StartInOffice;
        public OfficeView Office { get; private set; }
        public DemoMissionView Demo { get; private set; }
        private void Awake()
        {
            Application.targetFrameRate = 60; DemoFrontMenu.ApplyPreferences();
            var args=System.Environment.GetCommandLineArgs();bool check=System.Array.IndexOf(args,"--demo-check")>=0||System.Array.IndexOf(args,"--local-interaction-check")>=0||System.Array.IndexOf(args,"--six-ui-check")>=0||System.Array.IndexOf(args,"--module-ui-check")>=0||System.Array.IndexOf(args,"--orbit-ui-check")>=0;
            foreach(string flag in new[]{"--flow-check","--demo-check","--local-interaction-check","--six-ui-check","--module-ui-check","--progression-check","--orbit-ui-check"}){int i=System.Array.IndexOf(args,flag);if(i>=0&&i+1<args.Length){DemoSaveStore.TestDirectory=System.IO.Path.Combine(args[i+1],"TestSaves");break;}}
            var camera = Camera.main;
            if (camera == null) { var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); go.tag = "MainCamera"; camera = go.GetComponent<Camera>(); }
            camera.orthographic = true; camera.orthographicSize = 5.625f; camera.transform.position = new Vector3(0, 0, -10); camera.backgroundColor = new Color32(8, 15, 27, 255);
            if (SceneManager.GetActiveScene().name=="MainMenu"&&!check) { gameObject.AddComponent<DemoFrontMenu>().Initialize(); }
            else if (StartInOffice) { Office = gameObject.AddComponent<OfficeView>(); Office.Initialize(this, Art); }
            else { Demo = gameObject.AddComponent<DemoMissionView>(); Demo.Initialize(Art); }
            DemoIntegrationCheck.TryStart(this);
            LocalInteractionCheck.TryStart();
            DemoFlowCheck.TryStart();
            DemoSixUIQA.TryStart();
            DemoProgressionCheck.TryStart();
            DemoOrbitUIQA.TryStart();
            DemoNarrativeUIQA.TryStart();
            DemoProfileUIQA.TryStart();
        }
        public void EnterStarSystem() { DemoSession.EnterSimulation(); }
        public void ReturnOffice() { SceneManager.LoadScene("Office"); }
    }
}
