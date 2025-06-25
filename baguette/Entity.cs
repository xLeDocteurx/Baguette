using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace baguette
{
    public enum BoneIds
    {
        Waist = 0, // 0
        Neck = 5, // 1
        Head = 6, // 2
        ShoulderLeft = 8, // 3
        ForeLeft = 9, // 4
        HandLeft = 11, // 5
        ShoulderRight = 13, // 6
        ForeRight = 14, // 7
        HandRight = 16, // 8
        KneeLeft = 23, // 9
        FeetLeft = 24, // 10
        KneeRight = 26, // 11
        FeetRight = 27, // 12
    }

    public class Entity
    {
        /*public bool IsAlive { get; set; }*/
        public int Team { get; set; }
        public bool hasBomb { get; set; }
        public bool hasDiffuser { get; set; }
        public bool hasArmor { get; set; }
        public bool hasHelmet{ get; set; }
        public string Name { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public float Distance { get; set; }

        public float Angle { get; set; }
        public Vector3 PositionV3 { get; set; }
        public Vector3 ViewOffsetV3 { get; set; }
        public Vector2 PositionV2 { get; set; }
        public Vector2 ViewOffsetV2 { get; set; }

        public List<Vector3> bones3D { get; set; }
        public List<Vector2> bones2D { get; set; }
    }
}
