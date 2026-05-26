namespace Skyline.DataMiner.MediaOps.Live.WebApp
{
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;

using Microsoft.Owin.Hosting;

using Owin;

public static class Program
{
public static void Main()
{
// Load configuration from .env file
var envVars = LoadEnvFile();
var port = envVars.ContainsKey("WEBAPP_PORT") ? envVars["WEBAPP_PORT"] : "9000";
var host = envVars.ContainsKey("WEBAPP_HOST") ? envVars["WEBAPP_HOST"] : "+";
var baseAddress = $"http://{host}:{port}/";

try
{
// Add firewall rule for Windows (requires admin)
AddFirewallRule(int.Parse(port));
}
catch
{
// Silently fail if not admin or firewall rule already exists
}

using (WebApp.Start<Startup>(baseAddress))
{
Console.WriteLine($"MediaOps Live Control Surface running at {baseAddress}");
Console.WriteLine($"Access locally: http://localhost:{port}");
Console.WriteLine($"Access externally: http://{GetLocalIpAddress()}:{port}");
Console.WriteLine("Press Enter to stop...");
Console.ReadLine();
}
}

private static Dictionary<string, string> LoadEnvFile()
{
var vars = new Dictionary<string, string>();
try
{
var envPath = ".env";
if (System.IO.File.Exists(envPath))
{
foreach (var line in System.IO.File.ReadAllLines(envPath))
{
if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
var parts = line.Split(new[] { '=' }, 2);
if (parts.Length == 2)
{
vars[parts[0].Trim()] = parts[1].Trim();
}
}
}
}
catch { }
return vars;
}

private static string GetLocalIpAddress()
{
try
{
using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
{
socket.Connect("8.8.8.8", 65530);
var endPoint = socket.LocalEndPoint as IPEndPoint;
return endPoint?.Address.ToString() ?? "127.0.0.1";
}
}
catch
{
return "127.0.0.1";
}
}

private static void AddFirewallRule(int port)
{
try
{
// Only attempt on Windows
if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
return;

var psi = new System.Diagnostics.ProcessStartInfo
{
FileName = "netsh",
Arguments = $"advfirewall firewall add rule name=\"MediaOps Live WebApp {port}\" dir=in action=allow protocol=tcp localport={port}",
UseShellExecute = false,
RedirectStandardOutput = true,
CreateNoWindow = true
};
using (var process = System.Diagnostics.Process.Start(psi))
{
process?.WaitForExit();
}
}
catch
{
// Silently fail - rule may already exist or user may not have admin rights
}
}
}

public class Startup
{
public void Configuration(IAppBuilder app)
{
var config = new HttpConfiguration();
config.MapHttpAttributeRoutes();
config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{action}/{id}", new { id = RouteParameter.Optional });

app.UseWebApi(config);

var fileSystem = new Microsoft.Owin.FileSystems.PhysicalFileSystem("./Views");
var options = new Microsoft.Owin.StaticFiles.FileServerOptions
{
FileSystem = fileSystem,
EnableDefaultFiles = true,
};

app.UseFileServer(options);
}
}
}
