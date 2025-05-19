using ImGuiNET;
using ClickableTransparentOverlay;
using System.Numerics;
using System.Runtime.InteropServices;
using Memory;
using System.Diagnostics;

namespace Options
{
    public class Program : Overlay
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        public Mem memory = new Mem();
        Process p;

        public string mccProcessSteam = "MCC-Win64-Shipping";
        public string mccProcessWinstore = "MCCWinStore-Win64-Shipping";
        private string selectedProcessName;

        public string voidDrawAddress = "halo3.dll+0x278E9D";
        public string voidRedColorAddress = "halo3.dll+0x278EB8";
        public string voidGreenColorAddress = "halo3.dll+0x278EB7";
        public string voidBlueColorAddress = "halo3.dll+0x278EB6";

        public bool modulesUpdated = false;

        bool showWindow = true;
        bool startup = false;

        private Vector3 rgbColor3 = new Vector3(0, 0, 0);
        private Vector4 rgbColor4 = new Vector4(1, 1, 1, 1);
        private bool fixVoid = false;
        private bool colorCyclingActive = false;
        private float colorCycleSpeed = 5.0f;

        protected override void Render()
        {
            rgbColor4.X = rgbColor3.X;
            rgbColor4.Y = rgbColor3.Y;
            rgbColor4.Z = rgbColor3.Z;

            if (colorCyclingActive == true)
            {
                fixVoid = true;
            }

            if (showWindow)
            {
                ImGuiStylePtr style = ImGui.GetStyle();
                style.Colors[(int)ImGuiCol.TitleBgActive] = rgbColor4;
                style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0, 0, 0, 0.8f);
                style.Colors[(int)ImGuiCol.Border] = rgbColor4;
                style.Colors[(int)ImGuiCol.ResizeGrip] = rgbColor4;

                ImGui.Begin("Void options");

                ImGui.SetWindowSize(new Vector2(630, 140));
                
                ImGui.Checkbox("Fix void", ref fixVoid);

                ImGui.BeginChild("Color picker", new Vector2(245, 30));
                ImGui.ColorEdit3("Color picker", ref rgbColor3);
                ImGui.EndChild();

                ImGui.Checkbox("Color cycling", ref colorCyclingActive);
                ImGui.SameLine();
                ImGui.SliderFloat("Cycle speed", ref colorCycleSpeed, 0.0f, 5.0f);

                if (ImGui.Button("Set default void color"))
                {
                    rgbColor3.X = 27 / 255f;
                    rgbColor3.Y = 60 / 255f;
                    rgbColor3.Z = 141 / 255f;
                }

                ImGui.End();
            }
        }

        public Program()
        {
            Task task = Task.Run(async () => 
            {
                await GetProcess();

                byte redByte = Convert.ToByte(memory.ReadByte("halo3.dll+0x278B08"));
                byte greenByte = Convert.ToByte(memory.ReadByte("halo3.dll+0x278B07"));
                byte blueByte = Convert.ToByte(memory.ReadByte("halo3.dll+0x278B06"));

                float redFloat = redByte / 255f;
                float greenFloat = greenByte / 255f;
                float blueFloat = blueByte / 255f;

                rgbColor3.X = redFloat;
                rgbColor3.Y = greenFloat;
                rgbColor3.Z = blueFloat;

                while (true)
                {
                    if (GetAsyncKeyState(0x43) < 0)
                    {
                        showWindow = !showWindow;
                        Thread.Sleep(150);
                    }

                    if (colorCyclingActive && fixVoid)
                    {
                        float time = (float)ImGui.GetTime();
                        float red = (float)(Math.Sin(time * colorCycleSpeed) + 1) / 2;
                        float green = (float)(Math.Sin((time + 2) * colorCycleSpeed) + 1) / 2;
                        float blue = (float)(Math.Sin((time + 4) * colorCycleSpeed) + 1) / 2;

                        rgbColor3 = new Vector3(red, green, blue);
                    }

                    if (fixVoid)
                    {
                        byte[] colorBytes = new byte[3];

                        int redInt = (int)(rgbColor3.X * 255);
                        int greenInt = (int)(rgbColor3.Y * 255);
                        int blueInt = (int)(rgbColor3.Z * 255);

                        colorBytes[0] = (byte)redInt;
                        colorBytes[1] = (byte)greenInt;
                        colorBytes[2] = (byte)blueInt;

                        memory.FreezeValue(voidDrawAddress, "int", "65465");

                        memory.WriteMemory(voidRedColorAddress, "byte", colorBytes[0].ToString("X2"));
                        memory.WriteMemory(voidGreenColorAddress, "byte", colorBytes[1].ToString("X2"));
                        memory.WriteMemory(voidBlueColorAddress, "byte", colorBytes[2].ToString("X2"));
                    }
                    else
                    {
                        memory.UnfreezeValue(voidDrawAddress);
                        memory.WriteBytes(voidDrawAddress, new byte[2] { 0xB9, 0x10 });
                    }
                }
            });
        }

        public async Task GetProcess() 
        {
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    if (process.ProcessName.Equals(mccProcessSteam, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedProcessName = mccProcessSteam;
                        break;
                    }
                    else if (process.ProcessName.Equals(mccProcessWinstore, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedProcessName = mccProcessWinstore;
                        break;
                    }
                }

                p = Process.GetProcessesByName(selectedProcessName)[0];
                memory.OpenProcess(p.Id);

                if (startup == false)
                {
                    if (selectedProcessName != null)
                    {
                        Console.WriteLine("Found: " + selectedProcessName.ToString() + " (" + p.Id + ")");
                        Console.WriteLine("Use 'C' key to show/hide the overlay!");
                        startup = true;
                    }
                    else
                    {
                        showWindow = false;
                        Console.WriteLine("The MCC process was not found... Please open MCC and try again.");
                        Console.ReadLine();
                        Environment.Exit(0);
                    }
                }

                if (memory == null) return;
                if (memory.theProc == null) return;

                memory.theProc.Refresh();
                memory.modules.Clear();

                foreach (ProcessModule Module in memory.theProc.Modules)
                {
                    if (!string.IsNullOrEmpty(Module.ModuleName) && !memory.modules.ContainsKey(Module.ModuleName)) memory.modules.Add(Module.ModuleName, Module.BaseAddress);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("The MCC process was not found... Please open MCC and try again.");
                Console.WriteLine("Error: " + ex);

                while (true)
                {
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Special thanks to Lord Zedd <3");
            Program program = new Program();
            program.Start().Wait();
        }
    }
}
