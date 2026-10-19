using Unity.Entities;

namespace ECSFrame
{
    public struct Enemy : IComponentData
    {
        
    }

    public struct HP : IComponentData
    {
        public int value;
    }

    public struct ATK : IComponentData
    {
        public int value;
    }

    public struct ATK_CD : IComponentData
    {
        public float time;
        public float CD;
    }

    public struct Prop : IComponentData
    {
        
    }

    public struct Money : IComponentData
    {
        public int value;
    }
}
