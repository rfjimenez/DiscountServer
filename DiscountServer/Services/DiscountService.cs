using DiscountServer.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace DiscountServer.Services
{
    /// <summary>
    /// Service responsible for generating, storing, and validating discount codes.
    /// Codes are persisted to disk and survive service restarts.
    /// </summary>
    public class DiscountService
    {
        // Minimum and maximum allowed code length
        private const int MinCodeLength = 7;
        private const int MaxCodeLength = 8;

        // Maximum number of codes allowed per generation request
        private const int MaxCodesPerRequest = 2000;

        // Path to the persistent storage file
        private readonly string _storagePath;

        // Lock object for thread safety
        private readonly object _lock = new();

        // Dictionary of codes and their usage status (false = unused, true = used)
        private Dictionary<string, bool> _codes = new();

        /// <summary>
        /// Initializes the service, loads codes from persistent storage.
        /// </summary>
        /// <param name="configuration">Application configuration for storage path.</param>
        public DiscountService(IConfiguration configuration)
        {
            _storagePath = configuration["DiscountCodeStorage:Path"] ?? "Storage/discount_codes.json";
            EnsureStorageFolder();
            LoadCodes();
        }

        /// <summary>
        /// Generates a list of unique, random discount codes.
        /// Codes are persisted and can only be used once.
        /// </summary>
        /// <param name="count">Number of codes to generate (max 2000).</param>
        /// <param name="length">Length of each code (7-8).</param>
        /// <returns>List of generated codes, or empty list if parameters are invalid.</returns>
        public virtual List<string> GenerateCodes(ushort count, byte length)
        {
            if (count == 0 || count > MaxCodesPerRequest || length < MinCodeLength || length > MaxCodeLength)
                return new List<string>();

            var newCodes = new List<string>();
            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                {
                    string code;
                    // Ensure code uniqueness
                    do
                    {
                        code = GenerateRandomCode(length);
                    } while (_codes.ContainsKey(code));

                    _codes[code] = false; // Mark as unused
                    newCodes.Add(code);
                }
                SaveCodes();
            }
            return newCodes;
        }

        /// <summary>
        /// Marks a discount code as used if it exists and is unused.
        /// </summary>
        /// <param name="code">Discount code to use.</param>
        /// <returns>Result indicating success, already used, not found, or invalid request.</returns>
        public virtual DiscountCodeResult UseCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return DiscountCodeResult.InvalidRequest;

            lock (_lock)
            {
                if (!_codes.TryGetValue(code, out var used))
                    return DiscountCodeResult.NotFound;
                if (used)
                    return DiscountCodeResult.AlreadyUsed;

                _codes[code] = true; // Mark as used
                SaveCodes();
            }
            return DiscountCodeResult.Success;
        }

        /// <summary>
        /// Generates a random alphanumeric code of the specified length.
        /// </summary>
        /// <param name="length">Length of the code.</param>
        /// <returns>Randomly generated code.</returns>
        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var sb = new StringBuilder(length);
            foreach (var b in bytes)
                sb.Append(chars[b % chars.Length]);
            return sb.ToString();
        }

        /// <summary>
        /// Loads codes and their usage status from persistent storage.
        /// </summary>
        private void LoadCodes()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    _codes = JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new();
                }
            }
            catch
            {
                // If loading fails, start with an empty dictionary
                _codes = new Dictionary<string, bool>();
            }
        }

        /// <summary>
        /// Saves codes and their usage status to persistent storage.
        /// </summary>
        private void SaveCodes()
        {
            try
            {
                EnsureStorageFolder(); // Ensure folder exists before saving
                var json = JsonSerializer.Serialize(_codes);
                File.WriteAllText(_storagePath, json);
            }
            catch
            {
                // Optionally log error
            }
        }

        /// <summary>
        /// Ensures the storage folder exists before reading/writing codes.
        /// </summary>
        private void EnsureStorageFolder()
        {
            var folder = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }
    }
}