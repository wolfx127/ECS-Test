using Unity.IntegerTime;
using UnityEngine;

namespace GameObjectFrame
{
    public class EnemyData
    {
        private Transform Player;
        private GameObject gameObject;
        private Rigidbody rigidbody;
        private Vector2 forward;
        private QuadTreeMgr quadTreeMgr;
        private EnemyPool enemyPool;
        private GameObject Prop;

        public EnemyData(GameObject GO, Transform Player, Vector2 Forward, QuadTreeMgr quadTreeMgr, 
            EnemyPool pool, GameObject Prop)
        {
            gameObject = GO;
            this.Player = Player;
            rigidbody = gameObject.GetComponent<Rigidbody>();
            forward = Forward;
            this.quadTreeMgr = quadTreeMgr;
            this.enemyPool =  pool;
            this.Prop = Prop;
        }

        private float walkSpeed = 10f;
        private float walkCD = 10f;
        private float walkTimeCounter = 0;
        private bool isWalk = true;
        private float IdleCD = 15f;
        private float IdleTimeCounter = 0;
        private bool isIdle = false;
        private bool  isPursue = false;

        private bool isBack = false;

        public void Tick(float dt)
        {
            if (isPursue)
            {
                Vector3 v = gameObject.transform.position +
                            new Vector3(dt * walkSpeed * forward.x, 0, dt * walkSpeed * forward.y);
                rigidbody.MovePosition(v);
                quadTreeMgr.Update(gameObject);
                return;
            }

            Vector3 pos = gameObject.transform.position;
            if (!isBack && (pos.x < -200 || pos.x > 200 || pos.z < -200 || pos.z > 200))
            {
                isBack = true;
                Vector2 back = new Vector2(-pos.x, -pos.z);
                if (back.sqrMagnitude < 1e-6f) back = Vector2.up;
                forward = back.normalized;
                isWalk = true;
                isIdle = false;
                walkTimeCounter = -10;
            }

            if (!(pos.x < -200 || pos.x > 200 || pos.z < -200 || pos.z > 200)) isBack = false;

            if (isWalk && walkTimeCounter < walkCD)
            {
                Vector3 v = gameObject.transform.position +
                            new Vector3(dt * walkSpeed * forward.x, 0, dt * walkSpeed * forward.y);
                rigidbody.MovePosition(v);
                walkTimeCounter += dt;
                quadTreeMgr.Update(gameObject);
            }
            else if(isWalk && walkTimeCounter >= walkCD)
            {
                walkTimeCounter = 0;
                isWalk = false;
                isIdle = true;
                forward = RandomDirection();
            }

            if (isIdle && IdleTimeCounter < IdleCD)
            {
                IdleTimeCounter += dt;
            }
            else if(isIdle && IdleTimeCounter >= IdleCD)
            {
                IdleTimeCounter = 0;
                isIdle = false;
                isWalk = true;
            }
        }

        private static Vector2 RandomDirection()
        {
            return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        public void PursuePlayer()
        {
            forward = new Vector2(Player.position.x - gameObject.transform.position.x,
                Player.position.z - gameObject.transform.position.z).normalized;
            isPursue = true;
        }

        public void Walk()
        {
            forward = new Vector2(Random.Range(-1f,1f), Random.Range(-1f, 1f)).normalized;
            isPursue = false;
        }

        public void Reset()
        {
            isPursue = false;
            isWalk = true;
            isIdle = false;
            walkTimeCounter = 0;
            IdleTimeCounter = 0;
            forward = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        public void Die()
        {
            Object.Instantiate(Prop, gameObject.transform.position + new Vector3(0f, 10f, 0f), Quaternion.identity);
            enemyPool.DeleteEnemy(gameObject);
        }
    }
}
