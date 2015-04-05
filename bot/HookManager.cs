using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fasm;
using System.Threading;

namespace bot
{
    public static class  HookManager
    {
        public static IntPtr argumentAddress1;
        public static IntPtr argumentAddress2;
        public static int RandomOffset;
        private static Object Locker = new Object();
        private static uint bufferSize = 80;
        private static IntPtr HookAddress = IntPtr.Zero;


        public static IntPtr getHookAddress()
        {
            return HookAddress;
        }

        /*функция для внедрения в процесс игры asm кода и записи в функции DirectX перехода на этот код*/
        public static void inject()
        {
            /*получение адреса и опкодов*/
            IntPtr Address = D3D.Address;
            byte[] OpCodes = D3D.OpCode;
 
            /*внедрение в процесс игры*/
                      
            Process.EnterDebugMode();
            var dwThreadId = Process.GetProcessById((int)ProcessMemory.getProcessId()).Threads[0].Id;
            var ThreadHandle = ProcessMemory.OpenThr((uint)dwThreadId);

            var rnd = new Random();
            RandomOffset = rnd.Next(0, 60);
            HookAddress = ProcessMemory.AllocateMemory((uint)(6000 + rnd.Next(1, 2000))) + RandomOffset;

            argumentAddress1 = ProcessMemory.AllocateMemory(80);
            ProcessMemory.WriteBytes(argumentAddress1, new byte[80]);
            argumentAddress2 = ProcessMemory.AllocateMemory(bufferSize);
            ProcessMemory.WriteBytes(argumentAddress2, new byte[80]);
            var resultAddress = ProcessMemory.AllocateMemory(4);
            ProcessMemory.Write(resultAddress, 0);

            /*asm код который внедряется в память wow процесса*/
            List<string> asmLine = new List<string> {
                                            "pushfd",
                                            "pushad",
                                            "mov edx, 0",
                                            "mov ecx, " + resultAddress,
                                            "mov [ecx], edx",
                                            "@loop:",
                                            "mov eax, [ecx]",
                                            "cmp eax, " + 80,
                                            "jae @end",
                                            "mov eax, " + argumentAddress1,
                                            "add eax, [ecx]",
                                            "mov eax, [eax]",
                                            "test eax, eax",
                                            "je @out",
                                            "call eax",
                                            "mov ecx, " + resultAddress,
                                            "mov edx, " + argumentAddress2,
                                            "add edx, [ecx]",
                                            "mov [edx], eax",
                                            "mov edx, " + argumentAddress1,
                                            "add edx, [ecx]",
                                            "mov eax, 0",
                                            "mov [edx], eax",
                                            "@out:",
                                            "mov eax, [ecx]",
                                            "add eax, 4",
                                            "mov [ecx], eax",
                                            "jmp @loop",
                                            "@end:",
                                            "popad",
                                            "popfd"
                                        };
            ProcessMemory.Asm = new ManagedFasm(ProcessMemory.getHandle());
            ProcessMemory.Asm.Clear();
            foreach (var str in ObfuscateAsm(asmLine))
            {
                ProcessMemory.Asm.AddLine(str);
            }

            ProcessMemory.Asm.Inject((uint)HookAddress);
            var length = (uint)ProcessMemory.Asm.Assemble().Length;
            ProcessMemory.WriteBytes((IntPtr)((uint)HookAddress + length), OpCodes);
            ProcessMemory.Asm.Clear();
            ProcessMemory.Asm.AddLine("jmp " + (Address + OpCodes.Length));
            ProcessMemory.Asm.Inject((uint)(((uint)HookAddress + length) + OpCodes.Length));
            ProcessMemory.Asm.Clear();

            /*вставка перехода в начало функции в DirectX на наш Hook*/
            ProcessMemory.Asm.AddLine("jmp " + HookAddress);
            for (var k = 0; k <= ((OpCodes.Length - 5) - 1); k++)
            {
                ProcessMemory.Asm.AddLine("nop");
            }
            ProcessMemory.Asm.Inject((uint)Address);

        }//end inject

        /*функция для внедрения и вызова asm кода для вызова функций (непотокобезопасная, без обфускации) wow*/
        public static byte[] InjectAndExecuteOld(IEnumerable<string> asm, bool returnValue = false, int returnLength = 0)
        {
            ProcessMemory.Asm.Clear();
            foreach (var str in asm)
            {
                ProcessMemory.Asm.AddLine(str);
            }
            IntPtr dwAddress = ProcessMemory.AllocateMemory((uint)(ProcessMemory.Asm.Assemble().Length + 60));
            ProcessMemory.Asm.Inject((uint)dwAddress);
            ProcessMemory.Write(argumentAddress1, (int)dwAddress);
            while (ProcessMemory.Read(argumentAddress1) > 0)
            {
                Thread.Sleep(1);
            }
            byte[] result = new byte[0];
            if (returnValue)
            {
                result = ProcessMemory.ReadBytes((IntPtr)ProcessMemory.Read(argumentAddress2), (uint)returnLength);
            }
            ProcessMemory.Write(argumentAddress2, 0);
            ProcessMemory.FreeMemory(dwAddress);

            return result;
        }

        /*функция для внедрения и вызова asm кода для вызова функций wow (потокобезопасная, с обфускацией)*/
        public static byte[] InjectAndExecute(IEnumerable<string> asm, bool returnValue = false, int returnLength = 0)
        {
			var rnd = new Random();
            var offset = 0;
            IntPtr dwAddress;
            uint randomValue;


            lock (Locker)
            {
                offset = 0;
                randomValue = (uint)rnd.Next(0, 60);
                //Наша очередь может хранить 80/4 = 20 значений
                while (ProcessMemory.Read(argumentAddress1 + offset) != 0 || ProcessMemory.Read(argumentAddress2 + offset) != 0)
                {
                    offset += 4;
                    if (offset >= 80)
                    {
                        offset = 0;
                    }
                }
                ProcessMemory.Asm.Clear();
                foreach (var str in asm)
                {
                    for (var i = rnd.Next(0, 3); i >= 1; i--)
                    {
                        ProcessMemory.Asm.AddLine(GetFakeCommand());
                    }
                    ProcessMemory.Asm.AddLine(str);
                }
                dwAddress = (IntPtr)((uint)ProcessMemory.AllocateMemory((uint)(ProcessMemory.Asm.Assemble().Length + rnd.Next(60, 80))) + randomValue);
				System.Console.WriteLine("InjectAndExecute Address: " + dwAddress.ToString("X8") );
				ProcessMemory.Asm.Inject((uint)dwAddress);
                ProcessMemory.Write(argumentAddress1 + offset, (int)dwAddress );
            }
            while (ProcessMemory.Read(argumentAddress1 + offset) > 0)
            {
                Thread.Sleep(1);
            }
            byte[] result = new byte[0];
            if (returnValue)
            {
                result = ProcessMemory.ReadBytes((IntPtr)ProcessMemory.Read(argumentAddress2 + offset), (uint)returnLength);
            }
            ProcessMemory.Write(argumentAddress2 + offset, 0);
            ProcessMemory.FreeMemory((IntPtr)((uint)dwAddress - randomValue));

            return result;
        }

        /**функция выдающая строку со случайной фейковой asm командой**/
        internal static string GetFakeCommand()
        {
            var list = new List<string> { "mov edx, edx", "mov edi, edi", "xchg ebp, ebp", "mov esp, esp", "xchg esp, esp", "xchg edx, edx", "mov edi, edi" };
            var rnd = new Random();
            int num = rnd.Next(0, list.Count - 1);
            return list[num];
        }

        /**функция для добавления в список asm команд фейковых команд**/
        internal static IEnumerable<string> ObfuscateAsm(IList<string> asmLines)
        {
            var rnd = new Random();
            for (var i = asmLines.Count - 1; i >= 0; i--)
            {
                for (var k = rnd.Next(1, 4); k >= 1; k--)
                {
                    asmLines.Insert(i, GetFakeCommand());
                }
            }
            for (var j = rnd.Next(1, 4); j >= 1; j--)
            {
                asmLines.Add(GetFakeCommand());
            }
            return asmLines;
        }

    }
}
