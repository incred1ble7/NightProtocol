using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NightProtocol.Mechanics;
using Object = UnityEngine.Object;

public static class MechanicsSetup
{
    const string Root = "Assets/NightProtocol/Mechanics";
    static string Work { get { var path=Path.GetFullPath("Library/MechanicsChecks");Directory.CreateDirectory(path);return path; } }
    const string ScenePath = "Assets/Scenes/GreyBox_sanya.unity";
    static readonly string[] Names = { "SingleDoor", "DoubleDoor", "ElectricalPanel", "Generator", "LightSwitch", "StandaloneSwitch", "StandaloneRedLever", "StandaloneGeneratorLever" };
    static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    [MenuItem("Night Protocol/Mechanics/Build prefabs and Greybox setup")]
    public static void BuildMenu() { Debug.Log(Build()); }
    [MenuItem("Night Protocol/Mechanics/Validate prefabs and scene")]
    public static void ValidateMenu() { Debug.Log(Validate()); }
    [MenuItem("Night Protocol/Mechanics/Run interaction checks (Play Mode)")]
    public static void RuntimeTestMenu() { Debug.Log(RuntimeTest()); }
    public static string ExportPackage(string path)
    {
        AssetDatabase.SaveAssets();
        var assets=AssetDatabase.FindAssets("",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>!AssetDatabase.IsValidFolder(p) && !p.Contains("MechanicsEditorBridge")).Concat(new[]{ScenePath}).ToArray();
        AssetDatabase.ExportPackage(assets,path,ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
        return path;
    }
    public static string Build()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode before rebuilding.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new Exception("Open GreyBox_sanya before setup; current scene is left untouched.");
        if (scene.isDirty) EditorSceneManager.SaveScene(scene, Path.Combine(Work,"GreyBox_sanya.live-before.unity"), true);
        var backup = Path.Combine(Work, "GreyBox_sanya.before.unity");
        if (!File.Exists(backup)) File.Copy(ScenePath, backup);
        foreach (var sub in new[]{ "Prefabs", "Animations", "Controllers", "Materials" }) Directory.CreateDirectory(Root+"/"+sub);
        AssetDatabase.Refresh();
        ConfigureMaterials();
        foreach(var name in Names) BuildPrefab(name);
        BuildScene(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ElectricalPanel.prefab");
        EditorGUIUtility.PingObject(Selection.activeObject);
        return Validate();
    }

    static Texture2D Texture(string path, bool normal = false, bool linear = false)
    {
        if (!File.Exists(path)) return null;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        bool srgb = !(normal || linear);
        if (importer.textureType != type || importer.sRGBTexture != srgb)
        {
            importer.textureType = type; importer.sRGBTexture = srgb; importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Mat(string name, Color color, float metallic = 0, float smooth = 0.35f)
    {
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",smooth);
        Materials[name]=m; EditorUtility.SetDirty(m); return m;
    }
    static void Textured(string name, string prefix, string metallic = null)
    {
        var m=Mat(name,Color.white,1f,1f);
        m.SetTexture("_BaseMap",Texture(prefix+"_BaseColor.png"));
        var normal=Texture(prefix+"_Normal.png",true); m.SetTexture("_BumpMap",normal);
        if(normal) {m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",1f);}
        var metal=Texture(metallic??prefix+"_MetallicSmoothness.png",false,true);
        m.SetTexture("_MetallicGlossMap",metal); if(metal)m.EnableKeyword("_METALLICSPECGLOSSMAP");
        var occlusion=Texture(prefix+"_Occlusion.png",false,true);m.SetTexture("_OcclusionMap",occlusion);
        if(occlusion)m.EnableKeyword("_OCCLUSIONMAP");
        var emission=Texture(prefix+"_Emission.png");m.SetTexture("_EmissionMap",emission);
        if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.white*0.5f);}
        EditorUtility.SetDirty(m);
    }
    static void ConfigureMaterials()
    {
        Textured("M_ElectricalPanel","Assets/Assets/ElectricalPanel/Textures/ElectricalPanel");
        Textured("M_Generator","Assets/Assets/Generator/Textures/Generator");
        Textured("01",Root+"/Textures/01"); Textured("02",Root+"/Textures/02");
        Textured("M_LightSwitch_Old_Low","Assets/Assets/LightSwitch_Old_Low/Texture/LightSwitch_Old_Low",Root+"/Textures/LightSwitch_MetallicSmoothness.png");
        Mat("Material.001",new Color(0.12f,0.12f,0.12f));
        Mat("Material.002",new Color(0.12f,0.9f,0.04f));
        Mat("Material.003",new Color(0.9f,0.05f,0.04f));
        Mat("Material.004",new Color(0.05f,0.05f,0.05f));
        Mat("glass",new Color(0.06f,0.1f,0.12f),0.65f,0.88f);
        Mat("DemoSupport",new Color(0.18f,0.2f,0.22f),0.3f,0.3f);
    }
    static Transform Child(Transform root,string name) => root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    static AnimationClip Clip(string asset,string name,string targetPath,bool relative,bool reverse)
    {
        var original=AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/"+asset+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
        string path=Root+"/Animations/"+name+(reverse?"_OnToOff":"_OffToOn")+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);} else clip.ClearCurves();
        clip.name=Path.GetFileNameWithoutExtension(path);clip.frameRate=30;clip.wrapMode=WrapMode.ClampForever;
        foreach(var binding in AnimationUtility.GetCurveBindings(original))
        {
            // Only moving transforms: static root transforms must never overwrite placement.
            bool match=targetPath=="DOORS" ? (binding.path=="DoorLeaf" || binding.path=="DoorLeft" || binding.path=="DoorRight") : binding.path==targetPath;
            if(!match || !binding.propertyName.StartsWith("m_LocalRotation."))continue;
            var source=AnimationUtility.GetEditorCurve(original,binding);
            var keys=new Keyframe[31];
            for(int i=0;i<=30;i++) keys[i]=new Keyframe(i/30f,source.Evaluate((reverse?40f/30f:0f)+i/30f));
            var curve=new AnimationCurve(keys);
            for(int i=0;i<keys.Length;i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
            }
            var b=binding;if(relative)b.path="";
            AnimationUtility.SetEditorCurve(clip,b,curve);
        }
        clip.EnsureQuaternionContinuity();
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(AnimationUtility.GetCurveBindings(clip).Length==0)throw new Exception("No moving curves for "+name);
        EditorUtility.SetDirty(clip);return clip;
    }
    static AnimatorController Controller(string name,AnimationClip motion)
    {
        string path=Root+"/Controllers/"+name+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm=controller.layers[0].stateMachine;
        foreach(var st in sm.states)sm.RemoveState(st.state);
        controller.parameters=Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("Progress",AnimatorControllerParameterType.Float);
        controller.AddParameter("IsOn",AnimatorControllerParameterType.Bool);
        var state=sm.AddState("Off_to_On");state.motion=motion;state.speed=0f;
        state.timeParameter="Progress";state.timeParameterActive=true;state.writeDefaultValues=false;sm.defaultState=state;
        EditorUtility.SetDirty(controller);return controller;
    }
    static Animator AddAnimator(GameObject go,string asset,string label,string target,bool relative)
    {
        var on=Clip(asset,label,target,relative,false);Clip(asset,label,target,relative,true);
        var animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=Controller(label,on);
        animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        return animator;
    }
    static MechanicalInteractable Interaction(GameObject go,string label,float duration,params Animator[] animators)
    {
        var m=go.AddComponent<MechanicalInteractable>();m.displayName=label;m.travelSeconds=duration;m.animators=animators;return m;
    }
    static void Box(GameObject go,float minimum=0f)
    {
        var filter=go.GetComponent<MeshFilter>();if(!filter)return;
        var bounds=filter.sharedMesh.bounds;
        var box=go.AddComponent<BoxCollider>();box.center=bounds.center;
        box.size=new Vector3(Mathf.Max(bounds.size.x,minimum),Mathf.Max(bounds.size.y,minimum),Mathf.Max(bounds.size.z,minimum));
    }
    static void Proxy(GameObject go,MechanicalInteractable m)
    {
        var p=go.AddComponent<InteractionTarget>();p.target=m;
    }
    static void BuildPrefab(string name)
    {
        string modelPath=Root+"/Models/"+name+".fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        if(importer.animationType!=ModelImporterAnimationType.Generic || importer.animationCompression!=ModelImporterAnimationCompression.Off)
        {
            importer.animationType=ModelImporterAnimationType.Generic;importer.animationCompression=ModelImporterAnimationCompression.Off;
            importer.importAnimation=true;importer.SaveAndReimport();
        }
        var root=new GameObject(name);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
        PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        visual.name="Visual";visual.transform.SetParent(root.transform,false);
        foreach(var a in visual.GetComponentsInChildren<Animator>())Object.DestroyImmediate(a);
        foreach(var r in visual.GetComponentsInChildren<MeshRenderer>())
        {
            r.sharedMaterials=r.sharedMaterials.Select(m=>m && Materials.TryGetValue(m.name,out var mapped)?mapped:m).ToArray();
            var mesh=r.GetComponent<MeshFilter>();
            if(mesh && mesh.sharedMesh) {var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh.sharedMesh;}
        }
        if(name.EndsWith("Door"))
        {
            var anim=AddAnimator(visual,name,name,"DOORS",false);
            var m=Interaction(root,name=="SingleDoor"?"Single door":"Double door",1.1f,anim);m.offAction="Open";m.onAction="Close";
            foreach(var leaf in visual.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Door")))
            {
                var mc=leaf.GetComponent<MeshCollider>();if(mc)Object.DestroyImmediate(mc);
                Box(leaf.gameObject);var rb=leaf.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
            }
        }
        else if(name=="ElectricalPanel")
        {
            for(int i=1;i<=6;i++)
            {
                string part="Switch"+i.ToString("00");var t=Child(visual.transform,part);
                Object.DestroyImmediate(t.GetComponent<MeshCollider>());Box(t.gameObject,0.055f);
                var m=Interaction(t.gameObject,"Panel switch "+i,0.32f,AddAnimator(t.gameObject,name,part,part,true));
                var fixedPart=Child(visual.transform,"SwitchBase"+i.ToString("00"));Proxy(fixedPart.gameObject,m);
            }
            var red=Child(visual.transform,"MainLever");Object.DestroyImmediate(red.GetComponent<MeshCollider>());Box(red.gameObject,0.07f);
            Interaction(red.gameObject,"Red main lever",0.45f,AddAnimator(red.gameObject,name,"MainLever","MainLever",true));
        }
        else if(name=="Generator")
        {
            var t=Child(visual.transform,"GeneratorLever");
            Interaction(t.gameObject,"Generator lever",0.65f,AddAnimator(t.gameObject,name,"GeneratorLever","GeneratorLever",true));
            foreach(var f in t.GetComponentsInChildren<MeshFilter>())
            {
                Object.DestroyImmediate(f.GetComponent<MeshCollider>());Box(f.gameObject,0.06f);
            }
        }
        else if(name.StartsWith("Standalone"))
        {
            string part=name=="StandaloneSwitch"?"Lever":name=="StandaloneRedLever"?"Lever.001":"Lever.002";
            var t=Child(visual.transform,part);
            var m=Interaction(root,name=="StandaloneSwitch"?"Panel switch":name=="StandaloneRedLever"?"Red main lever":"Generator lever",name=="StandaloneGeneratorLever"?0.65f:0.35f,AddAnimator(t.gameObject,name,name,part,true));
            foreach(var f in t.GetComponentsInChildren<MeshFilter>())
            {
                Object.DestroyImmediate(f.GetComponent<MeshCollider>());Box(f.gameObject,0.055f);
            }
        }
        else
        {
            var t=Child(visual.transform,"Rocker");Object.DestroyImmediate(t.GetComponent<MeshCollider>());Box(t.gameObject,0.025f);
            var m=Interaction(root,"Light switch",0.2f,AddAnimator(t.gameObject,name,"LightSwitch","Rocker",true));
        }
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(root);
    }
    static GameObject Instance(string name,Transform parent,Vector3 pos)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab"));
        go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(0,180,0);return go;
    }
    static void BuildScene(Scene scene)
    {
        var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="NP_InteractiveMechanics");if(old)Object.DestroyImmediate(old);
        var area=new GameObject("NP_InteractiveMechanics");area.transform.position=new Vector3(0,0,-16);
        Instance("SingleDoor",area.transform,new Vector3(-5.6f,0,0));
        Instance("DoubleDoor",area.transform,new Vector3(-2.8f,0,0));
        Instance("ElectricalPanel",area.transform,new Vector3(0.6f,0.55f,0));
        Instance("Generator",area.transform,new Vector3(3.1f,0,0));
        var lightSwitch=Instance("LightSwitch",area.transform,new Vector3(5,1.45f,-0.12f));
        lightSwitch.transform.localScale=Vector3.one*1.5f;
        var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="LightSwitchSupport";post.transform.SetParent(area.transform,false);
        post.transform.localPosition=new Vector3(5,1.2f,0.02f);post.transform.localScale=new Vector3(0.3f,2.4f,0.2f);post.GetComponent<Renderer>().sharedMaterial=Materials["DemoSupport"];
        var lampGO=new GameObject("SwitchControlledLamp");lampGO.transform.SetParent(area.transform,false);lampGO.transform.localPosition=new Vector3(4.5f,2.9f,-1.2f);
        var lamp=lampGO.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=7;lamp.intensity=4;lamp.color=new Color(1f,0.86f,0.65f);lamp.enabled=false;
        lightSwitch.GetComponent<MechanicalInteractable>().controlledLights=new[]{lamp};
        // Existing environment is retained. Supply collision where imported meshes lacked it.
        foreach(var g in scene.GetRootGameObjects().Where(g=>g!=area))
        {
            foreach(var mf in g.GetComponentsInChildren<MeshFilter>())
                if(!mf.GetComponent<Collider>() && mf.sharedMesh) {var c=mf.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;}
            foreach(var cam in g.GetComponentsInChildren<Camera>())
            {
                if(cam.CompareTag("MainCamera")){cam.enabled=false;var listener=cam.GetComponent<AudioListener>();if(listener)listener.enabled=false;}
            }
        }
        var player=new GameObject("Greybox_Player");player.transform.SetParent(area.transform,false);player.transform.localPosition=new Vector3(0,0.12f,-6.5f);
        var cc=player.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=0.28f;cc.center=new Vector3(0,0.9f,0);cc.stepOffset=0.25f;cc.skinWidth=0.03f;
        var cameraGO=new GameObject("PlayerCamera");cameraGO.tag="MainCamera";cameraGO.transform.SetParent(player.transform,false);cameraGO.transform.localPosition=new Vector3(0,1.65f,0);
        var camera=cameraGO.AddComponent<Camera>();camera.fieldOfView=68;camera.nearClipPlane=0.05f;camera.farClipPlane=500;cameraGO.AddComponent<AudioListener>();
        var fps=player.AddComponent<GreyboxFirstPerson>();fps.viewCamera=camera;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,1,-16),Quaternion.Euler(15,0,0),11);
    }

    public static string Validate()
    {
        var report=new List<string>();
        foreach(var name in Names)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab");
            Require(prefab,"Missing prefab "+name);
            Require(prefab.GetComponentsInChildren<MeshRenderer>().Length>0,"Missing visible geometry "+name);
            var controls=prefab.GetComponentsInChildren<MechanicalInteractable>();
            Require(controls.Length==(name=="ElectricalPanel"?7:1),"Independent control count "+name);
            foreach(var c in controls)
            {
                Require(c.animators!=null && c.animators.Length>0,"Missing animator "+c.name);
                foreach(var a in c.animators)
                {
                    Require(a.runtimeAnimatorController,"Missing controller "+a.name);
                    Require(a.runtimeAnimatorController.animationClips.All(clip=>AnimationUtility.GetCurveBindings(clip).Length>0),"Empty animation "+a.name);
                }
            }
            report.Add("PASS "+name+": "+prefab.GetComponentsInChildren<MeshRenderer>().Length+" renderers, "+controls.Length+" independent controls");
        }
        var player=Object.FindAnyObjectByType<GreyboxFirstPerson>();Require(player,"Missing first-person controller");
        Require(player.viewCamera && player.viewCamera.enabled,"Missing player camera");
        Physics.SyncTransforms();
        Require(Physics.Raycast(player.transform.position+Vector3.up,Vector3.down,3),"Player has no ground");
        report.Add("PASS Greybox player, active camera and ground collision");
        string result=string.Join("\n",report);File.WriteAllText(Path.Combine(Work,"unity_validation.txt"),result);return result;
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}

    public static string RuntimeTest()
    {
        if(!Application.isPlaying)throw new Exception("Runtime test requires Play Mode.");
        var controls=Object.FindObjectsByType<MechanicalInteractable>();
        var report=new List<string>();
        Require(controls.Length==11,"Expected 11 independent targets (2 doors, 7 panel, generator, light)");
        foreach(var c in controls){c.SetState(false);c.Advance(10f);}
        Physics.SyncTransforms();
        foreach(var c in controls)
        {
            var before=controls.ToDictionary(x=>x,x=>x.Progress);
            var poseBefore=c.animators.SelectMany(a=>a.GetComponentsInChildren<Transform>()).Distinct().ToDictionary(t=>t,t=>t.localRotation);
            c.Toggle();c.Advance(c.travelSeconds*0.4f);Require(c.Progress>0.35f && c.Progress<0.45f,"Midpoint "+c.name);
            c.Toggle();c.Advance(c.travelSeconds*0.1f);Require(c.Progress>0.25f && c.Progress<0.35f,"Reversal "+c.name);
            c.SetState(true);c.Advance(10f);Require(c.IsOn && !c.IsMoving && c.Progress==1,"On endpoint "+c.name);
            Require(poseBefore.Any(p=>Quaternion.Angle(p.Value,p.Key.localRotation)>5f),"Animator did not move geometry: "+c.name);
            foreach(var other in controls.Where(x=>x!=c))Require(other.Progress==before[other],"Unrelated lever moved: "+other.name);
            c.SetState(false);c.Advance(10f);Require(!c.IsOn && c.Progress==0,"Off endpoint "+c.name);
            foreach(var p in poseBefore)Require(Quaternion.Angle(p.Value,p.Key.localRotation)<0.3f,"Did not restore pose "+c.name);
            report.Add("PASS "+c.displayName+": moves, reverses, restores, independent");
        }
        // Ray-based interaction must not pass through walls and must address the correct lever.
        Physics.SyncTransforms();
        var panel=controls.Where(c=>c.name.StartsWith("Switch") || c.name=="MainLever").ToArray();
        foreach(var c in panel)
        {
            var col=c.GetComponent<Collider>();Require(col,"Missing lever collider");
            var center=col.bounds.center;var ray=new Ray(center+Vector3.back*0.8f,Vector3.forward);
            Require(GreyboxFirstPerson.FindTarget(ray,1f)==c,"Aim selects incorrect lever: "+c.name);
        }
        report.Add("PASS All seven panel levers can be individually targeted from the front");
        foreach(var name in new[]{"SingleDoor","DoubleDoor"})
        {
            var door=controls.First(c=>c.gameObject.name==name);var center=door.transform.position+Vector3.up*1.1f;
            var ray=new Ray(center+Vector3.back,Vector3.forward);
            Require(Physics.Raycast(ray,2f),"Closed door has no collision "+name);
            door.SetState(true);door.Advance(10);Physics.SyncTransforms();
            Require(!Physics.Raycast(ray,2f),"Open doorway remains blocked "+name);
            door.SetState(false);door.Advance(10);Physics.SyncTransforms();
        }
        report.Add("PASS Closed doors block; opened doorways clear");
        var lightSwitch=controls.First(c=>c.gameObject.name=="LightSwitch");
        lightSwitch.SetState(true);Require(lightSwitch.controlledLights.All(l=>l.enabled),"Light failed on");
        lightSwitch.SetState(false);Require(lightSwitch.controlledLights.All(l=>!l.enabled),"Light failed off");
        report.Add("PASS Light switch changes the scene lamp");
        var player=Object.FindAnyObjectByType<GreyboxFirstPerson>();var body=player.GetComponent<CharacterController>();
        var savedPosition=player.transform.position;
        body.Move(Vector3.down*2f);Require(body.isGrounded,"Controller does not ground on floor");
        var grounded=player.transform.position;body.Move(Vector3.right*0.5f);
        Require(Vector3.Distance(grounded,player.transform.position)>0.45f,"Controller cannot walk on ground");
        var testDoor=controls.First(c=>c.gameObject.name=="SingleDoor");
        body.enabled=false;player.transform.position=testDoor.transform.position+Vector3.back*1.0f+Vector3.up*0.05f;body.enabled=true;
        body.Move(Vector3.forward*2f);Require(player.transform.position.z<testDoor.transform.position.z-0.2f,"Player walks through closed door");
        testDoor.SetState(true);testDoor.Advance(10);Physics.SyncTransforms();
        body.Move(Vector3.forward*1.7f);Require(player.transform.position.z>testDoor.transform.position.z+0.4f,"Player cannot pass open door");
        testDoor.SetState(false);testDoor.Advance(10);body.enabled=false;player.transform.position=savedPosition;body.enabled=true;
        report.Add("PASS CharacterController grounds, walks, blocks at closed door and passes open door");
        string result=string.Join("\n",report);File.WriteAllText(Path.Combine(Work,"runtime_validation.txt"),result);return result;
    }
    public static string Capture()
    {
        var player=Object.FindAnyObjectByType<GreyboxFirstPerson>();Require(player,"Missing player");
        var cam=player.viewCamera;
        var target=new RenderTexture(1600,900,24);var previous=cam.targetTexture;
        cam.targetTexture=target;cam.Render();
        var prev=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
        string path=Path.Combine(Work,"greybox_preview.png");File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=prev;cam.targetTexture=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);return path;
    }
}
