using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MineDebugHider
{
    public partial class Main : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandleA(string lib);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr Module, string Function);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr ProcessHandle, IntPtr Address, byte[] CodeToInject, uint Size, int NumberOfBytes);

        public Main()
        {
            InitializeComponent();
        }

        private bool UnHookFunction(string Function, string LibraryOfFunction, IntPtr ProcessHandle, int Size)
        {
            IntPtr LibraryModule = GetModuleHandleA(LibraryOfFunction);
            IntPtr FunctionAddress = GetProcAddress(LibraryModule, Function);
            byte[] OriginalCode = new byte[Size];
            Marshal.Copy(FunctionAddress, OriginalCode, 0, Size);
            byte[] NewCode = new byte[Size];
            for (int i = 0; i < Size; i++)
            {
                NewCode[i] = OriginalCode[i];
            }
            bool IsSuccess = WriteProcessMemory(ProcessHandle, FunctionAddress, NewCode, (uint)Size, 0);
            return IsSuccess;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text) || textBox1.Text == Process.GetCurrentProcess().Id.ToString())
            {
                MessageBox.Show("You can't leave the process ID textbox empty!", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            }
            else
            {
                try
                {
                    IntPtr KernelBaseModule = GetModuleHandleA("KernelBase.dll");
                    IntPtr Kernel32Module = GetModuleHandleA("Kernel32.dll");
                    IntPtr NtdllModule = GetModuleHandleA("ntdll.dll");
                    IntPtr Win32uModule = GetModuleHandleA("win32u.dll");
                    Process GetProcessHandle = Process.GetProcessById(Convert.ToInt32(textBox1.Text));
                    if (GetProcessHandle.ProcessName.Length == 0)
                    {
                        MessageBox.Show("The Process Not Found.", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    }
                    else
                    {
                        bool IsSuccessed = false;
                        if (checkBox1.Checked)
                        {
                            IntPtr IsDebuggerPresentAddress = GetProcAddress(KernelBaseModule, "IsDebuggerPresent");
                            byte[] IsDebuggerPresentHookedCode = { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xC3 };
                            IsSuccessed = WriteProcessMemory(GetProcessHandle.Handle, IsDebuggerPresentAddress, IsDebuggerPresentHookedCode, 6, 0);
                        }

                        if (checkBox2.Checked)
                        {
                            IntPtr NtQueryInformationProcessAddress = GetProcAddress(NtdllModule, "NtQueryInformationProcess");
                            byte[] NtQueryInformationProcessHookedCode = { 0xB8, 0x13, 0x00, 0x00, 0x00, 0xE8 };
                            IsSuccessed = WriteProcessMemory(GetProcessHandle.Handle, NtQueryInformationProcessAddress, NtQueryInformationProcessHookedCode, 6, 0);
                        }

                        if (checkBox3.Checked)
                        {
                            IntPtr NtUserBlockInputAddress = GetProcAddress(Win32uModule, "NtUserBlockInput");
                            byte[] NtUserBlockInputHookedCode = { 0xC2, 0x00, 0x00 };
                            IsSuccessed = WriteProcessMemory(GetProcessHandle.Handle, NtUserBlockInputAddress, NtUserBlockInputHookedCode, 3, 0);
                        }

                        if (checkBox4.Checked)
                        {
                            IntPtr NtUserSwitchDesktopAddress = GetProcAddress(Win32uModule, "NtUserSwitchDesktop");
                            byte[] NtUserSwitchDesktopHookedCode = { 0xC2, 0x00, 0x00 };
                            IsSuccessed = WriteProcessMemory(GetProcessHandle.Handle, NtUserSwitchDesktopAddress, NtUserSwitchDesktopHookedCode, 3, 0);
                        }

                        if (checkBox5.Checked)
                        {
                            IsSuccessed = UnHookFunction("DbgUiRemoteBreakin", "ntdll.dll", GetProcessHandle.Handle, 1);
                            IsSuccessed = UnHookFunction("DbgBreakPoint", "ntdll.dll", GetProcessHandle.Handle, 2);
                        }

                        if (checkBox6.Checked)
                        {
                            IntPtr GetThreadContextAddress = GetProcAddress(KernelBaseModule, "GetThreadContext");
                            byte[] GetThreadContextHookedCode = { 0x89, 0xFF, 0x55, 0x89, 0xE5, 0x5D, 0xC2, 0x08, 0x00 };
                            IsSuccessed = WriteProcessMemory(GetProcessHandle.Handle, GetThreadContextAddress, GetThreadContextHookedCode, 9, 0);
                        }

                        if (checkBox7.Checked)
                        {
                            IntPtr SetThreadContextAddress = GetProcAddress(KernelBaseModule, "SetThreadContext");
                            byte[] SetThreadContextHookedCode = { 0x89, 0xFF, 0x55, 0x89, 0xE5, 0x5D, 0xC2, 0x08, 0x00 };
                            IsSuccessed = WriteProcessMemory(GetProcessHandle.Handle, SetThreadContextAddress, SetThreadContextHookedCode, 9, 0);
                        }

                        if (checkBox8.Checked)
                        {
                            IntPtr ZwCloseAddress = GetProcAddress(NtdllModule, "ZwClose");
                            byte[] ZwCloseHookedCode = { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xC2, 0x04, 0x00 };
                            IsSuccessed = WriteProcessMemory(GetProcessHandle.Handle, ZwCloseAddress, ZwCloseHookedCode, 8, 0);
                        }

                        if (checkBox1.Checked || checkBox2.Checked || checkBox3.Checked || checkBox4.Checked || checkBox5.Checked || checkBox6.Checked || checkBox7.Checked || checkBox8.Checked)
                        {
                            if (IsSuccessed)
                            {
                                MessageBox.Show("Success, The Functions You Specified Have Been Hooked.", "Success", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("Error have been occured while hooking one of the functions you specified.", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                }
            }
        }

        private void checkBox1_MouseHover(object sender, EventArgs e)
        {
            ToolTip ShowInfo = new ToolTip();
            ShowInfo.SetToolTip(checkBox1, "Modifies IsDebuggerPresent to always return false.");
        }

        private void checkBox2_MouseHover(object sender, EventArgs e)
        {
            ToolTip ShowInfo = new ToolTip();
            ShowInfo.SetToolTip(checkBox2, "Modifies NtQueryInformationProcess to always return false, this can cause some programs to not work properly if it was using this function.");
        }

        private void checkBox3_MouseHover(object sender, EventArgs e)
        {
            ToolTip ShowInfo = new ToolTip();
            ShowInfo.SetToolTip(checkBox3, "Modifies NtUserBlockInput to not processed and return true (for blocked successfully), this anti-debug method are being used by some protectors and it can prevent mouse and keyboard events from reaching the application.");
        }

        private void checkBox4_MouseHover(object sender, EventArgs e)
        {
            ToolTip ShowInfo = new ToolTip();
            ShowInfo.SetToolTip(checkBox4, "Modifies NtUserSwitchDesktop to not processed and to return true, and this technique makes debugging impossible.");
        }

        private void checkBox5_MouseHover(object sender, EventArgs e)
        {
            ToolTip ShowInfo = new ToolTip();
            ShowInfo.SetToolTip(checkBox5, "Some Protectors Hooks DbgUiRemoteBreakin and DbgBreakPoint which prevents debug attaching, we are fixing it.");
        }

        private void checkBox6_MouseHover(object sender, EventArgs e)
        {
            ToolTip ShowInfo = new ToolTip();
            ShowInfo.SetToolTip(checkBox6, "a lot of Protectors Checks for Hardware Breakpoints using this function.");
        }

        private void checkBox7_MouseHover(object sender, EventArgs e)
        {
            ToolTip ShowInfo = new ToolTip();
            ShowInfo.SetToolTip(checkBox7, "Some Protectors Using This Function to clear hardware breakpoints.");
        }

        private void checkBox8_MouseHover(object sender, EventArgs e)
        {
            ToolTip ShowInfo = new ToolTip();
            ShowInfo.SetToolTip(checkBox8, "a lot Protectors Using This Anti-Debug Technique and it's actually pretty good and it works by calling CloseHandle with an invalide handle, we are hooking it to successed anyway.");
        }
    }
}