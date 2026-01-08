using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ESPFlasher.Services
{
    public class BuildInfoParser
    {
        private readonly ILogger _logger;
        
        public BuildInfoParser(ILogger logger)
        {
            _logger = logger;
        }
        
        public BuildInfo? ParseBuildInfoFile(string buildInfoPath)
        {
            try
            {
                if (!File.Exists(buildInfoPath))
                {
                    _logger.LogWarning($"build-info.txt not found: {buildInfoPath}");
                    return null;
                }
                
                var content = File.ReadAllText(buildInfoPath);
                var buildInfo = new BuildInfo();
                
                // Extract version (YYYYMMDD.BUILD format)
                var versionMatch = Regex.Match(content, @"Version:\s+(\S+)");
                if (versionMatch.Success)
                {
                    buildInfo.Version = versionMatch.Groups[1].Value.Trim();
                }
                
                // Extract build date
                var buildDateMatch = Regex.Match(content, @"Build Date:\s+(.+)");
                if (buildDateMatch.Success)
                {
                    var dateStr = buildDateMatch.Groups[1].Value.Trim();
                    if (DateTime.TryParse(dateStr, out var buildDate))
                    {
                        buildInfo.BuildDate = buildDate;
                    }
                }
                
                // Extract build number
                var buildNumberMatch = Regex.Match(content, @"Build Number:\s+(\d+)");
                if (buildNumberMatch.Success)
                {
                    if (int.TryParse(buildNumberMatch.Groups[1].Value, out var buildNumber))
                    {
                        buildInfo.BuildNumber = buildNumber;
                    }
                }
                
                // Extract firmware size
                var sizeMatch = Regex.Match(content, @"Firmware Size:\s+(\S+)");
                if (sizeMatch.Success)
                {
                    buildInfo.FirmwareSize = sizeMatch.Groups[1].Value.Trim();
                }
                
                // Extract git commit
                var commitMatch = Regex.Match(content, @"Git Commit:\s+(\S+)");
                if (commitMatch.Success)
                {
                    buildInfo.GitCommit = commitMatch.Groups[1].Value.Trim();
                }
                
                // Extract git branch
                var branchMatch = Regex.Match(content, @"Git Branch:\s+(\S+)");
                if (branchMatch.Success)
                {
                    buildInfo.GitBranch = branchMatch.Groups[1].Value.Trim();
                }
                
                // Validate required fields
                if (string.IsNullOrEmpty(buildInfo.Version))
                {
                    _logger.LogWarning($"build-info.txt missing Version field: {buildInfoPath}");
                    return null;
                }
                
                return buildInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to parse build-info.txt: {buildInfoPath}");
                return null;
            }
        }
    }
    
    public class BuildInfo
    {
        public string Version { get; set; } = string.Empty;
        public DateTime BuildDate { get; set; }
        public int BuildNumber { get; set; }
        public string FirmwareSize { get; set; } = string.Empty;
        public string GitCommit { get; set; } = string.Empty;
        public string GitBranch { get; set; } = string.Empty;
    }
}
