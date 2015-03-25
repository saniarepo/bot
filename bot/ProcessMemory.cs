using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace bot
{
    class ProcessMemory
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
 
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr handle, int lpBaseAddress, byte[] lpBuffer, int nSize, out  int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hProcess);

        private IntPtr handle;

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
        
        public ProcessMemory(int ProcessId) 
        {
            try
            {
                this.handle = OpenProcess(ProcessAccessFlags.All, false, ProcessId);
            }
            catch (Exception e) {
                this.handle = (IntPtr)0;
            }
            
        }

        public ProcessMemory(String ProcessName) 
        {
            int ProcessId = Process.GetProcessesByName(ProcessName)[0].Id;
            try
            {
                this.handle = OpenProcess(ProcessAccessFlags.All, false, ProcessId);
            }
            catch (Exception e) {
                this.handle = (IntPtr)0;
            }           
        }

        ~ProcessMemory() 
        {
            CloseHandle(this.handle);
        }

        
        public byte[] ReadBytes( int address, int length )
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

        public uint Read( int address )
        {
            if (this.handle == (IntPtr)0) 
            { 
                return 0; 
            }
            int length = 4;
            byte[] buffer = new byte[4];
            int bytesRead;
            
            ReadProcessMemory(this.handle, address, buffer, length, out bytesRead);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public bool WriteBytes(int address, byte[] buffer, out int bytesWritten)
        {
            if (this.handle == (IntPtr)0)
            {
                bytesWritten = 0;
                return false;
            }
            bool worked = WriteProcessMemory(this.handle, (IntPtr)address, buffer, (uint)buffer.Length, out bytesWritten);
            return worked;
        }



    }
}
