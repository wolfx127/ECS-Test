using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECSFrame
{
    public class BakeMonoToECS : MonoBehaviour
    {
        [Header("注入的预制体")]
        public GameObject enemyPrefab;
        public GameObject playerPrefab;
        public GameObject propPrefab;
        public GameObject floor;

        private class Baker : Baker<BakeMonoToECS>
        {
            public override void Bake(BakeMonoToECS authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);

                AddComponent(e, new PrefabRefs_comp
                {
                    Enemy = ToPrefabEntity(authoring.enemyPrefab),
                    Player = ToPrefabEntity(authoring.playerPrefab),
                    Prop = ToPrefabEntity(authoring.propPrefab),
                    Floor = ToPrefabEntity(authoring.floor)
                });
            }

            private Entity ToPrefabEntity(GameObject prefab)
            {
                return prefab != null
                    ? GetEntity(prefab, TransformUsageFlags.Dynamic)
                    : Entity.Null;
            }
        }
    }
}
