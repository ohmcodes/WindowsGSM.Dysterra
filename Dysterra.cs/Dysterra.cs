using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WindowsGSM.Plugins
{
    public class Dysterra : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Dysterra", // WindowsGSM.XXXX
            author = "ohmcodes",
            description = "WindowsGSM plugin for supporting Dysterra Dedicated Server",
            version = "1.0",
            url = "https://github.com/ohmcodes/WindowsGSM.Dysterra", // Github repository link (Best practice)
            color = "#FFA500" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "2214780"; // Game server appId
        //1562450 //dysterra playtest
        //2214780 //dedicated
        //1816360 //playtest
        //1527890 //the game


        // - Standard Constructor and properties
        public Dysterra(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public string StartPath => @"Dysterra/Binaries/Win64/DysterraServer-Win64-Shipping.exe"; // Game server start path
        public string FullName = "Dysterra Dedicated Server WGSM"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 0; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string ServerName = "Dysterra Dedicated Server WGSM";
        public string Port = "27015"; // Default port
        public string QueryPort = "27016"; // Default query port
        public string Defaultmap = "world"; // Default map name
        public string Maxplayers = "20"; // Default maxplayers
        public string Additional = $"-rcon=127.0.0.1 -rconport=27017 -rconpasswd=changeme"; // Additional server start parameter
        //-rconpasswd={base.serverData.GetRCONPassword()}

        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            // Copy Template Settings
            File.Copy(ServerPath.GetServersServerFiles(_serverData.ServerID, "Dysterra", "WorldSettings", "Survival_Landscape_Template.json"), ServerPath.GetServersServerFiles(_serverData.ServerID, "Dysterra", "WorldSettings", "MyServer.json"), true);

            // Set default worldsettings path
            _serverData.ServerParam += $"-worldsettings={ServerPath.GetServersServerFiles(_serverData.ServerID, "Dysterra","WorldSettings","MyServer.json")}";

            // Parse copied json file

            // Set WorldName
            // Set WorldInfo
            // Set MaxPlayers

            string jsonPath = ServerPath.GetServersServerFiles(_serverData.ServerID, "Dysterra", "WorldSettings", "MyServer.json");
            string json = File.ReadAllText(jsonPath);
            //dynamic jsonObj = JsonConvert.DeserializeObject(json);
            //jsonObj["WorldName"] = _serverData.ServerName;
            //jsonObj["WorldInfo"] = "WGSM Dysterra Dedicated Server";
            //jsonObj["MaxPlayers"] = int.Parse(_serverData.ServerMaxPlayer);


            // Convert the JSON string to a JObject:
            JObject jObject = JsonConvert.DeserializeObject(json) as JObject;
            // Select a nested property using a single string:
            JToken WorldName = jObject.SelectToken("WorldName");
            // Update the value of the property: 
            WorldName.Replace(_serverData.ServerName);

            JToken WorldInfo = jObject.SelectToken("WorldInfo");
            WorldInfo.Replace("WGSM Dysterra Dedicated Server");
            JToken MaxPlayers = jObject.SelectToken("MaxPlayers");
            MaxPlayers.Replace(int.Parse(_serverData.ServerMaxPlayer));

            // Convert the JObject back to a string:
            string test = JToken.FromObject(jObject).ToString();
            JObject o = JObject.Parse(test);
            //string updatedJsonString = jObject.ToString(Formatting.Indented);
            string output = JsonConvert.SerializeObject(o, Formatting.Indented);
            File.WriteAllText(jsonPath, output);

            await Task.Delay(1000);
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            // Prepare start parameter
            var param = new StringBuilder($"{_serverData.ServerParam} -log -customserver -QueryPort={_serverData.ServerQueryPort}");

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param.ToString(),
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }

            await Task.Delay(1000);
        }


        // - Stop server function
        public async Task Stop(Process p)
        {
            //await Task.Run(() =>
            //{
            //    if (p.StartInfo.RedirectStandardInput)
            //    {
            //        // Send "stop" command to StandardInput stream if EmbedConsole is on
            //        p.StandardInput.WriteLine("stop");
            //    }
            //    else
            //    {
            //        // Send "stop" command to game server process MainWindow
            //        ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "stop");
            //    }
            //});

            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c");
            });
            await Task.Delay(20000);
        }

        // - Update server function
        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            await Task.Run(() => { p.WaitForExit(); });
            return p;
        }

        // - Check if the installation is successful
        public bool IsInstallValid()
        {
            // Check executable file exists
            return File.Exists(ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "PackageInfo.bin");
            Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}
