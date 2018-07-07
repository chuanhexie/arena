using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class CombatStats : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Stats")]
        public float damage;
        public float poison;
        public float stun;
        public float fire;
        public float useSpeed;
        public float resourceEfficiency;

        [Header("Buff")]
        public bool isBuff = false;
        public bool isBuffInfinite = false;
        public float tempBuffDuration = 0;
    }
}