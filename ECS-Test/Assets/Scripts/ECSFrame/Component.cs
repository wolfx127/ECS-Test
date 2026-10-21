using Unity.Entities;
using Unity.Mathematics;

namespace ECSFrame
{
    public struct Enemy_comp : IComponentData
    {

    }

    public struct Prop_comp : IComponentData
    {

    }

    public struct Player_comp : IComponentData
    {

    }

    public struct Floor_comp : IComponentData
    {
        
    }

    public struct isUpdatePos_comp : IComponentData
    {
        public bool value;
    }

    public struct UpdatePos_comp : IComponentData
    {
        public float3 value;
    }

    public struct Money_comp : IComponentData
    {
        public int value;
        public int cacheValue;
    }

    public struct MoneyDelta_comp : IBufferElementData
    {
        public int value;
    }

    public struct EnemyCount_comp : IComponentData
    {
        public int value;
    }

    public struct EnemyDeathDelta_comp : IBufferElementData
    {
        public int value;
    }

    public struct MaxEnemies_comp : IComponentData
    {
        public int value;
    }

    public struct SpawnPerFrame_comp : IComponentData
    {
        public int value;
    }

    public struct Forward_comp : IComponentData
    {
        public float3 value;
        public float3 defaultValue;
    }

    public struct Speed_comp : IComponentData
    {
        public float value;
    }

    public struct isATK_comp : IComponentData
    {
        public bool value;
    }

    public struct ATK_CD_comp : IComponentData
    {
        public float timeCounter;
        public float CD;
    }

    public struct ATKRadius_comp : IComponentData
    {
        public float value;
    }

    public struct CatchRadius_comp : IComponentData
    {
        public float value;
    }

    public struct isHate_comp : IComponentData
    {
        public bool value;
    }

    public struct HateRadius_comp : IComponentData
    {
        public float value;
    }

    public struct isBack_comp : IComponentData
    {
        public bool value;
    }

    public struct isWalk_comp : IComponentData
    {
        public bool value;
    }

    public struct Walk_CD_comp : IComponentData
    {
        public float timeCounter;
        public float CD;
    }

    public struct isIdle_comp : IComponentData
    {
        public bool value;
    }

    public struct Idle_CD_comp : IComponentData
    {
        public float timeCounter;
        public float CD;
    }

    public struct isDead_comp : IComponentData
    {
        public bool value;
    }

    public struct PrefabRefs_comp : IComponentData
    {
        public Entity Enemy;
        public Entity Player;
        public Entity Prop;
        public Entity Floor;
    }

    public struct ECSModeTag_comp : IComponentData
    {
        
    }

}
