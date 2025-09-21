using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ESPFlasher.Models;

namespace ESPFlasher.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _db;
        private readonly ILogger _logger;
        private static readonly string[] CandidateCollections = new[]
        {
            // Actual collection from user's screenshot
            "esp_datalogger_firmware",
            // Backward compatible fallback
            "firmware_versions"
        };

        public FirestoreService(string projectId, string credentialsPath, ILogger logger)
        {
            _logger = logger;
            
            try
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
                _db = FirestoreDb.Create(projectId);
                _logger.LogInformation("Firestore service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firestore service");
                throw;
            }
        }

        public async Task<List<FirmwareVersion>> GetFirmwareVersionsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching firmware versions from Firestore");
                var versions = new List<FirmwareVersion>();

                // Try candidate collections until one returns documents
                QuerySnapshot? firstNonEmpty = null;
                string? usedCollection = null;
                foreach (var colName in CandidateCollections)
                {
                    try
                    {
                        var snapshot = await _db.Collection(colName).GetSnapshotAsync();
                        if (snapshot.Documents.Count > 0)
                        {
                            firstNonEmpty = snapshot;
                            usedCollection = colName;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, $"Error querying collection '{colName}', will try next");
                    }
                }

                if (firstNonEmpty == null)
                {
                    _logger.LogWarning("No firmware documents found in candidate collections");
                    return versions;
                }

                foreach (var document in firstNonEmpty.Documents)
                {
                    if (!document.Exists) continue;
                    var mapped = MapDocumentToFirmwareVersion(document, usedCollection!);
                    if (mapped != null)
                    {
                        versions.Add(mapped);
                    }
                }

                // Order by ReleaseDate desc if present, else by Version desc (string compare)
                versions = versions
                    .OrderByDescending(v => v.ReleaseDate)
                    .ThenByDescending(v => v.Version)
                    .ToList();

                _logger.LogInformation($"Retrieved {versions.Count} firmware versions");
                return versions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch firmware versions");
                throw;
            }
        }

        public async Task<FirmwareVersion?> GetLatestFirmwareVersionAsync()
        {
            try
            {
                // Reuse GetFirmwareVersionsAsync ordering
                var all = await GetFirmwareVersionsAsync();
                if (all.Count == 0) return null;

                // Prefer released=true if available
                var releasedLatest = all.FirstOrDefault(v => v.IsLatest);
                if (releasedLatest != null) return releasedLatest;

                return all.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch latest firmware version");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                foreach (var col in CandidateCollections)
                {
                    try
                    {
                        await _db.Collection(col).Limit(1).GetSnapshotAsync();
                        return true;
                    }
                    catch { /* try next */ }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firestore connection test failed");
                return false;
            }
        }

        private FirmwareVersion? MapDocumentToFirmwareVersion(DocumentSnapshot doc, string collectionName)
        {
            try
            {
                // Read raw dictionary for flexible field names
                var data = doc.ToDictionary();
                var fv = new FirmwareVersion();

                // Common mapping from screenshot: version, url, notes, timestamp, released
                if (data.TryGetValue("version", out var verObj) && verObj is string ver)
                {
                    fv.Version = ver;
                }
                else
                {
                    fv.Version = doc.Id; // fallback to doc id
                }

                if (data.TryGetValue("url", out var urlObj) && urlObj is string url)
                {
                    fv.StorageUrl = url;
                }
                else if (data.TryGetValue("storageUrl", out var surlObj) && surlObj is string surl)
                {
                    fv.StorageUrl = surl;
                }

                if (data.TryGetValue("notes", out var notesObj) && notesObj is string notes)
                {
                    fv.Description = notes;
                }
                else if (data.TryGetValue("description", out var descObj) && descObj is string desc)
                {
                    fv.Description = desc;
                }

                if (data.TryGetValue("timestamp", out var tsObj) && tsObj is Timestamp ts)
                {
                    fv.ReleaseDate = ts.ToDateTime();
                }
                else if (data.TryGetValue("releaseDate", out var rdObj) && rdObj is Timestamp rdTs)
                {
                    fv.ReleaseDate = rdTs.ToDateTime();
                }
                else
                {
                    fv.ReleaseDate = DateTime.MinValue;
                }

                if (data.TryGetValue("released", out var relObj) && relObj is bool released)
                {
                    fv.IsLatest = released; // treat released=true as candidate for latest
                }
                else if (data.TryGetValue("isLatest", out var ilObj) && ilObj is bool il)
                {
                    fv.IsLatest = il;
                }

                // Optional fields
                if (data.TryGetValue("fileSize", out var fsObj))
                {
                    if (fsObj is long l) fv.FileSize = l;
                    else if (fsObj is int i) fv.FileSize = i;
                }

                if (data.TryGetValue("checksum", out var csObj) && csObj is string cs)
                {
                    fv.Checksum = cs;
                }

                return fv;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to map document '{doc.Id}' in collection '{collectionName}'");
                return null;
            }
        }
    }
}
