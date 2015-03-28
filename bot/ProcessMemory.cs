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
    class ProcessMemory
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
 
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

        private IntPtr handle;
        private int processId;
        public ManagedFasm Asm;       

        
        public ProcessMemory(int processId) 
        {
            try
            {
                this.processId = processId;
                this.handle = OpenProcess(ProcessAccessFlags.All, false, processId);
            }
            catch (Exception e) {
                this.handle = (IntPtr)0;
            }
            
        }

        public ProcessMemory(String ProcessName) 
        {
            int processId;
            try
            {
                processId = Process.GetProcessesByName(ProcessName)[0].Id;
                this.processId = processId;
                this.handle = OpenProcess(ProcessAccessFlags.All, false, processId);
            }
            catch (Exception e) {
                this.handle = (IntPtr)0;
            }           
        }

        ~ProcessMemory() 
        {
            CloseHandle(this.handle);
        }

        
        public byte[] ReadBytes( IntPtr address, uint length )
        {
            if (this.handle == (IntPtr)0) 
            {   
                return null; 
            }
            byte[] buffer = new byte[length];
            int bytesRead;
            ReadProcessMemory(this.handle, address, buffer, length, out bytesRead);
            return buffer;
        }

        public uint Read( IntPtr address )
        {
            if (this.handle == (IntPtr)0) 
            { 
                return 0; 
            }
            uint length = 4;
            byte[] buffer = new byte[4];
            int bytesRead;
            
            ReadProcessMemory(this.handle, address, buffer, length, out bytesRead);
            return BitConverter.ToUInt32(buffer, 0);
        }

        
        public T ReadStruct<T>( IntPtr address ) where T : struct
        {
            if (this.handle == (IntPtr)0) 
            { 
                return default(T); 
            }
            uint length = 4;
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            int bytesRead;
            
            ReadProcessMemory(this.handle, address, buffer, length, out bytesRead);
            return BuffToStruct<T>(buffer);
        }


        public bool WriteBytes(IntPtr address, byte[] buffer)
        {
            if (this.handle == (IntPtr)0)
            {
                return false;
            }
            int bytesWritten;
            bool worked = WriteProcessMemory(this.handle, address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }

        public bool Write(IntPtr address, int value)
        {
            if (this.handle == (IntPtr)0)
            {
                return false;
            }
            byte[] buffer = BitConverter.GetBytes(value);

            int bytesWritten;
            bool worked = WriteProcessMemory(this.handle, address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }

        public bool WriteStruct<T>(IntPtr address, T value) where T : struct
        {
            if (this.handle == (IntPtr)0)
            {
                return false;
            }
            byte[] buffer = StructToBuff<T>(value);

            int bytesWritten;
            bool worked = WriteProcessMemory(this.handle, address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }

        public IntPtr AllocateMemory(uint length)
        {
            IntPtr addr = VirtualAllocEx(this.handle, IntPtr.Zero, length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            return addr;
        }

        public IntPtr OpenThr( uint dwThreadId)
        {
            return OpenThread(ThreadAccess.TO_INJECT, false, (uint)dwThreadId);
        }

        public bool FreeMemory(IntPtr address)
        {
            return VirtualFreeEx(this.handle, address, 0, FreeType.Release );
        }

        public IntPtr getHandle()
        {
            return this.handle;
        }

        public int getProcessId()
        {
            return this.processId;
        }
        
    }
}
