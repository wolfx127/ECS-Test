using System;
using UnityEngine;

//起到一个分发器的作用，
//因为多个mono的生命周期会打架，所以只用一个mono
//来分发引用，控制顺序，保证分发前实例已经建好
public class MonoManager : MonoBehaviour
{
    public bool UseECS = false;
    public bool UseMono = false;
    
    //分发注入名单


    private void Awake()
    {
        if(!UseMono && !UseECS) UseMono = true;
        
        if (UseMono)
        {
            UseECS = false;
        }

        if (UseECS)
        {
            UseMono = false;
        }
    }

    private void OnEnable()
    {
        
    }
}
