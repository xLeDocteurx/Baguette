using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace baguette
{
    internal static class Offsets
    {
        /* Offsets.cs */
        public static readonly int dwLocalPlayerPawn = 0x1834B18;
        public static readonly int dwEntityList = 0x19CFC48;
        public static readonly int dwViewMatrix = 0x1A31D30;
        public static readonly int dwWeaponC4 = 0x19D2D60;

        /* client.dll.cs */
        public static readonly int m_vOldOrigin = 0x1324; // Vector
        public static readonly int m_iTeamNum = 0x3E3; // uint8
        public static readonly int m_lifeState = 0x348; // uint8
        public static readonly int m_iHealth = 0x344; // int32
        public static readonly int m_ArmorValue = 0x2404; // int32
        public static readonly int m_hPlayerPawn = 0x80C; // CHandle<C_CSPlayerPawn>
        public static readonly int m_sSanitizedPlayerName = 0x770; // CUtlString
        public static readonly int m_vecViewOffset = 0xCB0; // CNetworkViewOffsetVector

        public static readonly int m_modelState = 0x170; // CModelState
        public static readonly int m_pGameSceneNode = 0x328; // CGameSceneNode*

        public static readonly int m_hOwnerEntity = 0x440; // CHandle<C_BaseEntity>
        public static readonly int m_pItemServices = 0x11B0; // CPlayer_ItemServices*
        public static readonly int m_bHasDefuser = 0x40; // bool
        public static readonly int m_bHasHelmet = 0x41; // bool
        public static readonly int m_bHasHeavyArmor = 0x42; // bool
    }
}
