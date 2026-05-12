using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds and repairs the baseline arena at runtime so network test builds do not
/// depend on fragile scene wiring. Existing named scene objects are reused.
/// </summary>
public static class ArenaMapRuntimeBuilder
{
    private const string RootName = "RuntimeArena";

    public static Transform[] EnsureArena()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            root = new GameObject(RootName);

        EnsureLighting(root.transform);
        EnsurePlatform(root.transform, "MainPlatform", new Vector3(0f, 0f, 0f), new Vector3(22f, 0.55f, 22f), true);
        EnsurePlatform(root.transform, "NorthLift", new Vector3(0f, 2.5f, 9.5f), new Vector3(7f, 0.35f, 2.2f), false);
        EnsurePlatform(root.transform, "SouthLift", new Vector3(0f, 2.5f, -9.5f), new Vector3(7f, 0.35f, 2.2f), false);
        EnsurePlatform(root.transform, "EastLift", new Vector3(9.5f, 2.5f, 0f), new Vector3(2.2f, 0.35f, 7f), false);
        EnsurePlatform(root.transform, "WestLift", new Vector3(-9.5f, 2.5f, 0f), new Vector3(2.2f, 0.35f, 7f), false);
        EnsureEdgeRails(root.transform);
        EnsureJumpPads(root.transform);
        EnsureImpulseGates(root.transform);
        EnsureSpinnerHazard(root.transform);
        EnsureDeathZone(root.transform);

        return EnsureSpawnPoints(root.transform);
    }

    private static void EnsureLighting(Transform root)
    {
        if (GameObject.Find("ArenaKeyLight") == null)
        {
            var key = new GameObject("ArenaKeyLight");
            key.transform.SetParent(root);
            key.transform.position = new Vector3(-4f, 11f, -5f);
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.75f, 0.9f, 1f);
            light.intensity = 0.8f;
            key.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        Color[] colors =
        {
            new Color(0f, 0.75f, 1f),
            new Color(1f, 0.18f, 0.58f),
            new Color(0.6f, 0.2f, 1f),
            new Color(1f, 0.83f, 0f)
        };

        Vector3[] positions =
        {
            new Vector3(11f, 4f, 11f),
            new Vector3(-11f, 4f, 11f),
            new Vector3(11f, 4f, -11f),
            new Vector3(-11f, 4f, -11f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            string name = $"ArenaNeonLight_{i + 1}";
            if (GameObject.Find(name) != null) continue;
            var go = new GameObject(name);
            go.transform.SetParent(root);
            go.transform.position = positions[i];
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 13f;
            light.intensity = 2.2f;
            light.color = colors[i];
        }
    }

    private static void EnsurePlatform(Transform root, string name, Vector3 position, Vector3 scale, bool cylinder)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = GameObject.CreatePrimitive(cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root);
        }

        go.transform.position = position;
        go.transform.localScale = scale;
        SetLayer(go, "Ground");

        var collider = go.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = false;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateMat(name + "_Mat", new Color(0.045f, 0.055f, 0.08f), new Color(0f, 0.45f, 0.85f));
    }

    private static void EnsureEdgeRails(Transform root)
    {
        Vector3[] positions =
        {
            new Vector3(0f, 0.85f, 11.2f),
            new Vector3(0f, 0.85f, -11.2f),
            new Vector3(11.2f, 0.85f, 0f),
            new Vector3(-11.2f, 0.85f, 0f)
        };
        Vector3[] scales =
        {
            new Vector3(14f, 0.4f, 0.35f),
            new Vector3(14f, 0.4f, 0.35f),
            new Vector3(0.35f, 0.4f, 14f),
            new Vector3(0.35f, 0.4f, 14f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            string name = $"LowRail_{i + 1}";
            GameObject rail = GameObject.Find(name);
            if (rail == null)
            {
                rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = name;
                rail.transform.SetParent(root);
            }

            rail.transform.position = positions[i];
            rail.transform.localScale = scales[i];
            SetLayer(rail, "Ground");
            var renderer = rail.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreateMat(name + "_Mat", new Color(0.02f, 0.02f, 0.035f), new Color(1f, 0.18f, 0.58f));
        }
    }

    private static void EnsureDeathZone(Transform root)
    {
        GameObject zone = GameObject.Find("DeathZone");
        if (zone == null)
        {
            zone = new GameObject("DeathZone");
            zone.transform.SetParent(root);
            zone.AddComponent<BoxCollider>();
        }

        zone.transform.position = new Vector3(0f, -8f, 0f);
        var box = zone.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = true;
            box.size = new Vector3(90f, 5f, 90f);
        }
    }

    private static void EnsureJumpPads(Transform root)
    {
        Vector3[] positions =
        {
            new Vector3(6.8f, 0.45f, 6.8f),
            new Vector3(-6.8f, 0.45f, 6.8f),
            new Vector3(6.8f, 0.45f, -6.8f),
            new Vector3(-6.8f, 0.45f, -6.8f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            string name = $"JumpPad_{i + 1}";
            GameObject pad = GameObject.Find(name);
            if (pad == null)
            {
                pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = name;
                pad.transform.SetParent(root);
            }

            pad.transform.position = positions[i];
            pad.transform.localScale = new Vector3(1.5f, 0.08f, 1.5f);

            var collider = pad.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;

            if (pad.GetComponent<ArenaJumpPad>() == null)
                pad.AddComponent<ArenaJumpPad>();

            var renderer = pad.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreateMat(name + "_Mat", new Color(0.04f, 0.02f, 0.055f), new Color(0.1f, 1f, 0.65f));
        }
    }

    private static void EnsureSpinnerHazard(Transform root)
    {
        GameObject spinner = GameObject.Find("CentralSpinnerHazard");
        if (spinner == null)
        {
            spinner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spinner.name = "CentralSpinnerHazard";
            spinner.transform.SetParent(root);
        }

        spinner.transform.position = new Vector3(0f, 1.05f, 0f);
        spinner.transform.localScale = new Vector3(12f, 0.22f, 0.38f);

        var collider = spinner.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;

        if (spinner.GetComponent<ArenaSpinnerHazard>() == null)
            spinner.AddComponent<ArenaSpinnerHazard>();

        var renderer = spinner.GetComponent<Renderer>();
        if (renderer != null)
                renderer.material = CreateMat("CentralSpinnerHazard_Mat", new Color(0.025f, 0.025f, 0.04f), new Color(1f, 0.35f, 0.05f));
    }

    private static void EnsureImpulseGates(Transform root)
    {
        Vector3[] positions =
        {
            new Vector3(0f, 0.9f, 7.2f),
            new Vector3(0f, 0.9f, -7.2f),
            new Vector3(7.2f, 0.9f, 0f),
            new Vector3(-7.2f, 0.9f, 0f)
        };

        Vector3[] directions =
        {
            Vector3.back,
            Vector3.forward,
            Vector3.left,
            Vector3.right
        };

        Vector3[] scales =
        {
            new Vector3(5.2f, 1.1f, 0.22f),
            new Vector3(5.2f, 1.1f, 0.22f),
            new Vector3(0.22f, 1.1f, 5.2f),
            new Vector3(0.22f, 1.1f, 5.2f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            string name = $"ImpulseGate_{i + 1}";
            GameObject gate = GameObject.Find(name);
            if (gate == null)
            {
                gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gate.name = name;
                gate.transform.SetParent(root);
            }

            gate.transform.position = positions[i];
            gate.transform.localScale = scales[i];

            var collider = gate.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;

            var gateLogic = gate.GetComponent<ArenaImpulseGate>();
            if (gateLogic == null)
                gateLogic = gate.AddComponent<ArenaImpulseGate>();
            gateLogic.Configure(directions[i], 18f, 4f);

            var renderer = gate.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreateMat(name + "_Mat", new Color(0.015f, 0.045f, 0.055f, 0.7f), new Color(0.1f, 1f, 0.95f));
        }
    }

    private static Transform[] EnsureSpawnPoints(Transform root)
    {
        var points = new List<Transform>();
        var existing = FindGameObjectsWithTagSafe("SpawnPoint");
        foreach (var go in existing)
            if (go != null) points.Add(go.transform);

        if (points.Count >= 4)
            return points.ToArray();

        GameObject spawnRoot = GameObject.Find("RuntimeSpawnPoints");
        if (spawnRoot == null)
        {
            spawnRoot = new GameObject("RuntimeSpawnPoints");
            spawnRoot.transform.SetParent(root);
        }

        Vector3[] positions =
        {
            new Vector3(6.5f, 2.2f, 0f),
            new Vector3(-6.5f, 2.2f, 0f),
            new Vector3(0f, 2.2f, 6.5f),
            new Vector3(0f, 2.2f, -6.5f)
        };

        points.Clear();
        for (int i = 0; i < positions.Length; i++)
        {
            string name = $"SpawnPoint_{i + 1}";
            GameObject sp = GameObject.Find(name);
            if (sp == null)
            {
                sp = new GameObject(name);
                sp.transform.SetParent(spawnRoot.transform);
            }

            sp.transform.position = positions[i];
            points.Add(sp.transform);
        }

        return points.ToArray();
    }

    private static Material CreateMat(string name, Color baseColor, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.name = name;
        mat.color = baseColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.25f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.75f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission * 0.65f);
        }
        return mat;
    }

    private static void SetLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0) go.layer = layer;
    }

    private static GameObject[] FindGameObjectsWithTagSafe(string tag)
    {
        try { return GameObject.FindGameObjectsWithTag(tag); }
        catch (UnityException) { return new GameObject[0]; }
    }
}
