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
            // RIGHT BUMPER (QUICKSELECT MENU)
            battleManagerSingleton.battleToolQuickSelectActive = Input.GetButton("LeftBumper");
            var battleToolQuickSelectInit = Input.GetButtonDown("LeftBumper");
            bool leftToolButtonActive = Input.GetAxisRaw("LeftTrigger") > 0;
            bool rightToolButtonActive = Input.GetAxisRaw("RightTrigger") > 0;

            var rawHorizontalInput = Input.GetAxisRaw("LeftJoystickHorizontal");
            var rawVerticalInput = -Input.GetAxisRaw("LeftJoystickVertical");
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
                UIManager.singleton.toolQuickSelectMenuForBattle.SetActive(false);

                // LEFT JOYSTICK (MOVEMENT)
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

                Vector2 movement = new Vector2(rawHorizontalInput * battleManagerSingleton.playerSpeed * Time.deltaTime, 
                    rawVerticalInput * battleManagerSingleton.playerSpeed * Time.deltaTime);
                
                battleManagerSingleton.player.GetComponent<Rigidbody2D>().velocity = movement;

                //AIM
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

                //TOOLS
                if (leftToolButtonActive)
                    battleManagerSingleton.InitToolUse(battleManagerSingleton.playerLeftTool);

                if (rightToolButtonActive)
                    battleManagerSingleton.InitToolUse(battleManagerSingleton.playerRightTool);
            }
            else
            {
                battleManagerSingleton.player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;

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
                }
            }
        }


    }


}