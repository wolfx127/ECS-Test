using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECSFrame
{
    public class UnitAuthoring : MonoBehaviour
    {
        public enum Kind { Enemy, Player, Prop, Floor }
        public Kind kind = Kind.Enemy;

        public float speed = 10f;
        public float walkCD = 10f, idleCD = 15f;
        public float atkCD = 0.2f;
        public int money = 25;

        private class Baker : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);

                switch (a.kind)
                {
                    case Kind.Enemy:
                        AddComponent(e, new Enemy_comp());
                        AddComponent(e, new isWalk_comp { value = true });
                        AddComponent(e, new Walk_CD_comp { timeCounter = 0, CD = a.walkCD });
                        AddComponent(e, new isIdle_comp { value = false });
                        AddComponent(e, new Idle_CD_comp { timeCounter = 0, CD = a.idleCD });
                        AddComponent(e, new isHate_comp { value = false });
                        AddComponent(e, new isDead_comp { value = false });
                        AddComponent(e, new Speed_comp { value = a.speed });
                        AddComponent(e, new Forward_comp { value = new float3(0, 0, 1), defaultValue = new float3(0, 0, 1) });
                        AddComponent(e, new HateRadius_comp { value = 30f });
                        AddComponent(e, new isBack_comp { value = false });
                        break;

                    case Kind.Player:
                        AddComponent(e, new Player_comp());
                        AddComponent(e, new Speed_comp { value = a.speed });
                        AddComponent(e, new isATK_comp { value = false });
                        AddComponent(e, new ATK_CD_comp { timeCounter = 0, CD = a.atkCD });
                        AddComponent(e, new isUpdatePos_comp{ value = false });
                        AddComponent(e, new UpdatePos_comp());
                        AddComponent(e, new Money_comp());
                        AddComponent(e, new HateRadius_comp{value = 30f});
                        AddComponent(e, new ATKRadius_comp{value = 10f});
                        AddComponent(e, new CatchRadius_comp{value = 7f});
                        AddBuffer<MoneyDelta_comp>(e);
                        break;

                    case Kind.Prop:
                        AddComponent(e, new Prop_comp());
                        AddComponent(e, new Money_comp { value = a.money });
                        break;
                    
                    case Kind.Floor:
                        break;
                }
            }
        }
    }
}