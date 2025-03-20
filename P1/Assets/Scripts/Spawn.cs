using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawn : MonoBehaviour
{  
    public GameObject prefab;
    public KeyCode spawnKey = KeyCode.Space;
    
    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            Instantiate(prefab, transform.position, Quaternion.identity);
        }
    }
}
