﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class BattleCollider : MonoBehaviour 
    {
        public bool playerIsImmune;
        public bool enemyIsImmune;

        public float curSpeed;
        public Vector3 curDirection;
        public float timeBeforeSelfDestroy;

        public GameObject prefabToSpawn;
        public GameObject prefabBattleColliderInstructionOnSelfDestroy;

        // Use this for initialization
        void Start () 
        {

        }
    }
}