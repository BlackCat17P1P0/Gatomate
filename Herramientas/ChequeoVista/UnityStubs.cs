// Réplica MÍNIMA de las partes de la API de Unity que usa la vista, SOLO para comprobar que el código compila
// (y para poder ejecutar fuera de Unity partes puras, como la síntesis de sonido). No es Unity: no dibuja nada.
// Las firmas están copiadas de la API de Unity de memoria; la comprobación definitiva es abrir el proyecto en Unity.
using System;
using System.Collections;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static implicit operator bool(Object o) { return !ReferenceEquals(o, null); }
    }
    public class Coroutine { }
    public class Component : Object
    {
        public GameObject gameObject { get { return null; } }
        public Transform transform { get { return null; } }
    }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator e) { return null; }
        public void StopAllCoroutines() { }
    }
    public class Transform : Component
    {
        public Transform parent;
        public Vector3 position, localPosition, localScale, eulerAngles;
        public Quaternion localRotation;
    }
    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public Transform transform { get { return null; } }
        public bool activeSelf { get { return true; } }
        public string tag;
        public void SetActive(bool b) { }
        public static GameObject Find(string name) { return null; }
        public T AddComponent<T>() where T : Component, new() { return new T(); }
    }
    public enum CameraClearFlags { Skybox, SolidColor }
    public class Camera : Behaviour
    {
        public static Camera main { get { return null; } }
        public static int allCamerasCount { get { return 0; } }
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public bool orthographic;
        public float orthographicSize;
        public Vector3 ScreenToWorldPoint(Vector3 v) { return v; }
        public Vector3 WorldToScreenPoint(Vector3 v) { return v; }
    }
    public class AudioListener : Behaviour { }
    public class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) { return new AudioClip(); }
        public bool SetData(float[] data, int offsetSamples) { return true; }
    }
    public class AudioSource : Behaviour
    {
        public AudioClip clip; public bool loop; public float volume, pitch;
        public void Play() { }
        public void PlayOneShot(AudioClip c, float vol) { }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
        public static Color Lerp(Color a, Color b, float t) { return a; }
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(); } }
        public static Vector2 right { get { return new Vector2(1, 0); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public Vector2 normalized { get { return this; } }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { return a; }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, float k) { return new Vector2(a.x * k, a.y * k); }
        public static Vector2 operator /(Vector2 a, float k) { return new Vector2(a.x / k, a.y / k); }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0f); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(); } }
    }
    public struct Quaternion
    {
        public static Quaternion identity { get { return new Quaternion(); } }
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion(); }
    }
    public struct Bounds { public Vector3 size; }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMax { get { return x + width; } }
        public float yMax { get { return y + height; } }
        public bool Contains(Vector2 p) { return p.x >= x && p.x < x + width && p.y >= y && p.y < y + height; }
    }
    public class Texture : Object { public int width, height; }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point, Bilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public class Texture2D : Texture
    {
        public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public FilterMode filterMode; public TextureWrapMode wrapMode;
        public void SetPixels32(Color32[] c) { }
        public void Apply() { }
        public static Texture2D whiteTexture { get { return null; } }
    }
    public class Sprite : Object
    {
        public Bounds bounds { get { return new Bounds(); } }
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu) { return new Sprite(); }
    }
    public class Renderer : Component { public int sortingOrder; }
    public class SpriteRenderer : Renderer { public Sprite sprite; public Color color; public bool enabled; }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum ScaleMode { StretchToFill, ScaleAndCrop, ScaleToFit }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public static class Application { public static void Quit() { } }
    public static class PlayerPrefs
    {
        public static bool HasKey(string k) { return false; }
        public static string GetString(string k) { return ""; }
        public static string GetString(string k, string d) { return d; }
        public static void SetString(string k, string v) { }
        public static int GetInt(string k, int d) { return d; }
        public static void SetInt(string k, int v) { }
        public static void DeleteKey(string k) { }
        public static void Save() { }
    }
    public static class Screen { public static int width { get { return 1280; } } public static int height { get { return 720; } } }
    public static class Time
    {
        public static float deltaTime { get { return 0.016f; } }
        public static float realtimeSinceStartup { get { return 0f; } }
    }
    public static class Random
    {
        private static readonly System.Random _r = new System.Random(1);
        public static float Range(float a, float b) { return a + (float)_r.NextDouble() * (b - a); }
        public static int Range(int a, int b) { return _r.Next(a, b); }
        public static float value { get { return (float)_r.NextDouble(); } }
    }
    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Rad2Deg = 57.29578f;
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static float Min(params float[] v) { float m = v[0]; foreach (float f in v) if (f < m) m = f; return m; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Max(params float[] v) { float m = v[0]; foreach (float f in v) if (f > m) m = f; return m; }
        public static float Floor(float a) { return (float)System.Math.Floor(a); }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static float Abs(float a) { return a < 0 ? -a : a; }
        public static float Clamp(float v, float a, float b) { return v < a ? a : (v > b ? b : v); }
        public static float Clamp01(float v) { return Clamp(v, 0f, 1f); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float Sin(float f) { return (float)Math.Sin(f); }
        public static float Cos(float f) { return (float)Math.Cos(f); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Exp(float f) { return (float)Math.Exp(f); }
        public static int RoundToInt(float f) { return (int)Math.Round(f); }
        public static float MoveTowards(float a, float b, float d) { return Math.Abs(b - a) <= d ? b : a + Math.Sign(b - a) * d; }
    }
    public enum EventType { MouseDown, MouseUp, KeyDown, Repaint, Layout, Used }
    public enum KeyCode { None, Q, W, E, R, S, A, F, M, T, F10, Backslash, Space, Return, Escape, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8 }
    public class Event
    {
        public static Event current { get { return null; } }
        public EventType type; public int button; public KeyCode keyCode; public char character;
        public Vector2 mousePosition { get { return new Vector2(); } }
        public void Use() { }
    }
    public class GUIStyleState { public Color textColor; }
    public class GUIContent { public GUIContent(string text) { } }
    public class GUIStyle
    {
        public GUIStyle(GUIStyle other) { }
        public int fontSize; public bool wordWrap, richText; public FontStyle fontStyle; public TextAnchor alignment;
        public GUIStyleState normal = new GUIStyleState();
        public float CalcHeight(GUIContent c, float w) { return 20f; }
        public Vector2 CalcSize(GUIContent c) { return new Vector2(); }
    }
    public class GUISkin { public GUIStyle label, textField; }
    public static class GUI
    {
        public static Color color;
        public static GUISkin skin { get { return null; } }
        public static void Label(Rect r, string s, GUIStyle st) { }
        public static string TextField(Rect r, string s, int max, GUIStyle st) { return s; }
        public static void DrawTexture(Rect r, Texture t) { }
        public static void DrawTexture(Rect r, Texture t, ScaleMode m, bool alpha) { }
        public static void DrawTextureWithTexCoords(Rect r, Texture t, Rect uv) { }
    }
    public enum RuntimeInitializeLoadType { AfterSceneLoad }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { }
    }
}
