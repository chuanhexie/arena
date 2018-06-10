using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace Arena
{
    public class CountdownRing : MonoBehaviour 
    {
        [Header("(REFERENCE)")]
        [Header("Target")]
        public GameObject targetGameObject;

        [Header("Type")]
        public CountdownRingType type;

        [Header("General")]
        public bool isReoccurring;

        [Header("Countdown")]
        public float currentCountdownValue;
        public float initialCountdownValue;

    }
}