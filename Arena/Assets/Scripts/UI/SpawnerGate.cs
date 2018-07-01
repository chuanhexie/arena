using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Arena
{
    public class SpawnerGate : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Child Plugins")]
        public GameObject gateGraphicObject;
        public GameObject enemyGraphicObject;

        [Header("References")]
        public GameObject linkedSpawner;
    }
}