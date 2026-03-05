using HarmonyLib;
using RustServerMetrics.HarmonyPatches.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

// ReSharper disable once InconsistentNaming

namespace RustServerMetrics.HarmonyPatches.Delayed;

[DelayedHarmonyPatch]
[HarmonyPatch]
internal static class InvokeHandlerBase_DoTick_Patch
{
    #region Members

    private static readonly Stopwatch Stopwatch = new();

    private static readonly List<CodeInstruction> NeedleSequenceToFind = new()
    {
        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(InvokeAction), nameof(InvokeAction.action))),
        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Action), nameof(Action.Invoke)))
    };

    private static readonly List<CodeInstruction> SequenceToInject = new()
    {
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InvokeHandlerBase_DoTick_Patch), nameof(InvokeWrapper)))
    };

    #endregion

    #region Patching
        
    [HarmonyPrepare]
    public static bool Prepare()
    {
        // ReSharper disable once InvertIf
        if (!RustServerMetricsLoader.__serverStarted)
        {
            UnityEngine.Debug.Log("Note: Cannot patch InvokeHandlerBase_DoTick_Patch yet. We will patch it upon server start.");
            return false;
        }

        return true;
    }

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.DeclaredMethod(typeof(InvokeHandlerBase<InvokeHandler>), nameof(InvokeHandlerBase<InvokeHandler>.DoTick));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> originalInstructions, MethodBase methodBase)
    {
        var instructionsList = originalInstructions.ToList();
            
        if (!instructionsList.ReplaceSequence(NeedleSequenceToFind, SequenceToInject))
        {
            UnityEngine.Debug.LogError("[ServerMetrics] Failed to patch InvokeHandlerBase_DoTick_Patch. Unable to find the expected injection point.");
        }

        return instructionsList;
    }
        
    #endregion

    #region Handler
        
    private static void InvokeWrapper(InvokeAction invokeAction)
    {
        try
        {
            Stopwatch.Restart();
            invokeAction.action.Invoke();
        }
        finally
        {
            Stopwatch.Stop();
            MetricsLogger.Instance?.ServerInvokes.LogTime(invokeAction.action.Method, Stopwatch.Elapsed.TotalMilliseconds);
        }
    }
        
    #endregion
}