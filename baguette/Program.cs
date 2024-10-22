using baguette;
using Swed64;
using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Swed swed = new Swed("cs2");
Console.WriteLine("CS2 found");

IntPtr clientPtr = swed.GetModuleBase("client.dll");
Console.WriteLine("client.dll found");

Renderer renderer = new Renderer();
renderer.initImageAssets();
Thread rendererThread = new Thread(new ThreadStart(renderer.Start().Wait));

rendererThread.Start();

Vector2 screenSize = renderer.screenSize;
List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();
Entity bomb = new Entity();

/* Offsets.cs */
int dwLocalPlayerPawn = 0x1831AE8;
int dwEntityList = 0x19CCAD8;
int dwViewMatrix = 0x1A2EC30;
int dwWeaponC4 = 0x19CFD60;

/* client.dll.cs */
int m_vOldOrigin = 0x1324; // Vector
int m_iTeamNum = 0x3E3; // uint8
int m_lifeState = 0x348; // uint8
int m_iHealth = 0x344; // int32
int m_hPlayerPawn = 0x80C; // CHandle<C_CSPlayerPawn>
int m_sSanitizedPlayerName = 0x770; // CUtlString
int m_vecViewOffset = 0xCB0; // CNetworkViewOffsetVector

int m_modelState = 0x170; // CModelState
int m_pGameSceneNode = 0x328; // CGameSceneNode*

int m_hOwnerEntity = 0x440; // CHandle<C_BaseEntity>
int m_pItemServices = 0x11B0; // CPlayer_ItemServices*
int m_bHasDefuser = 0x40; // bool
int m_bHasHelmet = 0x41; // bool
int m_bHasHeavyArmor = 0x42; // bool

while (true)
{
    // Console.WriteLine($"------");
    entities.Clear();

    IntPtr entityListPtr = swed.ReadPointer(clientPtr, dwEntityList);
    IntPtr listEntryPtr = swed.ReadPointer(entityListPtr, 0x10);
    IntPtr localPlayerPawnPtr = swed.ReadPointer(clientPtr, dwLocalPlayerPawn);

    ViewMatrix viewMatrix = Renderer.ReadMatrix(clientPtr + dwViewMatrix, swed);

    IntPtr c4Entity = swed.ReadPointer(swed.ReadPointer(clientPtr, dwWeaponC4));
    IntPtr bombOwnerPtr = swed.ReadPointer(c4Entity, m_hOwnerEntity);

    for (int i = 0; i < 64; i++)
    {
        IntPtr currentControllerPtr = swed.ReadPointer(listEntryPtr, i * 0x78);
        if(currentControllerPtr == IntPtr.Zero)
        {
            continue;
        }

        int pawnHandle = swed.ReadInt(currentControllerPtr, m_hPlayerPawn);
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

        int lifeState = swed.ReadInt(entryPlayerPawn, m_lifeState);
        if (lifeState != 256)
        {
            continue;
        }

        IntPtr sceneNodePtr = swed.ReadPointer(entryPlayerPawn, m_pGameSceneNode);
        if (sceneNodePtr == IntPtr.Zero)
        {
            continue;
        }

        IntPtr boneMatrixPtr = swed.ReadPointer(sceneNodePtr, m_modelState, + 0x80); // 0x80 would be dwBoneMatrix
        if (boneMatrixPtr == IntPtr.Zero)
        {
            continue;
        }

        // Populate entity
        Entity entity = new Entity();
        entity.Team = swed.ReadInt(entryPlayerPawn, m_iTeamNum);

        IntPtr entryItemServicesPtr = swed.ReadPointer(entryPlayerPawn, m_pItemServices);
        entity.hasDiffuser = swed.ReadBool(entryItemServicesPtr, m_bHasDefuser);
        entity.hasArmor = swed.ReadBool(entryItemServicesPtr, m_bHasHeavyArmor);
        entity.hasHelmet = swed.ReadBool(entryItemServicesPtr, m_bHasHelmet);

        entity.hasBomb = (int)bombOwnerPtr == pawnHandle;

        entity.Name = swed.ReadString(currentControllerPtr, m_sSanitizedPlayerName);
        entity.Health = swed.ReadInt(entryPlayerPawn, m_iHealth);
        entity.PositionV3 = swed.ReadVec(entryPlayerPawn, m_vOldOrigin);
        entity.ViewOffsetV3 = swed.ReadVec(entryPlayerPawn, m_vecViewOffset);
        entity.PositionV2 = Renderer.WorldToScreen(viewMatrix, entity.PositionV3, screenSize);
        entity.ViewOffsetV2 = Renderer.WorldToScreen(viewMatrix, Vector3.Add(entity.PositionV3, entity.ViewOffsetV3), screenSize);

        entity.Distance = Vector3.Distance(entity.PositionV3, localPlayer.PositionV3);
        
        entity.bones3D = Renderer.ReadBones(boneMatrixPtr, swed);
        entity.bones2D = Renderer.ReadBones2D(entity.bones3D, viewMatrix, screenSize);

        entities.Add(entity);
    }

    localPlayer.Team = swed.ReadInt(localPlayerPawnPtr, m_iTeamNum);
    localPlayer.Health = swed.ReadInt(localPlayerPawnPtr, m_iHealth);

    localPlayer.PositionV3 = swed.ReadVec(localPlayerPawnPtr, m_vOldOrigin);
    localPlayer.ViewOffsetV3 = swed.ReadVec(localPlayerPawnPtr, m_vecViewOffset);
    localPlayer.PositionV2 = Renderer.WorldToScreen(viewMatrix, localPlayer.PositionV3, screenSize);
    localPlayer.ViewOffsetV2 = Renderer.WorldToScreen(viewMatrix, Vector3.Add(localPlayer.PositionV3, localPlayer.ViewOffsetV3), screenSize);

    renderer.UpdateLocalEntities(entities);
    renderer.UpdateLocalPlayer(localPlayer);

    // Thread.Sleep(500);
    Thread.Sleep(1);
}