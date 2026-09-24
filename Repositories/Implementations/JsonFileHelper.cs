using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CtrlEat.Repositories.Implementations
{
    /// <summary>
    /// Utility class for JSON file operations
    /// Handles reading and writing JSON data with file locking
    /// </summary>
    public class JsonFileHelper
    {
        private readonly string _filePath;
        private readonly object _lockObject = new object();

        public JsonFileHelper(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            EnsureFileExists();
        }

        /// <summary>
        /// Ensure the JSON file exists, creating it if necessary
        /// </summary>
        private void EnsureFileExists()
        {
            lock (_lockObject)
            {
                if (!File.Exists(_filePath))
                {
                    var directory = Path.GetDirectoryName(_filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(_filePath, "[]");
                }
            }
        }

        /// <summary>
        /// Read and deserialize JSON data from file
        /// </summary>
        public List<T> ReadData<T>()
        {
            lock (_lockObject)
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new List<T>();
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    return JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to read JSON from {_filePath}", ex);
                }
            }
        }

        /// <summary>
        /// Serialize and write data to JSON file
        /// </summary>
        public void WriteData<T>(List<T> data)
        {
            lock (_lockObject)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var json = JsonSerializer.Serialize(data, options);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to write JSON to {_filePath}", ex);
                }
            }
        }
    }
}
