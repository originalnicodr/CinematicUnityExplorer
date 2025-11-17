using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; // 需要反射
using HarmonyLib; // 為了 AccessTools.TypeByName
using UniverseLib.Utility; // 為了 IsNullOrDestroyed

namespace UnityExplorerPlus.ParseUtility
{
    public static class ParseManager
    {
        // 使用 Type.FullName 作為鍵，因為我們在編譯時無法引用 Type
        private static readonly Dictionary<string, Func<object, string>> customToStringsByFullName = new();
        private static readonly Dictionary<Type, Func<object, string>> customToStrings = new(); // 為了運行時查找

        // 緩存已解析的 Type 對象，避免重複 GetType
        private static readonly Dictionary<string, Type> typeCache = new();

        private static Type GetCachedType(string typeFullName)
        {
            if (typeCache.TryGetValue(typeFullName, out Type cachedType))
            {
                return cachedType;
            }

            // 這會嘗試在所有已加載的程序集中查找類型
            Type foundType = AccessTools.TypeByName(typeFullName); // HarmonyLib 提供的工具方法非常有用
            if (foundType != null)
            {
                typeCache[typeFullName] = foundType;
            }
            return foundType;
        }

        public static string TryGetCustomToString(object value)
        {
            if (value.IsNullOrDestroyed())
            {
                return null;
            }

            Type type = value.GetType();
            if (customToStrings.TryGetValue(type, out var p))
            {
                return p(value);
            }

            // 如果沒有直接註冊，檢查是否有為基類註冊的ToString
            p = customToStrings.FirstOrDefault(x => x.Key.IsInstanceOfType(value)).Value;
            if (p != null)
            {
                return p(value);
            }

            return null; // 沒有找到自定義的ToString
        }

        public static bool IsTypeRegisteredForToString(Type type)
        {
            return customToStrings.ContainsKey(type) || customToStrings.Any(x => x.Key.IsInstanceOfType(type));
        }

        /// <summary>
        /// 使用類型完整名稱註冊 ToString 函數。
        /// </summary>
        /// <param name="typeFullName">類型完整名稱，如 "HutongGames.PlayMaker.FsmState"</param>
        /// <param name="toString">ToString 函數。</param>
        public static void RegisterToString(string typeFullName, Func<object, string> toString)
        {
            if (customToStringsByFullName.ContainsKey(typeFullName))
            {
                // 可以選擇拋出錯誤或覆蓋
                // Console.WriteLine($"Warning: Type {typeFullName} already registered for ToString, overriding.");
            }
            customToStringsByFullName[typeFullName] = toString;

            // 如果類型已經加載，則立即添加到強類型字典
            Type runtimeType = GetCachedType(typeFullName);
            if (runtimeType != null)
            {
                customToStrings[runtimeType] = toString;
            }
        }

        /// <summary>
        /// 泛型版本，但通常用於已知類型的註冊，對於 PlayMaker 類型您會直接使用字符串版本。
        /// </summary>
        public static void RegisterToString<T>(Func<T, string> toString)
        {
            // 修正這裡：應該傳遞 typeof(T).FullName
            RegisterToString(typeof(T).FullName, val => toString((T)val));
        }


        public static void Init()
        {
            // ----------------------------------------------------
            // 註冊 tk2d 相關類型 (使用反射，因為沒有直接引用)
            // ----------------------------------------------------
            RegisterToString("tk2dSpriteAnimationClip",
                clip => {
                    // 獲取名為 "name" 的字段，並進行空值檢查
                    FieldInfo nameField = clip?.GetType().GetField("name");
                    // FieldInfo.GetValue 返回 object，對於 string 類型可以直接 as string
                    string name = nameField?.GetValue(clip) as string ?? "UnnamedClip";
                    return $"<color=grey>tk2dSpriteAnimationClip: </color><color=green>{name}</color>";
                });

            RegisterToString("tk2dSpriteDefinition",
                def => {
                    // 獲取名為 "name" 的字段，並進行空值檢查
                    FieldInfo nameField = def?.GetType().GetField("name");
                    // FieldInfo.GetValue 返回 object，對於 string 類型可以直接 as string
                    string name = nameField?.GetValue(def) as string ?? "UnnamedDefinition";
                    return $"<color=grey>tk2dSpriteDefinition: </color><color=green>{name}</color>";
                });

            RegisterToString("tk2dSpriteAnimationFrame",
                frame => {
                    // 獲取名為 "spriteId" 的字段，並進行空值檢查
                    FieldInfo spriteIdField = frame?.GetType().GetField("spriteId");
                    // FieldInfo.GetValue 返回 object，需要先檢查是否為 null，然後調用 ToString() 轉換為字串
                    string spriteId = spriteIdField?.GetValue(frame)?.ToString() ?? "UnnamedSpriteId";
                    return $"<color=grey>tk2dSpriteAnimationFrame: </color><color=green>{spriteId}</color>";
                });

            // ----------------------------------------------------
            // 註冊 PlayMaker 相關類型 - 完全通過反射訪問屬性
            // ----------------------------------------------------

            // FsmState
            RegisterToString("HutongGames.PlayMaker.FsmState", state =>
            {
                PropertyInfo nameProp = state?.GetType().GetProperty("Name");
                string name = nameProp?.GetValue(state, null) as string ?? "UnnamedState";
                return $"<color=grey>Fsm State: </color><color=green>{name}</color>";
            });

            // FsmTransition
            RegisterToString("HutongGames.PlayMaker.FsmTransition", tran =>
            {
                Type tranType = tran?.GetType();
                PropertyInfo fsmEventProp = tranType?.GetProperty("FsmEvent");
                object fsmEvent = fsmEventProp?.GetValue(tran, null);

                string eventName = null;
                if (fsmEvent != null)
                {
                    PropertyInfo fsmEventNameProp = fsmEvent.GetType().GetProperty("Name");
                    eventName = fsmEventNameProp?.GetValue(fsmEvent, null) as string;
                }

                PropertyInfo eventNameFallbackProp = tranType?.GetProperty("EventName");
                string eventNameFallback = eventNameFallbackProp?.GetValue(tran, null) as string;

                PropertyInfo toFsmStateProp = tranType?.GetProperty("ToFsmState");
                object toFsmState = toFsmStateProp?.GetValue(tran, null);

                string toStateName = null;
                if (toFsmState != null)
                {
                    PropertyInfo toFsmStateNameProp = toFsmState.GetType().GetProperty("Name");
                    toStateName = toFsmStateNameProp?.GetValue(toFsmState, null) as string;
                }

                PropertyInfo toStateFallbackProp = tranType?.GetProperty("ToState");
                string toStateFallback = toStateFallbackProp?.GetValue(tran, null) as string;

                return $"<color=grey>Fsm Transition: </color><color=green>{(eventName ?? eventNameFallback)}</color><color=grey> -> </color><color=green>{(toStateName ?? toStateFallback)}</color>";
            });

            // FsmEvent
            RegisterToString("HutongGames.PlayMaker.FsmEvent", ev =>
            {
                PropertyInfo nameProp = ev?.GetType().GetProperty("Name");
                string name = nameProp?.GetValue(ev, null) as string ?? "UnnamedEvent";
                return $"<color=grey>Fsm Event: </color><color=green>{name}</color>";
            });

            // NamedVariable
            RegisterToString("HutongGames.PlayMaker.NamedVariable", v =>
            {
                Type varType = v?.GetType();
                PropertyInfo variableTypeProp = varType?.GetProperty("VariableType");
                string variableType = variableTypeProp?.GetValue(v, null)?.ToString() ?? "Unknown";

                PropertyInfo nameProp = varType?.GetProperty("Name");
                string name = nameProp?.GetValue(v, null) as string ?? "Unnamed";

                // 修正這裡：直接從 NamedVariable 實例獲取 'Value' 屬性，而不是嘗試 PropertyType
                PropertyInfo valueProp = varType?.GetProperty("Value"); // 獲取 NamedVariable 的 Value 屬性
                object actualValue = valueProp?.GetValue(v, null); // 獲取實際的值

                return $"<color=grey>Fsm Variable(</color><color=green>{variableType}</color><color=grey>): </color><color=green>{name}</color> | " + UniverseLib.Utility.ToStringUtility.ToStringWithType(actualValue, typeof(object));
            });
        }
    }
}