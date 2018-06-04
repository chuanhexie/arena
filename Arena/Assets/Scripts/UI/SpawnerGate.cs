using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Arena
{
    public class SpawnerGate : MonoBehaviour 
    {
        public GameObject gateGraphicObject;
        public GameObject enemyGraphicObject;

        public GameObject adjacentTile;
        public int horNodeCount;
        public int vertNodeCount;
        public float nodeSpacing;

        public List<GameObject> clockArrows;

        // Use this for initialization
        void Start () 
        {

        }

        // Update is called once per frame
        void Update () 
        {

        }


    }
}