using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
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
    public partial class MonitorShutDown : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("User32.dll")]

        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont,
            IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        private PrivateFontCollection fonts = new PrivateFontCollection();

        Font Font1;
        Font Font2;
        Font Font3;
        Font Font4;

        public bool IsAlive { get; }

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
        public MonitorShutDown()
        {
            InitializeComponent();

            byte[] fontData = Properties.Resources.BebasNeue_Regular;
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fonts.AddMemoryFont(fontPtr, Properties.Resources.BebasNeue_Regular.Length);
            AddFontMemResourceEx(fontPtr, (uint)Properties.Resources.BebasNeue_Regular.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);

            Font1 = new Font(fonts.Families[0], 12);
            Font2 = new Font(fonts.Families[0], 14);
            Font3 = new Font(fonts.Families[0], 12);
            Font4 = new Font(fonts.Families[0], 11);
        }
        public void Form1_Close(object sender, EventArgs e)
        {
            threadOne.Abort();
        }

        private void Form1_Resize(object sender, EventArgs e)
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
        public static int currentCPUTemp;
        public void GrabCPUInfo(ref Computer computer, ref UpdateVisitor update)
        {
            int maxTemp = Data.ShutdownTemp;
            computer.Accept(update);
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        {
                            if (computer.Hardware[i].Sensors[j].Value < maxTemp)
                            {
                                currentCPUTemp = (int)computer.Hardware[i].Sensors[j].Value;
                            }
                            if (computer.Hardware[i].Sensors[j].Value > maxTemp)
                            {
                                ShutDown();
                            }
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

        public static int currentGPULoad;
        public void GrabGPULoad(ref Computer computer, ref UpdateVisitor update)
        {
            computer.Accept(update);
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.GpuNvidia)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Load)
                        {
                            currentGPULoad = (int)computer.Hardware[i].Sensors[j].Value;
                        }
                    }
                }
            }
        }

        public static int currentCPULoad;
        public void GrabCPULoad(ref Computer computer, ref UpdateVisitor update)
        {
            computer.Accept(update);
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Load)
                        {
                            currentCPULoad = (int)computer.Hardware[i].Sensors[j].Value;
                        }
                    }
                }
            }
        }

        public void ShutDown()
        {
            string offTime = textBox1.Text;
            Process.Start("shutdown", $"/s /t {offTime}");
            System.Environment.Exit(0);
        }

        public void UpdateLoads()
        {
            label8.Text = $"{currentGPULoad}%";
            label10.Text = $"{currentCPULoad}%";
        }

        public void UpdateTemps()
        {
            label2.Text = $"{currentCPUTemp}°C";
            label7.Text = $"{currentGPUTemp}°C";
        }

        public void GetTemps(ref Computer computer, ref UpdateVisitor update)
        {
            while (true)
            {
                GrabCPUInfo(ref computer, ref update);
                GrabGPUInfo(ref computer, ref update);
                Data.SetCPUTemperature(currentCPUTemp);
                Data.SetGPUTemperature(currentGPUTemp);
                Thread.Sleep(1000);
            }
        }

        public void GetLoads(ref Computer computer, ref UpdateVisitor update)
        {
            while (true)
            {
                GrabCPULoad(ref computer, ref update);
                GrabGPULoad(ref computer, ref update);
                Data.SetCPULoad(currentCPULoad);
                Data.SetGPULoad(currentGPULoad);
                Thread.Sleep(1000);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Chax1");
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
            threadTwo = new Thread(() => GetLoads(ref computer, ref update));
            if (MaxCPUTempUI.Data.ShutdownTime == 0 || MaxCPUTempUI.Data.ShutdownTemp == 0)
            {
                MessageBox.Show("Please enter values");
                return;
            }
            else
            {
                Data.currentlyRunning = true;
                threadOne.Start();
                threadTwo.Start();
            }
            timer2.Start();
        }

        private void minimizeButton_Click(object sender, System.EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void closeButton_Click(object sender, System.EventArgs e)
        {
            if (threadOne != null)
            {
            threadOne.Abort();
            timer2.Stop();
            Application.Exit();
            Close();
            }
            if (threadOne != null || threadTwo != null)
            {
            threadOne.Abort();
            threadTwo.Abort();
            timer2.Stop();
            Application.Exit();
            Close();
            }
            else
            {
            timer2.Stop();
            Application.Exit();
            Close();
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

        private void MonitorShutDown_Load(object sender, EventArgs e)
        {
            label1.Font = Font2;
            label2.Font = Font2;
            label3.Font = Font2;
            label4.Font = Font2;
            label5.Font = Font1; 
            label6.Font = Font2;
            label7.Font = Font2;
            textBox1.Font = Font4;
            textBox2.Font = Font4;
            button1.Font = Font3;
            button3.Font = Font3;
            label8.Font = Font2;
            label10.Font = Font2;
            label9.Font = Font2;
            label11.Font = Font2;
        }

        private void ChangeMode(object sender, EventArgs e)
        {
            if (Data.currentlyRunning == true)
            {
                threadOne.Abort();
                timer2.Stop();
                label2.Text = "N/A";
                label7.Text = "N/A";
                label8.Text = "N/A";
                label10.Text = "N/A";
            }
            if (Data.monitorMode == true)
            {
                textBox1.Text = "";
                textBox2.Text = "";
                textBox1.Visible = true;
                label4.Visible = true;
                label5.Text = "MaxCPUTempUI";
                label3.Visible = true;
                label8.Visible = false;
                label10.Visible = false;
                label9.Visible = false;
                label11.Visible = false;
                textBox2.Visible = true;
                Data.SetShutdownTime(0);
                Data.SetShutdownTemp(0);
                Data.monitorMode = false;
            }
            else
            {
                textBox1.Visible = false;
                label4.Visible = false;
                textBox2.Visible = false;
                label3.Visible = false;
                label5.Text = "MaxCPUTempUI (MonitorOnly Mode)";
                label8.Visible = true;
                label10.Visible = true;
                label9.Visible = true;
                label11.Visible = true;
                Data.SetShutdownTime(2147483647);
                Data.SetShutdownTemp(2147483646);
                Data.monitorMode = true;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            UpdateTemps();
            UpdateLoads();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int value;
            if (int.TryParse(textBox1.Text, out value))
            {
                Data.SetShutdownTime(value);
            }
            else
            {
                MessageBox.Show("Invalid time");
                textBox1.SelectAll();
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            int value;
            if (int.TryParse(textBox2.Text, out value))
            {
                Data.SetShutdownTemp(value);
            }
            else
            {
                MessageBox.Show("Invalid Temp");
                textBox2.SelectAll();
            }
        }
    }
}