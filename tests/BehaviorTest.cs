// Runtime behavior test against the shim: verifies Host mapping logic end to end.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Fiddler;
using FiddlerWillowLite;

internal static class BehaviorTest
{
    private static int _failures;

    private static void Check(bool ok, string name)
    {
        Console.WriteLine((ok ? "PASS  " : "FAIL  ") + name);
        if (!ok) _failures++;
    }

    [STAThread]
    private static int Main()
    {
        // 保存并临时隔离磁盘配置，测试结束后还原，避免污染真实使用环境。
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FiddlerWillowLite", "config.json");
        var backup = File.Exists(configPath) ? File.ReadAllText(configPath) : null;

        var plugin = new WillowPlugin();

        var field = typeof(WillowPlugin).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);
        var config = (WillowConfig)field.GetValue(plugin);
        config.MappingEnabled = true;
        config.Projects.Clear();
        config.Projects.Add(new EnvProject
        {
            Name = "Dev",
            Enabled = true,
            Rules = new List<HostRule>
            {
                new HostRule { Domain = "api.example.com", Target = "127.0.0.1:8080", Enabled = true },
                new HostRule { Domain = "*.test.local", Target = "10.0.0.5", Enabled = true },
                new HostRule { Domain = "off.example.com", Target = "1.2.3.4", Enabled = false }
            }
        });

        var hit = new Session { hostname = "api.example.com", port = 443, fullUrl = "https://api.example.com/x" };
        plugin.AutoTamperRequestBefore(hit);
        Check(hit["x-overrideHost"] == "127.0.0.1:8080", "exact domain mapping writes x-overrideHost");
        Check(hit.bypassGateway, "matched session sets bypassGateway");
        Check(hit["ui-customcolumn"] == "Willow:Dev", "session list custom column marks project");

        var wildcard = new Session { hostname = "foo.test.local", port = 80 };
        plugin.AutoTamperRequestBefore(wildcard);
        Check(wildcard["x-overrideHost"] == "10.0.0.5:80", "wildcard mapping with port completion");

        var disabled = new Session { hostname = "off.example.com", port = 80 };
        plugin.AutoTamperRequestBefore(disabled);
        Check(disabled["x-overrideHost"] == null, "disabled rule is ignored");

        var other = new Session { hostname = "other.com", port = 80 };
        plugin.AutoTamperRequestBefore(other);
        Check(other["x-overrideHost"] == null, "unmatched host is untouched");

        config.MappingEnabled = false;
        var switchedOff = new Session { hostname = "api.example.com", port = 80 };
        plugin.AutoTamperRequestBefore(switchedOff);
        Check(switchedOff["x-overrideHost"] == null, "master switch disables mapping");

        plugin.OnBeforeUnload();

        // 还原用户真实配置（OnBeforeUnload 会把测试配置写盘）。
        if (backup != null)
        {
            File.WriteAllText(configPath, backup);
        }
        else if (File.Exists(configPath))
        {
            File.Delete(configPath);
        }

        Console.WriteLine();
        Console.WriteLine("FAILURES: " + _failures);
        return _failures == 0 ? 0 : 1;
    }
}
