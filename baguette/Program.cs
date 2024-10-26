using baguette;
using Swed64;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Xml.Linq;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("This is baguette !");
Console.WriteLine("Looking for cs2 process...");

while (Process.GetProcessesByName("cs2").Length == 0) {}
Swed swed = new Swed("cs2");

Console.WriteLine("CS2 found");

IntPtr clientPtr = swed.GetModuleBase("client.dll");
// Console.WriteLine("client.dll found");

Renderer renderer = new Renderer();
Thread rendererThread = new Thread(new ThreadStart(renderer.Start().Wait));
// renderer.initImageAssets();

rendererThread.Start();

Vector2 screenSize = renderer.screenSize;
List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();
Entity bomb = new Entity();

while (true)
{
    // Console.WriteLine($"------");
    entities.Clear();

    IntPtr entityListPtr = swed.ReadPointer(clientPtr, Offsets.dwEntityList);
    IntPtr listEntryPtr = swed.ReadPointer(entityListPtr, 0x10);
    IntPtr localPlayerPawnPtr = swed.ReadPointer(clientPtr, Offsets.dwLocalPlayerPawn);

    ViewMatrix viewMatrix = Renderer.ReadMatrix(clientPtr + Offsets.dwViewMatrix, swed);

    IntPtr c4Entity = swed.ReadPointer(swed.ReadPointer(clientPtr, Offsets.dwWeaponC4));
    IntPtr bombOwnerPtr = swed.ReadPointer(c4Entity, Offsets.m_hOwnerEntity);

    for (int i = 0; i < 64; i++)
    {
        IntPtr currentControllerPtr = swed.ReadPointer(listEntryPtr, i * 0x78);
        if(currentControllerPtr == IntPtr.Zero)
        {
            continue;
        }

        int pawnHandle = swed.ReadInt(currentControllerPtr, Offsets.m_hPlayerPawn);
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

        int lifeState = swed.ReadInt(entryPlayerPawn, Offsets.m_lifeState);
        if (lifeState != 256)
        {
            continue;
        }

        IntPtr sceneNodePtr = swed.ReadPointer(entryPlayerPawn, Offsets.m_pGameSceneNode);
        if (sceneNodePtr == IntPtr.Zero)
        {
            continue;
        }

        // IntPtr boneMatrixPtr = swed.ReadPointer(sceneNodePtr, m_modelState, + 0x80); // 0x80 would be dwBoneMatrix
        IntPtr boneMatrixPtr = swed.ReadPointer(sceneNodePtr, Offsets.m_modelState + 0x80); // 0x80 would be dwBoneMatrix
        if (boneMatrixPtr == IntPtr.Zero)
        {
            continue;
        }

        // Populate entity
        Entity entity = new Entity();
        entity.Team = swed.ReadInt(entryPlayerPawn, Offsets.m_iTeamNum);

        IntPtr entryItemServicesPtr = swed.ReadPointer(entryPlayerPawn, Offsets.m_pItemServices);
        entity.hasDiffuser = swed.ReadBool(entryItemServicesPtr, Offsets.m_bHasDefuser);
        entity.hasArmor = swed.ReadBool(entryItemServicesPtr, Offsets.m_bHasHeavyArmor);
        entity.hasHelmet = swed.ReadBool(entryItemServicesPtr, Offsets.m_bHasHelmet);

        entity.hasBomb = (int)bombOwnerPtr == pawnHandle;

        entity.Name = swed.ReadString(currentControllerPtr, Offsets.m_sSanitizedPlayerName);
        entity.Health = swed.ReadInt(entryPlayerPawn, Offsets.m_iHealth);
        entity.Armor = swed.ReadInt(entryPlayerPawn, Offsets.m_ArmorValue);
        entity.PositionV3 = swed.ReadVec(entryPlayerPawn, Offsets.m_vOldOrigin);
        entity.ViewOffsetV3 = swed.ReadVec(entryPlayerPawn, Offsets.m_vecViewOffset);
        entity.PositionV2 = Renderer.WorldToScreen(viewMatrix, entity.PositionV3, screenSize);
        entity.ViewOffsetV2 = Renderer.WorldToScreen(viewMatrix, Vector3.Add(entity.PositionV3, entity.ViewOffsetV3), screenSize);

        entity.Distance = Vector3.Distance(entity.PositionV3, localPlayer.PositionV3);
        
        entity.bones3D = Renderer.ReadBones(boneMatrixPtr, swed);
        entity.bones2D = Renderer.ReadBones2D(entity.bones3D, viewMatrix, screenSize);

        entities.Add(entity);
    }

    localPlayer.Team = swed.ReadInt(localPlayerPawnPtr, Offsets.m_iTeamNum);
    localPlayer.Health = swed.ReadInt(localPlayerPawnPtr, Offsets.m_iHealth);
    localPlayer.Armor = swed.ReadInt(localPlayerPawnPtr, Offsets.m_ArmorValue);

    localPlayer.PositionV3 = swed.ReadVec(localPlayerPawnPtr, Offsets.m_vOldOrigin);
    localPlayer.ViewOffsetV3 = swed.ReadVec(localPlayerPawnPtr, Offsets.m_vecViewOffset);
    localPlayer.PositionV2 = Renderer.WorldToScreen(viewMatrix, localPlayer.PositionV3, screenSize);
    localPlayer.ViewOffsetV2 = Renderer.WorldToScreen(viewMatrix, Vector3.Add(localPlayer.PositionV3, localPlayer.ViewOffsetV3), screenSize);

    renderer.UpdateLocalEntities(entities);
    renderer.UpdateLocalPlayer(localPlayer);

    // Thread.Sleep(500);
    Thread.Sleep(1);
}