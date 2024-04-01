using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MoreCompany
{
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

	[HarmonyPatch(typeof(SpringManAI), "DoAIInterval")]
	public static class SpringManAIIntervalPatch
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
					if (!foundCount && instruction.ToString() == "call void EnemyAI::SwitchToBehaviourState(int stateIndex)")
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

            if (!alreadyReplaced) MainClass.StaticLogger.LogWarning("SpringManAIIntervalPatch failed to replace newPlayerCount");

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

	[HarmonyPatch(typeof(EnemyAI), "GetAllPlayersInLineOfSight")]
    public static class GetAllPlayersInLineOfSightPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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

            if (!alreadyReplaced) MainClass.StaticLogger.LogWarning("GetAllPlayersInLineOfSightPatch failed to replace newPlayerCount");

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
}
