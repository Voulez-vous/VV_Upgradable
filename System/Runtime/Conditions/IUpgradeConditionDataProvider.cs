using System.Collections.Generic;

namespace VV.Upgradable
{
    public interface IUpgradeConditionDataProvider
    {
        /// <summary>
        /// Get a specific data value by key
        /// </summary>
        /// <param name="key">The data key to retrieve</param>
        /// <returns>The data value, or null if not found</returns>
        object GetData(string key);
        
        /// <summary>
        /// Get all available data keys
        /// </summary>
        /// <returns>Collection of available data keys</returns>
        IEnumerable<string> GetAvailableDataKeys();
        
        /// <summary>
        /// Check if a specific data key exists
        /// </summary>
        /// <param name="key">The data key to check</param>
        /// <returns>True if the key exists</returns>
        bool HasData(string key);
    }
}