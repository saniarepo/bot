using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace bot
{
    [StructLayout(LayoutKind.Sequential)]
    struct TSExplicitList // 12
    {
        public TSList baseClass; // 12
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TSList // 12
    {
        public int m_linkoffset; // 4
        public TSLink m_terminator; // 8
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TSLink // 8
    {
        public IntPtr m_prevlink; //TSLink *m_prevlink // 4
        public IntPtr m_next; // C_OBJECTHASH *m_next // 4
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TSHashTable // 44
    {
        public IntPtr vtable; // 4
        public TSExplicitList m_fulllist; // 12
        public int m_fullnessIndicator; // 4
        public TSGrowableArray m_slotlistarray; // 20
        public int m_slotmask; // 4
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TSBaseArray // 16
    {
        public IntPtr vtable; // 4
        public uint m_alloc; // 4
        public uint m_count; // 4
        public IntPtr m_data;//TSExplicitList* m_data; // 4
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TSFixedArray // 16
    {
        public TSBaseArray baseClass; // 16
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TSGrowableArray // 20
    {
        public TSFixedArray baseclass; // 16
        public uint m_chunk; // 4
    }
    [StructLayout(LayoutKind.Sequential)]
    struct CurMgr // 248 bytes x86, 456 bytes x64
    {
        public TSHashTable VisibleObjects; // m_objects 44
        public TSHashTable LazyCleanupObjects; // m_lazyCleanupObjects 44
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        // m_lazyCleanupFifo, m_freeObjects, m_visibleObjects, m_reenabledObjects, whateverObjects...
        public TSExplicitList[] Links; // Links[10] has all objects stored in VisibleObjects it seems 12 * 11 = 132
#if !X64
        public int Unknown1; // wtf is that and why x86 only? // 4
        public int Unknown2; // not sure if this actually reflects the new object manager structure, but it does get the rest of the struct aligned correctly
        public int Unknown3; // not sure if this actually reflects the new object manager structure, but it does get the rest of the struct aligned correctly
#endif
        public ulong ActivePlayer; // 8
        public int PlayerType; // 4
        public int MapId; // 4
        public IntPtr ClientConnection; // 4
        public IntPtr MovementGlobals; // 4
    }

    public enum ObjectManagerAddr
    {
        connection = 0xF24CF0,
        objectManager = 0x62C,
    }

    public class ObjectManager : IEnumerable
    {
        private CurMgr _curMgr;
        private IntPtr _baseAddress;
        private WoWGuid _activePlayer;
        private WoWPlayer _activePlayerObj;
        private ProcessMemory processMemory = null;
        private static List<WowObject> listObjects;

        public void UpdateBaseAddress()
        {
            var connection = (IntPtr)this.processMemory.Read((IntPtr)ObjectManagerAddr.connection, true);
            _baseAddress = (IntPtr)this.processMemory.Read(connection + (int)ObjectManagerAddr.objectManager);
        }

        public void setProcessMemory(ProcessMemory processMemory)
        {
            this.processMemory = processMemory;
        }
        
        
        private IntPtr BaseAddress
        {
            get { return _baseAddress; }
        }

        public WoWGuid ActivePlayer
        {
            get { return _activePlayer; }
        }

        public WoWPlayer ActivePlayerObj
        {
            get { return _activePlayerObj; }
        }

        public IntPtr ClientConnection
        {
            get { return _curMgr.ClientConnection; }
        }

        public IntPtr FirstObject()
        {
            return _curMgr.VisibleObjects.m_fulllist.baseClass.m_terminator.m_next;
        }

        public IntPtr NextObject(IntPtr current)
        {
            return (IntPtr)this.processMemory.Read(current + _curMgr.VisibleObjects.m_fulllist.baseClass.m_linkoffset + IntPtr.Size);
        }

        public IEnumerable GetObjects()
        {
            _curMgr = this.processMemory.ReadStruct<CurMgr>(BaseAddress);
            _activePlayer = new WoWGuid();
            IntPtr first = FirstObject();
            listObjects = new List<WowObject>();
            while (((first.ToInt64() & 1) == 0) && first != IntPtr.Zero)
            {
                var wowObject = new WowObject(first);
                if (wowObject.ObjectData.Guid == _curMgr.ActivePlayer)
                {
                    _activePlayerObj = new WoWPlayer(first);
                }
                listObjects.Add(wowObject);
                first = NextObject(first);
            }
            return listObjects;
        }

        public IEnumerator GetEnumerator()
        {
            return (IEnumerator<WowObject>)GetObjects().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public static WowObject GetObjectByGUID(ulong guid)
        {
            foreach (WowObject wowObject in listObjects)
            {
                if (wowObject.getCuid() == guid) return wowObject;
            }
            return null;
        }
    }
}
