using System;
using System.Collections.Generic;
using GameObjectFrame;
using UnityEngine;
using UnityEngine.InputSystem;

//起到一个分发器的作用，
//因为多个mono的生命周期会打架，所以只用一个mono
//来分发引用，控制顺序，保证分发前实例已经建好
//因为小项目所以加一点其他逻辑：接管所有非Mono的update逻辑
public class MonoManager : MonoBehaviour
{
    public bool UseECS = false;
    public bool UseMono = false;
    public HashSet<EnemyData> enemyList;
    public int playerMoney = 0;
    
    //准备注入
    private MoveCameraInputAction cameraInputAction;
    public GameObject cameraGO;
    public GameObject enemyPoolGO;
    public GameObject defaultEnemyPfb;
    public GameObject player;
    public GameObject prop;

    //被注入名单
    private CameraManager cameraMgr;
    private EnemyPool enemyPool;
    private QuadTreeMgr quadTreeMgr;
    private PlayerData playerData;

    private void Awake()
    {
        cameraInputAction = new MoveCameraInputAction();
        
        if(!UseMono && !UseECS) UseMono = true;
        
        if (UseMono)
        {
            UseECS = false;
            quadTreeMgr = new QuadTreeMgr();
            enemyPool = new EnemyPool(player, defaultEnemyPfb, enemyPoolGO, quadTreeMgr, prop);
            playerData = new PlayerData(player, quadTreeMgr, enemyPool);
        }

        if (UseECS)
        {
            UseMono = false;
        }
    }

    private void OnEnable()
    {
        cameraMgr = new CameraManager(cameraInputAction, cameraGO.GetComponent<Transform>());
    }

    private void Start()
    {
        if(UseMono)
        {
            
        }

        if (UseECS)
        {
            
        }
    }

    private void Update()
    {
        if (UseMono)
        {
            enemyPool.Tick();
            quadTreeMgr.Tick(Time.deltaTime);
            playerData.Tick(Time.deltaTime);
            playerMoney = playerData.money;
        }
    }

    private void FixedUpdate()
    {
        if(UseMono)
        {
            if (enemyPool == null) return;
            enemyPool.FixedTick(Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        cameraMgr.Tick(Time.deltaTime);
    }

    
    // 四叉树调试
    [SerializeField] int gizmoMaxDepth = 6;
    [SerializeField] float gizmoBlockHeight = 0.3f;   // 每层方块的厚度
    private void OnDrawGizmos()
    {
        if (!UseMono || quadTreeMgr == null) return;
        DrawQuadNode(quadTreeMgr.root);
    }

    private void DrawQuadNode(QuadNode node)
    {
        float w = node.pB.x - node.pA.x;
        float d = node.pB.y - node.pA.y;

        int span = Mathf.Max(1, gizmoMaxDepth - 1);
        float t = Mathf.Clamp01((float)(node.curDepth - 1) / span);
        Gizmos.color = Color.Lerp(Color.green, Color.blue, t);   // 绿 → 蓝，越深越蓝

        // 实心方块：深度越深方块越厚，绿底座上叠蓝色高塔
        float height = gizmoBlockHeight * node.curDepth;
        Vector3 center = new Vector3(node.pA.x + w * 0.5f, height * 0.5f, node.pA.y + d * 0.5f);
        Gizmos.DrawCube(center, new Vector3(w, height, d));

        if (node.curDepth >= gizmoMaxDepth) return;
        foreach (QuadNode child in node.childList)
            DrawQuadNode(child);
    }
}
