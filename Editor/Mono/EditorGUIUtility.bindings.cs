// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Custom mouse cursor shapes used with EditorGUIUtility.AddCursorRect.
    // Must Match EditorWindow::MouseCursor
    public enum MouseCursor
    {
        // Normal pointer arrow
        Arrow = 0,
        // Text cursor
        Text = 1,
        // Vertical resize arrows
        ResizeVertical = 2,
        // Horizontal resize arrows
        ResizeHorizontal = 3,
        // Arrow with a Link badge (for assigning pointers)
        Link = 4,
        // Arrow with small arrows for indicating sliding at number fields
        SlideArrow = 5,
        // Resize up-right for window edges
        ResizeUpRight = 6,
        // Resize up-Left for window edges.
        ResizeUpLeft = 7,
        // Arrow with the move symbol next to it for the sceneview
        MoveArrow = 8,
        // Arrow with the rotate symbol next to it for the sceneview
        RotateArrow = 9,
        // Arrow with the scale symbol next to it for the sceneview
        ScaleArrow = 10,
        // Arrow with the plus symbol next to it
        ArrowPlus = 11,
        // Arrow with the minus symbol next to it
        ArrowMinus = 12,
        // Cursor with a dragging hand for pan
        Pan = 13,
        // Cursor with an eye for orbit
        Orbit = 14,
        // Cursor with a magnifying glass for zoom
        Zoom = 15,
        // Cursor with an eye and stylized arrow keys for FPS navigation
        FPS = 16,
        // The current user defined cursor
        CustomCursor = 17,
        // Split resize up down arrows
        SplitResizeUpDown = 18,
        // Split resize left right arrows
        SplitResizeLeftRight = 19
    }

    // User message types.
    public enum MessageType
    {
        // Neutral message
        None = 0,
        // Info message
        Info = 1,
        // Warning message
        Warning = 2,
        // Error message
        Error = 3
    }

    // Enum that selects which skin to return from EditorGUIUtility.GetBuiltinSkin
    public enum EditorSkin
    {
        // The skin used for game views.
        Game = 0,
        // The skin used for inspectors.
        Inspector = 1,
        // The skin used for scene views.
        Scene = 2
    }

    [NativeHeader("Editor/Src/EditorResources.h"),
     NativeHeader("Runtime/Graphics/Texture2D.h"),
     NativeHeader("Runtime/Graphics/RenderTexture.h"),
     NativeHeader("Modules/TextRendering/Public/Font.h"),
     NativeHeader("Editor/Src/Utility/EditorGUIUtility.h")]
    public partial class EditorGUIUtility
    {
        public static extern string SerializeMainMenuToString();
        public static extern void SetMenuLocalizationTestMode(bool onoff);

        // Set icons rendered as part of [[GUIContent]] to be rendered at a specific size.
        public static extern void SetIconSize(Vector2 size);

        // Get a white texture.
        public static extern Texture2D whiteTexture {[NativeMethod("GetWhiteTexture")] get; }

        // Exactly the same as GUIUtility.systemCopyBuffer, but for some reason done
        // as a separate public API :(
        public new static string systemCopyBuffer
        {
            get => GUIUtility.systemCopyBuffer;
            set => GUIUtility.systemCopyBuffer = value;
        }

        internal static extern int skinIndex
        {
            [FreeFunction("GetEditorResources().GetSkinIdx")] get;
            [FreeFunction("GetEditorResources().SetSkinIdx")] set;
        }

        public static extern void SetWantsMouseJumping(int wantz);

        // Iterates over cameras and counts the ones that would render to a specified screen (doesn't involve culling)
        public static extern bool IsDisplayReferencedByCameras(int displayIndex);

        // Send an input event into the game.
        public static extern void QueueGameViewInputEvent(Event evt);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RenderGameViewCameras is no longer supported.Consider rendering cameras manually.", true)]
        public static void RenderGameViewCameras(RenderTexture target, int targetDisplay, Rect screenRect, Vector2 mousePosition, bool gizmos) {}

        internal static extern Object GetScript(string scriptClass);
        public static extern void SetIconForObject([NotNull] Object obj, Texture2D icon);
        [NativeThrows]
        internal static extern Object GetBuiltinExtraResource(Type type, string path);
        internal static extern BuiltinResource[] GetBuiltinResourceList(int classID);
        internal static extern AssetBundle GetEditorAssetBundle();
        internal static extern AssetBundle ReloadEditorAssetBundle();
        internal static extern void SetRenderTextureNoViewport(RenderTexture rt);
        internal static extern void SetVisibleLayers(int layers);
        internal static extern void SetLockedLayers(int layers);
        internal static extern bool IsGizmosAllowedForObject(Object obj);
        internal static extern void SetCurrentViewCursor(Texture2D texture, Vector2 hotspot, MouseCursor type);
        internal static extern void ClearCurrentViewCursor();
        internal static extern void CleanCache(string text);
        internal static extern void SetSearchIndexOfControlIDList(int index);
        internal static extern int GetSearchIndexOfControlIDList();
        internal static extern bool CanHaveKeyboardFocus(int id);

        // Duplicate of SetDefaultFont in UnityEngine. We need to call it from editor code as well,
        // while keeping both internal.
        internal static extern void SetDefaultFont(Font font);

        public static extern Texture2D GetIconForObject([NotNull] Object obj);

        // Render all ingame cameras bound to a specific Display.
        internal static extern void RenderPlayModeViewCamerasInternal(RenderTexture target, int targetDisplay, Vector2 mousePosition, bool gizmos, bool renderIMGUI);
        internal static extern void SetupWindowSpaceAndVSyncInternal(Rect screenRect);

        private static extern Texture2D FindTextureByName(string name);
        private static extern Texture2D FindTextureByType([NotNull] Type type);
        internal static extern string GetObjectNameWithInfo(Object obj);
        private static extern string GetTypeNameWithInfo(string typeName, int instanceID);
        private static extern void Internal_SetupEventValues(object evt);
        private static extern Vector2 Internal_GetIconSize();
        private static extern bool Internal_GetKeyboardRect(int id, out Rect rect);
        private static extern void Internal_MoveKeyboardFocus(bool forward);
        private static extern int Internal_GetNextKeyboardControlID(bool forward);
        private static extern void Internal_AddCursorRect(Rect r, MouseCursor m, int controlID);

        // preview materials handling
        internal enum PreviewType
        {
            Color,
            ColorVT,
            Alpha,
            AlphaVT,
            Transparent,
            TransparentVT,
            Normalmap,
            NormalmapVT,
            LightmapRGBM,
            LightmapDoubleLDR,
            LightmapFullHDR,
            GUITextureClipVertically,
        }
        internal static extern Material GetPreviewMaterial(PreviewType type);
    }

    [NativeHeader("Editor/Src/InspectorExpandedState.h"),
     StaticAccessor("GetInspectorExpandedState().GetSessionState()", StaticAccessorType.Dot)]
    public class SessionState
    {
        [ExcludeFromDocs] public SessionState() {}

        public static extern void SetBool(string key, bool value);
        public static extern bool GetBool(string key, bool defaultValue);
        public static extern void EraseBool(string key);
        public static extern void SetFloat(string key, float value);
        public static extern float GetFloat(string key, float defaultValue);
        public static extern void EraseFloat(string key);
        public static extern void SetInt(string key, int value);
        public static extern int GetInt(string key, int defaultValue);
        public static extern void EraseInt(string key);
        public static extern void SetString(string key, string value);
        public static extern string GetString(string key, string defaultValue);
        public static extern void EraseString(string key);
        public static extern void SetVector3(string key, Vector3 value);
        public static extern Vector3 GetVector3(string key, Vector3 defaultValue);
        public static extern void EraseVector3(string key);
        public static extern void EraseIntArray(string key);
        public static extern void SetIntArray(string key, int[] value);
        public static extern int[] GetIntArray(string key, [Unmarshalled]int[] defaultValue);
    }
}
