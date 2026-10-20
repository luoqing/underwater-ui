using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MovementSeafloorValidation
{
    [MenuItem("Underwater/Validate Movement And Seafloor")]
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject floor = new("Validation floor");
        floor.AddComponent<InfiniteSeafloorGrid>().Initialize(null, null);
        Dictionary<Vector2, Vector3> sharedNormals = new();
        int samples = 0;
        foreach (MeshFilter filter in floor.GetComponentsInChildren<MeshFilter>())
        {
            if (!filter.name.StartsWith("Fixed Seafloor")) continue;
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < vertices.Length; i += 64)
            {
                Vector3 point = filter.transform.TransformPoint(vertices[i]);
                Require(Mathf.Abs(point.y - UnderwaterWorld.SampleSeafloorHeight(point.x, point.z)) < 0.0001f,
                    "Seafloor vertex/collision mismatch");
                Vector2 key = new(point.x, point.z);
                if (sharedNormals.TryGetValue(key, out Vector3 normal))
                    Require(Vector3.Distance(normal, normals[i]) < 0.0001f, "Seafloor normal seam");
                sharedNormals[key] = normals[i];
                samples++;
            }
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 6 * 67)
            {
                Vector3 point = vertices[triangles[i]] * 0.21f + vertices[triangles[i + 1]] * 0.37f
                    + vertices[triangles[i + 2]] * 0.42f;
                point = filter.transform.TransformPoint(point);
                Require(Mathf.Abs(point.y - UnderwaterWorld.SampleSeafloorHeight(point.x, point.z)) < 0.0001f,
                    "Seafloor triangle/collision mismatch");
                samples++;
            }
        }
        Require(typeof(InfiniteSeafloorGrid).GetMethod("Update", System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic) == null, "Seafloor still relocates during Update");

        GameObject swimmer = new("Validation swimmer");
        Vector3 origin = new(0f, 25f, 0f);
        foreach (Vector3 direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down })
        {
            swimmer.transform.position = origin;
            Vector3 result = UnderwaterCollision.ResolveMove(swimmer.transform, origin + direction, 0.4f, 0.7f, 0.5f, out _);
            Require(Vector3.Distance(result, origin + direction) < 0.001f, "A swim direction is blocked in open water");
        }

        BoxCollider wall = CreateObstacle("Wall", new Vector3(0f, 25f, 3f), new Vector3(6f, 6f, 1f));
        Physics.SyncTransforms();
        swimmer.transform.position = origin;
        Vector3 stopped = UnderwaterCollision.ResolveMove(swimmer.transform, origin + Vector3.forward * 7f, 0.4f, 0.7f, 0.5f, out bool adjusted);
        Require(adjusted && stopped.z < 2.11f, "Swimmer passes through a wall");
        swimmer.transform.position = stopped;
        Vector3 backed = UnderwaterCollision.ResolveMove(swimmer.transform, stopped - Vector3.forward, 0.4f, 0.7f, 0.5f, out _);
        Require(backed.z < stopped.z - 0.99f, "Swimmer cannot back away from a wall");

        swimmer.transform.position = wall.bounds.center;
        Vector3 separated = UnderwaterCollision.ResolveMove(swimmer.transform, swimmer.transform.position + Vector3.forward * 0.1f,
            0.4f, 0.7f, 0.5f, out adjusted);
        Debug.Log($"UNDERWATER_OVERLAP_CHECK adjusted={adjusted} result={separated} surfaceDistance={Vector3.Distance(wall.ClosestPoint(separated), separated)}");
        Require(adjusted && Vector3.Distance(wall.ClosestPoint(separated), separated) >= 0.399f,
            "An initially overlapping swimmer remains inside an obstacle");

        CreateObstacle("Arch left", new Vector3(10f, 25f, 0f), new Vector3(1f, 6f, 2f));
        CreateObstacle("Arch right", new Vector3(14f, 25f, 0f), new Vector3(1f, 6f, 2f));
        CreateObstacle("Arch lintel", new Vector3(12f, 28.5f, 0f), new Vector3(5f, 1f, 2f));
        Physics.SyncTransforms();
        swimmer.transform.position = new Vector3(12f, 25f, -4f);
        Vector3 through = UnderwaterCollision.ResolveMove(swimmer.transform, new Vector3(12f, 25f, 4f),
            0.4f, 0.7f, 0.5f, out adjusted);
        Require(!adjusted && Vector3.Distance(through, new Vector3(12f, 25f, 4f)) < 0.001f,
            "A clear arch opening blocks the swimmer");

        swimmer.transform.position = new Vector3(0f, 10f, 40f);
        Vector3 grounded = UnderwaterCollision.ResolveMove(swimmer.transform, new Vector3(0f, -20f, 40f),
            0.4f, 0.7f, 0f, out adjusted);
        Require(adjusted && grounded.y >= UnderwaterWorld.SampleSeafloorHeight(grounded.x, grounded.z) + 0.699f,
            "Swimmer passes below the visible seafloor");
        swimmer.transform.position = new Vector3(399f, 25f, 64f);
        Vector3 boundary = UnderwaterCollision.ResolveMove(swimmer.transform, new Vector3(700f, 25f, 64f),
            0.4f, 0.7f, 0f, out adjusted);
        Require(adjusted && boundary.x <= 400f, "Swimmer can reach the mesh edge");
        Debug.Log($"UNDERWATER_MOVEMENT_VALIDATED {samples} terrain samples; six directions; wall stop/back; initial overlap; arch opening; floor; exploration boundary");
        ValidateFont();
    }

    public static void ValidateFont()
    {
        Font font = Resources.Load<Font>("Fonts/BlueLanternUI");
        Require(font != null, "Standalone UI font was not imported");
        const string characters = "W0123456789/、·○●↑↓←→海龟第盏距离米";
        font.RequestCharactersInTexture(characters, 24, FontStyle.Normal);
        Font latin = Resources.Load<Font>("Fonts/LiberationSans-Regular");
        if (latin != null) latin.RequestCharactersInTexture(characters, 24, FontStyle.Normal);
        foreach (char value in characters)
        {
            bool available = font.GetCharacterInfo(value, out CharacterInfo info, 24, FontStyle.Normal);
            Require(available && info.advance > 0, $"Standalone UI font has no glyph for U+{(int)value:X4}");
            bool latinAvailable = latin != null && latin.GetCharacterInfo(value, out _, 24, FontStyle.Normal);
            Debug.Log($"UNDERWATER_FONT_CHECK U+{(int)value:X4} '{value}' mainHas={font.HasCharacter(value)} mainGlyph={available} advance={info.advance} latinGlyph={latinAvailable}");
        }
        Debug.Log($"UNDERWATER_FONT_NAMES {string.Join(", ", font.fontNames)}");
    }

    private static BoxCollider CreateObstacle(string name, Vector3 center, Vector3 size)
    {
        GameObject obstacle = new(name);
        obstacle.transform.position = center;
        obstacle.AddComponent<UnderwaterObstacle>();
        BoxCollider collider = obstacle.AddComponent<BoxCollider>();
        collider.size = size;
        return collider;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
