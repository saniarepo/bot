using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace bot
{
    public class WowObject
    {
        [StructLayout(LayoutKind.Sequential)]
        struct WowObjStruct
        {
            IntPtr vtable;              // 0x00
            public IntPtr Descriptors;  // 0x4
            IntPtr unk1;                // 0x8
            public int ObjectType;      // 0xC
            int unk3;                   // 0x10
            IntPtr unk4;                // 0x14
            IntPtr unk5;                // 0x18
            IntPtr unk6;                // 0x1C
            IntPtr unk7;                // 0x20
            IntPtr unk8;                // 0x24
            public ulong Guid;          // 0x28
        }

        public enum WoWObjectType : int
        {
            Object = 0,
            Item = 1,
            Container = 2,
            Unit = 3,
            Player = 4,
            GameObject = 5,
            DynamicObject = 6,
            Corpse = 7,
            AreaTrigger = 8,
            SceneObject = 9,
            NumClientObjectTypes = 0xA,
            None = 0x270f,
        }

        [Flags]
        public enum WoWObjectTypeFlags
        {
            Object = 1 << WoWObjectType.Object,
            Item = 1 << WoWObjectType.Item,
            Container = 1 << WoWObjectType.Container,
            Unit = 1 << WoWObjectType.Unit,
            Player = 1 << WoWObjectType.Player,
            GameObject = 1 << WoWObjectType.GameObject,
            DynamicObject = 1 << WoWObjectType.DynamicObject,
            Corpse = 1 << WoWObjectType.Corpse,
            AreaTrigger = 1 << WoWObjectType.AreaTrigger,
            SceneObject = 1 << WoWObjectType.SceneObject

        }

        public enum ObjectFields
        {
            Guid = 0,
            Data = 2,
            Type = 4,
            EntryId = 5,
            DynamicFlags = 6,
            Scale = 7,
            End = 8,
        }

        [Flags]
        public enum ObjectDynamicFlags : uint
        {
            Invisible = 1 << 0,
            Lootable = 1 << 1,
            TrackUnit = 1 << 2,
            TaggedByOther = 1 << 3,
            TaggedByMe = 1 << 4,
            Unknown = 1 << 5,
            Dead = 1 << 6,
            ReferAFriendLinked = 1 << 7,
            IsTappedByAllThreatList = 1 << 8,
        }

        
        private IntPtr BaseAddress;
        private WowObjStruct ObjectData;
        private ProcessMemory processMemory = null;

        public WowObject(IntPtr address)
        {
            BaseAddress = address;
            ObjectData = processMemory.ReadStruct<WowObjStruct>(BaseAddress);
        }

        public void setProcessMemory(ProcessMemory processMemory)
        {
            this.processMemory = processMemory;
        }

        public bool IsValid { get { return BaseAddress != IntPtr.Zero; } }

        public T GetValue<T>(ObjectFields index) where T : struct
        {
            return processMemory.ReadStruct<T>(ObjectData.Descriptors + (int)index * IntPtr.Size);
        }

        public void SetValue<T>(ObjectFields index, T val) where T : struct
        {
            processMemory.WriteStruct<T>(ObjectData.Descriptors + (int)index * IntPtr.Size, val);
        }

        public bool IsA(WoWObjectTypeFlags flags)
        {
            return (GetValue<int>(ObjectFields.Type) & (int)flags) != 0;
        }

        public int Entry
        {
            get { return GetValue<int>(ObjectFields.EntryId); }
        }


    }
}
