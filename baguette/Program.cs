using baguette;
using CS2Dumper;
using Swed64;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("This is baguette !");

Console.WriteLine("Looking for cs2 process...");

while (Process.GetProcessesByName("cs2").Length == 0) { }

Renderer renderer = new Renderer();
Thread rendererThread = new Thread(new ThreadStart(renderer.Start().Wait));

rendererThread.Start();
Swed swed = new Swed("cs2");

IntPtr clientPtr = swed.GetModuleBase("client.dll");

Console.WriteLine("CS2 found");

[DllImport("user32.dll")]
static extern short GetAsyncKeyState(int vKey); // Handle hotkey

[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);


Vector2 screenSize = renderer.screenSize;
List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();
Entity bomb = new Entity();

while (true)
{
    // Console.WriteLine($"------");
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

        entity.Name = swed.ReadString(currentControllerPtr, (int)CS2Dumper.Schemas.ClientDll.CCSPlayerController.m_sSanitizedPlayerName);
        entity.Health = swed.ReadInt(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_BaseEntity.m_iHealth);
        entity.Armor = swed.ReadInt(entryPlayerPawn, (int)CS2Dumper.Schemas.ClientDll.C_CSPlayerPawn.m_ArmorValue);
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

    renderer.UpdateLocalEntities(entities);
    renderer.UpdateLocalPlayer(localPlayer);

    // Console.Clear();

    // IntPtr xxx = swed.ReadPointer(clientPtr, Offsets.attack);
    int entIndex = swed.ReadInt(localPlayerPawnPtr, (int)CS2Dumper.Schemas.ClientDll.C_CSPlayerPawnBase.m_iIDEntIndex);
    // Console.WriteLine($"Crosshair/EntityID : {entIndex}");

    if (renderer.triggerBotEnabled && (GetAsyncKeyState(0x6) < 0 || renderer.triggerBotAutoModeEnabled) && entIndex > 0) // mouse 4 or 5
    {
        /*
        swed.WriteInt(clientPtr + Offsets.attack, 65537); // attack +
        Thread.Sleep(1);
        swed.WriteInt(clientPtr + Offsets.attack, 256); // attack -
        */

        // Simulate the mouse down and mouse up events
        mouse_event(0x0002, 0, 0, 0, 0); // Left down
        // Thread.Sleep(1);
        Thread.Sleep(250); // Optional: add a slight delay
        mouse_event(0x0004, 0, 0, 0, 0); // Left up
    }


    // Thread.Sleep(500);
    Thread.Sleep(1);
}