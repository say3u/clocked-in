using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene-only authoring tool: leaves model files, furniture and lighting alone.
public static class OfficeWallColliderTools
{
    [Serializable] public class WallResult
    {
        public string path, action, mesh;
        public int colliders, rayHits;
        public bool blocksPlayerLayer;
    }
    [Serializable] public class Report
    {
        public string scene, backup;
        public int walls, added, repaired;
        public bool lightingUnchanged;
        public List<WallResult> results = new List<WallResult>();
    }
    static string HierarchyPath(Transform t) => t.parent == null ? t.name : HierarchyPath(t.parent) + "/" + t.name;
    static bool IsWall(MeshFilter filter)
    {
        if (!filter.sharedMesh || !filter.GetComponent<MeshRenderer>()) return false;
        string name = filter.name.ToLowerInvariant();
        if (name.Contains("fridge")) return false;
        if (name.Contains("door") && !name.Contains("doorway")) return false;
        var size = filter.GetComponent<MeshRenderer>().bounds.size;
        if (size.y < .5f) return false;
        string cleanName = Regex.Replace(name, @"\.\d+$", "");
        bool namedWall = name.Contains("wall") || name.Contains("partition") || name.Contains("doorway") || name.Contains("bathroom stall") ||
            new [] {"north", "south", "west", "east"}.Any(direction => cleanName == direction || cleanName.EndsWith(" " + direction));
        bool wallShape = size.y >= 2.3f && Mathf.Min(size.x, size.z) <= .75f && Mathf.Max(size.x, size.z) >= .8f;
        return namedWall || wallShape;
    }
    static int CountSurfaceHits(MeshFilter filter, Collider[] colliders)
    {
        var bounds = filter.GetComponent<MeshRenderer>().bounds;
        bool thinX = bounds.size.x < bounds.size.z;
        Vector3 normal = thinX ? Vector3.right : Vector3.forward;
        float extent = thinX ? bounds.extents.x : bounds.extents.z;
        int hits = 0;
        foreach (float height in new [] {.2f, .5f, .8f})
        foreach (float across in new [] {.15f, .35f, .65f, .85f})
        {
            Vector3 origin = bounds.center;
            origin.y = Mathf.Lerp(bounds.min.y, bounds.max.y, height);
            if (thinX) origin.z = Mathf.Lerp(bounds.min.z, bounds.max.z, across);
            else origin.x = Mathf.Lerp(bounds.min.x, bounds.max.x, across);
            origin += normal * (extent + .25f);
            if (colliders.Any(c => c.Raycast(new Ray(origin, -normal), out _, 2 * extent + .5f))) hits++;
        }
        return hits;
    }
    static string LightingState(Scene scene)
    {
        var lights = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true))
            .OrderBy(l => HierarchyPath(l.transform)).Select(l => EditorJsonUtility.ToJson(l));
        return string.Join("\n", lights) + "\n" + RenderSettings.ambientIntensity + "|" + RenderSettings.ambientLight + "|" + RenderSettings.reflectionIntensity;
    }
    public static Report Repair(Scene scene)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode before editing colliders.");
        string originalLighting = LightingState(scene);
        var walls = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)).Where(IsWall).ToArray();
        var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CharacterController>(true)).FirstOrDefault();
        int playerLayer = player ? player.gameObject.layer : 0;
        var report = new Report {scene = scene.path, walls = walls.Length};
        string backupDirectory = "Tools/Backups";
        Directory.CreateDirectory(backupDirectory);
        report.backup = backupDirectory + "/SampleScene.before-wall-colliders-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, report.backup, true))
        {
            // Some Editor versions require scene backups to use an Assets path.
            if (scene.isDirty) throw new IOException("Save your scene before repairing wall colliders.");
            File.Copy(scene.path, report.backup);
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Repair office wall colliders");
        foreach (var wall in walls)
        {
            var result = new WallResult {path = HierarchyPath(wall.transform), mesh = wall.sharedMesh.name, action = "already solid"};
            var colliders = wall.GetComponents<Collider>();
            var solid = colliders.Where(c => c.enabled && !c.isTrigger).ToArray();
            var existing = wall.GetComponent<MeshCollider>();
            if (solid.Length == 0 || (existing && existing.sharedMesh != wall.sharedMesh))
            {
                var collider = existing ? existing : Undo.AddComponent<MeshCollider>(wall.gameObject);
                Undo.RecordObject(collider, "Set wall collision mesh");
                collider.enabled = true;
                collider.isTrigger = false;
                collider.convex = false;
                collider.sharedMesh = wall.sharedMesh;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                if (existing) { report.repaired++; result.action = "repaired collider"; }
                else { report.added++; result.action = "added mesh collider"; }
            }
            report.results.Add(result);
        }
        Physics.SyncTransforms();
        foreach (var result in report.results)
        {
            var wall = walls.Single(w => HierarchyPath(w.transform) == result.path);
            var solid = wall.GetComponents<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
            result.colliders = solid.Length;
            result.rayHits = CountSurfaceHits(wall, solid);
            result.blocksPlayerLayer = !Physics.GetIgnoreLayerCollision(playerLayer, wall.gameObject.layer);
            if (solid.Length == 0) throw new Exception("Wall still has no solid collider: " + result.path);
        }
        report.lightingUnchanged = originalLighting == LightingState(scene);
        if (!report.lightingUnchanged) throw new Exception("Unexpected lighting change.");
        if (report.added + report.repaired > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save wall colliders.");
        }
        Undo.CollapseUndoOperations(group);
        return report;
    }
    [MenuItem("Tools/Office/Repair Wall Colliders")]
    public static void RepairActiveScene()
    {
        var report = Repair(SceneManager.GetActiveScene());
        Debug.Log("Office walls: " + report.walls + " checked, " + report.added + " colliders added, " + report.repaired + " repaired.");
    }
    public static void RunBatch()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var report = Repair(scene);
        Directory.CreateDirectory("Tools/WallColliders");
        File.WriteAllText("Tools/WallColliders/report.json", JsonUtility.ToJson(report, true));
        Debug.Log("OFFICE_WALL_COLLIDERS " + JsonUtility.ToJson(report));
    }
}
