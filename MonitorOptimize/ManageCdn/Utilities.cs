using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace ManageCdn
{
    public static class Utilities
    {
        public static bool IsRunningMocked { get; set; }
        public static Action<string> LoggerMethod { get; set; }
        public static Func<string> PauseMethod { get; set; }
        public static string ProjectPath { get; set; }

        static Utilities()
        {
            LoggerMethod = Console.WriteLine;
            PauseMethod = Console.ReadLine;
            ProjectPath = ".";
        }

        public static void Log(string message)
        {
            LoggerMethod.Invoke(message);
        }

        public static void Log(object obj)
        {
            if (obj != null)
            {
                LoggerMethod.Invoke(obj.ToString());
            }
            else
            {
                LoggerMethod.Invoke("(null)");
            }
        }

        public static void Log()
        {
            Utilities.Log("");
        }

        public static string ReadLine()
        {
            return PauseMethod.Invoke();
        }

        public static string CheckAddress(string url, IDictionary<string, string> headers = null)
        {
            if (!IsRunningMocked)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(300);
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            }
                        }
                        return client.GetAsync(url).Result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }

            return "[Running in PlaybackMode]";
        }

        public static void Print(IWebAppBase resource)
        {
            var builder = new StringBuilder().Append("Web app: ").Append(resource.Id)
                    .Append("Name: ").Append(resource.Name)
                    .Append("\n\tState: ").Append(resource.State)
                    .Append("\n\tResource group: ").Append(resource.ResourceGroupName)
                    .Append("\n\tRegion: ").Append(resource.Region)
                    .Append("\n\tDefault hostname: ").Append(resource.DefaultHostName)
                    .Append("\n\tApp service plan: ").Append(resource.AppServicePlanId)
                    .Append("\n\tHost name bindings: ");
            foreach (var binding in resource.GetHostNameBindings().Values)
            {
                builder = builder.Append("\n\t\t" + binding.ToString());
            }
            builder = builder.Append("\n\tSSL bindings: ");
            foreach (var binding in resource.HostNameSslStates.Values)
            {
                builder = builder.Append("\n\t\t" + binding.Name + ": " + binding.SslState);
                if (binding.SslState != null && binding.SslState != SslState.Disabled)
                {
                    builder = builder.Append(" - " + binding.Thumbprint);
                }
            }
            builder = builder.Append("\n\tApp settings: ");
            foreach (var setting in resource.GetAppSettings().Values)
            {
                builder = builder.Append("\n\t\t" + setting.Key + ": " + setting.Value + (setting.Sticky ? " - slot setting" : ""));
            }
            builder = builder.Append("\n\tConnection strings: ");
            foreach (var conn in resource.GetConnectionStrings().Values)
            {
                builder = builder.Append("\n\t\t" + conn.Name + ": " + conn.Value + " - " + conn.Type + (conn.Sticky ? " - slot setting" : ""));
            }
            Utilities.Log(builder.ToString());
        }

    }
}
