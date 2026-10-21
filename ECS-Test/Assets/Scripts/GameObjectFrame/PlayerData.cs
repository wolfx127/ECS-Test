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
        private Camera cam;
        private Rigidbody rb = null;
        
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
        static readonly int groundMask = LayerMask.GetMask("Ground");

        public int money = 0;
        
        public PlayerData(GameObject player, QuadTreeMgr quadTreeMgr, EnemyPool enemyPool, Camera cam)
        {
            this.player = player;
            this.quadTreeMgr = quadTreeMgr;
            this.enemyPool = enemyPool;
            this.cam = cam;
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

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (!cam) cam = Object.FindFirstObjectByType<Camera>();

                if (cam)
                {
                    Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

                    Vector3 v;
                    if (groundMask != 0 && Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
                        v = hit.point;
                    else
                        return;

                    v.y += 10f;

                    v.x = Mathf.Clamp(v.x, -200f, 200f);
                    v.z = Mathf.Clamp(v.z, -200f, 200f);

                    if (!rb) rb = player.GetComponent<Rigidbody>();
                    if (rb)
                    {
                        rb.position = v;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }
    }
}
