using System;
using HarmonyLib;
using BepInEx;
using UnityEngine;
using UObj = UnityEngine.Object;
using URand = UnityEngine.Random;
using System.Threading;
using System.IO;
using static Ultracoins.UltraCoins;
using System.Reflection;
/*using PluginConfig;
using PluginConfig.API.Fields;
using PluginConfig.API;
using System.Runtime.CompilerServices;*/

namespace Ultracoins
{
    internal class PluginInfo
    {
        public const string Name = "UltraCoins!";
        public const string GUID = "ironfarm.uk.uc";
        public const string Version = "1.0.6";
    }
    /*public static class ConfigManager
    {
        private static PluginConfigurator config;
        public static BoolField isEnabled;
        public static FloatField spread;
        public static FloatField tossDelay;
        public static BoolField altSpam;

        public static void Setup()
        {
            config = PluginConfigurator.Create(PluginInfo.Name, PluginInfo.GUID);
            isEnabled = new BoolField(config.rootPanel, "Enable Ultracoins", "field.isenabled", true, true);
            spread = new FloatField(config.rootPanel, "Coin Spread", "field.spread", 5f, true);
            tossDelay = new FloatField(config.rootPanel, "Toss Delay", "field.tossdelay", 0f, true);
            altSpam = new BoolField(config.rootPanel, "Alt Instant Reload", "field.altspam", false, true);

            isEnabled.onValueChange += (e) =>
            {
                spread.hidden = !e.value;
                tossDelay.hidden = !e.value;
                altSpam.hidden = !e.value;
            };

            tossDelay.onValueChange += (e) =>
            {
                Revolver_Patch.coinReady = true;
            };

            tossDelay.TriggerValueChangeEvent();
            isEnabled.TriggerValueChangeEvent();

            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string iconFilePath = Path.Combine(Path.Combine(workingDirectory, "Data"), "icon.png");
            ConfigManager.config.SetIconWithURL("file://" + iconFilePath);
        }

    }*/
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class UltraCoins : BaseUnityPlugin
    {
        public static float tossDelay = 0f;
        public static float spread = 5f;
        public static bool altSpam = true;
        public static bool isEnabled = true;
        public void Start()
        {
            Debug.Log("Ding!!!!!!!!!!!!!!!!!!");//
            //ConfigManager.Setup();
            Harmony harmony = new Harmony(PluginInfo.GUID);
            harmony.PatchAll();
        }
        [HarmonyPatch]
        public static class Revolver_Patch
        {
            public static float coinWait = 0f;
            public static bool coinReady = true;
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Revolver), "ThrowCoin")]
            public static bool patch_ThrowCoin(Revolver __instance)
            {
                if (isEnabled)
                {
                    if (!coinReady) return false;
                    float spread = UltraCoins.spread;
                    if (__instance.punch == null || !__instance.punch.gameObject.activeInHierarchy)
                    {
                        __instance.punch = MonoSingleton<FistControl>.Instance.currentPunch;
                    }
                    if (__instance.punch && !MonoSingleton<InputManager>.Instance.InputSource.Punch.IsPressed)
                    {
                        __instance.punch.CoinFlip();
                    }

                    GameObject gameObject = UObj.Instantiate<GameObject>(__instance.coin, __instance.camObj.transform.position + __instance.camObj.transform.up * -0.5f, __instance.camObj.transform.rotation);
                    gameObject.GetComponent<Coin>().sourceWeapon = __instance.gc.currentWeapon;
                    MonoSingleton<RumbleManager>.Instance.SetVibration(RumbleProperties.CoinToss);
                    //really don't know why this is here
                    Vector3 zero = Vector3.zero;
                    gameObject.GetComponent<Rigidbody>().AddForce(
                        __instance.camObj.transform.forward * (20f + URand.Range(-spread, spread)) +
                        Vector3.up * 15f +
                        __instance.camObj.transform.up * (URand.Range(-spread, spread)) +
                        __instance.camObj.transform.right * (URand.Range(-spread, spread)) +
                        (MonoSingleton<NewMovement>.Instance.ridingRocket ? MonoSingleton<NewMovement>.Instance.ridingRocket.rb.velocity : MonoSingleton<NewMovement>.Instance.rb.velocity) +
                        zero, ForceMode.VelocityChange);
                    __instance.pierceCharge = 0f;
                    __instance.pierceReady = false;
                    return false;
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Revolver), "Update")]
            public static void patch_CoinGatling(Revolver __instance)
            {
                if (isEnabled)
                {
                    if (coinWait > tossDelay)
                    {
                        coinReady = true;
                    }
                    else
                    {
                        coinWait += Time.deltaTime;
                    }
                    if (MonoSingleton<InputManager>.Instance.InputSource.Fire2.IsPressed && !GameStateManager.Instance.PlayerInputLocked && __instance.gunVariation == 1)
                    {
                        if (coinReady)
                        {
                            coinWait -= tossDelay;
                            __instance.ThrowCoin();
                            coinReady = false;
                        }
                        __instance.coinCharge = 399f;
                        __instance.wc.rev1charge = 399f;
                    }
                }
            }

            [HarmonyPatch(typeof(LeaderboardController), nameof(LeaderboardController.SubmitCyberGrindScore))]
            [HarmonyPrefix]
            public static bool no(LeaderboardController __instance)
            {
                return false;
            }

            [HarmonyPatch(typeof(LeaderboardController), nameof(LeaderboardController.SubmitLevelScore))]
            [HarmonyPrefix]
            public static bool nope(LeaderboardController __instance)
            {
                return false;
            }

            [HarmonyPatch(typeof(LeaderboardController), nameof(LeaderboardController.SubmitFishSize))]
            [HarmonyPrefix]
            public static bool notevenfish(LeaderboardController __instance)
            {
                return false;
            }

            [HarmonyPatch(typeof(Revolver), nameof(Revolver.InstaClick))]
            [HarmonyPostfix]
            public static void instaclicknochill(Revolver __instance)
            {
                __instance.gunReady = true;
                if (altSpam && isEnabled) __instance.shootReady = true;
                //(ConfigManager.altSpam.value && ConfigManager.isEnabled.value) __instance.shootReady = true;
            }
        }
    }
}
