using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class BattleObject : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        public float maxHP;
        public GameObject combatHitbox;

        [Space(10)]

        [Header("(REFERENCE)")]
        public float curHP;
        public bool isDead;

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