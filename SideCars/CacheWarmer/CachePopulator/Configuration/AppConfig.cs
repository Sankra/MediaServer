﻿namespace CachePopulator.Configuration
{
	public class AppConfig : IBlogStorageConfig
    {
        public string BlobStorageConnectionString { get; set; }
    }
}
