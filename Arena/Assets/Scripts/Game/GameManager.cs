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

            // INIT TOP DOWN AIM BOUNDARY
            UIManagerSingleton.InitPlayerTopDownAimBoundary();

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

            var rawHorizontalInput = Input.GetAxisRaw("LeftJoystickHorizontal");
            var rawVerticalInput = -Input.GetAxisRaw("LeftJoystickVertical");

            // LEFT JOYSTICK
            if (rawHorizontalInput == 0)
                rawHorizontalInput = Input.GetAxisRaw("LeftKeysHorizontal");
            if (rawVerticalInput == 0)
                rawVerticalInput = Input.GetAxisRaw("LeftKeysVertical");
            var horizontalInputDirection = 0;
            var verticalInputDirection = 0;

            if (rawHorizontalInput > 0)
                horizontalInputDirection = 1;
            else if (rawHorizontalInput < 0)
                horizontalInputDirection = -1;

            if (rawVerticalInput > 0)
                verticalInputDirection = 1;
            else if (rawVerticalInput < 0)
                verticalInputDirection = -1;

            var movementSpeed = battleManagerSingleton.playerWalkSpeed;
            if (battleManagerSingleton.playerIsRunning)
                movementSpeed = battleManagerSingleton.playerRunSpeed;

            Vector2 movement = new Vector2(rawHorizontalInput * movementSpeed * Time.deltaTime, 
                rawVerticalInput * movementSpeed * Time.deltaTime);
            
            battleManagerSingleton.player.GetComponent<Rigidbody2D>().velocity = movement;

            // RIGHT JOYSTICK
            rawHorizontalInput = Input.GetAxisRaw("RightJoystickHorizontal");
            rawVerticalInput = -Input.GetAxisRaw("RightJoystickVertical");
            if (rawHorizontalInput == 0)
                rawHorizontalInput = Input.GetAxisRaw("RightKeysHorizontal");
            if (rawVerticalInput == 0)
                rawVerticalInput = Input.GetAxisRaw("RightKeysVertical");
            horizontalInputDirection = 0;
            verticalInputDirection = 0;

            if (rawHorizontalInput > 0)
                horizontalInputDirection = 1;
            else if (rawHorizontalInput < 0)
                horizontalInputDirection = -1;

            if (rawVerticalInput > 0)
                verticalInputDirection = 1;
            else if (rawVerticalInput < 0)
                verticalInputDirection = -1;

            var rawInputAngle = Mathf.Atan2(-rawHorizontalInput, rawVerticalInput) * Mathf.Rad2Deg;
            rawInputAngle += 90;
            if (rawInputAngle < 0)
                rawInputAngle += 360;
            if (rawInputAngle > 360)
                rawInputAngle -= 360;
            if (battleToolQuickSelectInit && rawHorizontalInput == 0 && rawVerticalInput == 0)
                rawInputAngle = 90;
            
            if (!battleManagerSingleton.battleToolQuickSelectActive)
            {
                // HIDE TOOL QUICKSELECT
                UIManager.singleton.toolQuickSelectMenuForBattle.SetActive(false);

                //DIRECTIONAL AIM
                if (horizontalInputDirection != 0 || verticalInputDirection != 0)
                {
                    battleManagerSingleton.curDirectionX = horizontalInputDirection;
                    battleManagerSingleton.curDirectionY = verticalInputDirection;

                    var aimRotation = 0;

                    if (horizontalInputDirection == 1 && verticalInputDirection == 1)
                        aimRotation = 315;
                    else if (horizontalInputDirection == 1 && verticalInputDirection == 0)
                        aimRotation = 270;
                    else if (horizontalInputDirection == 1 && verticalInputDirection == -1)
                        aimRotation = 225;
                    else if (horizontalInputDirection == 0 && verticalInputDirection == -1)
                        aimRotation = 180;
                    else if (horizontalInputDirection == -1 && verticalInputDirection == -1)
                        aimRotation = 135;
                    else if (horizontalInputDirection == -1 && verticalInputDirection == 0)
                        aimRotation = 90;
                    else if (horizontalInputDirection == -1 && verticalInputDirection == 1)
                        aimRotation = 45;

                    battleManagerSingleton.playerDirectionalAim.transform.localRotation = Quaternion.Euler(0, 0, aimRotation);
                }

                battleManagerSingleton.playerDirectionalAim.transform.localPosition = new Vector2(battleManagerSingleton.curDirectionX / 2, battleManagerSingleton.curDirectionY / 2 - 0.5f);

                // TOP DOWN AIM (ALT AIM)
                if (battleManagerSingleton.playerTopDownAim.activeInHierarchy)
                    battleManagerSingleton.UpdatePlayerTopDownAim(new Vector2(
                        horizontalInputDirection * battleManagerSingleton.playerTopDownAimMovementSpeed,
                        verticalInputDirection * battleManagerSingleton.playerTopDownAimMovementSpeed));

                //TOOLS
                if (leftToolButtonActive)
                    battleManagerSingleton.InitToolUse(battleManagerSingleton.playerLeftTool);

                if (rightToolButtonActive)
                    battleManagerSingleton.InitToolUse(battleManagerSingleton.playerRightTool);
            }
            else
            {
                var toolQuickSelectMenuForBattle = UIManager.singleton.toolQuickSelectMenuForBattle;
                var toolQuickSelectMenuForBattleScript = toolQuickSelectMenuForBattle.GetComponent<ToolQuickSelectMenu>();
                toolQuickSelectMenuForBattle.SetActive(true);
                if (rawHorizontalInput != 0 || rawVerticalInput != 0)
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
    }
}