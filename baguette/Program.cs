﻿using baguette;
using CS2Dumper;
using Swed64;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketIO;
using SocketIO.Core;
using SocketIO.Serializer.Core;
using SocketIO.Serializer.SystemTextJson;
using SocketIOClient;
using SocketIOClient.Extensions;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;
using SocketIOClient.Transport.WebSockets;
using Newtonsoft.Json;
using System.IO;

bool isConnecting = false;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("This is baguette !");

Console.WriteLine("Looking for cs2 process...");

while (Process.GetProcessesByName("cs2").Length == 0) { }

Renderer renderer = new Renderer();
Thread rendererThread = new Thread(new ThreadStart(renderer.Start().Wait));
rendererThread.Start();

Swed swed = new Swed("cs2");
IntPtr clientPtr = swed.GetModuleBase("client.dll");
IntPtr engine2Ptr = swed.GetModuleBase("engine2.dll");
IntPtr matchmakingPtr = swed.GetModuleBase("matchmaking.dll");

Console.WriteLine("CS2 found");

[DllImport("user32.dll")]
static extern short GetAsyncKeyState(int vKey); // Handle hotkey

[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);


Vector2 screenSize = renderer.screenSize;
List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();
Entity bomb = new Entity();

Uri serverUri = new Uri("ws://94.125.162.126:3000");
var client = new SocketIOClient.SocketIO(serverUri);

client.OnError += async (sender, r) =>
{
    Console.WriteLine("Disconnected!");

    //try
    //{
    //    await client.DisconnectAsync();
    //    await client.ConnectAsync();
    //    Console.WriteLine("Reconnected!");
    //}
    //catch (Exception ex)
    //{
    //    Console.WriteLine("Error : " + ex.Message);
    //}
};

try
{
    isConnecting = true;
    await client.ConnectAsync();
    Console.WriteLine("Connected!");
    isConnecting = false;
}
catch (Exception ex)
{
    Console.WriteLine("Error : " + ex.Message);
}

while (true)
{
    entities.Clear();

    IntPtr entityListPtr = swed.ReadPointer(clientPtr, (int)CS2Dumper.Offsets.ClientDll.dwEntityList);
    IntPtr listEntryPtr = swed.ReadPointer(entityListPtr, 0x10);
    IntPtr localPlayerPawnPtr = swed.ReadPointer(clientPtr, (int)CS2Dumper.Offsets.ClientDll.dwLocalPlayerPawn);

    ViewMatrix viewMatrix = Renderer.ReadMatrix(clientPtr + (int)CS2Dumper.Offsets.ClientDll.dwViewMatrix, swed);

    IntPtr c4Entity = swed.ReadPointer(swed.ReadPointer(clientPtr, (int)CS2Dumper.Offsets.ClientDll.dwWeaponC4));
    IntPtr bombOwnerPtr = swed.ReadPointer(c4Entity, (int)CS2Dumper.Schemas.ClientDll.C_BaseEntity.m_hOwnerEntity);

    for (int i = 0; i < 64; i++)
    {
        IntPtr currentControllerPtr = swed.ReadPointer(listEntryPtr, i * 0x78);
        if(currentControllerPtr == IntPtr.Zero)
        {
            continue;
        }

        int pawnHandle = swed.ReadInt(currentControllerPtr, (int)CS2Dumper.Schemas.ClientDll.CCSPlayerController.m_hPlayerPawn);
        if (pawnHandle == 0)
        {
            continue;
        }

        IntPtr listEntry2Ptr = swed.ReadPointer(entityListPtr, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
        /*IntPtr listEntryPtr = swed.ReadPointer(listEntryFirstPtr, 0x8 * ((pawnHandle & 0x7FFF) >> 9));*/
        if (listEntry2Ptr == IntPtr.Zero)
        {
            continue;
        }

        IntPtr entryPlayerPawn = swed.ReadPointer(listEntry2Ptr, 0x78 * (pawnHandle  & 0x1FF));
        if (entryPlayerPawn == IntPtr.Zero)
        {
            continue;
        }

        int lifeState = swed.ReadInt(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BaseEntity.m_lifeState);
        if (lifeState != 256)
        {
            continue;
        }

        IntPtr sceneNodePtr = swed.ReadPointer(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BaseEntity.m_pGameSceneNode);
        if (sceneNodePtr == IntPtr.Zero)
        {
            continue;
        }

        // IntPtr boneMatrixPtr = swed.ReadPointer(sceneNodePtr, m_modelState, + 0x80); // 0x80 would be dwBoneMatrix
        IntPtr boneMatrixPtr = swed.ReadPointer(sceneNodePtr, (int)CS2Dumper.Schemas.ClientDll.CSkeletonInstance.m_modelState + 0x80); // 0x80 would be dwBoneMatrix
        if (boneMatrixPtr == IntPtr.Zero)
        {
            continue;
        }

        // Populate entity
        Entity entity = new Entity();
        entity.Team = swed.ReadInt(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BaseEntity.m_iTeamNum);

        IntPtr entryItemServicesPtr = swed.ReadPointer(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BasePlayerPawn.m_pItemServices);
        entity.hasDiffuser = swed.ReadBool(entryItemServicesPtr, (int)CS2Dumper.Schemas.ClientDll.CCSPlayer_ItemServices.m_bHasDefuser);
        entity.hasArmor = swed.ReadBool(entryItemServicesPtr, (int)CS2Dumper.Schemas.ClientDll.CCSPlayer_ItemServices.m_bHasHeavyArmor);
        entity.hasHelmet = swed.ReadBool(entryItemServicesPtr, (int)CS2Dumper.Schemas.ClientDll.CCSPlayer_ItemServices.m_bHasHelmet);

        entity.hasBomb = (int)bombOwnerPtr == pawnHandle;

        //entity.Name = swed.ReadString(currentControllerPtr, (int)CS2Dumper.Schemas.ClientDll.CBasePlayerController.m_iszPlayerName, 16);
        entity.Name = swed.ReadString(currentControllerPtr, (int)CS2Dumper.Schemas.ClientDll.CBasePlayerController.m_iszPlayerName, 8);
        entity.Health = swed.ReadInt(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BaseEntity.m_iHealth);
        entity.Armor = swed.ReadInt(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_CSPlayerPawn.m_ArmorValue);

        entity.Angle = swed.ReadVec(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BasePlayerPawn.v_angle).Y - 90;
        entity.PositionV3 = swed.ReadVec(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BasePlayerPawn.m_vOldOrigin);
        entity.ViewOffsetV3 = swed.ReadVec(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BaseModelEntity.m_vecViewOffset);
        entity.PositionV2 = Renderer.WorldToScreen(viewMatrix, entity.PositionV3, screenSize);
        entity.ViewOffsetV2 = Renderer.WorldToScreen(viewMatrix, Vector3.Add(entity.PositionV3, entity.ViewOffsetV3), screenSize);

        entity.Distance = Vector3.Distance(entity.PositionV3, localPlayer.PositionV3);
        
        entity.bones3D = Renderer.ReadBones(boneMatrixPtr, swed);
        entity.bones2D = Renderer.ReadBones2D(entity.bones3D, viewMatrix, screenSize);

        entities.Add(entity);
    }

    localPlayer.Team = swed.ReadInt(localPlayerPawnPtr, (int)CS2Dumper.Schemas.ClientDll.C_BaseEntity.m_iTeamNum);
    localPlayer.Health = swed.ReadInt(localPlayerPawnPtr, (int)CS2Dumper.Schemas.ClientDll.C_BaseEntity.m_iHealth);
    localPlayer.Armor = swed.ReadInt(localPlayerPawnPtr, (int)CS2Dumper.Schemas.ClientDll.C_CSPlayerPawn.m_ArmorValue);

    localPlayer.PositionV3 = swed.ReadVec(localPlayerPawnPtr, (int)CS2Dumper.Schemas.ClientDll.C_BasePlayerPawn.m_vOldOrigin);
    localPlayer.ViewOffsetV3 = swed.ReadVec(localPlayerPawnPtr, (int)CS2Dumper.Schemas.ClientDll.C_BaseModelEntity.m_vecViewOffset);
    localPlayer.PositionV2 = Renderer.WorldToScreen(viewMatrix, localPlayer.PositionV3, screenSize);
    localPlayer.ViewOffsetV2 = Renderer.WorldToScreen(viewMatrix, Vector3.Add(localPlayer.PositionV3, localPlayer.ViewOffsetV3), screenSize);

    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateLocalEntities(entities);

    int entIndex = swed.ReadInt(localPlayerPawnPtr, (int)CS2Dumper.Schemas.ClientDll.C_CSPlayerPawnBase.m_iIDEntIndex);

    if (
        !TriggerBot.shootLock
        &&
        renderer.triggerBotEnabled 
        &&
        (GetAsyncKeyState(0x6) < 0 || renderer.triggerBotAutoModeEnabled) 
        && 
        entIndex != -1
    ) // mouse 4 or 5
    {
        Thread shootThread = new Thread(new ThreadStart(() => {
            TriggerBot.shoot(entIndex, renderer.triggerBotReflexTime, renderer.triggerBotPressedDuration, renderer.triggerBotDelayBetweenClicks);
        }));
        shootThread.Start();
    }

    try
    {
        string jsonToSend = JsonConvert.SerializeObject(entities);
        await client.EmitAsync("players", jsonToSend);
    }
    catch (Exception ex)
    {
        //Console.WriteLine("Error players : " + ex.Message);
    }

    try
    {

        //IntPtr mapNamePtr = swed.ReadPointer(clientPtr, (int)CS2Dumper.Offsets.ClientDll.dwGlobalVars);
        //Console.WriteLine($"mapNamePtr : {mapNamePtr}");
        //string mapName = swed.ReadString(mapNamePtr, (int)CS2Dumper.Offsets.MatchmakingDll.dwGameTypes_mapName, 64);

        //IntPtr game_client_ptr = engine2Ptr + CS2Dumper.Offsets.Engine2Dll.dwNetworkGameClient;
        //Console.WriteLine($"game_client_ptr : {game_client_ptr}");

        ////string mapNameA = swed.ReadString(matchmakingPtr + CS2Dumper.Offsets.MatchmakingDll.dwGameTypes + CS2Dumper.Offsets.MatchmakingDll.dwGameTypes_mapName, 8);
        ////string mapNameB = swed.ReadString(matchmakingPtr + CS2Dumper.Offsets.MatchmakingDll.dwGameTypes_mapName, 8);

        //IntPtr mapNamePtr = swed.ReadPointer(matchmakingPtr, CS2Dumper.Offsets.MatchmakingDll.dwGameTypes, CS2Dumper.Offsets.MatchmakingDll.dwGameTypes_mapName);
        //Console.WriteLine($"mapNamePtr : {mapNamePtr}");

        //string mapName = swed.ReadString(mapNamePtr, 8);
        //Console.WriteLine($"mapName : {mapName}");

        //string jsonToSend = $"{{\"data\": \"{mapName}\" }}";
        string jsonToSend = $"{{\"data\": \"de_dust2\" }}";
        await client.EmitAsync("map", jsonToSend);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error map : " + ex.Message);

        if (!isConnecting)
        {
            isConnecting = true;
            Thread reconnectThread = new Thread(new ThreadStart(async () => {
                await client.DisconnectAsync();
                await client.ConnectAsync();
                isConnecting = false;
            }));
            reconnectThread.Start();
        }
    }

    Thread.Sleep((int)Math.Round(1000.0 / 60));
    //Thread.Sleep((int)Math.Round(5000.0));
}
