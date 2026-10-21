using System.Collections.Generic;
using UnityEngine;

namespace GameObjectFrame
{
    public class EnemyPool
    {
        private GameObject defaultEnemy;
        private GameObject enemyPoolGO;
        private QuadTreeMgr quadTreeMgr;
        private GameObject player;
        private GameObject Prop;
        private HashSet<EnemyData> enemyList;
        public Dictionary<GameObject, EnemyData> enemyDic;
        private int capacity = 1;
        private int size = 0;
        private int maxEnemies = 10000;
        private int spawnPerFrame = 10;
        
        private Stack<GameObject> pool = new Stack<GameObject>();

        public EnemyPool(GameObject player, GameObject defaultEnemy, GameObject enemyPoolGo, 
            QuadTreeMgr quadTreeMgr, GameObject Prop, int maxEnemies = 10000, int spawnPerFrame = 10)
        {
            this.player = player;
            this.defaultEnemy = defaultEnemy;
            this.enemyPoolGO = enemyPoolGo;
            this.quadTreeMgr = quadTreeMgr;
            enemyList = new HashSet<EnemyData>();
            enemyDic = new Dictionary<GameObject, EnemyData>();
            this.Prop = Prop;
            this.maxEnemies = Mathf.Max(1, maxEnemies);
            this.spawnPerFrame = Mathf.Max(0, spawnPerFrame);
        }

        public void Tick()
        {
            int n = Mathf.Clamp(spawnPerFrame, 0, maxEnemies - enemyList.Count);
            for (int i = 0; i < n; i++)
            {
                CreateEnemy();
            }
        }

        public void FixedTick(float dt)
        {
            foreach (var e in enemyList)
            {
                e.Tick(dt);
            }
        }

        public int MaxEnemies
        {
            get => maxEnemies;
            set => maxEnemies = Mathf.Max(1, value);
        }

        public int SpawnPerFrame
        {
            get => spawnPerFrame;
            set => spawnPerFrame = Mathf.Max(0, value);
        }


        private void AddCapacity()
        {
            if (size >= maxEnemies)
            {
                Debug.Log("对象池已达上限: " + size);
                return;
            }
            int count = capacity;
            if (capacity == 1) count++;
            for (int i = 0; i < count; i++)
            {
                GameObject enemy = Object.Instantiate(defaultEnemy, enemyPoolGO.transform);
                enemy.SetActive(false);
                enemy.tag = "Enemy";
                EnemyData data = new EnemyData(enemy, player.transform,
                    new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized, quadTreeMgr, 
                    this, Prop);
                enemyDic[enemy] = data;
                pool.Push(enemy);
                size++;
                if (size >= maxEnemies) return;
            }
            Debug.Log("扩容: " + capacity);
            capacity *= 2;
        }

        public GameObject CreateEnemy()
        {
            if(pool.Count <= 0) AddCapacity();
            if(pool.Count <= 0) return null;
            
            GameObject enemy = pool.Pop();
            EnemyData data = enemyDic[enemy];
            data.Reset();
            enemyList.Add(data);
            enemy.transform.position = new Vector3(Random.Range(-100f, 100f), 10f, Random.Range(-100f, 100f));
            enemy.SetActive(true);
            quadTreeMgr.Add(enemy);
            
            return enemy;
        }

        public void DeleteEnemy(GameObject enemy)
        {
            if (!enemy.CompareTag("Enemy") || !enemy.activeInHierarchy) return;
            if (!enemyDic.TryGetValue(enemy, out var data)) return;
            enemy.SetActive(false);
            quadTreeMgr.Remove(enemy);
            enemyList.Remove(data);
            enemy.transform.position = new Vector3(0,0,0);
            pool.Push(enemy);
        }
    }
}
