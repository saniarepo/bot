﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fasm;
using System.Threading;



namespace bot
{
    
    class Program
    {
        public static ProcessMemory procGameMemory = null;
        public static IntPtr argumentAddress1;
        public static IntPtr argumentAddress2;
        public static int RandomOffset;
        private Object Locker = new Object();

        static void Main(string[] args)
        {
            /*имя процесса игры*/
            String nameProcGame = "wow";

            uint bufferSize = 80;

            /*получение адреса и опкодов*/
            D3D d3d = new D3D();
            uint Address = d3d.getAddress();
            byte[] OpCodes = d3d.getOpcode();

            /** вывод адреса и опкодов **/
            System.Console.WriteLine("addr: " + Address.ToString("X8")+ "\n");
            System.Console.WriteLine("opcodes: \n");
            for (int i = 0; i < OpCodes.Length; i++) 
            {
                System.Console.Write(OpCodes[i].ToString("X2")+ " ");
            }

            /*внедрение в процесс игры*/
                      
            Process.EnterDebugMode();
            procGameMemory = new ProcessMemory(nameProcGame);
            if ((int)procGameMemory.getHandle() == 0) 
            {
                System.Console.WriteLine("\nProcess with name '" + nameProcGame + "'  not found");
                return;
            }
            var dwThreadId = Process.GetProcessesByName(nameProcGame)[0].Threads[0].Id;
            var ThreadHandle = procGameMemory.OpenThr((uint)dwThreadId);

            var rnd = new Random();
            RandomOffset = rnd.Next(0, 60);
            var HookAddress = procGameMemory.AllocateMemory((uint)(6000 + rnd.Next(1, 2000))) + RandomOffset;

            argumentAddress1 = procGameMemory.AllocateMemory(80);
            procGameMemory.WriteBytes(argumentAddress1, new byte[80]);
            argumentAddress2 = procGameMemory.AllocateMemory(bufferSize);
            procGameMemory.WriteBytes(argumentAddress2, new byte[80]);
            var resultAddress = procGameMemory.AllocateMemory(4);
            procGameMemory.Write(resultAddress, 0);

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

            
            procGameMemory.Asm = new ManagedFasm(procGameMemory.getHandle());
            procGameMemory.Asm.Clear();
            foreach (var str in ObfuscateAsm(asmLine))
            {
                procGameMemory.Asm.AddLine(str);
            }

            procGameMemory.Asm.Inject((uint)HookAddress);
            var length = (uint)procGameMemory.Asm.Assemble().Length;
            procGameMemory.WriteBytes((IntPtr)((uint)HookAddress + length), OpCodes);
            procGameMemory.Asm.Clear();
            procGameMemory.Asm.AddLine("jmp " + (Address + OpCodes.Length));
            procGameMemory.Asm.Inject((uint)(((uint)HookAddress + length) + OpCodes.Length));
            procGameMemory.Asm.Clear();
            
            /*вставка перехода в начало функции в DirectX на наш Hook*/
            procGameMemory.Asm.AddLine("jmp " + HookAddress);
            for (var k = 0; k <= ((OpCodes.Length - 5) - 1); k++)
            {
                procGameMemory.Asm.AddLine("nop");
            }
            procGameMemory.Asm.Inject(Address);
            
            /*вывод HookAddress*/
            System.Console.WriteLine("\nHookAddress: " + HookAddress.ToString("X8"));
            
        }

        /*функция для внедрения и вызова asm кода для вызова функций (непотокобезопасная, без обфускации) wow*/
        public byte[] InjectAndExecuteOld(IEnumerable<string> asm, bool returnValue = false, int returnLength = 0)
        {
            procGameMemory.Asm.Clear();
            foreach (var str in asm)
            {
                procGameMemory.Asm.AddLine(str);
            }
            IntPtr dwAddress = procGameMemory.AllocateMemory((uint)(procGameMemory.Asm.Assemble().Length + 60));
            procGameMemory.Asm.Inject((uint)dwAddress);
            procGameMemory.Write(argumentAddress1, (int)dwAddress);
            while (procGameMemory.Read((int)argumentAddress1) > 0)
            {
                Thread.Sleep(1);
            }
            byte[] result = new byte[0];
            if (returnValue)
            {
                result = procGameMemory.ReadBytes((int)procGameMemory.Read((int)argumentAddress2), (int)returnLength);
            }
            procGameMemory.Write(argumentAddress2, 0);
            procGameMemory.FreeMemory(dwAddress);

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
                while (procGameMemory.Read((int)(argumentAddress1 + offset)) != 0 || procGameMemory.Read((int)(argumentAddress2 + offset)) != 0)
                {
                    offset += 4;
                    if (offset >= 80)
                    {
                        offset = 0;
                    }
                }
                procGameMemory.Asm.Clear();
                foreach (var str in asm)
                {
                    for (var i = rnd.Next(0, 3); i >= 1; i--)
                    {
                        procGameMemory.Asm.AddLine(GetFakeCommand());
                    }
                    procGameMemory.Asm.AddLine(str);
                }
                dwAddress = (uint)procGameMemory.AllocateMemory((uint)(procGameMemory.Asm.Assemble().Length + rnd.Next(60, 80))) + randomValue;
                procGameMemory.Asm.Inject(dwAddress);
                procGameMemory.Write((IntPtr)argumentAddress1, (int)(dwAddress + offset));
            }
            while (procGameMemory.Read((int)(argumentAddress1 + offset)) > 0)
            {
                Thread.Sleep(1);
            }
            byte[] result = new byte[0];
            if (returnValue)
            {
                result = procGameMemory.ReadBytes((int)procGameMemory.Read((int)(argumentAddress2 + offset)), returnLength);
            }
            procGameMemory.Write(argumentAddress2 + offset, 0);
            procGameMemory.FreeMemory((IntPtr)(dwAddress - randomValue));

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

        
    }//end class Program
}
