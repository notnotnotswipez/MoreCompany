using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace MoreCompany;

[HarmonyPatch(typeof(ForestGiantAI), "LookForPlayers")]
public static class LookForPlayersForestGiantPatch
{
    public static void Prefix(ForestGiantAI __instance)
    {
        if (__instance.playerStealthMeters.Length != MainClass.newPlayerCount)
        {
            __instance.playerStealthMeters = new float[MainClass.newPlayerCount];
            for (int i = 0; i < MainClass.newPlayerCount; i++)
            {
                __instance.playerStealthMeters[i] = 0f;
            }
        }
    }
}

[HarmonyPatch(typeof(SpringManAI), "Update")]
public static class SpringManAIUpdatePatch
{
    public static bool Prefix(SpringManAI __instance, ref float ___timeSinceHittingPlayer,
        ref float ___stopAndGoMinimumInterval, ref bool ___wasOwnerLastFrame, ref bool ___stoppingMovement,
        ref float ___currentChaseSpeed, ref bool ___hasStopped, ref float ___currentAnimSpeed, ref float ___updateDestinationInterval, ref Vector3 ___tempVelocity, ref float ___targetYRotation, ref float ___setDestinationToPlayerInterval, ref float ___previousYRotation)
    {
        UpdateBase(__instance, ref ___updateDestinationInterval, ref ___tempVelocity, ref ___targetYRotation,
            ref ___setDestinationToPlayerInterval, ref ___previousYRotation);
        if (__instance.isEnemyDead)
        {
            return false;
        }

        int currentBehaviourStateIndex = __instance.currentBehaviourStateIndex;
        if (currentBehaviourStateIndex != 0 && currentBehaviourStateIndex == 1)
        {
            if (___timeSinceHittingPlayer >= 0f)
            {
                ___timeSinceHittingPlayer -= Time.deltaTime;
            }

            if (__instance.IsOwner)
            {
                if (___stopAndGoMinimumInterval > 0f)
                {
                    ___stopAndGoMinimumInterval -= Time.deltaTime;
                }

                if (!___wasOwnerLastFrame)
                {
                    ___wasOwnerLastFrame = true;
                    if (!___stoppingMovement && ___timeSinceHittingPlayer < 0.12f)
                    {
                        __instance.agent.speed = ___currentChaseSpeed;
                    }
                    else
                    {
                        __instance.agent.speed = 0f;
                    }
                }

                bool flag = false;
                for (int i = 0; i < MainClass.newPlayerCount; i++)
                {
                    if (__instance.PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i], false, false) &&
                        StartOfRound.Instance.allPlayerScripts[i]
                            .HasLineOfSightToPosition(__instance.transform.position + Vector3.up * 1.6f, 68f, 60,
                                -1f) &&
                        Vector3.Distance(
                            StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position,
                            __instance.eye.position) > 0.3f)
                    {
                        flag = true;
                    }
                }

                if (__instance.stunNormalizedTimer > 0f)
                {
                    flag = true;
                }

                if (flag != ___stoppingMovement && ___stopAndGoMinimumInterval <= 0f)
                {
                    ___stopAndGoMinimumInterval = 0.15f;
                    if (flag)
                    {
                        __instance.SetAnimationStopServerRpc();
                    }
                    else
                    {
                        __instance.SetAnimationGoServerRpc();
                    }

                    ___stoppingMovement = flag;
                }
            }

            if (___stoppingMovement)
            {
                if (__instance.animStopPoints.canAnimationStop)
                {
                    if (!___hasStopped)
                    {
                        ___hasStopped = true;
                        __instance.mainCollider.isTrigger = false;
                        if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(
                                __instance.transform.position, 70f, 25, -1f))
                        {
                            float num = Vector3.Distance(__instance.transform.position,
                                GameNetworkManager.Instance.localPlayerController.transform.position);
                            if (num < 4f)
                            {
                                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.9f, true);
                            }
                            else if (num < 9f)
                            {
                                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.4f, true);
                            }
                        }

                        if (___currentAnimSpeed > 2f)
                        {
                            RoundManager.PlayRandomClip(__instance.creatureVoice, __instance.springNoises, false,
                                1f, 0);
                            if (__instance.animStopPoints.animationPosition == 1)
                            {
                                __instance.creatureAnimator.SetTrigger("springBoing");
                            }
                            else
                            {
                                __instance.creatureAnimator.SetTrigger("springBoingPosition2");
                            }
                        }
                    }

                    __instance.creatureAnimator.SetFloat("walkSpeed", 0f);
                    ___currentAnimSpeed = 0f;
                    if (__instance.IsOwner)
                    {
                        __instance.agent.speed = 0f;
                        return false;
                    }
                }
            }
            else
            {
                if (___hasStopped)
                {
                    ___hasStopped = false;
                    __instance.mainCollider.isTrigger = true;
                }

                ___currentAnimSpeed = Mathf.Lerp(___currentAnimSpeed, 6f, 5f * Time.deltaTime);
                __instance.creatureAnimator.SetFloat("walkSpeed", ___currentAnimSpeed);
                if (__instance.IsOwner)
                {
                    __instance.agent.speed = Mathf.Lerp(__instance.agent.speed, ___currentChaseSpeed,
                        4.5f * Time.deltaTime);
                }
            }
        }

        return false;
    }

    private static void UpdateBase(EnemyAI __instance, ref float updateDestinationInterval, ref Vector3 tempVelocity, ref float targetYRotation, ref float setDestinationToPlayerInterval, ref float previousYRotation)
    {
        if (__instance.enemyType.isDaytimeEnemy && !__instance.daytimeEnemyLeaving)
            ReflectionUtils.InvokeMethod(__instance, typeof(EnemyAI), "CheckTimeOfDayToLeave", null);
        if (__instance.stunnedIndefinitely <= 0)
        {
            if ((double)__instance.stunNormalizedTimer >= 0.0)
            {
                __instance.stunNormalizedTimer -= Time.deltaTime / __instance.enemyType.stunTimeMultiplier;
            }
            else
            {
                __instance.stunnedByPlayer = (PlayerControllerB)null;
                if ((double)__instance.postStunInvincibilityTimer >= 0.0)
                    __instance.postStunInvincibilityTimer -= Time.deltaTime * 5f;
            }
        }

        if (!__instance.ventAnimationFinished && (double)__instance.timeSinceSpawn < (double)__instance.exitVentAnimationTime +
            0.004999999888241291 * (double)RoundManager.Instance.numberOfEnemiesInScene)
        {
            __instance.timeSinceSpawn += Time.deltaTime;
            if (!__instance.IsOwner)
            {
                Vector3 serverPosition = __instance.serverPosition;
                if (!(__instance.serverPosition != Vector3.zero))
                    return;
                __instance.transform.position = __instance.serverPosition;
                __instance.transform.eulerAngles = new Vector3(__instance.transform.eulerAngles.x, targetYRotation,
                    __instance.transform.eulerAngles.z);
            }
            else if ((double)updateDestinationInterval >= 0.0)
            {
                updateDestinationInterval -= Time.deltaTime;
            }
            else
            {
                __instance.SyncPositionToClients();
                updateDestinationInterval = 0.1f;
            }
        }
        else
        {
            if (!__instance.ventAnimationFinished)
            {
                __instance.ventAnimationFinished = true;
                if ((UnityEngine.Object)__instance.creatureAnimator != (UnityEngine.Object)null)
                    __instance.creatureAnimator.SetBool("inSpawningAnimation", false);
            }

            if (!__instance.IsOwner)
            {
                if (__instance.currentSearch.inProgress)
                    __instance.StopSearch(__instance.currentSearch);
                __instance.SetClientCalculatingAI(false);
                if (!__instance.inSpecialAnimation)
                {
                    __instance.transform.position = Vector3.SmoothDamp(__instance.transform.position, __instance.serverPosition,
                        ref tempVelocity, __instance.syncMovementSpeed);
                    __instance.transform.eulerAngles = new Vector3(__instance.transform.eulerAngles.x,
                        Mathf.LerpAngle(__instance.transform.eulerAngles.y, targetYRotation, 15f * Time.deltaTime),
                        __instance.transform.eulerAngles.z);
                }

                __instance.timeSinceSpawn += Time.deltaTime;
            }
            else if (__instance.isEnemyDead)
            {
                __instance.SetClientCalculatingAI(false);
            }
            else
            {
                if (!__instance.inSpecialAnimation)
                    __instance.SetClientCalculatingAI(true);
                if (__instance.movingTowardsTargetPlayer &&
                    (UnityEngine.Object)__instance.targetPlayer != (UnityEngine.Object)null)
                {
                    if ((double)setDestinationToPlayerInterval <= 0.0)
                    {
                        setDestinationToPlayerInterval = 0.25f;
                        __instance.destination = RoundManager.Instance.GetNavMeshPosition(
                            __instance.targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
                    }
                    else
                    {
                        __instance.destination = new Vector3(__instance.targetPlayer.transform.position.x, __instance.destination.y,
                            __instance.targetPlayer.transform.position.z);
                        setDestinationToPlayerInterval -= Time.deltaTime;
                    }
                }

                if (__instance.inSpecialAnimation)
                    return;
                if ((double)updateDestinationInterval >= 0.0)
                {
                    updateDestinationInterval -= Time.deltaTime;
                }
                else
                {
                    __instance.DoAIInterval();
                    updateDestinationInterval = __instance.AIIntervalTime;
                }

                if ((double)Mathf.Abs(previousYRotation - __instance.transform.eulerAngles.y) <= 6.0)
                    return;
                previousYRotation = __instance.transform.eulerAngles.y;
                targetYRotation = previousYRotation;
                if (__instance.IsServer)
                {
                    ReflectionUtils.InvokeMethod(__instance, typeof(EnemyAI),"UpdateEnemyRotationClientRpc", new object[]{(short)previousYRotation});
                }
                else
                {
                    ReflectionUtils.InvokeMethod(__instance, typeof(EnemyAI),"UpdateEnemyRotationServerRpc", new object[]{(short)previousYRotation});
                }
            }
        }
    }
}

[HarmonyPatch(typeof(SpringManAI), "DoAIInterval")]
public static class SpringManAIIntervalPatch
{
    public static bool Prefix(SpringManAI __instance)
    {
        if (__instance.moveTowardsDestination)
        {
            __instance.agent.SetDestination(__instance.destination);
        }
        __instance.SyncPositionToClients();
        if (StartOfRound.Instance.allPlayersDead)
        {
            return false;
        }
        if (__instance.isEnemyDead)
        {
            return false;
        }
        int currentBehaviourStateIndex = __instance.currentBehaviourStateIndex;
        if (currentBehaviourStateIndex != 0)
        {
            if (currentBehaviourStateIndex != 1)
            {
                return false;
            }
            if (__instance.searchForPlayers.inProgress)
            {
                __instance.StopSearch(__instance.searchForPlayers, true);
            }
            if (__instance.TargetClosestPlayer(1.5f, false, 70f))
            {
                PlayerControllerB previousTarget = ReflectionUtils.GetFieldValue<PlayerControllerB>(__instance, "previousTarget");
                if (previousTarget != __instance.targetPlayer)
                {
                    ReflectionUtils.SetFieldValue(__instance, "previousTarget", __instance.targetPlayer);
                    __instance.ChangeOwnershipOfEnemy(__instance.targetPlayer.actualClientId);
                }
                __instance.movingTowardsTargetPlayer = true;
                return false;
            }
            __instance.SwitchToBehaviourState(0);
            __instance.ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
        }
        else
        {
            if (!__instance.IsServer)
            {
                __instance.ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
                return false;
            }
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (__instance.PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i], false, false) && !Physics.Linecast(__instance.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault) && Vector3.Distance(__instance.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) < 30f)
                {
                    __instance.SwitchToBehaviourState(1);
                    return false;
                }
            }
            if (!__instance.searchForPlayers.inProgress)
            {
                __instance.movingTowardsTargetPlayer = false;
                __instance.StartSearch(__instance.transform.position, __instance.searchForPlayers);
                return false;
            }
        }
        return false;
    }
}

[HarmonyPatch(typeof(EnemyAI), "GetClosestPlayer")]
public static class GetClosestPlayerPatch
{
    public static bool Prefix(EnemyAI __instance, ref PlayerControllerB __result, bool requireLineOfSight = false, bool cannotBeInShip = false, bool cannotBeNearShip = false)
    {
        PlayerControllerB playerControllerB = null;
        __instance.mostOptimalDistance = 2000f;
        for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            if (__instance.PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i], cannotBeInShip, false))
            {
                if (cannotBeNearShip)
                {
                    if (StartOfRound.Instance.allPlayerScripts[i].isInElevator)
                    {
                        goto IL_124;
                    }
                    bool flag = false;
                    for (int j = 0; j < RoundManager.Instance.spawnDenialPoints.Length; j++)
                    {
                        if (Vector3.Distance(RoundManager.Instance.spawnDenialPoints[j].transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) < 10f)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag)
                    {
                        goto IL_124;
                    }
                }
                if (!requireLineOfSight || !Physics.Linecast(__instance.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position, 256))
                {
                    __instance.tempDist = Vector3.Distance(__instance.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                    if (__instance.tempDist < __instance.mostOptimalDistance)
                    {
                        __instance.mostOptimalDistance = __instance.tempDist;
                        playerControllerB = StartOfRound.Instance.allPlayerScripts[i];
                    }
                }
            }
            IL_124:;
        }
        __result = playerControllerB;
        return false;
    }
}

[HarmonyPatch(typeof(EnemyAI), "GetAllPlayersInLineOfSight")]
public static class GetAllPlayersInLineOfSightPatch
{
    public static bool Prefix(EnemyAI __instance, ref PlayerControllerB[] __result, float width = 45f, int range = 60, Transform eyeObject = null, float proximityCheck = -1f, int layerMask = -1)
    {
        if (layerMask == -1)
        {
            layerMask = StartOfRound.Instance.collidersAndRoomMaskAndDefault;
        }
        if (eyeObject == null)
        {
            eyeObject = __instance.eye;
        }
        if (__instance.enemyType.isOutsideEnemy && !__instance.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
        {
            range = Mathf.Clamp(range, 0, 30);
        }
        List<PlayerControllerB> list = new List<PlayerControllerB>(MainClass.newPlayerCount);
        for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            if (__instance.PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i], false, false))
            {
                Vector3 position = StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position;
                if (Vector3.Distance(__instance.eye.position, position) < (float)range && !Physics.Linecast(eyeObject.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    Vector3 vector = position - eyeObject.position;
                    if (Vector3.Angle(eyeObject.forward, vector) < width || Vector3.Distance(__instance.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) < proximityCheck)
                    {
                        list.Add(StartOfRound.Instance.allPlayerScripts[i]);
                    }
                }
            }
        }
        if (list.Count == MainClass.newPlayerCount)
        {
            __result = StartOfRound.Instance.allPlayerScripts;
            return false;
        }
        if (list.Count > 0)
        {
            __result = list.ToArray();
            return false;
        }
        __result = null;
        return false;
    }
}
    
[HarmonyPatch(typeof(DressGirlAI), "ChoosePlayerToHaunt")]
public static class DressGirlHauntPatch
{
    public static bool Prefix(DressGirlAI __instance)
    {
        ReflectionUtils.SetFieldValue(__instance, "timesChoosingAPlayer", ReflectionUtils.GetFieldValue<int>(__instance, "timesChoosingAPlayer") + 1);
        if (ReflectionUtils.GetFieldValue<int>(__instance, "timesChoosingAPlayer") > 1)
        {
            __instance.timer = __instance.hauntInterval - 1f;
        }

        __instance.SFXVolumeLerpTo = 0f;
        __instance.creatureVoice.Stop();
        __instance.heartbeatMusic.volume = 0f;
		    
        if (!ReflectionUtils.GetFieldValue<bool>(__instance, "initializedRandomSeed"))
        {
            ReflectionUtils.SetFieldValue(__instance, "ghostGirlRandom", new Random(StartOfRound.Instance.randomMapSeed + 158) );
        }

        float num = 0f;
        float num2 = 0f;
        int num3 = 0;
        int num4 = 0;
        for (int i = 0; i < MainClass.newPlayerCount; i++)
        {
            if (StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount > num3)
            {
                num3 = StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount;
                num4 = i;
            }

            if (StartOfRound.Instance.allPlayerScripts[i].insanityLevel > num)
            {
                num = StartOfRound.Instance.allPlayerScripts[i].insanityLevel;
                num2 = (float)i;
            }
        }

        int[] array = new int[MainClass.newPlayerCount];
        for (int j = 0; j < MainClass.newPlayerCount; j++)
        {
            if (!StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled)
            {
                array[j] = 0;
            }
            else
            {
                array[j] += 80;
                if (num2 == (float)j && num > 1f)
                {
                    array[j] += 50;
                }

                if (num4 == j)
                {
                    array[j] += 30;
                }

                if (!StartOfRound.Instance.allPlayerScripts[j].hasBeenCriticallyInjured)
                {
                    array[j] += 10;
                }

                if (StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer != null &&
                    StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer.scrapValue > 150)
                {
                    array[j] += 30;
                }
            }
        }

        __instance.hauntingPlayer =
            StartOfRound.Instance.allPlayerScripts[
                RoundManager.Instance.GetRandomWeightedIndex(array, ReflectionUtils.GetFieldValue<Random>(__instance, "ghostGirlRandom"))];
        if (__instance.hauntingPlayer.isPlayerDead)
        {
            for (int k = 0; k < StartOfRound.Instance.allPlayerScripts.Length; k++)
            {
                if (!StartOfRound.Instance.allPlayerScripts[k].isPlayerDead)
                {
                    __instance.hauntingPlayer = StartOfRound.Instance.allPlayerScripts[k];
                    break;
                }
            }
        }

        Debug.Log(string.Format("Little girl: Haunting player with playerClientId: {0}; actualClientId: {1}",
            __instance.hauntingPlayer.playerClientId, __instance.hauntingPlayer.actualClientId));
        __instance.ChangeOwnershipOfEnemy(__instance.hauntingPlayer.actualClientId);
        __instance.hauntingLocalPlayer = GameNetworkManager.Instance.localPlayerController == __instance.hauntingPlayer;
		    
        if (ReflectionUtils.GetFieldValue<Coroutine>(__instance, "switchHauntedPlayerCoroutine") != null)
        {
            __instance.StopCoroutine(ReflectionUtils.GetFieldValue<Coroutine>(__instance, "switchHauntedPlayerCoroutine"));
        }

        ReflectionUtils.SetFieldValue(__instance, "switchHauntedPlayerCoroutine", __instance.StartCoroutine( ReflectionUtils.InvokeMethod<IEnumerator>(__instance, "setSwitchingHauntingPlayer", null)));

        return false;
    }
}