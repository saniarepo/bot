using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
//using System.Diagnostics.Process;



namespace bot
{
    
    class Program
    {
        
        static void Main(string[] args)
        {
            uint addr;
            byte[] opcode = null; ;
            D3D d3d = new D3D();
            addr = d3d.getAddressD3D9();
            if (addr == 0)
            {
                addr = d3d.getAddressD3D11();
                if (addr == 0)
                {
                    System.Console.WriteLine("No address found");
                    return;
                }
                else 
                {
                    opcode = d3d.getOpcodeD3D11(addr);
                }
            }
            else 
            {
                opcode = d3d.getOpcodeD3D9(addr);
            }

            System.Console.WriteLine("addr: " + addr.ToString("X8")+ "\n");
            System.Console.WriteLine("opcodes: \n");
            for (int i = 0; i < opcode.Length; i++) 
            {
                System.Console.Write(opcode[i].ToString("X2")+ " ");
            }

        }

        
    }//end class Program
}
