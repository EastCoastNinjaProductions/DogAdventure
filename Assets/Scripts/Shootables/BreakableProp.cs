﻿using UnityEngine;

[RequireComponent(typeof(Shootable))]
public class BreakableProp : MonoBehaviour
{
    
    public float health;
    public GameObject fxPrefab;

    void Start()
    {
        GetComponent<Shootable>().onShootDelegate += OnShoot;
    }
    
    public void OnShoot(PlayerShotInfo info)
    {
        health -= info.damage;
        if (health <= 0)
        {
            Break();
        }
    }

    void Break()
    {
        GameObject fx = Instantiate(fxPrefab);
        fx.transform.position = transform.position;
        Destroy(gameObject);
    }

}
