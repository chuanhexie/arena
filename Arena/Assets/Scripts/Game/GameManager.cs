using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Pathfinding;

namespace Arena
{
    public class GameManager : MonoBehaviour 
    {
        // INIT SINGLETON VARIABLES
        private BattleManager battleManagerSingleton;
        private GridManager gridManagerSingleton;
        private UIManager UIManagerSingleton;

        public bool isLeftTriggerAxisInUse;

        // Use this for initialization
        void Start () 
        {
            // STORE SINGLETONS
            battleManagerSingleton = BattleManager.singleton;
            gridManagerSingleton = GridManager.singleton;
            UIManagerSingleton = UIManager.singleton;

            // GENERATE TILES & ADD ALL BLOCKS TO BATTLE OBJECTS LIST
            gridManagerSingleton.GenerateTiles();

            // INIT BATTLE MANAGER
            battleManagerSingleton.InitBattle();

            // INIT HP & STATUS DISPLAYS (ON CANVAS) TO FOLLOW ENEMIES & BLOCKS
            UIManagerSingleton.InitBattleObjectStatsDisplays();

            // INPUT
            isLeftTriggerAxisInUse = false;
        }


        // Update is called once per frame
        void FixedUpdate () 
        {
            // CHECK PLAYER INPUT & PERFORM APPROPRIATE ACTIONS
            CheckInput();

            // BATTLE LOGIC
            battleManagerSingleton.UpdateBattleLogic();

            // UI LOGIC: BATTLE
            UIManagerSingleton.UpdateUILogic();
        }

        public static GameManager singleton;
        void Awake()
        {
            singleton = this;
        }

        // CHECK PLAYER INPUT & PERFORM APPROPRIATE ACTIONS
        public void CheckInput()
        {
            // --------------- SHOULDER BUTTONS START --------------------
            // LEFT TRIGGER, LOGIC TO CHECK IF FIRST DOWN OR NOT FOR INITIALIZING QUICKSELECT
            var battleToolQuickSelectInit = false;
            if (Input.GetAxisRaw("LeftTrigger") > 0)
            {
                if (isLeftTriggerAxisInUse == false)
                {
                    isLeftTriggerAxisInUse = true;
                    battleToolQuickSelectInit = true;
                }
            }
            if (Input.GetAxisRaw("LeftTrigger") == 0)
            {
                isLeftTriggerAxisInUse = false;
            }
            battleManagerSingleton.battleToolQuickSelectActive = isLeftTriggerAxisInUse;

            // RUN BUTTON
            BattleManager.singleton.playerIsRunning = ((
                !BattleManager.singleton.isPlayerUsingMirage &&
                (Input.GetAxisRaw("RightTrigger") > 0) || Input.GetButton("RightControl")) && 
                !BattleManager.singleton.isPlayerCurrentlyUsingTool &&
                BattleManager.singleton.curStamina > 0);

            // TOOL BUTTONS
            bool leftToolButtonActive = Input.GetButton("LeftBumper");
            if (!leftToolButtonActive)
                leftToolButtonActive = Input.GetButton("LeftControl");
            bool rightToolButtonActive = Input.GetButton("RightBumper");
            if (!rightToolButtonActive)
                rightToolButtonActive = Input.GetButton("Space");

            // --------------- SHOULDER BUTTONS END --------------------

            var rawHorizontalInputLeft = Input.GetAxisRaw("LeftJoystickHorizontal");
            var rawVerticalInputLeft = -Input.GetAxisRaw("LeftJoystickVertical");

            // LEFT JOYSTICK
            if (rawHorizontalInputLeft == 0)
                rawHorizontalInputLeft = Input.GetAxisRaw("LeftKeysHorizontal");
            if (rawVerticalInputLeft == 0)
                rawVerticalInputLeft = Input.GetAxisRaw("LeftKeysVertical");
            var horizontalInputDirectionLeft = 0;
            var verticalInputDirectionLeft = 0;

            if (rawHorizontalInputLeft > 0)
                horizontalInputDirectionLeft = 1;
            else if (rawHorizontalInputLeft < 0)
                horizontalInputDirectionLeft = -1;

            if (rawVerticalInputLeft > 0)
                verticalInputDirectionLeft = 1;
            else if (rawVerticalInputLeft < 0)
                verticalInputDirectionLeft = -1;

            var movementSpeed = battleManagerSingleton.playerWalkSpeed;
            if (battleManagerSingleton.isPlayerUsingMirage)
                movementSpeed = battleManagerSingleton.playerMirageMovementSpeed;
            if (battleManagerSingleton.playerIsRunning)
                movementSpeed = battleManagerSingleton.playerRunSpeed;

            Vector2 movement = new Vector2(rawHorizontalInputLeft * movementSpeed * Time.deltaTime, 
                rawVerticalInputLeft * movementSpeed * Time.deltaTime);
            
            battleManagerSingleton.player.GetComponent<Rigidbody2D>().velocity = movement;

            // RIGHT JOYSTICK
            var rawHorizontalInputRight = Input.GetAxisRaw("RightJoystickHorizontal");
            var rawVerticalInputRight = -Input.GetAxisRaw("RightJoystickVertical");
            if (rawHorizontalInputRight == 0)
                rawHorizontalInputRight = Input.GetAxisRaw("RightKeysHorizontal");
            if (rawVerticalInputRight == 0)
                rawVerticalInputRight = Input.GetAxisRaw("RightKeysVertical");
            var horizontalInputDirectionRight = 0;
            var verticalInputDirectionRight = 0;

            if (rawHorizontalInputRight > 0)
                horizontalInputDirectionRight = 1;
            else if (rawHorizontalInputRight < 0)
                horizontalInputDirectionRight = -1;

            if (rawVerticalInputRight > 0)
                verticalInputDirectionRight = 1;
            else if (rawVerticalInputRight < 0)
                verticalInputDirectionRight = -1;

            var rawInputAngle = Mathf.Atan2(-rawHorizontalInputRight, rawVerticalInputRight) * Mathf.Rad2Deg;
            rawInputAngle += 90;
            if (rawInputAngle < 0)
                rawInputAngle += 360;
            if (rawInputAngle > 360)
                rawInputAngle -= 360;
            if (battleToolQuickSelectInit && rawHorizontalInputRight == 0 && rawVerticalInputRight == 0)
                rawInputAngle = 90;
            
            if (!battleManagerSingleton.battleToolQuickSelectActive)
            {
                // HIDE TOOL QUICKSELECT
                UIManager.singleton.toolQuickSelectMenuForBattle.SetActive(false);


                //DIRECTIONAL AIM
                var dirAimHorInput = horizontalInputDirectionRight;
                var dirAimVertInput = verticalInputDirectionRight;
                if (battleManagerSingleton.isPlayerUsingMirage)
                {
                    dirAimHorInput = horizontalInputDirectionLeft;
                    dirAimVertInput = verticalInputDirectionLeft;
                }

                if (dirAimHorInput != 0 || dirAimVertInput != 0)
                {
                    battleManagerSingleton.curDirectionX = dirAimHorInput;
                    battleManagerSingleton.curDirectionY = dirAimVertInput;

                    var aimRotation = 0;

                    if (dirAimHorInput == 1 && dirAimVertInput == 1)
                        aimRotation = 315;
                    else if (dirAimHorInput == 1 && dirAimVertInput == 0)
                        aimRotation = 270;
                    else if (dirAimHorInput == 1 && dirAimVertInput == -1)
                        aimRotation = 225;
                    else if (dirAimHorInput == 0 && dirAimVertInput == -1)
                        aimRotation = 180;
                    else if (dirAimHorInput == -1 && dirAimVertInput == -1)
                        aimRotation = 135;
                    else if (dirAimHorInput == -1 && dirAimVertInput == 0)
                        aimRotation = 90;
                    else if (dirAimHorInput == -1 && dirAimVertInput == 1)
                        aimRotation = 45;

                    battleManagerSingleton.playerDirectionalAim.transform.localRotation = Quaternion.Euler(0, 0, aimRotation);
                }

                Vector2 directionalAimOffset = new Vector2(battleManagerSingleton.curDirectionX / 2, battleManagerSingleton.curDirectionY / 2 - 0.5f);
                battleManagerSingleton.playerDirectionalAim.transform.localPosition = battleManagerSingleton.playerDirectionalAimCenter + directionalAimOffset;

                // TOP DOWN AIM (ALT AIM)
                if (battleManagerSingleton.playerTopDownAim.activeInHierarchy)
                    battleManagerSingleton.UpdatePlayerTopDownAim(new Vector2(
                        horizontalInputDirectionRight * battleManagerSingleton.playerTopDownAimMovementSpeed,
                        verticalInputDirectionRight * battleManagerSingleton.playerTopDownAimMovementSpeed));

                //TOOLS
                if (!battleManagerSingleton.isPlayerUsingMirage)
                {
                    if (leftToolButtonActive)
                        battleManagerSingleton.InitToolUse(battleManagerSingleton.playerLeftTool);

                    if (rightToolButtonActive)
                        battleManagerSingleton.InitToolUse(battleManagerSingleton.playerRightTool);
                }
            }
            else
            {
                var toolQuickSelectMenuForBattle = UIManager.singleton.toolQuickSelectMenuForBattle;
                var toolQuickSelectMenuForBattleScript = toolQuickSelectMenuForBattle.GetComponent<ToolQuickSelectMenu>();
                toolQuickSelectMenuForBattle.SetActive(true);
                if (rawHorizontalInputRight != 0 || rawVerticalInputRight != 0)
                    toolQuickSelectMenuForBattleScript.selectionAngle = rawInputAngle;
                else if (battleToolQuickSelectInit)
                    toolQuickSelectMenuForBattleScript.selectionAngle = 90;

                var battleQuickSelectCurrentlySelectedTool = toolQuickSelectMenuForBattleScript.GetCurrentlySelectedTool();
                if (battleQuickSelectCurrentlySelectedTool != null)
                {
                    if (leftToolButtonActive)
                    {
                        if (battleQuickSelectCurrentlySelectedTool == battleManagerSingleton.playerRightTool)
                            battleManagerSingleton.SwapLeftAndRightTools();
                        else
                        {
                            battleManagerSingleton.playerLeftTool = battleQuickSelectCurrentlySelectedTool;
                        }
                    }

                    if (rightToolButtonActive)
                    {
                        if (battleQuickSelectCurrentlySelectedTool == battleManagerSingleton.playerLeftTool)
                            battleManagerSingleton.SwapLeftAndRightTools();
                        else
                        {
                            battleManagerSingleton.playerRightTool = battleQuickSelectCurrentlySelectedTool;
                        }
                    }

                    if (leftToolButtonActive || rightToolButtonActive)
                    {
                        battleManagerSingleton.SetPlayerTopDownAim();
                    }
                }
            }
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
}