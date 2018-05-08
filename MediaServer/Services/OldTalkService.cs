﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaServer.Configuration;
using MediaServer.Extensions;
using MediaServer.Models;
using MediaServer.Services.Cache;
using MediaServer.Services.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MediaServer.Services
{
	public class OldTalkService : IOldTalkService
    {
		readonly CloudBlobClient cloudBlobClient;
		readonly ThumbnailService thumbnailService;
		readonly MediaCache cache;
		readonly BlobStoragePersistence blobStoragePersistence;
      
		public OldTalkService(IBlogStorageConfig blobStorageConfig, ThumbnailService thumbnailService, MediaCache cache, BlobStoragePersistence blobStoragePersistence)
		{
			var storageAccount = CloudStorageAccount.Parse(blobStorageConfig.BlobStorageConnectionString);         
            cloudBlobClient = storageAccount.CreateCloudBlobClient();
			this.thumbnailService = thumbnailService;
			this.cache = cache;
			this.blobStoragePersistence = blobStoragePersistence;
		}

		public async Task<IEnumerable<Talk>> GetTalksFromConference(Conference conference) {
			var talks = await blobStoragePersistence.GetTalksFromConferences(conference);
			return talks.OrderByDescending(talk => talk.DateOfTalk);
		}

		public async Task<Talk> GetTalkByName(Conference conference, string name)
		    => await blobStoragePersistence.GetTalkByName(conference, name);
        
        public async Task SaveTalkFromConference(Conference conference, Talk talk)
		    => await blobStoragePersistence.SaveTalkFromConference(conference, talk);

        public async Task DeleteTalkFromConference(Conference conference, Talk talk)
		    => await blobStoragePersistence.DeleteTalk(conference, talk);  

        public async Task<IReadOnlyList<string>> GetTalkNamesFromConference(Conference conference)
		{
			var usedVideos = await cache.GetOrSet(
				cache.GetTalkNamesKey(conference.Id),
				() => GetUsedVideosFromConference(conference));
			return usedVideos;         
		}
      
		// TODO: Create generic get talk with optional predicate...
		// TODO: cache every talk
        public async Task<IEnumerable<LatestTalk>> GetLatestTalks(IEnumerable<Conference> conferences) {			
            var talks = new List<LatestTalk>();
            foreach (var conference in conferences) {
				var containerForConference = cloudBlobClient.GetContainerForConference(conference);
                await containerForConference.CreateIfNotExistsAsync();
                // TODO: Support more than 200 items....
                var token = new BlobContinuationToken();
				var blobs = await containerForConference.ListBlobsSegmentedAsync(BlobStoragePersistence.TalkPrefix, token); 
                foreach (var blob in blobs.Results.Cast<CloudBlockBlob>()) {
                    using (var memoryStream = new MemoryStream()) {
                        await blob.DownloadToStreamAsync(memoryStream);
                        var talkContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                        var talk = JsonConvert.DeserializeObject<Talk>(talkContent);
						talk.ConferenceId = conference.Id;
                        var latestTalk = new LatestTalk(conference, talk, blob.Properties.LastModified.Value);
                        talks.Add(latestTalk);
                    }
                }
            }

			return talks.OrderByDescending(t => t.TimeStamp).Take(9);
        }

		async Task<IReadOnlyList<string>> GetUsedVideosFromConference(Conference conference)
        {
            // TODO: Support more than 200 items
            var token = new BlobContinuationToken();
            var containerForConference = cloudBlobClient.GetContainerForConference(conference);
            var blobs = await containerForConference.ListBlobsSegmentedAsync(BlobStoragePersistence.TalkPrefix, token);
            var usedVideos = new List<string>();
            foreach (var blob in blobs.Results.Cast<CloudBlockBlob>())
            {
                using (var memoryStream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(memoryStream);
                    var talkContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                    var talk = JObject.Parse(talkContent);
                    var videoName = (string)talk["VideoName"];
                    usedVideos.Add(videoName);
                }
            }

            return usedVideos;
        }
    }
}
