using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Editor-only route probes. Every successful route is traversed using the real
// swimmer collision solver in short steps, including every ascent and detour.
[InitializeOnLoad]
public static class AdventureReachabilityValidation
{
    private const string ActiveKey = "Underwater.Reachability.Active";
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float Step = 0.25f;
    private const float CompletionRadius = 4.15f;
    private static readonly string[] Roles = { "turtle", "manta", "clownfish", "whale" };

    static AdventureReachabilityValidation()
    {
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ActiveKey + ".Deadline", DateTime.UtcNow.AddMinutes(4).Ticks.ToString());
        UnderwaterBuild.CreateScene();
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (DateTime.UtcNow.Ticks > long.Parse(SessionState.GetString(ActiveKey + ".Deadline", "0")))
        {
            SessionState.SetBool(ActiveKey, false);
            Debug.LogError("UNDERWATER_REACHABILITY_FAILED Timeout entering the production world");
            EditorApplication.Exit(1);
            return;
        }
        if (!EditorApplication.isPlaying || Time.frameCount < 3 || OceanMissionDirector.Instance == null) return;
        SessionState.SetBool(ActiveKey, false);
        try
        {
            ValidateInPlayMode();
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogError("UNDERWATER_REACHABILITY_FAILED " + error);
            EditorApplication.Exit(1);
        }
    }

    public static void ValidateInPlayMode()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Route probes require the production Play Mode world.");
        OceanMissionDirector director = OceanMissionDirector.Instance;
        CinematicDive dive = Camera.main.GetComponent<CinematicDive>();
        List<GameObject> beacons = (List<GameObject>)typeof(OceanMissionDirector).GetField("adventureBeacons", Fields).GetValue(director);
        Physics.SyncTransforms();
        foreach (GameObject beacon in beacons) ReportBeaconGeometry(beacon.transform.position);
        ValidateRescueSightline();

        GameObject probe = new("Editor-only continuous route probe");
        string previousRole = "turtle";
        int passed = 0;
        List<string> failures = new();
        try
        {
            foreach (string role in Roles)
            {
                dive.ConfigurePlayerRole(role);
                float radius = Read(dive, "playerRadius");
                float clearance = Read(dive, "playerFloorClearance");
                float nose = Read(dive, "playerNoseDistance");
                Vector3 position = new(0f, 4.3f, -7f);
                for (int index = 0; index < beacons.Count; index++)
                {
                    Vector3 target = beacons[index].transform.position;
                    bool found = false;
                    float bestDistance = float.MaxValue;
                    Vector3 bestPosition = position;
                    int attempts = 0;
                    foreach (Vector3[] route in CandidateRoutes(position, target))
                    {
                        attempts++;
                        if (Traverse(probe.transform, position, target, route, radius, clearance, nose,
                            out Vector3 result, out float length, out int contacts, out float closest))
                        {
                            Debug.Log($"UNDERWATER_REACHABLE {role} beacon={index + 1} attempts={attempts} travelled={length:F2}m nearest={closest:F3}m contacts={contacts} from={position:F3} end={result:F3} waypoints={Describe(route)}");
                            position = result;
                            passed++;
                            found = true;
                            break;
                        }
                        if (closest < bestDistance) { bestDistance = closest; bestPosition = result; }
                    }
                    if (!found)
                    {
                        string failure = $"{role} beacon={index + 1} attempts={attempts} nearest={bestDistance:F3}m last={bestPosition:F3} target={target:F3}";
                        Debug.LogWarning("UNDERWATER_ROUTE_NOT_FOUND " + failure);
                        failures.Add(failure);
                        // Do not teleport the probe to a later stage: its route depends on reaching this one.
                        break;
                    }
                }
                previousRole = role;
            }
        }
        finally
        {
            dive.ConfigurePlayerRole(previousRole);
            UnityEngine.Object.Destroy(probe);
        }
        if (failures.Count > 0)
            throw new InvalidOperationException("Finite route candidates did not establish reachability; this does not prove no route exists. " + string.Join("; ", failures));
        Debug.Log($"UNDERWATER_REACHABILITY_VALIDATED {passed}/12 role/beacon routes; continuous {Step:F2}m collision steps, no teleport or collision bypass");
    }

    private static IEnumerable<Vector3[]> CandidateRoutes(Vector3 start, Vector3 target)
    {
        yield return new[] { target };
        yield return new[] { new Vector3(start.x, 7.2f, start.z), new Vector3(target.x, 7.2f, target.z), target };
        Vector3[] approachDirections = { Vector3.up, Vector3.left, Vector3.right, Vector3.back, Vector3.forward,
            new Vector3(-1f, 1f, 0f).normalized, new Vector3(1f, 1f, 0f).normalized,
            new Vector3(0f, 1f, -1f).normalized, new Vector3(0f, 1f, 1f).normalized };
        foreach (float offset in new[] { -8f, 8f, -16f, 16f, -24f, 24f })
        foreach (Vector3 direction in approachDirections)
        {
            Vector3 goal = target + direction * 3.9f;
            float lane = target.x + offset;
            yield return new[] { new Vector3(start.x, 7.2f, start.z), new Vector3(lane, 7.2f, start.z),
                new Vector3(lane, 7.2f, goal.z), new Vector3(lane, goal.y, goal.z), goal };
        }
    }

    private static bool Traverse(Transform mover, Vector3 start, Vector3 target, Vector3[] route,
        float radius, float clearance, float nose, out Vector3 result, out float length, out int contacts, out float closest)
    {
        mover.position = start;
        length = 0f;
        contacts = 0;
        closest = Vector3.Distance(start, target);
        foreach (Vector3 waypoint in route)
        {
            int stalled = 0;
            for (int step = 0; step < 2500; step++)
            {
                closest = Mathf.Min(closest, Vector3.Distance(mover.position, target));
                if (closest < CompletionRadius) { result = mover.position; return true; }
                float beforeDistance = Vector3.Distance(mover.position, waypoint);
                if (beforeDistance < 0.08f) break;
                Vector3 desired = Vector3.MoveTowards(mover.position, waypoint, Step);
                Vector3 next = UnderwaterCollision.ResolveMove(mover, desired, radius, clearance, nose, out bool adjusted);
                if (adjusted) contacts++;
                if (next.y > 7.501f || next.y < -17.001f) { result = mover.position; return false; }
                length += Vector3.Distance(mover.position, next);
                mover.position = next;
                float afterDistance = Vector3.Distance(next, waypoint);
                stalled = afterDistance >= beforeDistance - 0.001f ? stalled + 1 : 0;
                if (stalled >= 24) { result = mover.position; return false; }
            }
        }
        result = mover.position;
        closest = Mathf.Min(closest, Vector3.Distance(result, target));
        return closest < CompletionRadius;
    }

    private static void ReportBeaconGeometry(Vector3 position)
    {
        List<string> touching = new();
        foreach (Collider collider in Physics.OverlapSphere(position, 0.15f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (collider.GetComponentInParent<UnderwaterObstacle>() != null)
                touching.Add(collider.transform.root.name + "/" + collider.name);
        }
        Debug.Log($"UNDERWATER_BEACON_GEOMETRY position={position:F3} floor={UnderwaterWorld.SampleSeafloorHeight(position.x, position.z):F3} solidNearCenter={string.Join(",", touching)}");
    }

    public static void ValidateRescueSightline()
    {
        OceanMissionDirector director = OceanMissionDirector.Instance;
        GameObject turtle = (GameObject)typeof(OceanMissionDirector).GetField("rescueTurtle", Fields).GetValue(director);
        if (turtle == null) throw new InvalidOperationException("Rescue turtle missing in production world");
        FishingLineRescue line = (FishingLineRescue)typeof(OceanMissionDirector).GetField("fishingLine", Fields).GetValue(director);
        bool temporary = line == null;
        if (temporary)
        {
            line = turtle.AddComponent<FishingLineRescue>();
            line.Initialize(director);
        }
        try
        {
            Renderer[] renderers = turtle.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (!line.TryGetNextKnotPosition(out Vector3 knot)) throw new InvalidOperationException("Rescue knot is missing");
            Physics.SyncTransforms();
            Vector3 camera = turtle.transform.position + new Vector3(-5.5f, 1.5f, -8f);
            string bodyBlocker = Blocker(camera, bounds.center, turtle.transform);
            string knotBlocker = Blocker(camera, knot, turtle.transform);
            Debug.Log($"UNDERWATER_RESCUE_SIGHTLINE camera={camera:F3} turtle={turtle.transform.position:F3} center={bounds.center:F3} knot={knot:F3} bodyBlocker={bodyBlocker} knotBlocker={knotBlocker}");
            if (bodyBlocker == "clear" && knotBlocker == "clear") return;
            foreach (float height in new[] { 1.5f, 3f, 4.5f })
            for (int angle = 0; angle < 16; angle++)
            {
                float radians = angle * Mathf.PI / 8f;
                Vector3 candidate = turtle.transform.position + new Vector3(Mathf.Sin(radians) * 9.7f, height, Mathf.Cos(radians) * 9.7f);
                candidate.y = Mathf.Min(candidate.y, 7.2f);
                if (candidate.y < UnderwaterWorld.SampleSeafloorHeight(candidate.x, candidate.z) + 1.85f) continue;
                if (Blocker(candidate, bounds.center, turtle.transform) != "clear" || Blocker(candidate, knot, turtle.transform) != "clear") continue;
                bool occupied = false;
                foreach (Collider collider in Physics.OverlapSphere(candidate, 1.1f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                    if (collider.GetComponentInParent<UnderwaterObstacle>() != null && !collider.transform.IsChildOf(turtle.transform)) occupied = true;
                if (occupied) continue;
                Debug.Log($"UNDERWATER_RESCUE_CLEAR_VIEW candidate={candidate:F3} bodyAndKnotClear=true radius=1.1 offset={(candidate - turtle.transform.position):F3}");
                return;
            }
            Debug.LogWarning("UNDERWATER_RESCUE_CLEAR_VIEW No sampled clear view; additional camera candidates are needed");
        }
        finally
        {
            if (temporary)
            {
                line.RemoveAll();
                UnityEngine.Object.Destroy(line);
            }
        }
    }

    private static string Blocker(Vector3 origin, Vector3 target, Transform turtle)
    {
        Vector3 delta = target - origin;
        RaycastHit[] hits = Physics.RaycastAll(origin, delta.normalized, delta.magnitude - 0.02f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(turtle) || hit.collider.GetComponentInParent<UnderwaterObstacle>() == null) continue;
            return hit.collider.name + " parent=" + hit.collider.transform.parent?.name + " distance=" + hit.distance.ToString("F3");
        }
        return "clear";
    }

    private static float Read(CinematicDive dive, string name) => (float)typeof(CinematicDive).GetField(name, Fields).GetValue(dive);
    private static string Describe(Vector3[] route)
    {
        List<string> points = new();
        foreach (Vector3 point in route) points.Add(point.ToString("F2"));
        return string.Join(" -> ", points);
    }
}
