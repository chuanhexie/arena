using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class BattleObject : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        public float maxHP;
        public GameObject defensiveCombatHitbox;
        public GameObject offensiveCombatHitbox;

        [Space(10)]

        [Header("(REFERENCE)")]
        public float curHP;
        public bool isDead;
        public bool isPlayer;


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