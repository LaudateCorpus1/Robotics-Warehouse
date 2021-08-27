using System.Collections.Generic;
using System;
using Unity.Robotics.PerceptionRandomizers.Shims;
using UnityEngine;
using UnityEditor;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;
using Unity.Simulation.Warehouse;
using UnityEngine.Perception.Randomization.Scenarios;
using Object = UnityEngine.Object;

[Serializable]
[AddRandomizerMenu("Robotics/Floor Box Randomizer")]
public class FloorBoxRandomizerShim : RandomizerShim
{
    public int numBoxToSpawn;
    public int maxPlacementTries = 100;
    FloatParameter random = new FloatParameter {value = new UniformSampler(0f, 1f)};

    SurfaceObjectPlacer placer;
    GameObject parentFloorBoxes;
    List<CollisionConstraint> constraints;
    CollisionConstraint turtleConstraint;
    AppParam appParam;
    ShelfBoxRandomizerShim m_ShelfBoxRandomizerShim;

    protected override void OnAwake()
    {
        if (WarehouseManager.instance == null)
        {
            var warehouseManager = GameObject.FindObjectOfType<WarehouseManager>();
            appParam = warehouseManager.appParam;
        }
        // Add collision constraints to spawned shelves
        var tags = tagManager.Query<ShelfBoxRandomizerTag>();
        if (!Application.isPlaying)
            tags = GameObject.FindObjectsOfType<ShelfBoxRandomizerTag>();

        constraints = new List<CollisionConstraint>();

        foreach (var tag in tags)
        {
            var shelf = new CollisionConstraint(tag.transform.position.x, tag.transform.position.z, tag.GetComponentInChildren<Renderer>().bounds.extents.x);
            constraints.Add(shelf);
        }

        base.OnAwake();
    }

    protected override void OnScenarioStart()
    {
        var scenario = GameObject.FindObjectOfType<Scenario<ScenarioConstants>>();
        m_ShelfBoxRandomizerShim = scenario.GetRandomizer<ShelfBoxRandomizerShim>();
        base.OnScenarioStart();
    }

    protected override void OnIterationStart()
    {
        if (GameObject.Find("GeneratedWarehouse") == null)
            return;

        // Create floor boundaries for spawning
        var bounds = new Bounds(Vector3.zero, new Vector3(appParam.width, 0, appParam.length));
        placer = new SurfaceObjectPlacer(bounds, random, maxPlacementTries);

        // Instantiate boxes at arbitrary location
        parentFloorBoxes = new GameObject("FloorBoxes");
        for (int i = 0; i < numBoxToSpawn; i++) 
        {
            GameObject o;
            if (!Application.isPlaying)
            {
                o = PrefabUtility.InstantiatePrefab(m_ShelfBoxRandomizerShim.GetBoxPrefab()) as GameObject;
                o.transform.parent = parentFloorBoxes.transform;
            }
            else
                o = Object.Instantiate(m_ShelfBoxRandomizerShim.GetBoxPrefab(), parentFloorBoxes.transform);
            o.AddComponent<FloorBoxRandomizerTag>();
            o.AddComponent<RotationRandomizerTag>();
        }

        // Begin placement interation
        placer.IterationStart();
        
        var tags = tagManager.Query<FloorBoxRandomizerTag>();
        if (!Application.isPlaying)
            tags = GameObject.FindObjectsOfType<FloorBoxRandomizerTag>();

        foreach (var tag in tags)
        {
            bool success = placer.PlaceObject(tag.gameObject);
            if (!success)
            {
                return;
            }
        }
    }

    protected override void OnIterationEnd()
    {
        if (!Application.isPlaying)
                Object.DestroyImmediate(parentFloorBoxes);
            else
                Object.Destroy(parentFloorBoxes);
    }
}