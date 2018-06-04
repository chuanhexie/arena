using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arena
{
    public class BattleObjectStatsDisplay : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Child Plugins")]
        public GameObject hpText;

        [Space(10)]

        [Header("(REFERENCE)")]
        public GameObject representedBattleObject;

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