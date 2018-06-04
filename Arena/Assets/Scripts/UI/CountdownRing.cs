using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace Arena
{
    public class CountdownRing : MonoBehaviour 
    {
        public GameObject targetGameObject;
        public float currentCountdownValue;
        public float initialCountdownValue;
        public CountdownRingType type;
    }
}