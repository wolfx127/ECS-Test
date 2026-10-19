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
        
        private Stack<GameObject> pool = new Stack<GameObject>();

        public EnemyPool(GameObject player, GameObject defaultEnemy, GameObject enemyPoolGo, 
            QuadTreeMgr quadTreeMgr, GameObject Prop)
        {
            this.player = player;
            this.defaultEnemy = defaultEnemy;
            this.enemyPoolGO = enemyPoolGo;
            this.quadTreeMgr = quadTreeMgr;
            enemyList = new HashSet<EnemyData>();
            enemyDic = new Dictionary<GameObject, EnemyData>();
            this.Prop = Prop;
        }

        public void Tick()
        {
            for (int i = 0; i < 10; i++)
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


        private void AddCapacity()
        {
            if (size >= 10000)
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
                enemyList.Add(data);
                enemyDic[enemy] = data;
                pool.Push(enemy);
                size++;
                if (size >= 10000) return;
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
