using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace MoreCompany
{

    public static class InstructionUtils {

        public static bool DoesSafeMatchInstruction(List<CodeInstruction> instructions, int index, OpCode opCode) {
            if (index >= instructions.Count) {
                return false;
            }

            CodeInstruction codeInstruction = instructions[index];
            if (codeInstruction.opcode != opCode) {
                return false;
            }

            return true;
        }
    }

	[HarmonyPatch(typeof(ForestGiantAI), "LookForPlayers")]
	public static class LookForPlayersForestGiantPatch
	{
		public static void Prefix(ref ForestGiantAI __instance)
		{
            if (__instance.playerStealthMeters.Length != MainClass.newPlayerCount)
			{
				Array.Resize(ref __instance.playerStealthMeters, MainClass.newPlayerCount);
				for (int i = 0; i < MainClass.newPlayerCount; i++)
				{
					__instance.playerStealthMeters[i] = 0f;
				}
			}
		}
    }

    [HarmonyPatch(typeof(BlobAI), "Start")]
    public static class BlobAIStartPatch
    {
        public static void Postfix(ref BlobAI __instance)
        {
            Collider[] ragdollColliders = new Collider[MainClass.newPlayerCount];
            ReflectionUtils.SetFieldValue(__instance, "ragdollColliders", ragdollColliders);
        }
    }

    [HarmonyPatch(typeof(CrawlerAI), "Start")]
    public static class CrawlerAIStartPatch
    {
        public static void Postfix(ref CrawlerAI __instance)
        {
            Collider[] nearPlayerColliders = new Collider[MainClass.newPlayerCount];
            ReflectionUtils.SetFieldValue(__instance, "nearPlayerColliders", nearPlayerColliders);
        }
    }

    [HarmonyPatch(typeof(SpringManAI), "Update")]
	public static class SpringManAIUpdatePatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool foundCount = false;
            bool alreadyReplaced = false;
            foreach (var instruction in instructions)
            {
                if (!alreadyReplaced)
                {
                    if (!foundCount && instruction.ToString() == "call static float UnityEngine.Vector3::Distance(UnityEngine.Vector3 a, UnityEngine.Vector3 b)")
                    {
                        foundCount = true;
                    }
                    else if (foundCount && instruction.ToString() == "ldc.i4.4 NULL")
                    {
                        alreadyReplaced = true;
                        CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
                        newInstructions.Add(codeInstruction);
                        continue;
                    }
                }

                newInstructions.Add(instruction);
            }

            if (!alreadyReplaced) MainClass.StaticLogger.LogWarning("SpringManAIUpdatePatch failed to replace newPlayerCount");

            return newInstructions.AsEnumerable();
        }
    }

	[HarmonyPatch(typeof(EnemyAI), "GetClosestPlayer")]
	public static class GetClosestPlayerPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool alreadyReplaced = false;
            foreach (var instruction in instructions)
            {
                if (!alreadyReplaced)
                {
					if (instruction.ToString() == "ldc.i4.4 NULL")
					{
						alreadyReplaced = true;
						CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
						newInstructions.Add(codeInstruction);
                        continue;
					}
				}

                newInstructions.Add(instruction);
            }

            if (!alreadyReplaced) MainClass.StaticLogger.LogWarning("GetClosestPlayerPatch failed to replace newPlayerCount");

            return newInstructions.AsEnumerable();
        }
	}

    [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.Update))]
    public static class BushWolfEnemyPlayerObservePatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = instructions.ToList();
            FieldInfo livingPlayersField = typeof(StartOfRound).GetField(nameof(StartOfRound.livingPlayers), BindingFlags.Instance | BindingFlags.Public);
            MethodInfo mathfClamp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new []{typeof(int), typeof(int), typeof(int)});

            bool didPatch = false;

            for (int i = 0; i < codeInstructions.Count; i++) {
                CodeInstruction activeInstruction = codeInstructions[i];
                if (activeInstruction.opcode == OpCodes.Ldfld && (FieldInfo)activeInstruction.operand == livingPlayersField) {
                    if (InstructionUtils.DoesSafeMatchInstruction(codeInstructions, i + 1, OpCodes.Ldc_I4_2) && InstructionUtils.DoesSafeMatchInstruction(codeInstructions, i + 2, OpCodes.Sub)) {
                        // Remove the ldc.i4.2 and sub instruction
                        codeInstructions.RemoveAt(i + 1);
                        codeInstructions.RemoveAt(i + 1);

                        // Insert at i+2 (Skipping the ldc.i4.0) the ldc.i4.3
                        codeInstructions.Insert(i+2, new CodeInstruction(OpCodes.Ldc_I4_3));

                        // Replace the method call from max to clamp
                        codeInstructions[i + 3].operand = mathfClamp;
                        didPatch = true;
                        break;
                    }
                }
            }

            if (!didPatch) MainClass.StaticLogger.LogWarning("BushWolfEnemy.Update failed to patch observation rule (Max 3 observers)");

            return codeInstructions;
        }
    }

    [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.GetClosestPlayerToNest))]
    public static class BushWolfEnemyClosestPlayerNestPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool alreadyReplaced = false;
            foreach (var instruction in instructions)
            {
                if (!alreadyReplaced)
                {
                    if (instruction.ToString() == "ldc.i4.4 NULL")
                    {
                        alreadyReplaced = true;
                        CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
                        newInstructions.Add(codeInstruction);
                        continue;
                    }
                }

                newInstructions.Add(instruction);
            }

            if (!alreadyReplaced) MainClass.StaticLogger.LogWarning("GetClosestPlayerToNest failed to replace newPlayerCount");

            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.DoAIInterval))]
    public static class BushWolfTargetDelayPatch {
        public static void Prefix(BushWolfEnemy __instance, ref int ___checkPlayer) {
            PlayerControllerB currentPlayerCheck = StartOfRound.Instance.allPlayerScripts[___checkPlayer];
            int iterations = 0;
            while ((!currentPlayerCheck.isPlayerControlled || currentPlayerCheck.isPlayerDead) && iterations < StartOfRound.Instance.allPlayerScripts.Length) {
                ___checkPlayer = (___checkPlayer + 1) % StartOfRound.Instance.allPlayerScripts.Length;
                currentPlayerCheck = StartOfRound.Instance.allPlayerScripts[___checkPlayer];
                iterations++;
            }
        }
    }

    [HarmonyPatch(typeof(EnemyAI), "GetAllPlayersInLineOfSight")]
    public static class GetAllPlayersInLineOfSightPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            int alreadyReplaced = 0;
            foreach (var instruction in instructions)
            {
				if (instruction.opcode == OpCodes.Ldc_I4_4)
                {
                    alreadyReplaced++;
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = AccessTools.Field(typeof(MainClass), "newPlayerCount");
				}

                newInstructions.Add(instruction);
            }

            if (alreadyReplaced != 2) MainClass.StaticLogger.LogWarning($"GetAllPlayersInLineOfSightPatch failed to replace newPlayerCount: {alreadyReplaced}/2");

            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(DressGirlAI), "ChoosePlayerToHaunt")]
    public static class DressGirlHauntPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            int alreadyReplaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.ToString() == "ldc.i4.4 NULL")
                {
                    alreadyReplaced++;
                    CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
                    newInstructions.Add(codeInstruction);
                    continue;
                }

                newInstructions.Add(instruction);
            }

            if (alreadyReplaced != 3) MainClass.StaticLogger.LogWarning($"DressGirlHauntPatch failed to replace newPlayerCount: {alreadyReplaced}/3");

            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(ButlerEnemyAI), "Start")]
    public static class ButlerEnemyAIPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            int alreadyReplaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.ToString() == "ldc.i4.4 NULL")
                {
                    alreadyReplaced++;
                    CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
                    newInstructions.Add(codeInstruction);
                    continue;
                }

                newInstructions.Add(instruction);
            }

            if (alreadyReplaced != 3) MainClass.StaticLogger.LogWarning($"ButlerEnemyAIPatch failed to replace newPlayerCount: {alreadyReplaced}/3");
            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(CaveDwellerAI), "GetAllPlayerBodiesInLineOfSight")]
    public static class GetAllPlayerBodiesInLineOfSightPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            int alreadyReplaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_4)
                {
                    alreadyReplaced++;
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = AccessTools.Field(typeof(MainClass), "newPlayerCount");
                }

                newInstructions.Add(instruction);
            }

            if (alreadyReplaced != 2) MainClass.StaticLogger.LogWarning($"GetAllPlayerBodiesInLineOfSightPatch failed to replace newPlayerCount: {alreadyReplaced}/2");

            return newInstructions.AsEnumerable();
        }
    }
}
