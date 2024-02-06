using HarmonyLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
#if UNHOLLOWER
using IL2CPPUtils = UnhollowerBaseLib.UnhollowerUtils;
#endif
#if INTEROP
using IL2CPPUtils = Il2CppInterop.Common.Il2CppInteropUtils;
#endif

namespace UnityExplorer.UI.Widgets
{
    public class TimeScaleWidget
    {
        public TimeScaleWidget(GameObject parent)
        {
            Instance = this;

            ConstructUI(parent);

            InitPatch();
        }

        static TimeScaleWidget Instance;

        ButtonRef lockBtn;
        bool locked;
        InputFieldRef timeInput;
        float desiredTime = 1;
        bool settingTimeScale;
        bool pause;
        Slider slider;

        public void Update()
        {
            // Fallback in case Time.timeScale patch failed for whatever reason
            if (locked)
                SetTimeScale(desiredTime);

            if (!timeInput.Component.isFocused)
                timeInput.Text = Time.timeScale.ToString("F2");
        }

        public void PauseToggle(){
            pause = !pause;
            if (!pause) {
                SetTimeScale(1f); //or previous timescale
            }
            locked = pause;
            desiredTime = pause ? 0f : 1f;
            slider.value = desiredTime;

            UpdatePauseButton();
        }

        public bool IsPaused(){
            return pause;
        }

        public void SetTimeScale(float time)
        {
            settingTimeScale = true;
            Time.timeScale = time;
            settingTimeScale = false;
        }

        // UI event listeners

        void OnTimeInputEndEdit(string val)
        {
            if (float.TryParse(val, out float f))
            {
                desiredTime = f;
                slider.value = f;
            }
        }

        void OnPauseButtonClicked()
        {
            if (pause){
                pause = false;
                desiredTime = 1f;
                slider.value = desiredTime;
                SetTimeScale(desiredTime);
            }
            else {
                OnTimeInputEndEdit(timeInput.Text);
                // We assume the normal timescale is 1.0, but we will stop setting it on Update() so the game can handle it.
                SetTimeScale(1.0f);
            }
            
            locked = !locked;

            UpdatePauseButton();
        }

        void UpdatePauseButton()
        {
            Color color = locked ? new Color(0.3f, 0.3f, 0.2f) : new Color(0.2f, 0.2f, 0.2f);
            RuntimeHelper.SetColorBlock(lockBtn.Component, color, color * 1.2f, color * 0.7f);
            lockBtn.ButtonText.text = locked ? "Unlock" : "Lock";
        }

        // UI Construction

        void ConstructUI(GameObject parent)
        {
            Text timeLabel = UIFactory.CreateLabel(parent, "TimeLabel", "Time:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(timeLabel.gameObject, minHeight: 25, minWidth: 35);

            timeInput = UIFactory.CreateInputField(parent, "TimeInput", "timeScale");
            UIFactory.SetLayoutElement(timeInput.Component.gameObject, minHeight: 25, minWidth: 40);
            timeInput.Component.GetOnEndEdit().AddListener(OnTimeInputEndEdit);

            timeInput.Text = string.Empty;
            timeInput.Text = Time.timeScale.ToString();

            GameObject sliderObj = UIFactory.CreateSlider(parent, "Slider_time_scale", out slider);
            UIFactory.SetLayoutElement(sliderObj, minHeight: 25, minWidth: 75, flexibleWidth: 0);
            slider.onValueChanged.AddListener((newTimeScale) => desiredTime = newTimeScale);
            slider.m_FillImage.color = Color.clear;
            slider.value = 1;
            slider.minValue = 0f;
            slider.maxValue = 2f;

            lockBtn = UIFactory.CreateButton(parent, "PauseButton", "Lock", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(lockBtn.Component.gameObject, minHeight: 25, minWidth: 50);
            lockBtn.OnClick += OnPauseButtonClicked;
        }

        // Only allow Time.timeScale to be set if the user hasn't "locked" it or if we are setting the value internally.

        static void InitPatch()
        {

            try
            {
                MethodInfo target = typeof(Time).GetProperty("timeScale").GetSetMethod();
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(target) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(target,
                    prefix: new(AccessTools.Method(typeof(TimeScaleWidget), nameof(Prefix_Time_set_timeScale))));
            }
            catch { }
        }

        static bool Prefix_Time_set_timeScale()
        {
            return !Instance.locked || Instance.settingTimeScale;
        }
    }
}
