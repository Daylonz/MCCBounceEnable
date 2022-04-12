using System;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using Process.NET;
using Process.NET.Memory;
using Process.NET.Patterns;
using SharpDX.XInput;

namespace MCCBounceEnable
{
    public partial class UIForm : Form
    {
        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static readonly int VK_MENU = 0x12; //Alt key.
        private static readonly int VK_O = 0x4F; //O key.
        private static readonly int VK_W = 0x57; //W key.
        private static readonly String[] PROCESS_NAMES = {"MCC-Win64-Shipping", "MCC-Win64-Shipping-WinStore", "MCCWinStore-Win64-Shipping"};
        private DateTime lastHotkeyPress = DateTime.Now;

        Controller controller = null;

        public readonly IMemoryPattern tickRatePattern = new DwordPattern("48 8B 05 ?? ?? ?? ?? F3 0F 10 40 04 C3 CC CC CC 48 8B 05");
        public readonly IMemoryPattern wireFramePattern = new DwordPattern("BB 02 00 00 00 0F B6");

        private IntPtr lastTRAddr = IntPtr.Zero;
        private IntPtr lastWFAddr = IntPtr.Zero;

        byte[] value30 = { 0x1E, 0x00, 0x89, 0x88, 0x08, 0x3D };
        byte[] value60 = { 0x3C, 0x00, 0x89, 0x88, 0x88, 0x3C };

        public UIForm()
        {
            InitializeComponent();
            checkForController();
        }

        private void checkForController()
        {
            var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
            foreach (var selectControler in controllers)
            {
                if (selectControler.IsConnected)
                {
                    controller = selectControler;
                    break;
                }
            }
        }

        private static ProcessSharp getProcess()
        {
            System.Diagnostics.Process MCCProcess;
            foreach (String processName in PROCESS_NAMES)
            {
                MCCProcess = System.Diagnostics.Process.GetProcessesByName(processName).FirstOrDefault();
                if (MCCProcess != null)
                {
                    return new ProcessSharp(processName, MemoryType.Remote);
                }
            }
            MessageBox.Show("Could not locate process. Please ensure MCC is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
            return null;
        }

        public void setTickrate(int desired)
        {
            try
            {
                var process = getProcess();
                process.Memory = new ExternalProcessMemory(process.Handle);
                byte[] addressValue;
                if (lastTRAddr != IntPtr.Zero)
                {
                    addressValue = process.Memory.Read(lastTRAddr, 6);
                    if (!addressValue.SequenceEqual(value30) && !addressValue.SequenceEqual(value60))
                    {
                        PatternScanner scanner = new PatternScanner(process["halo2.dll"]);
                        PatternScanResult result = scanner.Find(tickRatePattern);
                        if (!result.Found)
                        {
                            MessageBox.Show("Could not find tick rate in memory. Make sure you're in a game!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        byte[] offset = process.Memory.Read(result.ReadAddress + 3, 4);
                        IntPtr addressLocation = (result.ReadAddress + 7 + BitConverter.ToInt32(offset, 0));
                        addressValue = process.Memory.Read(addressLocation, 8);
                        IntPtr tickAddress = new IntPtr(BitConverter.ToInt64(addressValue, 0) + 2);
                        lastTRAddr = tickAddress;
                    }
                }
                else
                {
                    PatternScanner scanner = new PatternScanner(process["halo2.dll"]);
                    PatternScanResult result = scanner.Find(tickRatePattern);
                    if (!result.Found)
                    {
                        MessageBox.Show("Could not find tick rate in memory. Make sure you're in a game!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    byte[] offset = process.Memory.Read(result.ReadAddress + 3, 4);
                    IntPtr addressLocation = (result.ReadAddress + 7 + BitConverter.ToInt32(offset, 0));
                    addressValue = process.Memory.Read(addressLocation, 8);
                    IntPtr tickAddress = new IntPtr(BitConverter.ToInt64(addressValue, 0) + 2);
                    lastTRAddr = tickAddress;
                }

                if (desired == 30)
                {
                    process.Memory.Write(lastTRAddr, value30);
                }

                if (desired == 60)
                {
                    process.Memory.Write(lastTRAddr, value60);
                }
            }
            catch (Win32Exception e)
            {
                lastTRAddr = IntPtr.Zero;
                setTickrate(desired);
            }
        }

        public void toggleWireFrame(bool activate)
        {
            MessageBox.Show("This feature has been temporarily disabled.", "Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
            try
            {
                var process = getProcess();
                process.Memory = new ExternalProcessMemory(process.Handle);
                if (lastWFAddr == IntPtr.Zero)
                {
                    PatternScanner scanner = new PatternScanner(process["halo2.dll"]);
                    PatternScanResult result = scanner.Find(wireFramePattern);
                    if (!result.Found)
                    {
                        MessageBox.Show("Could not find wire frame in memory. Make sure you're in a game!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    byte[] offset = process.Memory.Read(result.ReadAddress + 8, 4);
                    IntPtr addressLocation = (result.ReadAddress + 12 + BitConverter.ToInt32(offset, 0));
                    lastWFAddr = addressLocation;
                }

                if (activate)
                {
                    process.Memory.Write(lastWFAddr, 1);
                }
                else
                {
                    process.Memory.Write(lastWFAddr, 0);
                }
            }
            catch (Win32Exception e)
            {
                lastWFAddr = IntPtr.Zero;
                toggleWireFrame(activate);
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
            short keyState3 = GetAsyncKeyState(VK_W);

            bool altIsPressed = ((keyState1 >> 15) & 0x0001) == 0x0001;
            bool oIsPressed = ((keyState2 >> 15) & 0x0001) == 0x0001;
            bool wIsPressed = ((keyState3 >> 15) & 0x0001) == 0x0001;

            if (controller == null)
            {
                checkForController();
            }

            try
            {
                if ((controller != null && controller.GetState().Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder) && controller.GetState().Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))
                || (altIsPressed && oIsPressed))
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
                        SystemSounds.Asterisk.Play();
                        lastHotkeyPress = DateTime.Now;
                    }
                }

                if (altIsPressed && wIsPressed)
                {
                    if (DateTime.Now.Subtract(lastHotkeyPress).TotalSeconds > 1)
                    {
                        toggleWireFrame(!checkBox2.Checked);
                        checkBox2.Checked = !checkBox2.Checked;
                        SystemSounds.Hand.Play();
                        lastHotkeyPress = DateTime.Now;
                    }
                }
            }
            catch (SharpDX.SharpDXException)
            {
                controller = null;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            toggleWireFrame(checkBox2.Checked);
        }
    }
}
