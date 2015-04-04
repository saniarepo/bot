using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Fasm;


namespace bot
{
    public static class ProcessMemory
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);
 
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr handle, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out  int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAcces,
            bool bInheritHandle,
            uint dwThreadId
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
           uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
           int dwSize, FreeType dwFreeType);
        
        [Flags]

        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200),
            TO_INJECT = (0x1F03FF)
        }

        [Flags]
        public enum FreeType
        {
            Decommit = 0x4000,
            Release = 0x8000,
        }

        public static T BuffToStruct<T>(byte[] arr) where T : struct
        {

            GCHandle gch = GCHandle.Alloc(arr, GCHandleType.Pinned); // зафиксировать в памяти
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0); // и взять его адрес
            T ret = (T)Marshal.PtrToStructure(ptr, typeof(T)); // создать структуру
            gch.Free(); // снять фиксацию
            return ret;

        }

        public static byte[] StructToBuff<T>(T value) where T : struct
        {

            byte[] arr = new byte[Marshal.SizeOf(value)]; // создать массив
            GCHandle gch = GCHandle.Alloc(arr, GCHandleType.Pinned); // зафиксировать в памяти
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0); // и взять его адрес
            Marshal.StructureToPtr(value, ptr, true); // копировать в массив
            gch.Free(); // снять фиксацию
            return arr;

        }

        private static IntPtr handle;
        private static uint processId;
        public static ManagedFasm Asm;
        public static IntPtr ImageBase = (IntPtr)0x400000;

        
        public static void attachProcess(uint procId) 
        {
            try
            {
                processId = procId;
                handle = OpenProcess(ProcessAccessFlags.All, false, processId);
            }
            catch (Exception e) {
                handle = (IntPtr)0;
            }
            
        }

        public static void attachProcess(String ProcessName) 
        {
            try
            {
                processId = (uint)Process.GetProcessesByName(ProcessName)[0].Id;       
                handle = OpenProcess(ProcessAccessFlags.All, false, processId);
            }
            catch (Exception e) {
                handle = (IntPtr)0;
            }           
        }

        public static void deattach() 
        {
            CloseHandle(handle);
        }

        
        public static byte[] ReadBytes( IntPtr address, uint length )
        {
            if (handle == (IntPtr)0) 
            {   
                return null; 
            }
            byte[] buffer = new byte[length];
            int bytesRead;
            ReadProcessMemory(handle, address, buffer, length, out bytesRead);
            return buffer;
        }

        public static uint Read( IntPtr address )
        {
            if (handle == (IntPtr)0) 
            { 
                return 0; 
            }
            uint length = 4;
            byte[] buffer = new byte[4];
            int bytesRead;
            
            ReadProcessMemory(handle, address, buffer, length, out bytesRead);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static ulong Read(IntPtr address, bool size64)
        {
            if (handle == (IntPtr)0)
            {
                return 0;
            }
            
            uint length = (uint)8;
            byte[] buffer = new byte[length];
            int bytesRead;

            ReadProcessMemory(handle, address, buffer, length, out bytesRead);
            return BitConverter.ToUInt64(buffer, 0);
        }

        
        public static T ReadStruct<T>( IntPtr address ) where T : struct
        {
            if (handle == (IntPtr)0) 
            { 
                return default(T); 
            }
            uint length = 4;
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            int bytesRead;
            
            ReadProcessMemory(handle, address, buffer, length, out bytesRead);
            return BuffToStruct<T>(buffer);
        }


        public static bool WriteBytes(IntPtr address, byte[] buffer)
        {
            if (handle == (IntPtr)0)
            {
                return false;
            }
            int bytesWritten;
            bool worked = WriteProcessMemory(handle, address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }

        public static bool Write(IntPtr address, int value)
        {
            if (handle == (IntPtr)0)
            {
                return false;
            }
            byte[] buffer = BitConverter.GetBytes(value);

            int bytesWritten;
            bool worked = WriteProcessMemory(handle, address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }

        public static bool Write(IntPtr address, ulong value)
        {
            if (handle == (IntPtr)0)
            {
                return false;
            }
            byte[] buffer = BitConverter.GetBytes(value);

            int bytesWritten;
            bool worked = WriteProcessMemory(handle, address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }

        public static bool Write(IntPtr address, float value)
        {
            if (handle == (IntPtr)0)
            {
                return false;
            }
            byte[] buffer = BitConverter.GetBytes(value);

            int bytesWritten;
            bool worked = WriteProcessMemory(handle, address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }

        public static bool WriteStruct<T>(IntPtr address, T value) where T : struct
        {
            if (handle == (IntPtr)0)
            {
                return false;
            }
            
            byte[] buffer = StructToBuff<T>(value);

            int bytesWritten;
            bool worked = WriteProcessMemory(handle, address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }

        public static IntPtr AllocateMemory(uint length)
        {
            IntPtr addr = VirtualAllocEx(handle, IntPtr.Zero, length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            return addr;
        }

        public static IntPtr OpenThr( uint dwThreadId)
        {
            return OpenThread(ThreadAccess.TO_INJECT, false, (uint)dwThreadId);
        }

        public static bool FreeMemory(IntPtr address)
        {
            return VirtualFreeEx(handle, address, 0, FreeType.Release );
        }

        public static IntPtr getHandle()
        {
            return handle;
        }

        public static uint getProcessId()
        {
            return processId;
        }

        public static IntPtr GetAbsolute(uint offset)
        {
            return (IntPtr)((uint)ImageBase + offset);
        }
        
    }
}
