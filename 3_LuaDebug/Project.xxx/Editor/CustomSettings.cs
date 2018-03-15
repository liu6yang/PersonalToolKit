using UnityEngine;
using System;
using System.Collections.Generic;
using LuaInterface;
using JJYGame;

using BindType = ToLuaMenu.BindType;
using UnityEngine.UI;
using System.Reflection;

public static class CustomSettings
{
    public static string FrameworkPath = AppConst.FrameworkRoot;
    public static string saveDir = FrameworkPath + "/ToLua/Source/Generate/";
    public static string luaDir = FrameworkPath + "/Lua/";
    public static string toluaBaseType = FrameworkPath + "/ToLua/BaseType/";
    public static string toluaLuaDir = FrameworkPath + "/ToLua/Lua";

    //导出时强制做为静态类的类型(注意customTypeList 还要添加这个类型才能导出)
    //unity 有些类作为sealed class, 其实完全等价于静态类
    public static List<Type> staticClassTypes = new List<Type>
    {
        typeof(UnityEngine.Application),
        typeof(UnityEngine.Time),
        typeof(UnityEngine.Screen),
        typeof(UnityEngine.SleepTimeout),
        typeof(UnityEngine.Input),
        typeof(UnityEngine.Resources),
        typeof(UnityEngine.Physics),
        typeof(UnityEngine.RenderSettings),
        typeof(UnityEngine.QualitySettings),
        typeof(UnityEngine.GL),
    };

    //附加导出委托类型(在导出委托时, customTypeList 中牵扯的委托类型都会导出， 无需写在这里)
    public static DelegateType[] customDelegateList =
    {
        _DT(typeof(Action)),
        _DT(typeof(UnityEngine.Events.UnityAction)),
        _DT(typeof(DG.Tweening.TweenCallback)),
    };

    //在这里添加你要导出注册到lua的类型列表
    public static BindType[] customTypeList =
    {                
        //------------------------为例子导出--------------------------------
        //_GT(typeof(TestEventListener)),
        //_GT(typeof(TestAccount)),
        //_GT(typeof(Dictionary<int, TestAccount>)).SetLibName("AccountMap"),
        //_GT(typeof(KeyValuePair<int, TestAccount>)),    
        //_GT(typeof(TestExport)),
        //_GT(typeof(TestExport.Space)),
        //-------------------------------------------------------------------        
        _GT(typeof(Debugger)).SetNameSpace(null),

//#if USING_DOTWEENING
        _GT(typeof(DG.Tweening.DOTween)),
        _GT(typeof(DG.Tweening.Tween)).SetBaseType(typeof(System.Object)).AddExtendType(typeof(DG.Tweening.TweenExtensions)),
        _GT(typeof(DG.Tweening.Sequence)).AddExtendType(typeof(DG.Tweening.TweenSettingsExtensions)),
        _GT(typeof(DG.Tweening.Tweener)).AddExtendType(typeof(DG.Tweening.TweenSettingsExtensions)),
        _GT(typeof(DG.Tweening.LoopType)),
        _GT(typeof(DG.Tweening.Ease)),
        _GT(typeof(DG.Tweening.PathMode)),
        _GT(typeof(DG.Tweening.PathType)),
        _GT(typeof(DG.Tweening.RotateMode)),
        _GT(typeof(Component)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Transform)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Light)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Material)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Rigidbody)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Camera)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(AudioSource)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions>)),
        _GT(typeof(LineRenderer)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(TrailRenderer)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),    
//#else
//        _GT(typeof(Component)),
//        _GT(typeof(Transform)),
//        _GT(typeof(Material)),
//        _GT(typeof(Light)),
//        _GT(typeof(Rigidbody)),
//        _GT(typeof(Camera)),
//        _GT(typeof(AudioSource)),
//        //_GT(typeof(LineRenderer))
//        //_GT(typeof(TrailRenderer))
//#endif   
                        
        _GT(typeof(Behaviour)),
        _GT(typeof(MonoBehaviour)),
        _GT(typeof(GameObject)),
        _GT(typeof(TrackedReference)),
        _GT(typeof(Physics)),
        _GT(typeof(Collider)),
        _GT(typeof(Time)),
        _GT(typeof(Texture)),
        _GT(typeof(Texture2D)),
        _GT(typeof(Shader)),
        _GT(typeof(Renderer)),
        _GT(typeof(WWW)),
        _GT(typeof(WWWForm)),
        _GT(typeof(Screen)),
        _GT(typeof(CameraClearFlags)),
        _GT(typeof(AudioClip)),
        _GT(typeof(AssetBundle)),
        _GT(typeof(ParticleSystem)),
        _GT(typeof(AsyncOperation)).SetBaseType(typeof(System.Object)),
        _GT(typeof(LightType)),
        _GT(typeof(SleepTimeout)),
        _GT(typeof(Animator)),
        _GT(typeof(AnimatorStateInfo)),
        _GT(typeof(Matrix4x4)),
        _GT(typeof(Input)),
        _GT(typeof(KeyCode)),
        _GT(typeof(SkinnedMeshRenderer)),
        _GT(typeof(Space)),

        _GT(typeof(MeshRenderer)),
        _GT(typeof(ParticleEmitter)),
        _GT(typeof(ParticleRenderer)),
        _GT(typeof(ParticleAnimator)),

        _GT(typeof(BoxCollider)),
        _GT(typeof(MeshCollider)),
        _GT(typeof(SphereCollider)),
        _GT(typeof(CharacterController)),
        _GT(typeof(CapsuleCollider)),

        _GT(typeof(Animation)),
        _GT(typeof(AnimationClip)).SetBaseType(typeof(UnityEngine.Object)),
        _GT(typeof(AnimationState)),
        _GT(typeof(AnimationBlendMode)),
        _GT(typeof(QueueMode)),
        _GT(typeof(PlayMode)),
        _GT(typeof(WrapMode)),

        _GT(typeof(QualitySettings)),
        _GT(typeof(RenderSettings)),
        _GT(typeof(BlendWeights)),
        _GT(typeof(RenderTexture)),
        _GT(typeof(RectTransform.Axis)),
        _GT(typeof(ToggleGroup)),
         _GT(typeof(Rect)),
         _GT(typeof(Slider)),
        _GT(typeof(Toggle)),
        _GT(typeof(Canvas)),
        _GT(typeof(GraphicRaycaster)),
        //for JJYGame
        _GT(typeof(CollisionHelper)),
        _GT(typeof(SequenceFrameAnim)),
        _GT(typeof(SingleSequenceFrameAnim)),
        _GT(typeof(QQChartUtil)),
          _GT(typeof(IOSPaySDK)),
          _GT(typeof(Pay)),
           _GT(typeof(IDCardValidation)),
        _GT(typeof(Application)),
        _GT(typeof(Sprite)),
        _GT(typeof(InputField)),
        _GT(typeof(LayoutElement)),
        _GT(typeof(Image)),
        _GT(typeof(RectTransform)),
        _GT(typeof(Text)),
        _GT(typeof(VerticalLayoutGroup)),
        _GT(typeof(Button)),
        _GT(typeof(ScrollRect)),
        _GT(typeof(Util)),
        _GT(typeof(AppConst)),
        _GT(typeof(LuaHelper)),
        _GT(typeof(ByteBuffer)),
        _GT(typeof(LuaBehaviour)),
         _GT(typeof(PlayerPrefs)),
         _GT(typeof(Rigidbody2D)),
         _GT(typeof(ForceMode2D)),
         _GT(typeof(Vector2)),
        _GT(typeof(GameManager)),
        _GT(typeof(LuaManager)),
        _GT(typeof(PanelManager)),
        _GT(typeof(SoundManager)),
        _GT(typeof(TimerManager)),
        _GT(typeof(ThreadManager)),
        _GT(typeof(NetworkManager)),
        _GT(typeof(ResourceManager)),
        _GT(typeof(SceneLoadManager)),
        _GT(typeof(TPAtlasManager)),
        _GT(typeof(ControlShader)),
        _GT(typeof(ParticleSystemController)),
        _GT(typeof(DepthParticleSystemController)),
        _GT(typeof(CaptureGemCellDestroy)),
        _GT(typeof(CandyCaptureGemCellDestroy)),
        _GT(typeof(UnityEngine.SystemInfo)),
        #if UNITY_IPHONE || UNITY_ANDROID
        _GT(typeof(UnityEngine.Handheld)),
        #endif
         
        // 2017-4-17 ADD
        _GT(typeof(LayoutGroup)),
        _GT(typeof(LayerMask)),
        _GT(typeof(GridLayoutGroup)),
        _GT(typeof(CanvasRenderer)),
        _GT(typeof(Outline)),
        _GT(typeof(ShowFPS)),
        _GT(typeof(ParticleScaler)),
        _GT(typeof(ParticleScalerW)),
        _GT(typeof(ProceduralMaterial)),
        _GT(typeof(Mesh)),
        _GT(typeof(MeshFilter)),
        _GT(typeof(Ray)),
        _GT(typeof(RaycastHit)),

        _GT(typeof(YsdkCallback)),
          #if UNITY_ANDROID
        _GT(typeof(AnySDKInit)),
         #endif
#if UNITY_IPHONE
        _GT(typeof(UnityEngine.iOS.DeviceGeneration)),
        _GT(typeof(UnityEngine.iOS.Device)),
#endif

        //2017-4-21 ADD
        _GT(typeof(ButtonLongPress)),
        _GT(typeof(MonoBehaviourHelper)),
        _GT(typeof(SpriteRenderer)),
        _GT(typeof(Physics2D)),
        _GT(typeof(Ray2D)),
        _GT(typeof(RaycastHit2D)),
        //2017-5-23 ADD
        _GT(typeof(DataGrid)),
        _GT(typeof(ItemRender)),
        //2017-8-4 ADD
        _GT(typeof(NotificationMessageScripts)),
        _GT(typeof(DateTime)),

        // 2017-8-23
        _GT(typeof(UnityEngine.Events.UnityEvent)),
        _GT(typeof(UnityEngine.Events.UnityEvent<float>)),
        _GT(typeof(UnityEngine.Events.UnityEvent<bool>)),
        _GT(typeof(UnityEngine.Random)),

        // 2017-10-16 语音SDK,web
        _GT(typeof(OffLineVoice)),
#if UNITY_IOS || UNITY_ANDROID || UNITY_WP8
        _GT(typeof(UniWebView)),
#endif //UNITY_IOS || UNITY_ANDROID
        
        // 2017-12-22 反射代理类
        _GT(typeof(ReflectionHelper)),

        _GT(typeof(LuaDebugTool)),
        _GT(typeof(LuaValueInfo)),
    };

    public static List<Type> dynamicList = new List<Type>()
    {
        /*typeof(MeshRenderer),
        typeof(ParticleEmitter),
        typeof(ParticleRenderer),
        typeof(ParticleAnimator),

        typeof(BoxCollider),
        typeof(MeshCollider),
        typeof(SphereCollider),
        typeof(CharacterController),
        typeof(CapsuleCollider),

        typeof(Animation),
        typeof(AnimationClip),
        typeof(AnimationState),        

        typeof(BlendWeights),
        typeof(RenderTexture),
        typeof(Rigidbody),*/
    };

    //重载函数，相同参数个数，相同位置out参数匹配出问题时, 需要强制匹配解决
    //使用方法参见例子14
    public static List<Type> outList = new List<Type>()
    {

    };

    static BindType _GT(Type t)
    {
        return new BindType(t);
    }

    static DelegateType _DT(Type t)
    {
        return new DelegateType(t);
    }
}
