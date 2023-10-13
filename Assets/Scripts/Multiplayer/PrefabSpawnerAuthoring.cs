using Unity.Entities;
using UnityEngine;

public struct PrefabSpawner : IComponentData
{
    public Entity prefab;
}

[DisallowMultipleComponent]
public class PrefabSpawnerAuthoring : MonoBehaviour
{
    public GameObject prefab;

    class Baker : Baker<PrefabSpawnerAuthoring>
    {
        public override void Bake(PrefabSpawnerAuthoring authoring)
        {
            PrefabSpawner component = default(PrefabSpawner);
            component.prefab = GetEntity(authoring.prefab);
            AddComponent(component);
        }
    }
}