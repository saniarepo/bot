using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Direct3D11;
using SlimDX.DirectWrite;
using SlimDX.DXGI;
using SlimDX.Multimedia;
using SlimDX.Windows;
using SlimDX.XACT3;
using SlimDX.XAPO;
using System.Windows.Forms;

namespace bot
{
    public static class D3D
    {

        public static IntPtr Address = IntPtr.Zero;
        public static byte[] OpCode = null;
        
        public static IntPtr getAddressD3D9()
        {

            int offset = 0xa8;
            try
            {
                //Создаем устройство D3D9
                var device = new SlimDX.Direct3D9.Device(
                    new SlimDX.Direct3D9.Direct3D(),
                    0,
                    DeviceType.Hardware,
                    Process.GetCurrentProcess().MainWindowHandle,
                    CreateFlags.HardwareVertexProcessing,
                    new[] { new PresentParameters() });
                using (device)
                {
                    
                    //Считываем необходимый нам адрес расположения в памяти D3D9 функции по смещению 0xA8 от указателя на Com объект
                    IntPtr _D3D9Adress = (IntPtr)ProcessMemory.Read((IntPtr)ProcessMemory.Read((IntPtr)device.ComPointer + offset));
                    return _D3D9Adress;
                }

            }
            catch (Exception e)
            {
                return (IntPtr)0;
            }
        }

        public static IntPtr getAddressD3D11()
        {
            int offset = 0x20;
            try
            {
                //Создаем форму отрисовки для получения устройства D3D11
                    var renderForm = new RenderForm();
                    var description = new SwapChainDescription()
                    {
                        BufferCount = 1,
                        Flags = SwapChainFlags.None,
                        IsWindowed = true,
                        ModeDescription = new ModeDescription(100, 100, new Rational(60, 1), SlimDX.DXGI.Format.R8G8B8A8_UNorm),
                        OutputHandle = renderForm.Handle,
                        SampleDescription = new SampleDescription(1, 0),
                        SwapEffect = SlimDX.DXGI.SwapEffect.Discard,
                        Usage = SlimDX.DXGI.Usage.RenderTargetOutput
                    };
                    SlimDX.Direct3D11.Device device;
                    SlimDX.DXGI.SwapChain swapChain;
                    var result = SlimDX.Direct3D11.Device.CreateWithSwapChain(
                        DriverType.Hardware,
                        DeviceCreationFlags.None,
                        description,
                        //Здесь мы получаем устройство
                        out device,
                        out swapChain);
                    if (result.IsSuccess) using (device) using (swapChain)
                        {
                           
                            //Считываем наш SwapChain
                            IntPtr _D3D11Adress = (IntPtr)ProcessMemory.Read((IntPtr)ProcessMemory.Read((IntPtr)swapChain.ComPointer) + offset);
                            return _D3D11Adress;
                        }
                    else 
                    {
                        return (IntPtr)0;
                    }

                
            }
            catch (Exception e)
            {
                return (IntPtr)0;
            }

        }//end func

        public static byte[] getOpcodeD3D9(IntPtr address)
        {
            if ((int)address == 0) return null;
            try
            {
                byte[] _D3D9OpCode = (int)ProcessMemory.Read((IntPtr)address) != 0x6a ? ProcessMemory.ReadBytes((IntPtr)address, 5) : ProcessMemory.ReadBytes((IntPtr)address, 7);
                return _D3D9OpCode;
            }
            catch (Exception e)
            {
                return null;
            }


        }//end func


        public static byte[] getOpcodeD3D11(IntPtr address)
        {
            if ((int)address == 0) return null;
            try
            {
                byte[] _D3D11OpCode = ProcessMemory.ReadBytes((IntPtr)address, 5);
                return _D3D11OpCode;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static void findAddressAndOpcode()
        {
            ProcessMemory.attachProcess((uint)Process.GetCurrentProcess().Id);
            Address = ((int)getAddressD3D9() == 0) ? getAddressD3D11() : getAddressD3D9();
            OpCode = ((int)getAddressD3D9() == 0) ? getOpcodeD3D11(getAddressD3D11()) : getOpcodeD3D9(getAddressD3D9());
            ProcessMemory.deattach();
        }

    }
}
