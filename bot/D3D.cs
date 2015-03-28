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
    class D3D
    {
        public IntPtr getAddressD3D9()
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
                    //Открываем текущий процесс
                    var processMemory = new ProcessMemory((int)Process.GetCurrentProcess().Id);
                    //Считываем необходимый нам адрес расположения в памяти D3D9 функции по смещению 0xA8 от указателя на Com объект
                    IntPtr _D3D9Adress = (IntPtr)processMemory.Read((IntPtr)processMemory.Read((IntPtr)device.ComPointer + offset));
                    return _D3D9Adress;
                }

            }
            catch (Exception e)
            {
                return (IntPtr)0;
            }
        }

        public IntPtr getAddressD3D11()
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
                            //И открыв текущий процесс - считаем адрес функции и опкоды
                            var processMemory = new ProcessMemory((int)Process.GetCurrentProcess().Id);
                            //Считываем наш SwapChain
                            IntPtr _D3D11Adress = (IntPtr)processMemory.Read((IntPtr)processMemory.Read((IntPtr)swapChain.ComPointer) + offset);
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

        public byte[] getOpcodeD3D9(IntPtr address)
        {
            if ((int)address == 0) return null;
            try
            {
                var processMemory = new ProcessMemory((int)Process.GetCurrentProcess().Id);
                byte[] _D3D9OpCode = (int)processMemory.Read((IntPtr)address) != 0x6a ? processMemory.ReadBytes((IntPtr)address, 5) : processMemory.ReadBytes((IntPtr)address, 7);
                return _D3D9OpCode;
            }
            catch (Exception e)
            {
                return null;
            }


        }//end func


        public byte[] getOpcodeD3D11(IntPtr address)
        {
            if ((int)address == 0) return null;
            try
            {
                var processMemory = new ProcessMemory((int)Process.GetCurrentProcess().Id);
                byte[] _D3D11OpCode = processMemory.ReadBytes((IntPtr)address, 5);
                return _D3D11OpCode;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public IntPtr getAddress()
        {
            return ((int)this.getAddressD3D9() == 0) ? this.getAddressD3D11() : this.getAddressD3D9();
        }

        public byte[] getOpcode() 
        {
            return ((int)this.getAddressD3D9() == 0) ? this.getOpcodeD3D11(this.getAddressD3D11()) : this.getOpcodeD3D9(this.getAddressD3D9()); 
        }
    }
}
