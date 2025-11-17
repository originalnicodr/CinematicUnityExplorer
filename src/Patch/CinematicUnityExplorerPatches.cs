using System;
using System.Reflection; // 需要這個命名空間來使用 MethodInfo, PropertyInfo 等
using HarmonyLib;
using UnityEngine;
using UnityExplorer;
using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Widgets;
using UniverseLib.UI.Models;
using UnityExplorerPlus.ParseUtility; // 引用您的 ParseManager 命名空間

namespace CinematicUnityExplorer.Patches
{
    [HarmonyPatch]
    public static class CinematicUnityExplorerPatches
    {
        // --------------------------------------------------------------------------------------
        // 靜態緩存 PlayMakerFSM 的 Type 物件，避免重複查找
        // --------------------------------------------------------------------------------------
        private static Type _playMakerFSMType = null;
        private static Type GetPlayMakerFSMType()
        {
            if (_playMakerFSMType == null)
            {
                // 使用 UnityExplorer 自己的 ReflectionUtility.GetTypeByName 來獲取 PlayMakerFSM 類型
                // 確保這裡的字符串是 PlayMakerFSM 的完整類型名稱，包括命名空間
                _playMakerFSMType = ReflectionUtility.GetTypeByName("PlayMakerFSM");

                if (_playMakerFSMType == null)
                {
                    //ExplorerCore.LogWarning("Could not find PlayMakerFSM type using ReflectionUtility.GetTypeByName. " +
                    //                        "Make sure 'HutongGames.PlayMaker.PlayMakerFSM' is the correct full type name.");
                }
            }
            return _playMakerFSMType;
        }


        // --------------------------------------------------------------------------------------
        // 補丁 1: 劫持 ToStringUtility.ToStringWithType
        // --------------------------------------------------------------------------------------
        [HarmonyPatch(typeof(ToStringUtility), nameof(ToStringUtility.ToStringWithType))]
        [HarmonyPrefix]
        public static bool ToStringWithType_Prefix(object value, Type fallbackType, bool includeNamespace, ref string __result)
        {
            string customResult = ParseManager.TryGetCustomToString(value);

            if (customResult != null)
            {
                __result = customResult;
                return false;
            }

            return true;
        }

        // --------------------------------------------------------------------------------------
        // 補丁 2: 確保 CacheObjectBase 顯示富文本
        // --------------------------------------------------------------------------------------
        [HarmonyPatch(typeof(CacheObjectBase), "SetDataToCell")]
        [HarmonyPostfix]
        public static void CacheObjectBase_SetDataToCell_Postfix(CacheObjectBase __instance, CacheObjectCell cell)
        {
            if (__instance.Value == null) return;

            if (ParseManager.IsTypeRegisteredForToString(__instance.Value.GetType()))
            {
                MethodInfo setValueStateMethod = AccessTools.Method(typeof(CacheObjectBase), "SetValueState");
                if (setValueStateMethod != null)
                {
                    Type valueStateType = AccessTools.Inner(typeof(CacheObjectBase), "ValueState");
                    if (valueStateType != null)
                    {
                        // 確保使用正確的構造函數或設置屬性
                        // ValueState 是一個 struct，通常有所有參數的構造函數
                        // 如果沒有，則需要創建默認實例後，再用反射設置字段。
                        // 根據您原始的 UnityExplorerPlus 邏輯 `new(valueRichText: true, inputActive: self.CanWrite, applyActive: self.CanWrite, inspectActive: true)`
                        // 這個構造函數順序是 `bool valueRichText, bool inputActive, bool applyActive, bool inspectActive`
                        object valueState = Activator.CreateInstance(
                            valueStateType,
                            true,                     // valueRichText: true
                            __instance.CanWrite,      // inputActive
                            __instance.CanWrite,      // applyActive
                            true                      // inspectActive
                        );

                        // 調用 SetValueState 方法
                        setValueStateMethod.Invoke(__instance, new object[] { cell, valueState });
                    }
                    else
                    {
                        //ExplorerCore.LogWarning("Could not find CacheObjectBase.ValueState type via reflection.");
                    }
                }
                else
                {
                    //ExplorerCore.LogWarning("Could not find CacheObjectBase.SetValueState method via reflection.");
                }
            }
        }

        // --------------------------------------------------------------------------------------
        // 補丁 3: 在 ComponentList 中顯示 PlayMakerFSM 的 FsmName
        // --------------------------------------------------------------------------------------
        [HarmonyPatch(typeof(ComponentList), nameof(ComponentList.SetComponentCell))]
        [HarmonyPostfix]
        public static void ComponentList_SetComponentCell_Postfix(ComponentList __instance, ComponentCell cell, int index)
        {
            // 調試日誌，確認 Harmony Patch 被觸發
            //ExplorerCore.LogWarning($"ComponentList_SetComponentCell_Postfix: __instance={__instance}, cell={cell}, index={index}");

            // 確保 __instance.Parent 是 GameObjectInspector 的實例
            if (__instance.Parent is GameObjectInspector gameObjectInspector)
            {
                // 獲取 GameObjectInspector 正在檢查的實際 Unity GameObject
                GameObject targetGameObject = gameObjectInspector.Target;

                if (targetGameObject != null)
                {
                    // 獲取該 GameObject 上的所有組件
                    Component[] allUnityComponents = targetGameObject.GetComponents<Component>();

                    // 檢查索引是否有效
                    if (index >= 0 && index < allUnityComponents.Length)
                    {
                        Component actualUnityComponent = allUnityComponents[index];

                        // 調試日誌，顯示獲取到的組件類型
                        //ExplorerCore.LogWarning($"Actual Unity Component at index {index}: {actualUnityComponent.GetType().Name}");

                        // 獲取 PlayMakerFSM 的 Type 物件
                        Type playMakerFSMType = GetPlayMakerFSMType();

                        // 檢查獲取到的組件是否是 PlayMakerFSM 類型或其子類
                        if (playMakerFSMType != null && playMakerFSMType.IsInstanceOfType(actualUnityComponent))
                        {
                            // 現在 actualUnityComponent 確實是 PlayMakerFSM 或其子類型的實例
                            // 我們需要通過反射來獲取 FsmName 屬性
                            try
                            {
                                // 獲取 FsmName 屬性
                                PropertyInfo fsmNameProperty = playMakerFSMType.GetProperty(
                                    "FsmName",
                                    BindingFlags.Public | BindingFlags.Instance
                                );

                                if (fsmNameProperty != null)
                                {
                                    // 從 actualUnityComponent 實例中獲取 FsmName 的值
                                    object fsmNameObject = fsmNameProperty.GetValue(actualUnityComponent, null);
                                    string fsmName = fsmNameObject as string;

                                    if (!string.IsNullOrEmpty(fsmName))
                                    {
                                        cell.Button.ButtonText.text = cell.Button.ButtonText.text
                                            + "<color=grey>(</color><color=#7FFF00>"
                                            + fsmName
                                            + "</color><color=grey>)</color>";
                                    }
                                }
                                else
                                {
                                    //ExplorerCore.LogWarning("PlayMakerFSM.FsmName property not found via reflection.");
                                }
                            }
                            catch (Exception ex)
                            {
                                //ExplorerCore.LogWarning($"Error getting PlayMakerFSM.FsmName via reflection: {ex}");
                            }
                        }
                    }
                    else
                    {
                        //ExplorerCore.LogWarning($"Index {index} out of bounds for {targetGameObject.name}'s components.");
                    }
                }
                else
                {
                    //ExplorerCore.LogWarning("GameObjectInspector.Target is null.");
                }
            }
            else
            {
                //ExplorerCore.LogWarning("Parent of ComponentList is not a GameObjectInspector.");
            }
        }
    }
}