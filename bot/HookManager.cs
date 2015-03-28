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
    class HookManager
    {
        public ProcessMemory processMemory = null;
        private D3D d3d = null;
        public IntPtr argumentAddress1;
        public IntPtr argumentAddress2;
        public int RandomOffset;
        private Object Locker = new Object();
        private uint bufferSize = 80;
        private IntPtr HookAddress = IntPtr.Zero;

        public HookManager(ProcessMemory processMemory, D3D d3d)
        {
            this.processMemory = processMemory;
            this.d3d = d3d;
        }

        public IntPtr getHookAddress()
        {
            return this.HookAddress;
        }

        /*функция для внедрения в процесс игры asm кода и записи в функции DirectX перехода на этот код*/
        public void inject()
        {
            /*получение адреса и опкодов*/
            IntPtr Address = d3d.getAddress();
            byte[] OpCodes = d3d.getOpcode();

            /** вывод адреса и опкодов **/
            System.Console.WriteLine("addr: " + Address.ToString("X8") + "\n");
            System.Console.WriteLine("opcodes: \n");
            for (int i = 0; i < OpCodes.Length; i++)
            {
                System.Console.Write(OpCodes[i].ToString("X2") + " ");
            }

            
            /*внедрение в процесс игры*/
                      
            Process.EnterDebugMode();
            var dwThreadId = Process.GetProcessById(this.processMemory.getProcessId()).Threads[0].Id;
            var ThreadHandle = processMemory.OpenThr((uint)dwThreadId);

            var rnd = new Random();
            RandomOffset = rnd.Next(0, 60);
            HookAddress = processMemory.AllocateMemory((uint)(6000 + rnd.Next(1, 2000))) + RandomOffset;

            argumentAddress1 = processMemory.AllocateMemory(80);
            processMemory.WriteBytes(argumentAddress1, new byte[80]);
            argumentAddress2 = processMemory.AllocateMemory(bufferSize);
            processMemory.WriteBytes(argumentAddress2, new byte[80]);
            var resultAddress = processMemory.AllocateMemory(4);
            processMemory.Write(resultAddress, 0);

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
            processMemory.Asm = new ManagedFasm(processMemory.getHandle());
            processMemory.Asm.Clear();
            foreach (var str in ObfuscateAsm(asmLine))
            {
                processMemory.Asm.AddLine(str);
            }

            processMemory.Asm.Inject((uint)HookAddress);
            var length = (uint)processMemory.Asm.Assemble().Length;
            processMemory.WriteBytes((IntPtr)((uint)HookAddress + length), OpCodes);
            processMemory.Asm.Clear();
            processMemory.Asm.AddLine("jmp " + (Address + OpCodes.Length));
            processMemory.Asm.Inject((uint)(((uint)HookAddress + length) + OpCodes.Length));
            processMemory.Asm.Clear();

            /*вставка перехода в начало функции в DirectX на наш Hook*/
            processMemory.Asm.AddLine("jmp " + HookAddress);
            for (var k = 0; k <= ((OpCodes.Length - 5) - 1); k++)
            {
                processMemory.Asm.AddLine("nop");
            }
            processMemory.Asm.Inject((uint)Address);

        }//end inject

        /*функция для внедрения и вызова asm кода для вызова функций (непотокобезопасная, без обфускации) wow*/
        public byte[] InjectAndExecuteOld(IEnumerable<string> asm, bool returnValue = false, int returnLength = 0)
        {
            processMemory.Asm.Clear();
            foreach (var str in asm)
            {
                processMemory.Asm.AddLine(str);
            }
            IntPtr dwAddress = processMemory.AllocateMemory((uint)(processMemory.Asm.Assemble().Length + 60));
            processMemory.Asm.Inject((uint)dwAddress);
            processMemory.Write(argumentAddress1, (int)dwAddress);
            while (processMemory.Read(argumentAddress1) > 0)
            {
                Thread.Sleep(1);
            }
            byte[] result = new byte[0];
            if (returnValue)
            {
                result = processMemory.ReadBytes((IntPtr)processMemory.Read(argumentAddress2), (uint)returnLength);
            }
            processMemory.Write(argumentAddress2, 0);
            processMemory.FreeMemory(dwAddress);

            return result;
        }

        /*функция для внедрения и вызова asm кода для вызова функций wow (потокобезопасная, с обфускацией)*/
        public byte[] InjectAndExecute(IEnumerable<string> asm, bool returnValue = false, int returnLength = 0)
        {
            var rnd = new Random();
            var offset = 0;
            uint dwAddress;
            uint randomValue;


            lock (Locker)
            {
                offset = 0;
                randomValue = (uint)rnd.Next(0, 60);
                //Наша очередь может хранить 80/4 = 20 значений
                while (processMemory.Read(argumentAddress1 + offset) != 0 || processMemory.Read(argumentAddress2 + offset) != 0)
                {
                    offset += 4;
                    if (offset >= 80)
                    {
                        offset = 0;
                    }
                }
                processMemory.Asm.Clear();
                foreach (var str in asm)
                {
                    for (var i = rnd.Next(0, 3); i >= 1; i--)
                    {
                        processMemory.Asm.AddLine(GetFakeCommand());
                    }
                    processMemory.Asm.AddLine(str);
                }
                dwAddress = (uint)processMemory.AllocateMemory((uint)(processMemory.Asm.Assemble().Length + rnd.Next(60, 80))) + randomValue;
                processMemory.Asm.Inject(dwAddress);
                processMemory.Write((IntPtr)argumentAddress1, (int)(dwAddress + offset));
            }
            while (processMemory.Read(argumentAddress1 + offset) > 0)
            {
                Thread.Sleep(1);
            }
            byte[] result = new byte[0];
            if (returnValue)
            {
                result = processMemory.ReadBytes((IntPtr)processMemory.Read(argumentAddress2 + offset), (uint)returnLength);
            }
            processMemory.Write(argumentAddress2 + offset, 0);
            processMemory.FreeMemory((IntPtr)(dwAddress - randomValue));

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
