using UnityEngine;
using UnityEngine.UI;

namespace Siderectomy
{
    public sealed class OfficeView : MonoBehaviour
    {
        public const float TerminalX = 4.5f;
        private PrototypeApp app;
        private PrototypeAssets assets;
        private SpriteRenderer character;
        private Text prompt;
        private float animationTime;
        private int facing = 1;
        private Sprite square;
        private DemoDialogueView storyView;
        public string LastError {get;private set;}
        public Vector3 CharacterPosition { get { return character.transform.position; } }

        public void Initialize(PrototypeApp owner, PrototypeAssets art)
        {
            app = owner; assets = art;
            DemoSession.Ensure();
            var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply();
            square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            Block("Wall", 0, 0, 20, 12, new Color32(37, 45, 55, 255), -10);
            Block("WallInset", 0, 1.2f, 15.5f, 5, new Color32(48, 57, 65, 255), -9);
            Block("Floor", 0, -3.9f, 20, 3.4f, new Color32(63, 68, 73, 255), -5);
            Block("FloorLine", 0, -2.2f, 20, 0.06f, new Color32(123, 138, 146, 255), -4);
            Block("Door", -6.7f, -0.65f, 1.7f, 3.1f, new Color32(25, 31, 39, 255), -4);
            Block("Desk", TerminalX, -1.65f, 2.6f, 1.1f, new Color32(102, 112, 121, 255), -2);
            Block("TerminalFrame", TerminalX, -0.55f, 1.9f, 1.25f, new Color32(15, 25, 32, 255), -1);
            Block("TerminalScreen", TerminalX, -0.5f, 1.55f, 0.85f, PrototypeUi.Accent * new Color(0.55f, 0.55f, 0.55f, 1), 0);
            Block("CeilingStrip", 0, 3.6f, 13, 0.08f, new Color32(155, 184, 188, 255), -4);
            var go = new GameObject("Operator"); go.transform.SetParent(transform); go.transform.position = new Vector3(-4.5f, -2.295f, 0);
            character = go.AddComponent<SpriteRenderer>(); character.sortingOrder = 5;
            SetFrame(0); character.flipX = true;
            var ui = PrototypeUi.CanvasRoot();
            PrototypeUi.Label(ui, "Title", "裁星计划", 70, 45, 800, 70, 46);
            PrototypeUi.Label(ui, "Subtitle", "PROJECT SIDERECTOMY  /  测试办公室", 73, 117, 1000, 35, 20, PrototypeUi.Muted);
            PrototypeUi.Label(ui, "TerminalSign", "文明处置终端", 1110, 280, 330, 42, 27, PrototypeUi.Accent);
            PrototypeUi.Box(ui, "ControlsPanel", 70, 749, 1460, 106, PrototypeUi.Panel);
            prompt = PrototypeUi.Label(ui, "Interaction", "A / D 移动 · 靠近右侧终端，按 E 进入星系测试", 97, 762, 1360, 66, 26);
            PrototypeUi.Label(ui, "GreyboxNote", "灰模场景 · 角色使用已提供的行走差分", 73, 180, 1050, 32, 18, PrototypeUi.Muted);
            var profile=DemoSession.Profile;
            PrototypeUi.Label(ui,"OfficeProfile",profile==null?"尚未建立档案":profile.name+" · 已解锁 "+profile.highestUnlockedLevel+" / "+DemoProgression.LevelCount+"关 · "+(profile.hasCheckpoint?"局内现场已保存":"下一次进入第"+profile.currentLevel+"关"),73,227,1050,40,22,PrototypeUi.Accent);
            PrototypeUi.Button(ui,"OfficeContinue",profile!=null&&profile.hasCheckpoint?"继续已保存的模拟":"接入终端 / 开始模拟",1015,565,500,58,()=>TryEnter());
            PrototypeUi.Button(ui,"OfficeMainMenu","返回开始界面 / 管理存档",1015,640,500,58,()=>UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));
            for(int i=1;i<=DemoProgression.LevelCount;i++){int level=i;bool unlocked=profile==null?level==1:DemoProgression.CanEnter(profile,level);string state=!unlocked?"未解锁":DemoProgression.Completed(profile,level)?"已通关":"可进入";var button=PrototypeUi.Button(ui,"OfficeLevel"+level,"第"+level+"关\n"+state,73+(i-1)*145,300,132,56,()=>{try{DemoSession.Ensure();if(!DemoNarrativeSession.TestMode&&DemoNarrative.OfficePending(DemoSession.Profile))throw new System.InvalidOperationException("请先完成办公室剧情");DemoSession.SaveProgress(null,level,DemoSession.Profile.applyCount,DemoSession.Profile.secondEnding);app.EnterStarSystem();}catch(System.Exception ex){LastError=ex.Message;}});button.GetComponentInChildren<Text>().fontSize=16;button.interactable=unlocked&&(profile==null||!profile.hasCheckpoint);}
            PrototypeUi.Label(ui,"OfficeCheckpointHint",profile!=null&&profile.hasCheckpoint?"已有局内记录：继续模拟后，可在暂停菜单放弃或重开。":"完成当前关卡目标后解锁下一关；已解锁关卡可重玩。",73,367,1070,40,17,PrototypeUi.Muted);
            if(!DemoNarrativeSession.TestMode){
                try{DemoNarrativeSession.OfficePump();}catch(System.Exception ex){LastError="剧情加载失败："+ex.Message;}
                storyView=gameObject.AddComponent<DemoDialogueView>();
                storyView.Initialize(ui,s=>DemoNarrativeSession.Commit(s),DemoNarrativeSession.OfficePump);
            }

        }

        private void Block(string name, float x, float y, float width, float height, Color color, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(transform); go.transform.position = new Vector3(x, y, 0); go.transform.localScale = new Vector3(width, height, 1);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = square; sr.color = color; sr.sortingOrder = order;
        }
        private void Update()
        {
            if (character == null) return;
            if(storyView!=null&&storyView.Active)return;
            float direction = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            Move(direction, Time.deltaTime);
            bool near = Mathf.Abs(character.transform.position.x - TerminalX) < 1.55f;
            prompt.text = near ? "按 E 接入终端  /  开始星系测试" : "A / D 移动 · 靠近右侧终端，按 E 进入星系测试";
            if(!string.IsNullOrEmpty(LastError))prompt.text="操作未完成："+LastError;
            prompt.color = near ? PrototypeUi.Accent : PrototypeUi.Ink;
            if (near && Input.GetKeyDown(KeyCode.E)) Interact();
        }
        public void Move(float direction, float delta)
        {
            var pos = character.transform.position; pos.x = Mathf.Clamp(pos.x + direction * 4.2f * delta, -7.7f, 7.7f); character.transform.position = pos;
            // The right-facing source poses overlap at the feet. Mirror the clean left pair for this prototype.
            if (Mathf.Abs(direction) > 0.01f) { facing = direction > 0 ? 1 : -1; animationTime += delta; SetFrame((int)(animationTime / 0.17f) % 2); }
            else { animationTime = 0; SetFrame(0); }
            character.flipX = facing > 0;
        }
        private bool TryEnter(){try{app.EnterStarSystem();return true;}catch(System.Exception ex){LastError=ex.Message;return false;}}
        private void SetFrame(int index) { if (assets != null && assets.CharacterFrames != null && assets.CharacterFrames.Length > index) character.sprite = assets.CharacterFrames[index]; }
        public bool Interact()
        {
            if (Mathf.Abs(character.transform.position.x - TerminalX) >= 1.55f) return false;
            return TryEnter();
        }
    }
}
