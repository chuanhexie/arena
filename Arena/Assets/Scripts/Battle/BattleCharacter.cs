using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace Arena
{
    public class BattleCharacter : MonoBehaviour 
    {
        public bool isPlayer;

        public Sprite sprite;
        public float walkSpeed;
        public float runSpeed;

        public float remainingStunTime;
        public float poisonDamageFrequency;
        public float poisonNextDamageCountdown;

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