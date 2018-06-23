using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class Tool : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Prefabs")]
        public List<GameObject> battleColliderInstructionPrefabs;

        [Header("Sprites")]
        public Sprite thumbnail;

        [Header("General")]
        public GameObject combatStats;
        public float combatStatsReduction;
        public bool usesMana;
        public bool isBleed;
        public bool isReloadTool;
        public float knockbackForce;

        [Header("(REFERENCE)")]
        [Header("Reload")]
        public int curAmmoClip = 1;
        public int maxAmmoCLip = 1;
    }
}