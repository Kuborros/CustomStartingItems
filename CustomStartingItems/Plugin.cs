﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace CustomStartingItems
{
    [BepInPlugin("com.kuborro.plugins.fp2.customstartitemsenable", "CustomStartingItems", "1.0.2")]
    [BepInIncompatibility("com.eps.plugin.fp2.potion-seller")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(PatchMenuDifficulty));
        }
    }

    public class PatchMenuDifficulty
    {
        static string mainDescription;
        static bool goModeFromCustomItems;

        //Reflection magic
        private static readonly MethodInfo m_State_ItemSelect = typeof(MenuDifficulty).GetMethod("State_ItemSelect", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo m_State_Main = typeof(MenuDifficulty).GetMethod("State_Main", BindingFlags.NonPublic | BindingFlags.Instance);


        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuDifficulty),"Start",MethodType.Normal)]
        static void PatchMenuDifficultyStart(ref MenuDifficulty __instance,ref bool ___disableCustomItems)
        {
            //Disable the hardcoded off switch
            ___disableCustomItems = false;
            //Move "Return" button bit down
            __instance.menuOptions[4].start.y = -272;

            //"Borrow" an instance of MenuDigit containing all the item sprites.
            FPHudDigit itemSprites = __instance.optionGroupRightside.transform.GetChild(2).transform.GetChild(2).GetComponent<FPHudDigit>();
            if (itemSprites != null) {
                //Proper icon for element burst
                __instance.customItemIconList[0] = itemSprites.digitFrames[6];
                __instance.itemPanel.transform.GetChild(4).transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = itemSprites.digitFrames[6];
                //Proper icon for fire charm
                __instance.customItemIconList[7] = itemSprites.digitFrames[16];
                __instance.itemPanel.transform.GetChild(11).transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = itemSprites.digitFrames[16];

                //Replace broken potions with items (TODO: Maybe sometime fix up the potions)
                __instance.customItemList[2] = FPPowerup.MAX_LIFE_UP;
                __instance.customItemIconList[2] = itemSprites.digitFrames[7];
                __instance.itemPanel.transform.GetChild(6).transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = itemSprites.digitFrames[7];

                __instance.customItemList[3] = FPPowerup.POWERUP_START;
                __instance.customItemIconList[3] = itemSprites.digitFrames[12];
                __instance.itemPanel.transform.GetChild(7).transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = itemSprites.digitFrames[12];

            } else 
            {
                Plugin.Logger.LogError("Could not find sprites to borrow! Bad!");
            }
            //Move second item in custom submenu to sorting layer zero, to not be drawn above menus
            __instance.optionGroupRightside.transform.GetChild(2).transform.GetChild(2).GetComponent<SpriteRenderer>().sortingOrder = 0;

            //Store the original description string
            mainDescription = __instance.itemTipLabel.GetComponent<TextMesh>().text;

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuDifficulty), "UpdateMenuPosition", MethodType.Normal)]
        static void PatchMenuDifficultyUpdateMenuPosition(ref MenuDifficulty __instance, ref int ___menuSelection)
        {
            if (___menuSelection >= 5)
            {
                __instance.itemTipLabel.GetComponent<TextMesh>().text = "Choose your own set of items!\nSelect a slot to see the possible selections.\nEvery item here can be collected later on!";
                goModeFromCustomItems = true;
            }
            else
            {
                if (__instance.itemTipLabel.GetComponent<TextMesh>().text != mainDescription)
                {
                    __instance.itemTipLabel.GetComponent<TextMesh>().text = mainDescription;
                }
                goModeFromCustomItems = false;
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuDifficulty), "State_WaitForMenu", MethodType.Normal)]
        static bool PatchMenuDifficultyWaitForMenu(MenuDifficulty __instance, GameObject ___targetMenu, ref int ___menuSelection)
        {
            float num = 5f * FPStage.frameScale;
            __instance.transform.position = new Vector3(__instance.transform.position.x, (__instance.transform.position.y * (num - 1f) - 360f) / num, __instance.transform.position.z);
            if (___targetMenu == null)
            {
                if (goModeFromCustomItems)
                {
                    __instance.state = (FPObjectState)Delegate.CreateDelegate(typeof(FPObjectState), __instance, m_State_ItemSelect);
                    ___menuSelection = 5;
                }
                else
                {
                    __instance.state = (FPObjectState)Delegate.CreateDelegate(typeof(FPObjectState), __instance, m_State_Main);
                }
            }
            //Skip original code
            return false;
        }
    }
}
