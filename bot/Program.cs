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
            ProcessMemory wowProc = new ProcessMemory(nameProcGame);
            if ((int)wowProc.getHandle() == 0)
            {
                System.Console.WriteLine("\nProcess with name '" + nameProcGame + "'  not found");
                return;
            }

            HookManager hm = new HookManager(wowProc, new D3D());
            hm.inject();
            
            /*вывод HookAddress*/
            System.Console.WriteLine("\nHookAddress: " + hm.getHookAddress().ToString("X8"));

        }
        
        
    }//end class Program
}
