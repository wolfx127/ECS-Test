using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameObjectFrame
{
    public class PlayerData
    {
        private GameObject player;
        private QuadTreeMgr quadTreeMgr;
        private EnemyPool enemyPool;
        
        private List<GameObject> hatedEnemies = new List<GameObject>();
        private float hateRadius = 30f;
        private float hateCD = 0.2f;
        private float hateTimeCounter = 0;
        private float attackRadius = 10f;
        private float attackCD = 0.2f;
        private float attackTimeCounter = 0;
        private float catchRadius = 7f;
        private Collider[] props = new Collider[64];
        static readonly int propMask = LayerMask.GetMask("Prop");

        public int money = 0;
        
        public PlayerData(GameObject player, QuadTreeMgr quadTreeMgr, EnemyPool enemyPool)
        {
            this.player = player;
            this.quadTreeMgr = quadTreeMgr;
            this.enemyPool = enemyPool;   // ← 之前漏了这行，字段是 null 会 NRE
        }

        public void Tick(float dt)
        {
            if (hateTimeCounter >= hateCD)
            {
                foreach (var GO in hatedEnemies)
                {
                    enemyPool.enemyDic[GO].Walk();
                }
                hatedEnemies.Clear();
                hatedEnemies.AddRange(quadTreeMgr.GetEnemiesByCircle(
                    new Vector2(player.transform.position.x, player.transform.position.z), hateRadius));
                foreach (var GO in hatedEnemies)
                {
                    enemyPool.enemyDic[GO].PursuePlayer();
                }
                hateTimeCounter = 0;
            }
            else hateTimeCounter += dt;

            if (attackTimeCounter >= attackCD && Mouse.current.rightButton.wasReleasedThisFrame)
            {
                List<GameObject> list = quadTreeMgr.GetEnemiesByCircle(
                    new Vector2(player.transform.position.x, player.transform.position.z), attackRadius);
                foreach (var GO in list)
                {
                    enemyPool.enemyDic[GO].Die();
                }
                attackTimeCounter = 0;
            }
            else if (attackTimeCounter < attackCD) attackTimeCounter += dt;
            
            Vector3 bottom = player.transform.position - Vector3.up * 10f;
            Vector3 top    = player.transform.position + Vector3.up * 10f;

            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, catchRadius, props, propMask);
            for (int i = 0; i < n; i++)
            {
                money += 25;
                Object.Destroy(props[i].gameObject);
            }
        }
    }
}
