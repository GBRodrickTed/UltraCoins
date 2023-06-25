using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UObj = UnityEngine.Object;
using URand = UnityEngine.Random;

namespace Ultracoins
{
    [BepInPlugin("ironfarm.uk.uc", "UltraCoins!", "1.0.0")]
    public class UltraCoins : BaseUnityPlugin
    {
        public void Start()
        {
            Harmony harmony = new Harmony("ironfarm.uk.uc");
            harmony.PatchAll();
        }
        [HarmonyPatch]
        public class Revolver_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Revolver), "ThrowCoin")]
            public static bool patch_ThrowCoin(ref Revolver __instance)
            {
                float spread = 5f;
                if (__instance.punch == null || !__instance.punch.gameObject.activeInHierarchy)
                {
                    __instance.punch = MonoSingleton<FistControl>.Instance.currentPunch;
                }
                if (__instance.punch)
                {
                    __instance.punch.CoinFlip();
                }
                GameObject gameObject = UObj.Instantiate<GameObject>(__instance.coin, __instance.camObj.transform.position + __instance.camObj.transform.up * -0.5f, __instance.camObj.transform.rotation);
                gameObject.GetComponent<Coin>().sourceWeapon = __instance.gc.currentWeapon;
                MonoSingleton<RumbleManager>.Instance.SetVibration("rumble.coin_toss");
                //really don't know why this is here
                Vector3 zero = Vector3.zero;
                gameObject.GetComponent<Rigidbody>().AddForce(
                    __instance.camObj.transform.forward * (20f + URand.Range(-spread, spread)) +
                    Vector3.up * 15f +
                    __instance.camObj.transform.up * (URand.Range(-spread, spread)) +
                    __instance.camObj.transform.right * (URand.Range(-spread, spread)) +
                    (MonoSingleton<NewMovement>.Instance.ridingRocket ? MonoSingleton<NewMovement>.Instance.ridingRocket.rb.velocity : MonoSingleton<NewMovement>.Instance.rb.velocity) +
                    zero, ForceMode.VelocityChange
                    );
                __instance.pierceCharge = 0f;
                __instance.pierceReady = false;
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Revolver), "Update")]
            public static void patch_CoinGatling(Revolver __instance)
            {
                if (MonoSingleton<InputManager>.Instance.InputSource.Fire2.IsPressed && __instance.gunVariation == 1)
                {
                    __instance.ThrowCoin();
                }
            }
        }
    }
}
