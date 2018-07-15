using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Pathfinding;
using Rewired;

namespace Arena
{
    [RequireComponent(typeof(CharacterController))]
    public class GameManager : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("General")]
        public List<GameObject> defaultPlayerToolModels;

        [Header("Input")]
        public GameObject inputValuesContainerGO;

        [Header("Singletons")]
        private BattleManager battleManagerSingleton;
        private GridManager gridManagerSingleton;
        private UIManager UIManagerSingleton;
        private MenuManager menuManagerSingleton;

        [Header("(REFERENCE)")]
        public GameState curGameState;
        public int playerId = 0; // The Rewired player id of the main (and only) player
        public bool isPaused;

        public bool isLeftTriggerAxisInUse;

        private Player inputPlayer; // The Rewired Player

        // gameplay reference
        public List<Tool> playerAllTools;

        [Header("Input")]
        public InputValues inputValuesScript;

        // Use this for initialization
        void Start () 
        {
            // STORE SINGLETONS
            battleManagerSingleton = BattleManager.singleton;
            gridManagerSingleton = GridManager.singleton;
            UIManagerSingleton = UIManager.singleton;
            menuManagerSingleton = MenuManager.singleton;

            // INPUT
            inputValuesScript = inputValuesContainerGO.GetComponent<InputValues>();

            // PLAYER TOOLS
            InitPlayerDefaultTools();

            if (curGameState == GameState.shop)
            {
                // MENU MANAGER
                menuManagerSingleton.mainCanvas = UIManagerSingleton.canvas;
                menuManagerSingleton.mainCanvasTransform = UIManagerSingleton.canvas.GetComponent<Transform>();
                menuManagerSingleton.InitMenuManager();
            }
            else if (curGameState == GameState.battle)
            {
                // GENERATE TILES & ADD ALL BLOCKS TO BATTLE OBJECTS LIST
                gridManagerSingleton.GenerateTiles();

                // INIT BATTLE MANAGER
                battleManagerSingleton.InitBattle();

                // UI: INIT HP & STATUS DISPLAYS (ON CANVAS) TO FOLLOW ENEMIES & BLOCKS
                UIManagerSingleton.InitBattleObjectStatsDisplays();

                // UI: tool quickselect in-battle
                UIManagerSingleton.toolQuickSelectMenuForBattle = UIManagerSingleton.InitToolQuickSelectMenu();
            }
        }


        // Update is called once per frame
        void FixedUpdate () 
        {
            // CHECK PLAYER INPUT & PERFORM APPROPRIATE ACTIONS
            UpdatePlayerInputValues();

            if (curGameState == GameState.battle)
            {
                // BATTLE LOGIC
                battleManagerSingleton.UpdateBattleLogic();

                // UI LOGIC: BATTLE
                UIManagerSingleton.UpdateUILogic();
            }
        }

        public static GameManager singleton;
        void Awake()
        {
            singleton = this;

            // Get the Rewired Player object for this player and keep it for the duration of the character's lifetime
            inputPlayer = ReInput.players.GetPlayer(playerId);
        }

        // CHECK PLAYER INPUT & PERFORM APPROPRIATE ACTIONS
        public void UpdatePlayerInputValues()
        {
            // AXES
            inputValuesScript.vertMovement = RoundAxisVal(inputPlayer.GetAxis("Move Vertical"));
            inputValuesScript.horMovement = RoundAxisVal(inputPlayer.GetAxis("Move Horizontal"));
            inputValuesScript.vertAim = RoundAxisVal(inputPlayer.GetAxis("Aim Vertical"));
            inputValuesScript.horAim = RoundAxisVal(inputPlayer.GetAxis("Aim Horizontal"));

            // BUTTONS
            inputValuesScript.leftToolOrPageLeft = inputPlayer.GetButton("Left Tool / Page Left");
            inputValuesScript.rightToolOrPageRight = inputPlayer.GetButton("Right Tool / Page Right");
            inputValuesScript.quickselectOrMoreInfo = inputPlayer.GetButton("Quickselect / More Info");
            inputValuesScript.runOrCursorSpeed = inputPlayer.GetButton("Run / Menu Speed");
            inputValuesScript.heal = inputPlayer.GetButton("Heal");
        }

        public float RoundAxisVal(float _baseAxisVal)
        {
            float output = 0;
            if (_baseAxisVal > 0)
                output = 1;
            else if (_baseAxisVal < 0)
                output = -1;
            return output;
        }

        public void InitPlayerDefaultTools()
        {
            foreach (GameObject curToolModelGO in defaultPlayerToolModels)
                playerAllTools.Add(Instantiate(curToolModelGO).GetComponent<Tool>());
        }

        public static Vector2 radianToDirection(float _input, bool _isDegree = false)
        {
            if (_isDegree)
                _input = _input * Mathf.Deg2Rad;

            return new Vector2((float)Mathf.Cos(_input), (float)Mathf.Sin(_input));
        }

        public static float directionVectorToAngle (Vector2 _direction)
        {
            float output = Mathf.Atan(_direction.y/_direction.x);
            if (_direction.x < 0)
                output *= -1;
            if (_direction.y < 0)
                output += 180;

            return output;
        }

        public static int layermaskToLayer(LayerMask _layerMask)
        {
            int layerNumber = 0;
            int layer = _layerMask.value;
            while (layer > 0) {
                layer = layer >> 1;
                layerNumber++;
            }
            return layerNumber - 1;
        }

        public static Vector2 getDirectionTowardsPosition(Vector2 _sourceGO, Vector2 _targetGO)
        {
            Vector2 dir = (_targetGO - _sourceGO).normalized;
            return dir;
        }

        public static LayerMask RemoveFromLayerMask(LayerMask _inputMask, LayerMask _maskToRemove)
        {
            LayerMask invertedInputMask = ~_inputMask;
            return ~(invertedInputMask | _maskToRemove);
        }

        public static float NegativeToZero(float _input)
        {
            if (_input < 0)
                _input = 0;

            return _input;
        }
    }

    public enum GameState
    {
        battle,shop
    }
}