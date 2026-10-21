using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace ECSFrame
{
    #region 敌人走路

    [BurstCompile]
    public partial struct EnemyWalkSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new EnemyWalkJob
            {
                dt = SystemAPI.Time.DeltaTime,
                seed = state.GlobalSystemVersion,
                blend = 0.25f
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EnemyWalkJob : IJobEntity
    {
        public float dt;
        public uint seed;
        public float blend;

        public void Execute(
            in Enemy_comp enemy,
            ref Forward_comp forward,
            in LocalTransform transform,
            ref PhysicsVelocity velocity,
            in Speed_comp speed,
            ref Walk_CD_comp walk_CD,
            ref isWalk_comp isWalk,
            ref isIdle_comp isIdle,
            ref Idle_CD_comp idle_CD,
            in isHate_comp isHate,
            ref isBack_comp isBack,
            Entity entity
        )
        {
            if (isHate.value) return;

            float3 p = transform.Position;
            if (!isBack.value && (p.x < -200 || p.x > 200 || p.z < -200 || p.z > 200))
            {
                isBack.value = true;
                var back = math.normalizesafe(new float3(-transform.Position.x, 0f, -transform.Position.z));
                if (math.lengthsq(back) < 1e-6f) back = new float3(0f, 0f, 1f);

                forward.value = back;
                forward.defaultValue = back;
                isWalk.value = true;
                isIdle.value = false;
                walk_CD.timeCounter = -10f;
            }
            
            if(!(p.x < -200 || p.x > 200 || p.z < -200 || p.z > 200)) isBack.value = false;

            var curH = new float3(velocity.Linear.x, 0f, velocity.Linear.z);

            if (isWalk.value)
            {
                var dir = math.normalizesafe(new float3(forward.value.x, 0f, forward.value.z));
                var desired = new float3(dir.x * speed.value, 0f, dir.z * speed.value);
                var newH = math.lerp(curH, desired, blend);
                velocity.Linear = new float3(newH.x, velocity.Linear.y, newH.z);
                walk_CD.timeCounter += dt;
            }
            else
            {
                var newH = math.lerp(curH, float3.zero, blend);
                velocity.Linear = new float3(newH.x, velocity.Linear.y, newH.z);
            }

            velocity.Angular = float3.zero;

            if (walk_CD.timeCounter >= walk_CD.CD)
            {
                var rng = Random.CreateFromIndex(seed + (uint)entity.Index);

                var r2 = rng.NextFloat2Direction();
                var f = new float3(r2.x, 0f, r2.y);
                forward.value = f;
                forward.defaultValue = f;

                // idleCD随机
                idle_CD.timeCounter = 0f;
                idle_CD.CD = rng.NextFloat(2f, 10f);

                walk_CD.timeCounter = 0;
                isWalk.value = false;
                isIdle.value = true;
            }
        }
    }

    #endregion

    #region 敌人静止

    [BurstCompile]
    public partial struct EnemyIdleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new EnemyIdleJob{dt = SystemAPI.Time.DeltaTime}.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EnemyIdleJob : IJobEntity
    {
        public float dt;
        public void Execute(
            in Enemy_comp enemy,
            ref Idle_CD_comp idle_CD,
            ref isIdle_comp isIdle,
            ref isWalk_comp isWalk,
            in isHate_comp isHate
        )
        {
            if (isHate.value) return;
            if (isIdle.value)
            {
                idle_CD.timeCounter += dt;
            }

            if (idle_CD.timeCounter >= idle_CD.CD)
            {
                idle_CD.timeCounter = 0;
                isIdle.value = false;
                isWalk.value = true;
            }
        }
    }

    #endregion

    #region 敌人仇恨

    [BurstCompile]
    [UpdateBefore(typeof(EnemyPursueSystem))]
    public partial struct EnemyHateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<Player_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<Player_comp>(out var playerEntity)) return;
            var playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
            state.Dependency = new EnemyHateJob { playerPos = playerPos }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EnemyHateJob : IJobEntity
    {
        public float3 playerPos;
        public void Execute(
            in Enemy_comp enemy,
            ref Forward_comp forward,
            ref isHate_comp isHate,
            in LocalTransform transform,
            in HateRadius_comp radius
        )
        {
            float3 distance = playerPos - transform.Position;
            if (distance.x * distance.x + distance.z * distance.z <= radius.value * radius.value)
            {
                if(!isHate.value) isHate.value = true;
                forward.value = math.normalizesafe(new float3(distance.x, 0f, distance.z));
            }
            else if (isHate.value && distance.x * distance.x + distance.z * distance.z > radius.value * radius.value)
            {
                isHate.value = false;
                forward.value = forward.defaultValue;
            }
        }
    }

    #endregion

    #region 敌人追击

    [BurstCompile]
    public partial struct EnemyPursueSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new EnemyPursueJob { blend = 0.25f }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EnemyPursueJob : IJobEntity
    {
        public float blend;

        public void Execute(
            in Enemy_comp enemy,
            ref Forward_comp forward,
            in Speed_comp speed,
            in isHate_comp isHate,
            ref PhysicsVelocity velocity
        )
        {
            if (!isHate.value)
            {
                forward.value = forward.defaultValue;
                return;
            }

            var dir = math.normalizesafe(new float3(forward.value.x, 0f, forward.value.z));
            var desired = new float3(dir.x * speed.value, 0f, dir.z * speed.value);
            var curH = new float3(velocity.Linear.x, 0f, velocity.Linear.z);
            var newH = math.lerp(curH, desired, blend);
            velocity.Linear = new float3(newH.x, velocity.Linear.y, newH.z);

            if (velocity.Linear.y > 0f)
                velocity.Linear = new float3(velocity.Linear.x, 0f, velocity.Linear.z);

            velocity.Angular = float3.zero;
        }
    }

    #endregion

    #region 敌人死亡

    [BurstCompile]
    public partial struct EnemyDieSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<PrefabRefs_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<EnemyCount_comp>(out Entity counterEntity)) return;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().
                    CreateCommandBuffer(state.WorldUnmanaged).
                    AsParallelWriter();
            var prefabs = SystemAPI.GetSingleton<PrefabRefs_comp>();
            
            state.Dependency = new  EnemyDieJob
            {
                ecb = ecb,
                propPrefab = prefabs.Prop,
                counterEntity = counterEntity
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EnemyDieJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public Entity propPrefab;
        public Entity counterEntity;

        public void Execute(
            [ChunkIndexInQuery] int sortKey,
            in Enemy_comp enemy,
            in isDead_comp isDead,
            in LocalTransform transform,
            Entity e
        )
        {
            if (!isDead.value) return;
            ecb.DestroyEntity(sortKey, e);

            ecb.AppendToBuffer(sortKey, counterEntity, new EnemyDeathDelta_comp { value = 1 });

            if (propPrefab != Entity.Null)
            {
                Entity tempE = ecb.Instantiate(sortKey, propPrefab);
                ecb.SetComponent(sortKey, tempE, LocalTransform.FromPosition(
                    new float3(transform.Position.x, 10f, transform.Position.z)));
            }
        }
    }

    #endregion

    #region 生成敌人

    [BurstCompile]
    public partial struct CreateEnemySystem : ISystem
    {
        public const int DefaultMaxEnemies = 10000;
        public const int DefaultSpawnPerFrame = 10;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<PrefabRefs_comp>();

            var counterQuery = state.GetEntityQuery(ComponentType.ReadOnly<EnemyCount_comp>());
            if (counterQuery.IsEmpty)
            {
                Entity counter = state.EntityManager.CreateSingleton<EnemyCount_comp>();
                state.EntityManager.AddBuffer<EnemyDeathDelta_comp>(counter);
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.GetSingleton<PrefabRefs_comp>();
            if (refs.Enemy == Entity.Null) return;

            int maxEnemies = DefaultMaxEnemies;
            if (SystemAPI.HasSingleton<MaxEnemies_comp>())
                maxEnemies = SystemAPI.GetSingleton<MaxEnemies_comp>().value;
            if (maxEnemies <= 0) return;

            int spawnPerFrame = DefaultSpawnPerFrame;
            if (SystemAPI.HasSingleton<SpawnPerFrame_comp>())
                spawnPerFrame = SystemAPI.GetSingleton<SpawnPerFrame_comp>().value;
            if (spawnPerFrame <= 0) return;

            Entity counterEntity = SystemAPI.GetSingletonEntity<EnemyCount_comp>();
            var counter = SystemAPI.GetComponent<EnemyCount_comp>(counterEntity);
            if (counter.value >= maxEnemies) return;

            int toSpawn = math.min(spawnPerFrame, maxEnemies - counter.value);
            var rng = Random.CreateFromIndex(state.GlobalSystemVersion);

            for (int i = 0; i < toSpawn; i++)
            {
                Entity e = state.EntityManager.Instantiate(refs.Enemy);
                
                var mass = state.EntityManager.GetComponentData<PhysicsMass>(e);
                mass.InverseInertia = float3.zero;
                state.EntityManager.SetComponentData(e, mass);

                var pos = new float3(rng.NextFloat(-100f, 100f), 10f, rng.NextFloat(-100f, 100f));

                state.EntityManager.SetComponentData(e, LocalTransform.FromPosition(pos));

                var dir2 = rng.NextFloat2Direction();
                var f = new float3(dir2.x, 0f, dir2.y);
                state.EntityManager.SetComponentData(e, new Forward_comp { value = f, defaultValue = f });
            }

            counter.value += toSpawn;
            SystemAPI.SetComponent(counterEntity, counter);
        }
    }
    #endregion

    #region 敌人受击

    [BurstCompile]
    public partial struct EnemyInjurySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<isATK_comp>();
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<Player_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<Player_comp>(out Entity playerE)) return;
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerE).Position;
            float radius = SystemAPI.GetComponent<ATKRadius_comp>(playerE).value;
            bool isATK = SystemAPI.GetComponent<isATK_comp>(playerE).value;
            state.Dependency = new EnemyInjuryJob
            {
                playerPos = playerPos,
                radius = radius,
                isATK = isATK
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EnemyInjuryJob : IJobEntity
    {
        public float3 playerPos;
        public float radius;
        public bool isATK;
        public void Execute(
            in Enemy_comp enemy,
            in LocalTransform transform,
            ref isDead_comp isDead
            )
        {
            float3 distance = playerPos - transform.Position;
            if (isATK && distance.x * distance.x + distance.z * distance.z <= radius * radius)
            {
                isDead.value = true;
            }
        }
    }

    #endregion

    #region 敌人状态维持

    [BurstCompile]
    public partial struct EnemyStateKeepSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new EnemyStateKeepJob
            {
                maxSpeed = 12f,   // 水平速度上限,防止被挤飞
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EnemyStateKeepJob : IJobEntity
    {
        public float maxSpeed;

        public void Execute(
            in Enemy_comp enemy,
            ref PhysicsMass mass,
            ref PhysicsVelocity velocity)
        {
            //类似Rigidbody的Freeze Rotation
            mass.InverseInertia = float3.zero;
            velocity.Angular = float3.zero;

            var v = velocity.Linear;

            //禁止叠罗汉，取消向上的速度
            if (v.y > 0f) v.y = 0f;

            //限制水平速度，防止挤压加速
            var hv = new float2(v.x, v.z);
            if (math.length(hv) > maxSpeed)
            {
                hv = math.normalize(hv) * maxSpeed;
                v.x = hv.x;
                v.z = hv.y;
            }

            //兜底
            if (!math.all(math.isfinite(v))) v = float3.zero;

            velocity.Linear = v;
        }
    }

    #endregion
    
    #region 生成玩家

    [BurstCompile]
    public partial struct SpawnPlayerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<PrefabRefs_comp>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<Player_comp>()) return;

            var refs = SystemAPI.GetSingleton<PrefabRefs_comp>();
            if (refs.Player == Entity.Null) return;

            Entity player = state.EntityManager.Instantiate(refs.Player);

            state.EntityManager.SetComponentData(player,
                LocalTransform.FromPosition(new float3(0f, 0f, 0f)));
        }
    }

    #endregion

    #region 玩家攻击输入

    [UpdateBefore(typeof(EnemyInjurySystem))]
    public partial struct PlayerAttackInputSystem : ISystem
    {
        private int atkHoldFrames;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<Player_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<Player_comp>();
            float dt = SystemAPI.Time.DeltaTime;

            var cd  = SystemAPI.GetComponent<ATK_CD_comp>(playerEntity);
            var atk = SystemAPI.GetComponent<isATK_comp>(playerEntity);

            if (cd.timeCounter > 0f) cd.timeCounter -= dt;

            if (atk.value)
            {
                atkHoldFrames--;
                if (atkHoldFrames <= 0) atk.value = false;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            bool pressed = mouse != null && mouse.rightButton.wasReleasedThisFrame;

            if (pressed && cd.timeCounter <= 0f && !atk.value)
            {
                atk.value = true;
                atkHoldFrames = 3;
                cd.timeCounter = cd.CD;
            }

            SystemAPI.SetComponent(playerEntity, cd);
            SystemAPI.SetComponent(playerEntity, atk);
        }
    }

    #endregion

    #region 玩家移动输入

    public partial struct PlayerTeleportSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<Player_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || !mouse.leftButton.wasReleasedThisFrame) return;

            if (!SystemAPI.TryGetSingletonEntity<Player_comp>(out var playerEntity)) return;

            var cam = UnityEngine.Camera.main;
            if (!cam) cam = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Camera>();
            if (!cam) return;

            UnityEngine.Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

            var ground = new UnityEngine.Plane(UnityEngine.Vector3.up, UnityEngine.Vector3.zero);
            if (!ground.Raycast(ray, out float dist)) return;

            UnityEngine.Vector3 p = ray.GetPoint(dist);
            p.y += 10f;
            p.x = UnityEngine.Mathf.Clamp(p.x, -200f, 200f);
            p.z = UnityEngine.Mathf.Clamp(p.z, -200f, 200f);

            SystemAPI.SetComponent(playerEntity, LocalTransform.FromPosition(p));

            if (SystemAPI.HasComponent<PhysicsVelocity>(playerEntity))
                SystemAPI.SetComponent(playerEntity, new PhysicsVelocity());
        }
    }

    

        #endregion

    #region 玩家捡道具

    [BurstCompile]
    public partial struct PlayerCatchPropSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<Player_comp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if(!SystemAPI.TryGetSingletonEntity<Player_comp>(out Entity playerE)) return;
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerE).Position;
            float radius = SystemAPI.GetComponent<CatchRadius_comp>(playerE).value;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            state.Dependency = new PlayerCatchPropJob
            {
                ecb = ecb, 
                playerPos = playerPos,
                radius = radius,
                playerE = playerE
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct PlayerCatchPropJob : IJobEntity
    {
        public float3 playerPos;
        public float radius;
        public Entity playerE;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(
            [ChunkIndexInQuery] int sortkey,
            Entity e,
            in Prop_comp prop,
            in LocalTransform transform,
            in Money_comp money
        )
        {
            float3 distance = playerPos - transform.Position;
            if (distance.x * distance.x + distance.z * distance.z > radius * radius) return;

            //每捡到一个道具就往玩家单例里加一点缓存值,然后主线程累加进总值
            ecb.AppendToBuffer(sortkey, playerE, new MoneyDelta_comp { value = money.value });
            ecb.DestroyEntity(sortkey, e);
        }
    }

    #endregion

    #region 数据结算

    [BurstCompile]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial struct EnemyCountReconcileSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyCount_comp>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity counterEntity = SystemAPI.GetSingletonEntity<EnemyCount_comp>();
            var buffer = SystemAPI.GetBuffer<EnemyDeathDelta_comp>(counterEntity);
            if (buffer.Length == 0) return;

            int deaths = 0;
            for (int i = 0; i < buffer.Length; i++) deaths += buffer[i].value;
            buffer.Clear();

            var counter = SystemAPI.GetComponent<EnemyCount_comp>(counterEntity);
            counter.value = math.max(0, counter.value - deaths);
            SystemAPI.SetComponent(counterEntity, counter);
        }
    }

    [BurstCompile]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial struct PlayerMoneyCacheSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<Player_comp>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<Player_comp>(out Entity playerE)) return;

            var buffer = SystemAPI.GetBuffer<MoneyDelta_comp>(playerE);
            if (buffer.Length == 0) return;

            int sum = 0;
            for (int i = 0; i < buffer.Length; i++) sum += buffer[i].value;
            buffer.Clear();

            var money = SystemAPI.GetComponent<Money_comp>(playerE);
            money.cacheValue += sum;
            SystemAPI.SetComponent(playerE, money);
        }
    }

    [BurstCompile]
    [UpdateAfter(typeof(PlayerMoneyCacheSystem))]
    public partial struct PlayerMoneyMergeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<Player_comp>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<Player_comp>(out Entity playerE)) return;

            var money = SystemAPI.GetComponent<Money_comp>(playerE);
            if (money.cacheValue == 0) return;

            money.value += money.cacheValue;
            money.cacheValue = 0;
            SystemAPI.SetComponent(playerE, money);
        }
    }

    #endregion

    #region 地板

    [BurstCompile]
    public partial struct SpawnFloorSystem : ISystem
    {
        EntityQuery floorQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSModeTag_comp>();
            state.RequireForUpdate<PrefabRefs_comp>();
            floorQuery = state.GetEntityQuery(ComponentType.ReadOnly<Floor_comp>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (floorQuery.CalculateEntityCount() > 0) return;

            var refs = SystemAPI.GetSingleton<PrefabRefs_comp>();
            if (refs.Floor == Entity.Null) return;

            Entity floor = state.EntityManager.Instantiate(refs.Floor);
            state.EntityManager.AddComponent<Floor_comp>(floor);
        }
    }

    

    #endregion
}
