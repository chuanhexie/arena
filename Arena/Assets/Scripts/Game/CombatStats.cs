using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class CombatStats : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        public float damage;
        public float poison;
        public float stun;
        public float fire;
        public float useSpeed;
        public float resourceEfficiency;
    }
}