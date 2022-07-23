using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaxCPUTempUI.Properties;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;
using System.Threading;
using System.Runtime.InteropServices;
using System.Management;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Windows.Forms.Layout;

namespace MaxCPUTempUI
{
    public partial class MonitoringOnly : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }
        public MonitoringOnly()
        {
            InitializeComponent();
        }
        private void MonitoringOnly_Resize(object sender, EventArgs e)
        {
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (this.WindowState == FormWindowState.Minimized)
            {
                ContextMenu cm = new ContextMenu();
                Hide();
                notifyIcon1.ContextMenu = new ContextMenu(new MenuItem[]
                {
                        new MenuItem("Open", Open),
                        new MenuItem("-"),
                        new MenuItem("Exit", Exit)
                });
                notifyIcon1.Visible = true;
                new ToastContentBuilder()
                .AddHeroImage(new Uri("https://i.imgur.com/QZKo94i.png"))
                .AddText("MaxCPUTempUI was minimized to system tray")
                .AddText("Right click on the icon to open again")
                .SetBackgroundActivation()
                .Show();
            }
        }
        public void MonitoringOnly_Close(object sender, EventArgs e)
        {
            threadOne.Abort();
            threadTwo.Abort();
        }

        private void ExitApp(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Minimize(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void ChangeMode(object sender, EventArgs e)
        {
            MonitorShutDown monitorshutdown = new MonitorShutDown();
            this.Close();
            monitorshutdown.Show();
        }

        public Thread threadOne;
        public Thread threadTwo;
        private void click(object sender, EventArgs e)
        {
            UpdateVisitor update = new UpdateVisitor();
            Computer computer = new Computer();
            computer.Open();
            computer.CPUEnabled = true;
            computer.GPUEnabled = true;
            threadOne = new Thread(() => GetTemps(ref computer, ref update));
            threadOne.Start();
            threadTwo = new Thread(() => RedrawForm());
            threadTwo.Start();
        }

        private void RedrawForm()
        {
            Application.DoEvents();
            Thread.Sleep(80);
        }

        public void GetTemps(ref Computer computer, ref UpdateVisitor update)
        {
            while (true)
            {
                GrabCPUInfo(ref computer, ref update);
                GrabGPUInfo(ref computer, ref update); 
                this.Invoke((MethodInvoker)delegate ()
                {
                    label3.Text = $"{currentCPUTemp}°C";
                    label7.Text = $"{currentGPUTemp}°C";
                });
                Thread.Sleep(1000);
            }
        }

        private void onMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Chax1");
        }


        public static int currentCPUTemp;
        public void GrabCPUInfo(ref Computer computer, ref UpdateVisitor update)
        {
            computer.Accept(update);
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        {
                            currentCPUTemp = (int)computer.Hardware[i].Sensors[j].Value;
                        }
                    }
                }
            }
        }
        public static int currentGPUTemp;
        public void GrabGPUInfo(ref Computer computer, ref UpdateVisitor update)
        {
            computer.Accept(update);
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.GpuNvidia)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        {
                            currentGPUTemp = (int)computer.Hardware[i].Sensors[j].Value;
                        }
                    }
                }
            }
        }
        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            notifyIcon1.Visible = false;

            Application.Exit();
            notifyIcon1.Dispose();
        }

        void Open(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }
    }
}

