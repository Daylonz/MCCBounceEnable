using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using Process.NET;
using Process.NET.Memory;
using Process.NET.Patterns;

namespace MCCBounceEnable
{
    public partial class UIForm : Form
    {
        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static readonly int VK_MENU = 0x12; //Alt key.
        private static readonly int VK_O = 0x4F; //O key.
        private DateTime lastHotkeyPress = DateTime.Now;

        public readonly IMemoryPattern test = new DwordPattern("48 8B 0D ?? ?? ?? ?? F3 0F 10 49 10");
        private IntPtr lastAddr = IntPtr.Zero;

        byte[] value30 = { 0x1E, 0x00, 0x89, 0x88, 0x08, 0x3D };
        byte[] value60 = { 0x3C, 0x00, 0x89, 0x88, 0x88, 0x3C };

        public UIForm()
        {
            InitializeComponent();
        }

        private static ProcessSharp getProcess()
        {
            var MCCProcess = System.Diagnostics.Process.GetProcessesByName("MCC-Win64-Shipping").FirstOrDefault();
            var MCCProcessWinStore = System.Diagnostics.Process.GetProcessesByName("MCC-Win64-Shipping-WinStore").FirstOrDefault();
            if (MCCProcess == null && MCCProcessWinStore == null)
            {
                MessageBox.Show("Could not locate process. Please ensure MCC is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            if (MCCProcess != null)
                return new ProcessSharp("MCC-Win64-Shipping", MemoryType.Remote);

            return new ProcessSharp("MCC-Win64-Shipping-WinStore", MemoryType.Remote);
        }

        public void setTickrate(int desired)
        {
            try
            {
                var process = getProcess();
                process.Memory = new ExternalProcessMemory(process.Handle);
                byte[] addressValue;
                if (lastAddr != IntPtr.Zero)
                {
                    addressValue = process.Memory.Read(lastAddr, 6);
                    if (!addressValue.SequenceEqual(value30) && !addressValue.SequenceEqual(value60))
                    {
                        PatternScanner scanner = new PatternScanner(process["halo2.dll"]);
                        PatternScanResult result = scanner.Find(test);
                        if (!result.Found)
                        {
                            MessageBox.Show("Could not find tick rate in memory. Make sure you're in a game!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        byte[] offset = process.Memory.Read(result.ReadAddress + 3, 4);
                        IntPtr addressLocation = (result.ReadAddress + 7 + BitConverter.ToInt32(offset, 0));
                        addressValue = process.Memory.Read(addressLocation, 8);
                        IntPtr tickAddress = new IntPtr(BitConverter.ToInt64(addressValue, 0) + 2);
                        lastAddr = tickAddress;
                    }
                }
                else
                {
                    PatternScanner scanner = new PatternScanner(process["halo2.dll"]);
                    PatternScanResult result = scanner.Find(test);
                    if (!result.Found)
                    {
                        MessageBox.Show("Could not find tick rate in memory. Make sure you're in a game!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    byte[] offset = process.Memory.Read(result.ReadAddress + 3, 4);
                    IntPtr addressLocation = (result.ReadAddress + 7 + BitConverter.ToInt32(offset, 0));
                    addressValue = process.Memory.Read(addressLocation, 8);
                    IntPtr tickAddress = new IntPtr(BitConverter.ToInt64(addressValue, 0) + 2);
                    lastAddr = tickAddress;
                }

                if (desired == 30)
                {
                    process.Memory.Write(lastAddr, value30);
                }

                if (desired == 60)
                {
                    process.Memory.Write(lastAddr, value60);
                }
            }
            catch (Win32Exception e)
            {
                lastAddr = IntPtr.Zero;
                setTickrate(desired);
            }
        }

        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void UIForm_Load(object sender, EventArgs e)
        {
            if (!IsAdministrator())
            {
                MessageBox.Show("MCCBounceEnable must be ran as administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                setTickrate(30);
            }
            else
            {
                setTickrate(60);
            }
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            short keyState1 = GetAsyncKeyState(VK_MENU);
            short keyState2 = GetAsyncKeyState(VK_O);

            bool altIsPressed = ((keyState1 >> 15) & 0x0001) == 0x0001;
            bool oIsPressed = ((keyState2 >> 15) & 0x0001) == 0x0001;

            if (altIsPressed && oIsPressed)
            {
                if (DateTime.Now.Subtract(lastHotkeyPress).TotalSeconds > 1)
                {
                    if (checkBox1.Checked)
                    {
                        setTickrate(30);
                    }
                    else
                    {
                        setTickrate(60);
                    }
                    checkBox1.Checked = !checkBox1.Checked;
                    System.Media.SystemSounds.Asterisk.Play();
                    lastHotkeyPress = DateTime.Now;
                }
            }
        }
    }
}
