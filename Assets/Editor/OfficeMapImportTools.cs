using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class OfficeMapImportTools
{
    const string ScenePath="Assets/Scenes/SampleScene.unity";
    const string ModelPath="Assets/HorrorGame_Office.fbx";
    const string Work="Tools/MapExport";
    [Serializable] public class LightState { public string path,json; public Vector3 position,scale; public Quaternion rotation; }
    [Serializable] public class Baseline {public string environment,guid;public LightState[] lights;}
    [Serializable] public class MaterialRecord {public string name,baseTexture;public float[] color;public float metallic,smoothness;}
    [Serializable] public class MaterialManifest {public MaterialRecord[] materials;}
    static string PathOf(Transform t) => t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static string EnvironmentBlock() => Regex.Match(File.ReadAllText(ScenePath),@"(?ms)^--- !u!104 .*?(?=^--- !u!)").Value;
    static string LightJson(Light light)
    {
        string json=EditorJsonUtility.ToJson(light);
        // Identity/owner pointers are session-specific and must never be restored.
        foreach(string key in new[]{"m_GameObject","m_CorrespondingSourceObject","m_PrefabInstance","m_PrefabAsset"})
            json=Regex.Replace(json,"\\\""+key+"\\\"\\s*:\\s*\\{[^}]*\\}\\s*,?","");
        return json;
    }
    public static void CaptureBeforeExport()
    {
        Directory.CreateDirectory(Work);
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var baseline=new Baseline {environment=EnvironmentBlock(),guid=AssetDatabase.AssetPathToGUID(ModelPath),
            lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Select(l=>new LightState {
                path=PathOf(l.transform),json=LightJson(l),position=l.transform.localPosition,rotation=l.transform.localRotation,scale=l.transform.localScale}).ToArray()};
        File.WriteAllText(Work+"/before.json",JsonUtility.ToJson(baseline,true));
        string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        File.Copy(ScenePath,Work+"/SampleScene.before-"+stamp+".unity");
        File.Copy(ModelPath,Work+"/HorrorGame_Office.before-"+stamp+".fbx");
        Debug.Log("OFFICE_EXPORT_BASELINE_SAVED lights="+baseline.lights.Length);
    }
    public static void FinishImport()
    {
        Directory.CreateDirectory(Work);
        var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText(Work+"/before.json"));
        AssetDatabase.ImportAsset(ModelPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        if(AssetDatabase.AssetPathToGUID(ModelPath)!=baseline.guid)throw new Exception("Office GUID changed.");
        var manifest=JsonUtility.FromJson<MaterialManifest>(File.ReadAllText(Work+"/materials.json"));
        string materialFolder="Assets/OfficeAssetPack/Materials/ImportedMap";
        if(!AssetDatabase.IsValidFolder(materialFolder))AssetDatabase.CreateFolder("Assets/OfficeAssetPack/Materials","ImportedMap");
        var remaps=importer.GetExternalObjectMap();
        int created=0;
        foreach(var record in manifest.materials)
        {
            var id=new AssetImporter.SourceAssetIdentifier(typeof(Material),record.name);
            if(remaps.TryGetValue(id,out var mapped) && mapped!=null)continue;
            if(record.name.StartsWith("M_Office_2.",StringComparison.Ordinal))
            {
                importer.AddRemap(id,AssetDatabase.LoadAssetAtPath<Material>("Assets/OfficeAssetPack/Materials/Office_Atlas_2.mat"));
                continue;
            }
            // Preserve established imported materials; only externalize new asset materials.
            bool newAsset=record.name.StartsWith("Keypad",StringComparison.Ordinal) || record.name=="Buttons" || record.name=="Metal.001";
            if(!newAsset)continue;
            string path=materialFolder+"/"+Regex.Replace(record.name,@"[^A-Za-z0-9_.-]","_")+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor",new Color(record.color[0],record.color[1],record.color[2],record.color[3]));
                material.SetFloat("_Metallic",record.metallic);material.SetFloat("_Smoothness",record.smoothness);
                if(!string.IsNullOrEmpty(record.baseTexture))material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(record.baseTexture));
                AssetDatabase.CreateAsset(material,path);created++;
            }
            importer.AddRemap(id,material);
        }
        importer.SaveAndReimport();AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var roots=scene.GetRootGameObjects();
        var office=roots.Single(g=>g.name=="HorrorGame_Office");
        PrefabUtility.RemoveUnusedOverrides(new[]{office},InteractionMode.AutomatedAction);
        var lights=roots.SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>PathOf(l.transform));
        foreach(var original in baseline.lights)
        {
            if(!lights.TryGetValue(original.path,out var light))throw new Exception("Missing original light: "+original.path);
            EditorJsonUtility.FromJsonOverwrite(original.json,light);
            light.transform.localPosition=original.position;light.transform.localRotation=original.rotation;light.transform.localScale=original.scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);PrefabUtility.RecordPrefabInstancePropertyModifications(light.transform);
        }
        int colliders=0;
        foreach(var filter in office.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh)throw new Exception("Missing mesh: "+filter.name);
            foreach(var collider in filter.GetComponents<MeshCollider>())
            {
                collider.sharedMesh=null;collider.sharedMesh=filter.sharedMesh;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);colliders++;
            }
        }
        var wallReport=OfficeWallColliderTools.Repair(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save updated map.");
        if(EnvironmentBlock()!=baseline.environment)throw new Exception("Scene environmental lighting changed.");
        foreach(var original in baseline.lights)
            if(LightJson(lights[original.path])!=original.json)throw new Exception("Lighting differs: "+original.path);
        if(lights.Count!=baseline.lights.Length)throw new Exception("Unexpected extra light sources.");
        var filters=office.GetComponentsInChildren<MeshFilter>(true);
        var renderers=office.GetComponentsInChildren<MeshRenderer>(true);
        var keypad=office.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name=="Server Lab Keypad");
        if(!keypad)throw new Exception("Keypad is missing from the imported map.");
        if(renderers.Any(r=>r.sharedMaterials.Any(m=>m==null)))throw new Exception("A map renderer has a missing material.");
        Physics.SyncTransforms();
        IGPUResidentRenderPipeline.ReinitializeGPUResidentDrawer();
        var result=new {saved=scene.path,meshes=filters.Length,keypad=PathOf(keypad),keypadWorldPosition=keypad.position.ToString(),newMaterials=created,refreshedColliders=colliders,addedWallColliders=wallReport.added,repairedWallColliders=wallReport.repaired,lightingUnchanged=true};
        File.WriteAllText(Work+"/result.json",JsonUtility.ToJson(new ExportResult {saved=scene.path,meshes=filters.Length,keypad=PathOf(keypad),newMaterials=created,refreshedColliders=colliders,addedWallColliders=wallReport.added,lightingUnchanged=true},true));
        Debug.Log("OFFICE_MAP_EXPORT_VERIFIED "+JsonUtility.ToJson(new ExportResult {saved=scene.path,meshes=filters.Length,keypad=PathOf(keypad),newMaterials=created,refreshedColliders=colliders,addedWallColliders=wallReport.added,lightingUnchanged=true}));
    }
    [Serializable] public class ExportResult {public string saved,keypad;public int meshes,newMaterials,refreshedColliders,addedWallColliders;public bool lightingUnchanged;}
}
