using System.Collections.Generic;
using UnityEngine;

namespace GameObjectFrame
{
    public class QuadTreeMgr
    {
        private Vector2 pA = new Vector2(-220, -220);
        private Vector2 pB = new Vector2(220, 220);

        private Dictionary<GameObject, Vector2> addEnemyBuffer;
        private Dictionary<GameObject, Vector2> updateEnemyBuffer;
        private Dictionary<GameObject, Vector2> removeEnemyBuffer;

        private float CD = 0.2f;
        private float timeCounter = 0;

        public QuadNode root;

        public QuadTreeMgr()
        {
            root = new QuadNode(pA, pB);
            addEnemyBuffer = new Dictionary<GameObject, Vector2>();
            updateEnemyBuffer = new Dictionary<GameObject, Vector2>();
            removeEnemyBuffer = new Dictionary<GameObject, Vector2>();
        }

        public void Add(GameObject enemy)
        {
            if (!enemy.CompareTag("Enemy") || addEnemyBuffer.ContainsKey(enemy)) return;
            addEnemyBuffer.Add(enemy, new Vector2(enemy.transform.position.x, enemy.transform.position.z));
        }
        private void ClearAddRemoveBuffer()
        {
            foreach (var GO in addEnemyBuffer.Keys)
            {
                root.AddEnemy(GO);
            }
            addEnemyBuffer.Clear();
        }

        public void Update(GameObject enemy)
        {
            if (!enemy.CompareTag("Enemy") || updateEnemyBuffer.ContainsKey(enemy)) return;
            updateEnemyBuffer.Add(enemy, new Vector2(enemy.transform.position.x, enemy.transform.position.z));
        }
        private void UpdateOneEnemy(QuadNode node, GameObject enemy)
        {
            Vector2 p = updateEnemyBuffer[enemy];
            if (p.x < node.pA.x || p.x > node.pB.x || p.y < node.pA.y || p.y > node.pB.y) return;

            QuadNode oldNode = node;
            Stack<QuadNode> s = new Stack<QuadNode>();
            while (oldNode.childList.Count > 0)
            {
                float midX = (oldNode.pA.x + oldNode.pB.x) * 0.5f;
                float midY = (oldNode.pA.y + oldNode.pB.y) * 0.5f;
                int index = 0;
                if (p.x < midX)
                {
                    index = 1; 
                    if (p.y < midY) 
                        index = 2;
                }
                else if (p.y < midY) 
                    index = 3;
                oldNode = oldNode.childList[index];
                s.Push(oldNode);
            }

            if (!oldNode.enemyList.Contains(enemy)) return;
            oldNode.enemyList.Remove(enemy);

            p = new Vector2(enemy.transform.position.x, enemy.transform.position.z);
            while (s.Count > 0)
            {
                QuadNode newNode = s.Pop();
                if (p.x >= newNode.pA.x && p.y >= newNode.pA.y && p.x <= newNode.pB.x && p.y <= newNode.pB.y)
                {
                    newNode.AddEnemy(enemy);
                    return;
                }
            }
            root.AddEnemy(enemy);
        }
        private void ClearUpdateEnemyBuffer()
        {
            foreach (var GO in updateEnemyBuffer.Keys)
            {
                UpdateOneEnemy(root, GO);
            }
            updateEnemyBuffer.Clear();
        }

        public void Remove(GameObject enemy)
        {
            if (!enemy.CompareTag("Enemy") || removeEnemyBuffer.ContainsKey(enemy)) return;
            removeEnemyBuffer.Add(enemy, new Vector2(enemy.transform.position.x, enemy.transform.position.z));
        }
        private void RemoveOneEnemy(QuadNode node, GameObject enemy, Vector2 p)
        {
            if (p.x < node.pA.x || p.x > node.pB.x || p.y < node.pA.y || p.y > node.pB.y) return;

            if (node.childList.Count > 0)
            {
                float midX = (node.pA.x + node.pB.x) * 0.5f;
                float midY = (node.pA.y + node.pB.y) * 0.5f;
                int index = 0;
                if (p.x < midX)
                { 
                    index = 1;
                    if (p.y < midY) 
                        index = 2; 
                }
                else if (p.y < midY) 
                    index = 3;
                RemoveOneEnemy(node.childList[index], enemy, p);
            }
            else
            {
                node.enemyList.Remove(enemy);
            }
        }
        private void ClearRemoveEnemyBuffer()
        {
            foreach (var kv in removeEnemyBuffer)
            {
                RemoveOneEnemy(root, kv.Key, kv.Value);
            }
            removeEnemyBuffer.Clear();
        }

        public void Tick(float dt)
        {
            timeCounter += dt;
            if (timeCounter >= CD)
            {
                timeCounter = 0;
                ClearAddRemoveBuffer();
                ClearRemoveEnemyBuffer();
                ClearUpdateEnemyBuffer();
                root.TryMerge();
            }
        }

        ///////////////////////////////////////////////////////////////

        public List<GameObject> GetEnemiesByAABB(Vector2 pA, Vector2 pB)
        {
            List<GameObject> result = new List<GameObject>();
            GetEnemiesByAABB(root, pA, pB, result);
            return result;
        }
        private void GetEnemiesByAABB(QuadNode node, Vector2 pA, Vector2 pB, List<GameObject> result)
        {
            if (node.pA.x > pB.x || node.pA.y > pB.y || node.pB.x < pA.x || node.pB.y < pA.y) return;

            if (node.childList.Count > 0)
            {
                foreach (QuadNode n in node.childList)
                {
                    GetEnemiesByAABB(n, pA, pB, result);
                }
            }
            else
            {
                if (node.pA.x >= pA.x && node.pA.y >= pA.y && node.pB.x <= pB.x && node.pB.y <= pB.y)
                    result.AddRange(node.enemyList);
                else
                {
                    foreach (var GO in node.enemyList)
                    {
                        Vector2 p = new Vector2(GO.transform.position.x, GO.transform.position.z);
                        if (p.x >= pA.x && p.x <= pB.x && p.y >= pA.y && p.y <= pB.y)
                            result.Add(GO);
                    }
                }
            }
        }

        public List<GameObject> GetEnemiesByCircle(Vector2 center, float radius)
        {
            List<GameObject> result = new List<GameObject>();
            GetEnemiesByCircle(root, center, radius, result);
            return result;
        }
        private void GetEnemiesByCircle(QuadNode node, Vector2 center, float radius, List<GameObject> result)
        {
            float cx = Mathf.Clamp(center.x, node.pA.x, node.pB.x);
            float cy = Mathf.Clamp(center.y, node.pA.y, node.pB.y);
            float Cx = center.x - cx;
            float Cy = center.y - cy;
            if (Cx * Cx + Cy * Cy > radius * radius) return;

            if (node.childList.Count > 0)
            {
                foreach (QuadNode n in node.childList)
                {
                    GetEnemiesByCircle(n, center, radius, result);
                }
            }
            else
            {
                Vector2 closestPoint = node.pA;
                Vector2 farthestPoint = node.pB;
                if (Mathf.Abs(closestPoint.x - center.x) > Mathf.Abs(node.pB.x - center.x))
                {
                    closestPoint.x = node.pB.x;
                    farthestPoint.x = node.pA.x;
                }
                if (Mathf.Abs(closestPoint.y - center.y) > Mathf.Abs(node.pB.y - center.y))
                {
                    closestPoint.y = node.pB.y;
                    farthestPoint.y = node.pA.y;
                }
                float Fx = center.x - farthestPoint.x;
                float Fy = center.y - farthestPoint.y;
                if (Fx * Fx + Fy * Fy <= radius * radius)
                {
                    result.AddRange(node.enemyList);
                }
                else
                {
                    foreach (var GO in node.enemyList)
                    {
                        Fx = GO.transform.position.x - center.x;
                        Fy = GO.transform.position.z - center.y;
                        if (Fx * Fx + Fy * Fy <= radius * radius)
                            result.Add(GO);
                    }
                }
            }
        }
    }

    public struct QuadNode
    {
        private static int maxDepth = 7;
        private static int maxCapacity = 10;

        public List<QuadNode> childList;
        public List<GameObject> enemyList;

        public Vector2 pA;//左下角
        public Vector2 pB;//右上角

        public int curDepth;

        public QuadNode(Vector2 pA, Vector2 pB)
        {
            this.pA = pA;
            this.pB = pB;
            childList = new List<QuadNode>();
            enemyList = new List<GameObject>();
            curDepth = 1;
        }

        public QuadNode(Vector2 pA, Vector2 pB, int curDepth, bool isRight, bool isUp)
        {
            this.curDepth = curDepth + 1;

            this.pA = new Vector2(pA.x, pA.y);
            this.pB = new Vector2(pB.x, pB.y);
            float midX = (pA.x + pB.x) * 0.5f;
            float midY = (pA.y + pB.y) * 0.5f;
            if (isRight) this.pA.x = midX;
            else         this.pB.x = midX;
            if (isUp)    this.pA.y = midY;
            else         this.pB.y = midY;

            childList = new List<QuadNode>();
            enemyList = new List<GameObject>();
        }

        // 0右上 1左上 2左下 3右下
        private int GetChildIndex(Vector2 p)
        {
            float midX = (pA.x + pB.x) * 0.5f;
            float midY = (pA.y + pB.y) * 0.5f;
            int index = 0;
            if (p.x < midX)
            {
                index = 1;
                if (p.y < midY) index = 2;
            }
            else if (p.y < midY) index = 3;
            return index;
        }

        public void AddEnemy(GameObject enemy)
        {
            if (!enemy.CompareTag("Enemy")) return;
            Vector2 p = new Vector2(enemy.transform.position.x, enemy.transform.position.z);
            if (p.x < pA.x || p.x > pB.x || p.y < pA.y || p.y > pB.y) return;

            if (childList.Count > 0)
            {
                childList[GetChildIndex(p)].AddEnemy(enemy);
                return;
            }

            enemyList.Add(enemy);
            if (enemyList.Count > maxCapacity && curDepth < maxDepth)
                CreateChild();
        }

        private void CreateChild()
        {
            childList.Add(new QuadNode(pA, pB, curDepth, true, true));
            childList.Add(new QuadNode(pA, pB, curDepth, false, true));
            childList.Add(new QuadNode(pA, pB, curDepth, false, false));
            childList.Add(new QuadNode(pA, pB, curDepth, true, false));

            foreach (var GO in enemyList)
            {
                childList[GetChildIndex(new Vector2(GO.transform.position.x, GO.transform.position.z))].AddEnemy(GO);
            }

            enemyList.Clear();
        }

        public void TryMerge()
        {
            if (childList.Count == 0) return;

            foreach (QuadNode child in childList)
                child.TryMerge();

            bool allLeaf = true;
            int total = 0;
            foreach (QuadNode child in childList)
            {
                total += child.enemyList.Count;
                if (child.childList.Count > 0) allLeaf = false;
            }

            if (allLeaf && total <= maxCapacity)
            {
                foreach (QuadNode child in childList)
                {
                    enemyList.AddRange(child.enemyList);
                    child.enemyList.Clear();
                }
                childList.Clear();
            }
        }
    }
}
