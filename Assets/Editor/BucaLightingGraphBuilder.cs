#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Shader Graph's authoring model is internal. Keep this version-specific adapter
// in the Editor; the saved graph contains ordinary editable nodes, no custom HLSL.
internal sealed class BucaLightingGraphBuilder
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    readonly object graph;
    int nodeIndex;
    static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).FirstOrDefault(t => t != null)
        ?? throw new InvalidOperationException("Shader Graph type unavailable: " + name);
    static object New(string name) => Activator.CreateInstance(TypeOf(name), true);
    static object Get(object obj, string name) => obj.GetType().GetProperty(name, Flags)?.GetValue(obj)
        ?? obj.GetType().GetField(name, Flags)?.GetValue(obj);
    static void Set(object obj, string name, object value)
    {
        var property = obj.GetType().GetProperty(name, Flags);
        if (property != null) { property.SetValue(obj, value); return; }
        var field = obj.GetType().GetField(name, Flags);
        if (field == null) throw new MissingMemberException(obj.GetType().Name, name);
        field.SetValue(obj, value);
    }
    static object Call(object obj, string name, params object[] args)
    {
        var method = obj.GetType().GetMethods(Flags).First(m => m.Name == name && !m.IsGenericMethod &&
            m.GetParameters().Length >= args.Length && m.GetParameters().Skip(args.Length).All(p => p.IsOptional) &&
            m.GetParameters().Take(args.Length).Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v));
        var full = method.GetParameters().Select((p, i) => i < args.Length ? args[i] : p.DefaultValue).ToArray();
        return method.Invoke(obj, full);
    }
    BucaLightingGraphBuilder()
    {
        graph = New("UnityEditor.ShaderGraph.GraphData");
        Set(graph, "path", "Buca/Prototype");
        Call(graph, "AddContexts");
        var target = New("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget");
        Call(target, "TrySetActiveSubTarget", TypeOf("UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget"));
        var unlit = Get(target, "activeSubTarget");
        Set(unlit, "keepLightingVariants", false);
        Set(unlit, "defaultSSAO", false);
        Set(unlit, "defaultDecalBlending", false);
        Set(target, "castShadows", false);
        Set(target, "allowMaterialOverride", false);
        var targets = Array.CreateInstance(TypeOf("UnityEditor.ShaderGraph.Target"), 1);
        targets.SetValue(target, 0);
        var fields = TypeOf("UnityEditor.ShaderGraph.BlockFields");
        var descriptors = new[] { "VertexDescription.Position", "VertexDescription.Normal", "VertexDescription.Tangent", "SurfaceDescription.BaseColor" }
            .Select(s => { var parts = s.Split('.'); return fields.GetNestedType(parts[0], Flags).GetField(parts[1], Flags).GetValue(null); }).ToArray();
        var blocks = Array.CreateInstance(descriptors[0].GetType(), descriptors.Length);
        for (int i = 0; i < descriptors.Length; i++) blocks.SetValue(descriptors[i], i);
        Call(graph, "InitializeOutputs", targets, blocks);
    }
    object Node(string type, string label = null)
    {
        var node = New("UnityEditor.ShaderGraph." + type);
        Call(graph, "AddNode", node);
        if (label != null) Set(node, "name", label);
        var state = Get(node, "drawState");
        Set(state, "position", new Rect((nodeIndex / 5) * 330 - 1200, (nodeIndex % 5) * 250, 220, 180));
        Set(node, "drawState", state);
        nodeIndex++;
        return node;
    }
    object Property(string type, string reference, string label, object value)
    {
        var prop = New("UnityEditor.ShaderGraph.Internal." + type);
        Set(prop, "displayName", label);
        Set(prop, "overrideReferenceName", reference);
        Set(prop, "generatePropertyBlock", true);
        if (value != null) Set(prop, "value", value);
        Call(graph, "AddGraphInput", prop);
        var node = Node("PropertyNode");
        Set(node, "property", prop);
        return node;
    }
    object Float(float value)
    {
        var node = Node("Vector1Node");
        var method = node.GetType().GetMethod("FindSlot", Flags).MakeGenericMethod(TypeOf("UnityEditor.ShaderGraph.MaterialSlot"));
        var slot = method.Invoke(node, new object[] { 1 });
        Set(slot, "value", value);
        return node;
    }
    void Connect(object a, int output, object b, int input) =>
        Call(graph, "Connect", Call(a, "GetSlotReference", output), Call(b, "GetSlotReference", input));
    object Binary(string type, object a, int aSlot, object b, int bSlot, string label = null)
    {
        var node = Node(type, label);
        Connect(a, aSlot, node, 0); Connect(b, bSlot, node, 1);
        return node;
    }
    public static void Create(string path)
    {
        var b = new BucaLightingGraphBuilder();
        var albedo = b.Property("ColorShaderProperty", "_BaseColor", "Paint colour", Color.white);
        var texture = b.Property("Texture2DShaderProperty", "_BaseMap", "Saved paint texture", null);
        var sample = b.Node("SampleTexture2DNode"); b.Connect(texture, 0, sample, 1);
        var paint = b.Binary("MultiplyNode", albedo, 0, sample, 0, "Paint and grain");
        var normal = b.Node("NormalVectorNode");
        var light = b.Node("MainLightDirectionNode");
        var towardLight = b.Node("NegateNode"); b.Connect(light, 0, towardLight, 0);
        var dot = b.Binary("DotProductNode", normal, 0, towardLight, 1);
        var ramp = b.Node("SmoothstepNode", "Soft stylized diffuse ramp");
        b.Connect(b.Float(-.25f), 0, ramp, 0); b.Connect(b.Float(.85f), 0, ramp, 1); b.Connect(dot, 2, ramp, 2);
        var shadeStrength = b.Property("Vector1ShaderProperty", "_ShadeStrength", "Shadow side brightness", .30f);
        var shade = b.Binary("MultiplyNode", paint, 2, shadeStrength, 0);
        var lit = b.Binary("MultiplyNode", paint, 2, b.Float(1.12f), 0);
        var diffuse = b.Node("LerpNode", "Rounded light and shade");
        b.Connect(shade, 2, diffuse, 0); b.Connect(lit, 2, diffuse, 1); b.Connect(ramp, 3, diffuse, 2);
        var view = b.Node("ViewDirectionNode");
        var halfAdd = b.Binary("AddNode", view, 0, towardLight, 1);
        var halfNormal = b.Node("NormalizeNode"); b.Connect(halfAdd, 2, halfNormal, 0);
        var specDot = b.Binary("DotProductNode", normal, 0, halfNormal, 1);
        var clamp = b.Node("SaturateNode"); b.Connect(specDot, 2, clamp, 0);
        var specPower = b.Binary("PowerNode", clamp, 1, b.Float(42), 0);
        var highlightStrength = b.Property("Vector1ShaderProperty", "_HighlightStrength", "Lacquer highlight", .20f);
        var spec = b.Binary("MultiplyNode", specPower, 2, highlightStrength, 0);
        var fresnel = b.Node("FresnelNode", "Subtle edge highlight"); b.Connect(b.Float(4), 0, fresnel, 2);
        var rimStrength = b.Property("Vector1ShaderProperty", "_RimStrength", "Edge highlight strength", .065f);
        var rim = b.Binary("MultiplyNode", fresnel, 3, rimStrength, 0);
        var rimTint = b.Binary("MultiplyNode", rim, 2, paint, 2);
        var withRim = b.Binary("AddNode", diffuse, 3, rimTint, 2);
        var final = b.Binary("AddNode", withRim, 2, spec, 2);
        var getNodes = b.graph.GetType().GetMethod("GetNodes", Flags).MakeGenericMethod(TypeOf("UnityEditor.ShaderGraph.BlockNode"));
        var blocks = (IEnumerable)getNodes.Invoke(b.graph, null);
        var baseColor = blocks.Cast<object>().First(n => Get(n, "name").ToString().Contains("Base Color") || Get(Get(n, "descriptor"), "name").ToString() == "BaseColor");
        b.Connect(final, 2, baseColor, 0);
        Call(b.graph, "ValidateGraph");
        var json = TypeOf("UnityEditor.ShaderGraph.Serialization.MultiJson").GetMethod("Serialize", Flags).Invoke(null, new[] { b.graph }) as string;
        File.WriteAllText(path, json);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Prototype Shader Graph failed to import: " + path);
    }
}
#endif
