using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace bot
{
    
    class Program
    {
        static void Main(string[] args)
        {
            /*имя процесса игры*/
            String nameProcGame = "wow";
            D3D.findAddressAndOpcode();

            /** вывод адреса и опкодов **/
            System.Console.WriteLine("addr: " + D3D.Address.ToString("X8") + "\n");
            System.Console.WriteLine("opcodes: \n");
            for (int i = 0; i < D3D.OpCode.Length; i++)
            {
                System.Console.Write(D3D.OpCode[i].ToString("X2") + " ");
            }

            ProcessMemory.attachProcess(nameProcGame);
            if ((int)ProcessMemory.getHandle() == 0)
            {
                System.Console.WriteLine("\nProcess with name '" + nameProcGame + "'  not found");
                return;
            }

            HookManager.inject();
            
            /*вывод HookAddress*/
            System.Console.WriteLine("\nHookAddress: " + HookManager.getHookAddress().ToString("X8"));

			String command = "";
			while (command != "quit") 
			{
				System.Console.WriteLine("Input command ('quit - exit'): ");
				command = System.Console.ReadLine();
				Command.execute(command);
			}

        }
        
        
    }//end class Program
}
