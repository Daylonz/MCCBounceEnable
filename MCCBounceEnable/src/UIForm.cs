﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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
        public readonly IMemoryPattern test = new DwordPattern("48 8B 0D ?? ?? ?? ?? F3 0F 10 49 10");

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
            var process = getProcess();
            process.Memory = new ExternalProcessMemory(process.Handle);

            PatternScanner scanner = new PatternScanner(process["halo2.dll"]);
            PatternScanResult result = scanner.Find(test);
            if (!result.Found)
            {
                MessageBox.Show("Could not find tick rate in memory. Make sure you're in a game!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            byte[] offset = process.Memory.Read(result.ReadAddress + 3, 4);
            IntPtr addressLocation = (result.ReadAddress + 7 + BitConverter.ToInt32(offset, 0));
            byte[] addressValue = process.Memory.Read(addressLocation, 8);
            IntPtr tickAddress = new IntPtr(BitConverter.ToInt64(addressValue, 0) + 2);
            if (desired == 30)
            {
                process.Memory.Write(tickAddress, value30);
                MessageBox.Show("Tick rate set to 30.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (desired == 60)
            {
                process.Memory.Write(tickAddress, value60);
                MessageBox.Show("Tick rate set to 60.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    }
}
