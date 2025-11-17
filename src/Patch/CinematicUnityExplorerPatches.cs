using System;
using HarmonyLib;
using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorerPlus.ParseUtility; // 引用您的 ParseManager 命名空間

namespace CinematicUnityExplorer.Patches
{
    [HarmonyPatch] // 這個標籤是讓 Harmony 掃描這個類中的所有 [HarmonyPatch] 方法
    public static class CinematicUnityExplorerPatches
    {
        // --------------------------------------------------------------------------------------
        // 補丁 1: 劫持 ToStringUtility.ToStringWithType
        // 這是最核心的補丁，讓我們的 ParseManager 的自定義 ToString 能夠生效。
        // --------------------------------------------------------------------------------------
        [HarmonyPatch(typeof(ToStringUtility), nameof(ToStringUtility.ToStringWithType))]
        [HarmonyPrefix]
        public static bool ToStringWithType_Prefix(object value, Type fallbackType, bool includeNamespace, ref string __result)
        {
            // 嘗試從 ParseManager 獲取自定義的 ToString 結果
            string customResult = ParseManager.TryGetCustomToString(value);

            if (customResult != null)
            {
                __result = customResult; // 如果有自定義結果，則設置給 __result
                return false;           // 返回 false，阻止原始 ToStringUtility.ToStringWithType 方法執行
            }

            return true; // 返回 true，讓原始方法繼續執行
        }

        // --------------------------------------------------------------------------------------
        // 補丁 2: 確保 CacheObjectBase 顯示富文本
        // 當 UnityExplorer 顯示一個被我們自定義 ToString 處理過的物件時，
        // 需要確保其單元格的文本渲染器支持富文本。
        // --------------------------------------------------------------------------------------
        [HarmonyPatch(typeof(CacheObjectBase), "SetDataToCell")]
        [HarmonyPostfix]
        public static void CacheObjectBase_SetDataToCell_Postfix(CacheObjectBase __instance, CacheObjectCell cell)
        {
            if (__instance.Value == null) return;

            // 如果該類型或其基類已註冊自定義 ToString 處理
            if (ParseManager.IsTypeRegisteredForToString(__instance.Value.GetType()))
            {
                // 我們需要讓 UnityExplorer 知道這個單元格的文本應該被解析為富文本。
                // CacheObjectBase.SetValueState 是內部方法，需要反射來調用。
                // 原始 UnityExplorerPlus 的 ParseManager 使用了 reflect 擴展方法。
                // 如果沒有那個擴展方法，我們需要手動調用反射。

                // 獲取 SetValueState 方法
                MethodInfo setValueStateMethod = AccessTools.Method(typeof(CacheObjectBase), "SetValueState");
                if (setValueStateMethod != null)
                {
                    // 創建一個 CacheObjectBase.ValueState 實例，設置 valueRichText 為 true
                    // CacheObjectBase.ValueState 是一個結構體，可能沒有公共構造函數
                    // 需要通過反射來創建或查找其屬性。
                    // 這是最複雜的部分，因為 ValueState 可能沒有公共 API。
                    // 作為簡化，如果 UnityExplorer 在 ToStringUtility.ToStringWithType 返回富文本後
                    // 自動處理 richText，那麼這部分可以省略。
                    // 如果它不自動處理，你需要更深入地研究 UnityExplorer 的內部。

                    // 這裡先提供一個基於假設的方案，如果它不工作，則需要進一步調試。
                    // 理想情況下，應該直接設置 cell 內部的 Text 組件的 .richText = true;
                    // 但 CacheObjectCell 本身沒有直接暴露 Text 組件。

                    // 嘗試設置一個已註冊類型的 richText 狀態。
                    // 注意：這是對 UnityExplorer 內部實現的推測，可能需要根據實際情況調整。
                    // 這個邏輯和您一開始的 `UnityExplorerPlus.ParseUtility.ParseManager` 中的 `On.UnityExplorer.CacheObject.CacheObjectBase.SetDataToCell` 邏輯是匹配的。
                    // `SetValueState` 方法通常是這樣被調用的：
                    // `self.Reflect().SetValueState(cell.Reflect(), new(valueRichText: true, inputActive: self.CanWrite, applyActive: self.CanWrite, inspectActive: true));`

                    // 您需要確保 `UniverseLib.Reflection.ReflectExtensions` 是可用的，
                    // 或者您手動實現類似的功能。

                    // 為了避免對 ReflectExtensions 的依賴，這裡直接使用 Harmony 的 AccessTools 獲取內部類型和字段。
                    // 獲取 ValueState 類型
                    Type valueStateType = AccessTools.Inner(typeof(CacheObjectBase), "ValueState");
                    if (valueStateType != null)
                    {
                        // 創建 ValueState 實例 (需要所有參數)
                        object valueState = Activator.CreateInstance(
                            valueStateType,
                            true, // valueRichText
                            __instance.CanWrite, // inputActive
                            __instance.CanWrite, // applyActive
                            true // inspectActive
                        );

                        // 調用 SetValueState 方法
                        setValueStateMethod.Invoke(__instance, new object[] { cell, valueState });
                    }
                }
            }
        }
    }
}