using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using DetectiveDesk;

public static class DetectiveSceneBuilder
{
    const string Root = "Assets/DetectiveDesk";
    static Material wood, walnut, wall, brass, bronze, parchment, green, velvet, black, glass, stone, silver, red, blue, gold;
    static Font uiFont;
    static DetectiveGame game;
    static Transform environment, props, decor;
    static Color Amber = new Color(.85f, .58f, .29f);

    [MenuItem("Tools/Detective Desk/Generate Starter Scene")]
    public static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Root + "/Generated");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        InitMaterials();
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("DetectiveOffice");
        environment = Group("Environment", root.transform);
        props = Group("GameplayObjects", root.transform);
        decor = Group("Decorations", root.transform);
        game = root.AddComponent<DetectiveGame>();
        CameraSetup(root.transform);
        BuildRoom();
        BuildGlobe();
        BuildStatues();
        BuildDeskProps();
        BuildUI(root.transform);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.24f, .20f, .16f);
        RenderSettings.fog = false;
        EditorSceneManager.SaveScene(scene, Root + "/Generated/DetectiveOffice.unity");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Detective Desk", "Generated scene saved to Assets/DetectiveDesk/Generated/DetectiveOffice.unity. Press Play to explore.", "OK");
    }

    static Transform Group(string name, Transform parent) { var go = new GameObject(name); go.transform.SetParent(parent); return go.transform; }
    static Material Mat(string name, Color color, float metallic = 0, float smooth = .4f, bool emissive = false)
    {
        string path = Root + "/Generated/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            m = new Material(shader); AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color); m.color = color;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emissive) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * .25f); }
        EditorUtility.SetDirty(m); return m;
    }
    static void InitMaterials()
    {
        wood = Mat("DarkDeskMahogany", new Color(.19f,.09f,.055f), .12f,.42f);
        walnut = Mat("DarkWalnut", new Color(.115f,.065f,.045f));
        wall = Mat("NoirWall",new Color(.075f,.057f,.047f));
        brass = Mat("AntiqueBrass",new Color(.68f,.41f,.13f),.85f,.64f);
        bronze = Mat("BronzeTrim",new Color(.35f,.19f,.085f),.7f,.55f);
        parchment = Mat("Parchment",new Color(.64f,.48f,.29f),0,.18f);
        green = Mat("EmeraldGlass",new Color(.028f,.21f,.105f),.18f,.82f);
        velvet = Mat("CrimsonVelvet",new Color(.26f,.015f,.027f),0,.2f);
        black = Mat("BlackMetal",new Color(.025f,.029f,.031f),.5f,.65f);
        glass = Mat("WhiskeyAmber",new Color(.75f,.39f,.12f),.12f,.9f);
        stone = Mat("StatueStone",new Color(.48f,.47f,.40f));
        silver = Mat("Steel",new Color(.56f,.54f,.48f),.85f,.68f);
        red = Mat("RedPin",new Color(.9f,.09f,.065f),.2f,.54f);
        blue = Mat("BluePin",new Color(.06f,.31f,.78f),.1f,.45f);
        gold = Mat("GlowingGold",new Color(1f,.61f,.16f),.6f,.65f,true);
    }
    static GameObject Prim(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material mat, Vector3? rotation=null)
    {
        var o = GameObject.CreatePrimitive(type); o.name = name; o.transform.SetParent(parent);
        o.transform.position = position; o.transform.localScale = scale;
        if (rotation.HasValue) o.transform.eulerAngles = rotation.Value;
        o.GetComponent<Renderer>().sharedMaterial = mat; return o;
    }
    static GameObject Cube(string n, Transform p, Vector3 pos, Vector3 scale, Material m, Vector3? rot=null) => Prim(n,PrimitiveType.Cube,p,pos,scale,m,rot);
    static GameObject Sphere(string n,Transform p,Vector3 pos,Vector3 scale,Material m) => Prim(n,PrimitiveType.Sphere,p,pos,scale,m);
    static GameObject Cylinder(string n,Transform p,Vector3 pos,Vector3 scale,Material m,Vector3? rot=null)=>Prim(n,PrimitiveType.Cylinder,p,pos,scale,m,rot);
    static void CameraSetup(Transform parent)
    {
        var cgo = new GameObject("MainCamera"); cgo.tag = "MainCamera"; cgo.transform.SetParent(parent);
        cgo.transform.position = new Vector3(0, 1.48f, -5.8f);
        cgo.transform.rotation = Quaternion.Euler(0,0,0);
        var camera = cgo.AddComponent<Camera>(); camera.fieldOfView = 57; camera.nearClipPlane = .03f; camera.farClipPlane = 100;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.026f,.024f,.028f);
        cgo.AddComponent<AudioListener>();
        LightAt("Warm banker's lamp", new Vector3(4.0f,3.1f,-.45f), new Color(1f,.69f,.39f), 5f, 9f);
        LightAt("Amber fill", new Vector3(-1f,3.5f,-.9f), new Color(1f,.59f,.34f), 1.5f, 11f);
        LightAt("Cool window fill", new Vector3(-5f,2.6f,-1f), new Color(.21f,.47f,.85f), 1.8f, 8f);
    }
    static Light LightAt(string name, Vector3 p, Color c, float intensity,float range)
    {
        var go = new GameObject(name); go.transform.position=p; go.transform.SetParent(environment);
        var l=go.AddComponent<Light>(); l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;return l;
    }
    static void BuildRoom()
    {
        Cube("Back wall",environment,new Vector3(0,1.9f,2.25f),new Vector3(12,5,.2f),wall);
        Cube("Floor",environment,new Vector3(0,-.14f,0),new Vector3(12,.2f,9),walnut);
        Cube("Desk slab",environment,new Vector3(0,.74f,-.42f),new Vector3(10.8f,.15f,3.5f),wood);
        Cube("Desk front apron",environment,new Vector3(0,.43f,-1.88f),new Vector3(10.5f,.53f,.16f),walnut);
        for(int s=-1;s<=1;s+=2)
        {
            Cube("Desk leg",environment,new Vector3(s*4.9f,.33f,-.1f),new Vector3(.29f,.65f,.29f),walnut);
            Cube("Back wall trim",environment,new Vector3(0,.95f+s*.19f,2.07f),new Vector3(12,.075f,.17f),bronze);
        }
        // Map is a standalone framed parchment object: no pins, strings, photographs, or overlays.
        Cube("Map frame",environment,new Vector3(.45f,2.8f,2.045f),new Vector3(7.0f,2.72f,.12f),bronze);
        var map=Cube("CLEAN MAP - replace texture here",environment,new Vector3(.45f,2.8f,1.966f),new Vector3(6.82f,2.55f,.025f),parchment);
        var mapMat = new Material(parchment); mapMat.name="WorldMapSurface";
        var tex = MakeMapTexture(); mapMat.mainTexture=tex;
        if (mapMat.HasProperty("_BaseMap")) mapMat.SetTexture("_BaseMap",tex);
        string mPath=Root+"/Generated/WorldMapSurface.mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(mPath);
        if (old) { EditorUtility.CopySerialized(mapMat,old); UnityEngine.Object.DestroyImmediate(mapMat); mapMat=old; }
        else { AssetDatabase.CreateAsset(mapMat,mPath); }
        map.GetComponent<Renderer>().sharedMaterial=mapMat;
        // Window occupies the left margin; a dark glass surface with blue rainy streaks.
        Cube("Window dark recess",environment,new Vector3(-4.68f,2.53f,1.98f),new Vector3(2.0f,3.2f,.08f),black);
        var win=Cube("Rainy window",environment,new Vector3(-4.68f,2.53f,1.92f),new Vector3(1.9f,3.09f,.023f),blue);
        var wm=new Material(black); wm.name="RainWindowSurface";var rain=MakeRainTexture();wm.mainTexture=rain;
        if (wm.HasProperty("_BaseMap"))wm.SetTexture("_BaseMap",rain);
        var wp=Root+"/Generated/RainWindowSurface.mat";var wold=AssetDatabase.LoadAssetAtPath<Material>(wp);
        if(wold){EditorUtility.CopySerialized(wm,wold); UnityEngine.Object.DestroyImmediate(wm);wm=wold;}else AssetDatabase.CreateAsset(wm,wp);
        win.GetComponent<Renderer>().sharedMaterial=wm;
        Cube("Window left rail",environment,new Vector3(-5.69f,2.53f,1.87f),new Vector3(.075f,3.35f,.1f),walnut);
        Cube("Window right rail",environment,new Vector3(-3.66f,2.53f,1.87f),new Vector3(.075f,3.35f,.1f),walnut);
        Cube("Window crossbar",environment,new Vector3(-4.68f,2.5f,1.87f),new Vector3(2.04f,.065f,.1f),walnut);
        Cube("Window sill",environment,new Vector3(-4.68f,.9f,1.8f),new Vector3(2.15f,.09f,.25f),wood);
        // Green banker lamp far right, partly blocking right wall.
        Cylinder("Lamp brass base",environment,new Vector3(4.7f,.88f,.8f),new Vector3(.52f,.07f,.36f),brass);
        Cylinder("Lamp stem",environment,new Vector3(4.7f,1.57f,.8f),new Vector3(.055f,.7f,.055f),brass);
        Sphere("Lamp emerald shade",environment,new Vector3(4.7f,2.31f,.8f),new Vector3(1.15f,.30f,.6f),green);
        Cube("Lamp gold lip",environment,new Vector3(4.7f,2.17f,.8f),new Vector3(1.1f,.045f,.48f),brass);
        Sphere("Lamp bulb",environment,new Vector3(4.7f,2.10f,.8f),new Vector3(.14f,.085f,.14f),gold);
    }
    static Texture2D MakeMapTexture()
    {
        const int w=1024,h=400;string p=Root+"/Generated/Cartography.png";
        var t=new Texture2D(w,h,TextureFormat.RGBA32,false);
        var px=new Color32[w*h];var baseCol=new Color32(183,149,97,255);
        var random=new System.Random(15);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            int noise=random.Next(-12,13); int grid=((x%48)==0||(y%48)==0)?-11:0;
            px[y*w+x]=new Color32((byte)Mathf.Clamp(baseCol.r+noise+grid,0,255),(byte)Mathf.Clamp(baseCol.g+noise+grid,0,255),(byte)Mathf.Clamp(baseCol.b+noise+grid,0,255),255);
        }
        // Abstract stylized landmasses. This is a procedural PLACEHOLDER, not geographic cartography.
        DrawLand(px,w,h,new []{new Vector2(.09f,.73f),new Vector2(.16f,.86f),new Vector2(.29f,.81f),new Vector2(.35f,.72f),new Vector2(.28f,.6f),new Vector2(.24f,.48f),new Vector2(.18f,.51f),new Vector2(.14f,.37f),new Vector2(.10f,.48f)});
        DrawLand(px,w,h,new []{new Vector2(.29f,.42f),new Vector2(.34f,.36f),new Vector2(.38f,.22f),new Vector2(.35f,.08f),new Vector2(.32f,.16f),new Vector2(.27f,.33f)});
        DrawLand(px,w,h,new []{new Vector2(.45f,.78f),new Vector2(.56f,.85f),new Vector2(.78f,.84f),new Vector2(.87f,.68f),new Vector2(.79f,.58f),new Vector2(.73f,.5f),new Vector2(.64f,.59f),new Vector2(.56f,.51f),new Vector2(.50f,.60f)});
        DrawLand(px,w,h,new []{new Vector2(.50f,.54f),new Vector2(.60f,.49f),new Vector2(.63f,.33f),new Vector2(.58f,.15f),new Vector2(.51f,.19f),new Vector2(.47f,.39f)});
        DrawLand(px,w,h,new []{new Vector2(.78f,.24f),new Vector2(.85f,.26f),new Vector2(.88f,.15f),new Vector2(.80f,.12f)});
        t.SetPixels32(px);t.Apply();File.WriteAllBytes(p,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(p);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
    static void DrawLand(Color32[] pixels,int w,int h,Vector2[] vertices)
    {
        var c=new Color32(110,103,64,255);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float a=(float)x/w,b=(float)y/h;bool inside=false;
            for(int i=0,j=vertices.Length-1;i<vertices.Length;j=i++)
            {
                var v=vertices[i];var q=vertices[j];
                if((v.y>b)!=(q.y>b) && a<(q.x-v.x)*(b-v.y)/(q.y-v.y)+v.x)inside=!inside;
            }
            if(inside) pixels[y*w+x]=c;
        }
    }
    static Texture2D MakeRainTexture()
    {
        const int w=256,h=512;string p=Root+"/Generated/RainWindow.png";var t=new Texture2D(w,h);
        var pix=new Color32[w*h];var rand=new System.Random(99);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)pix[y*w+x]=new Color32((byte)(9+rand.Next(9)),(byte)(17+rand.Next(15)),(byte)(26+rand.Next(20)),255);
        for(int i=0;i<190;i++)
        {
            int x=rand.Next(w), start=rand.Next(h), len=rand.Next(4,75);
            for(int k=0;k<len && start+k<h;k++)pix[(start+k)*w+x]=new Color32(75,116,149,(byte)255);
        }
        t.SetPixels32(pix);t.Apply();File.WriteAllBytes(p,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(p);return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
    static void Interact(GameObject parent,DeskInteractable.Action action)
    {
        // OnMouseDown requires a collider on the same GameObject as the script.
        // Compute the bounds of all child geometry in the parent's local coordinates.
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var r in parent.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            Bounds wb = r.bounds;
            foreach (int dx in new []{-1,1}) foreach (int dy in new []{-1,1}) foreach (int dz in new []{-1,1})
            {
                Vector3 point = parent.transform.InverseTransformPoint(wb.center + Vector3.Scale(wb.extents,new Vector3(dx,dy,dz)));
                if (first) { b = new Bounds(point,Vector3.zero); first = false; } else b.Encapsulate(point);
            }
        }
        var collider = parent.AddComponent<BoxCollider>();
        collider.center=b.center;collider.size=b.size + Vector3.one*.02f;
        // Disable collider hits on children so the parent receives mouse events.
        foreach(var c in parent.GetComponentsInChildren<Collider>()) if(c != collider) c.enabled=false;
        var it=parent.AddComponent<DeskInteractable>();it.action=action;it.game=game;
    }
    static void BuildGlobe()
    {
        var main=new GameObject("MechanicalGlobe");main.transform.SetParent(props);
        var stand=main.transform;
        // Left-of-center, slightly enlarged; visible above desk and below map.
        Cylinder("Globe pedestal",stand,new Vector3(-1.45f,1.00f,-.33f),new Vector3(.43f,.14f,.43f),brass);
        Cylinder("Globe spindle",stand,new Vector3(-1.45f,1.35f,-.33f),new Vector3(.09f,.27f,.09f),brass);
        var rotor=Group("RotatingSphere",stand);rotor.position=new Vector3(-1.45f,2.15f,-.33f);game.globeRotor=rotor;
        Sphere("Dark ocean shell",rotor,rotor.position,new Vector3(1.36f,1.36f,1.36f),black);
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI*2/10f;
            // Small gearlike brass nodes to indicate clockwork without obscuring the shell.
            Sphere("Clockwork node "+i,rotor,rotor.position + new Vector3(Mathf.Cos(a)*.55f,Mathf.Sin(a)*.55f,-.50f),new Vector3(.12f,.12f,.07f),brass);
        }
        for(int i=0;i<6;i++)
        {
            float a=i*30;
            var band=Cylinder("Meridian line "+i,rotor,rotor.position,new Vector3(.698f,.012f,.698f),brass,new Vector3(90,a,0));
            // Cylinder is a thin disk placeholder for latitude/meridian in the first pass.
            band.GetComponent<Collider>().enabled=false;
            band.SetActive(false); // enable when swapped with real ring meshes
        }
        // Brass meridian arc approximated by thin spheres along an ellipse, no opaque cover.
        for(int i=0;i<=25;i++)
        {
            float a=(i/25f)*Mathf.PI;
            Sphere("Meridian arc",stand,rotor.position+new Vector3(Mathf.Sin(a)*.76f,Mathf.Cos(a)*.76f,0),new Vector3(.034f,.034f,.034f),brass);
        }
        Cylinder("RPM base",stand,new Vector3(-.49f,1.43f,-.75f),new Vector3(.32f,.07f,.32f),brass,new Vector3(90,0,0));
        Cylinder("RPM dial face",stand,new Vector3(-.49f,1.43f,-.807f),new Vector3(.27f,.04f,.27f),black,new Vector3(90,0,0));
        Cube("RPM needle",stand,new Vector3(-.44f,1.47f,-.867f),new Vector3(.15f,.015f,.02f),red,new Vector3(0,0,43));
        Interact(main,DeskInteractable.Action.Globe);
        var glow=LightAt("Globe inner warm glow",new Vector3(-1.45f,2.2f,-.25f),new Color(1f,.55f,.18f),.5f,2.5f);
        game.globeLight=glow;
    }
    static void BuildStatues()
    {
        for(int i=0;i<2;i++)
        {
            float x=-3.55f+i*.72f;var p=new GameObject("StatueCollectibleSlot_"+i);p.transform.SetParent(props);
            Cube("Engraved display base",p.transform,new Vector3(x,.90f,-.30f),new Vector3(.58f,.16f,.52f),brass);
            Cylinder("Stone torso",p.transform,new Vector3(x,1.26f,-.30f),new Vector3(.19f,.26f,.16f),i==0?black:stone);
            Sphere("Stone head",p.transform,new Vector3(x,1.57f,-.30f),new Vector3(.25f,.27f,.22f),i==0?black:stone);
            Interact(p,DeskInteractable.Action.Statue);
        }
        Cylinder("Small jeweler hammer handle",decor,new Vector3(-3.22f,.91f,-1.03f),new Vector3(.055f,.48f,.055f),wood,new Vector3(90,0,65));
        Cylinder("Hammer head",decor,new Vector3(-2.98f,.95f,-1.0f),new Vector3(.15f,.11f,.11f),brass,new Vector3(0,0,90));
        Cylinder("Whiskey bottle",decor,new Vector3(-4.83f,1.18f,-.55f),new Vector3(.16f,.37f,.16f),glass);
        Cylinder("Bottle cap",decor,new Vector3(-4.83f,1.58f,-.55f),new Vector3(.08f,.09f,.08f),black);
        Cylinder("Whiskey glass",decor,new Vector3(-4.28f,.97f,-1.03f),new Vector3(.22f,.15f,.22f),glass);
        Cylinder("Glass rim",decor,new Vector3(-4.28f,1.12f,-1.03f),new Vector3(.23f,.025f,.23f),silver);
    }
    static void BuildDeskProps()
    {
        var pins=new GameObject("PinBox - click to select pin");pins.transform.SetParent(props);
        Cube("Pin coffer",pins.transform,new Vector3(1.63f,.91f,-.15f),new Vector3(1.38f,.25f,.73f),wood);
        Cube("Crimson velvet tray",pins.transform,new Vector3(1.63f,1.052f,-.15f),new Vector3(1.23f,.035f,.59f),velvet);
        Cube("Open lid",pins.transform,new Vector3(1.63f,1.40f,.27f),new Vector3(1.37f,.65f,.09f),wood,new Vector3(-12,0,0));
        Cube("Inside lid velvet",pins.transform,new Vector3(1.63f,1.40f,.18f),new Vector3(1.27f,.56f,.03f),velvet,new Vector3(-12,0,0));
        Material[] colors={red,blue,gold,green,stone};
        for(int r=0;r<2;r++)for(int c=0;c<7;c++)
        {
            float x=1.13f+c*.17f,z=-.34f+r*.22f;
            Cylinder("Pin stalk",pins.transform,new Vector3(x,1.1f,z),new Vector3(.018f,.065f,.018f),brass);
            Sphere("Colored pin head",pins.transform,new Vector3(x,1.18f,z),new Vector3(.085f,.085f,.085f),colors[(r*7+c)%colors.Length]);
        }
        Interact(pins,DeskInteractable.Action.PinBox);
        var gun=new GameObject("ToyPinLauncher - aimed up at map");gun.transform.SetParent(props);
        Cylinder("Launcher brass mount",gun.transform,new Vector3(3.2f,.94f,.35f),new Vector3(.32f,.12f,.32f),brass);
        Cylinder("Toy barrel",gun.transform,new Vector3(3.18f,1.55f,.31f),new Vector3(.15f,.75f,.15f),brass,new Vector3(43,0,0));
        Cylinder("Launcher side wheel",gun.transform,new Vector3(3.25f,1.19f,.16f),new Vector3(.26f,.055f,.26f),bronze,new Vector3(0,0,90));
        Sphere("Red tip",gun.transform,new Vector3(3.18f,2.1f,.78f),new Vector3(.10f,.10f,.10f),red);
        Interact(gun,DeskInteractable.Action.Launcher);
        var files=new GameObject("CaseFiles");files.transform.SetParent(props);
        for(int i=0;i<4;i++)Cube("Manila file "+i,files.transform,new Vector3(2.2f,.855f+i*.015f,-1.20f),new Vector3(2.05f,.016f,.92f),parchment,new Vector3(0,-8,0));
        Interact(files,DeskInteractable.Action.Files);
        var stamp=new GameObject("SOLVED Stamp");stamp.transform.SetParent(props);
        Cube("Stamp body",stamp.transform,new Vector3(4.22f,.93f,-.71f),new Vector3(.52f,.13f,.45f),black);
        Cylinder("Stamp handle",stamp.transform,new Vector3(4.22f,1.19f,-.71f),new Vector3(.11f,.25f,.11f),wood);
        Sphere("Stamp knob",stamp.transform,new Vector3(4.22f,1.46f,-.71f),new Vector3(.30f,.24f,.30f),wood);
        Interact(stamp,DeskInteractable.Action.Stamp);
        var solved=Cube("SOLVED indication (appears after stamping)",files.transform,new Vector3(2.2f,.97f,-1.20f),new Vector3(1.0f,.014f,.14f),red);
        game.stampMark=solved;
    }
    static GameObject UIPanel(string name,Transform parent,Vector2 anchorMin,Vector2 anchorMax,Vector2 pivot,Vector2 position,Vector2 size,Color color)
    {
        var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);
        var r=o.GetComponent<RectTransform>();r.anchorMin=anchorMin;r.anchorMax=anchorMax;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;
        o.GetComponent<Image>().color=color;return o;
    }
    static Text UIText(string name,Transform parent,string value,int size,Color color,Vector2 pos,Vector2 dimensions,TextAnchor align=TextAnchor.MiddleLeft)
    {
        var o=new GameObject(name,typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);
        var rt=o.GetComponent<RectTransform>();rt.anchorMin=new Vector2(0,.5f);rt.anchorMax=new Vector2(0,.5f);rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=pos;rt.sizeDelta=dimensions;
        var t=o.GetComponent<Text>();t.font=uiFont;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;return t;
    }
    static Button UIButton(string label,Transform parent,Vector2 pos,Vector2 size,Action onClick)
    {
        var p=UIPanel("Button_"+label,parent,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(0,.5f),pos,size,new Color(.12f,.11f,.08f,.96f));
        var b=p.AddComponent<Button>();b.targetGraphic=p.GetComponent<Image>();b.onClick.AddListener(()=>onClick());
        var t=UIText(label,p.transform,label,19,new Color(.94f,.72f,.35f),new Vector2(10,0),size-new Vector2(20,0),TextAnchor.MiddleCenter);
        return b;
    }
    static void BuildUI(Transform root)
    {
        var canvGO=new GameObject("GameUI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvGO.transform.SetParent(root);
        var canvas=canvGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scale=canvGO.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        var eventGO=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));eventGO.transform.SetParent(root);
        Color bg=new Color(.028f,.024f,.020f,.94f), text=new Color(.93f,.71f,.36f);
        var bar=UIPanel("Upper left HUD",canvGO.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(0,1),Vector2.zero,new Vector2(735,63),bg);
        UIButton("SET",bar.transform,new Vector2(4,-31),new Vector2(60,54),game.ToggleSettings);
        UIText("Money label",bar.transform,"MONEY",16,text,new Vector2(78,-31),new Vector2(85,40));
        game.moneyText=UIText("Money value",bar.transform,"€422.00",23,text,new Vector2(161,-31),new Vector2(132,40));
        UIText("Income label",bar.transform,"INCOME",16,text,new Vector2(309,-31),new Vector2(90,40));
        game.incomeText=UIText("Income value",bar.transform,"+€12.50 / min",21,new Color(.48f,.85f,.45f),new Vector2(402,-31),new Vector2(177,40));
        UIText("Gold label",bar.transform,"GOLD",16,text,new Vector2(591,-31),new Vector2(67,40));
        game.goldText=UIText("Gold value",bar.transform,"0",23,text,new Vector2(660,-31),new Vector2(50,40));
        var right=UIPanel("Top right",canvGO.transform,Vector2.one,Vector2.one,Vector2.one,new Vector2(-12,-12),new Vector2(216,52),bg);
        var up=new GameObject("UPGRADES Button",typeof(RectTransform),typeof(Image),typeof(Button));up.transform.SetParent(right.transform,false);
        var ur=up.GetComponent<RectTransform>();ur.anchorMin=Vector2.zero;ur.anchorMax=Vector2.one;ur.offsetMin=Vector2.zero;ur.offsetMax=Vector2.zero;
        up.GetComponent<Image>().color=new Color(.11f,.14f,.08f);up.GetComponent<Button>().onClick.AddListener(game.ToggleUpgrades);
        UIText("UPGRADES",up.transform,"UPGRADES",22,text,new Vector2(14,0),new Vector2(192,52),TextAnchor.MiddleCenter);
        var hint=UIPanel("Hint panel",canvGO.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,16),new Vector2(600,52),new Color(.028f,.024f,.02f,.80f));
        game.hintText=UIText("Hint text",hint.transform,"Click the globe, statues, pin box, files or stamp",17,text,new Vector2(18,0),new Vector2(570,50),TextAnchor.MiddleCenter);
        var settings=UIPanel("Settings panel",canvGO.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(530,260),bg);
        UIText("Settings heading",settings.transform,"SETTINGS",32,text,new Vector2(24,87),new Vector2(450,45));
        UIText("Settings detail",settings.transform,"Press Esc to close. Prototype settings panel.",19,text,new Vector2(24,10),new Vector2(480,75));
        UIButton("CLOSE",settings.transform,new Vector2(175,-78),new Vector2(170,51),game.ToggleSettings);
        game.settingsPanel=settings;settings.SetActive(false);
        var upgrades=UIPanel("Upgrades panel",canvGO.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(560,295),bg);
        UIText("Upgrades heading",upgrades.transform,"UPGRADES",32,text,new Vector2(26,105),new Vector2(470,50));
        UIText("Upgrade description",upgrades.transform,"Income upgrade: +€5/min for €100",21,text,new Vector2(26,36),new Vector2(500,45));
        UIButton("BUY €100",upgrades.transform,new Vector2(32,-45),new Vector2(220,55),game.UpgradeIncome);
        UIButton("CLOSE",upgrades.transform,new Vector2(310,-45),new Vector2(180,55),game.ToggleUpgrades);
        game.upgradesPanel=upgrades;upgrades.SetActive(false);
    }
}
