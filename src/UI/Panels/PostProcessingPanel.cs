using UnityExplorer.CacheObject;
using UnityExplorer.ObjectExplorer;
using UnityExplorer.Inspectors;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif

namespace UnityExplorer.UI.Panels
{
    internal class PostProcessingPanel : UEPanel
    {
        public PostProcessingPanel(UIBase owner) : base(owner)
        {
        }

        public override string Name => "Post-processing";
        public override UIManager.Panels PanelType => UIManager.Panels.PostProcessingPanel;
        public override int MinWidth => 500;
        public override int MinHeight => 500;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        public static Dictionary<string,List<PPEffect>> postProcessingEffects = null;
        static ButtonRef updateEffects;
        List<GameObject> UIElements = new List<GameObject>();

        public class PPEffect
        {
            public PPEffect(object obj)
            {
                Object = obj;

                ReflectionInspector inspector = Pool<ReflectionInspector>.Borrow();
                inspector.Target = obj.TryCast();
                //ReflectionInspector inspector = InspectorManager.CreateInspectorWithoutWindow<ReflectionInspector>(obj.TryCast(), false, null);//Pool<ReflectionInspector>.Borrow();//
                
                Type objType = obj is Type type ? type : obj.GetActualType();
                List<CacheMember> members = CacheMemberFactory.GetCacheMembers(objType, inspector);
                
                foreach (CacheMember member in members){
                    //ExplorerCore.LogWarning(entry.NameForFiltering);

                    if (member.NameForFiltering.EndsWith(".active")){
                        member.Evaluate();
                        Active = member;
                    }

                    if (member.NameForFiltering.EndsWith(".name")){
                        member.Evaluate();
                        Name = member;
                    }
                }
            }

            public object Object { get; }
            public CacheMember Active { get; }
            public CacheMember Name { get; }

            public override string ToString() => $"{Object}";
        }

        public void UpdatePPElements(){
            if(postProcessingEffects != null){
                postProcessingEffects.Clear();
            }

            postProcessingEffects = new Dictionary<string,List<PPEffect>>();

            string[] universalClassEffects = {
                "Vignette",
                "Bloom",
                "ColorAdjustments",
                "DepthOfField",
                "ChromaticAberration",
                "Tonemapping",
                "FilmGrain",
                "WhiteBalance",
                "ShadowsMidtonesHighlights",
                "MotionBlur",
                "LiftGammaGain",
                "LensDistortion",
                "ScreenSpaceAmbientOcclusion",
                "ChannelMixer",
                "ColorCurves",
                "SplitToning"
            };

            foreach (string effect in universalClassEffects){
                AddEffect("UnityEngine.Rendering.Universal", effect);
            }

            string[] postProcessingClassEffects = {
                "Vignette",
                "Bloom",
                "Grain",
                //"Fog",
                "DepthOfField",
                //"Tonemapper",
                "LensDistortion",
                "ChromaticAberration",
                "AmbientOcclusion",
                "AutoExposure",
                "ScreenSpaceReflections"
                };

            foreach (string effect in postProcessingClassEffects){
                AddEffect("UnityEngine.Rendering.PostProcessing", effect);
            }

            BuildEffectTogglers();
        }

        private void AddEffect(string baseClass, string effect){
            List<object> currentResults = SearchProvider.UnityObjectSearch("", $"{baseClass}.{effect}", ChildFilter.Any, SceneFilter.ActivelyLoaded);

            //ExplorerCore.LogWarning($"Found {currentResults.Count()} objects for {baseClass}.{effect}");

            foreach (object obj in currentResults){
                PPEffect entry = new PPEffect(obj);
                
                // Ignore non-active objects and objects without a name, since those tend to be irrelevant
                if ((bool) entry.Active.Value && entry.Name.Value.ToString() != ""){
                    if (!postProcessingEffects.ContainsKey(effect)){
                        postProcessingEffects.Add(effect, new List<PPEffect>());
                    }

                    postProcessingEffects[effect].Add(entry);
                }
            }
        }

        private void SetEffect(bool value, List<PPEffect> effects){
            foreach (PPEffect obj in effects){
                obj.Active.TrySetUserValue(value);
            }
        }

        private void BuildEffectTogglers(){
            foreach (var comp in UIElements){
                UnityEngine.Object.Destroy(comp);
                //UIElements.Remove(comp);
            }

            foreach (string effect in postProcessingEffects.Keys)
            {
                GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, $"{effect}_Group", false, false, true, false, 4,
                    default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
                UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);
                UIElements.Add(horiGroup);

                //Active toggle
                Toggle groupEffectToggle = new Toggle();
                GameObject toggleEffect = UIFactory.CreateToggle(horiGroup, $"Toggle {effect}", out groupEffectToggle, out Text effectToggleText);
                UIFactory.SetLayoutElement(toggleEffect, minHeight: 25);
                groupEffectToggle.onValueChanged.AddListener(value => SetEffect(value, postProcessingEffects[effect]));
                groupEffectToggle.isOn = true; // we picked up only the active effects
                effectToggleText.text = effect;

                GameObject buttonsGroup = UIFactory.CreateHorizontalGroup(horiGroup, $"{effect}_Group", false, false, true, false, 4,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleRight);
                UIFactory.SetLayoutElement(buttonsGroup, minHeight: 25, flexibleWidth: 9999);
                UIElements.Add(buttonsGroup);

                for (int i = 0; i < postProcessingEffects[effect].Count; i++){
                    PPEffect obj = postProcessingEffects[effect][i];

                    ButtonRef openEffect = UIFactory.CreateButton(buttonsGroup, $"Inspect {obj.Object}", $"Obj{i}");
                    UIFactory.SetLayoutElement(openEffect.GameObject, minHeight: 25, minWidth: 40);
                    openEffect.OnClick += () => InspectorManager.InspectWithFilters(obj.Object, effect,
                        UnityExplorer.Inspectors.MemberFilter.Property |
                        UnityExplorer.Inspectors.MemberFilter.Field
                    );
                }
            }
        }

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        protected override void ConstructPanelContent()
        {
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "MainOptions", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);
            //UIElements.Add(horiGroup);

            updateEffects = UIFactory.CreateButton(horiGroup, "RefreshEffects", "Refresh Effects");
            UIFactory.SetLayoutElement(updateEffects.GameObject, minWidth: 150, minHeight: 25);
            updateEffects.OnClick += UpdatePPElements;

            Text openComponentLabel = UIFactory.CreateLabel(horiGroup, "OpenComponentLabel", "Open object in inspector  ", TextAnchor.MiddleRight, Color.white, false, 15);
            UIFactory.SetLayoutElement(openComponentLabel.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 9999);
        }
    }
}
