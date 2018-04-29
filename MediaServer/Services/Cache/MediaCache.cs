﻿using System;
using System.Threading.Tasks;
using MediaServer.Models;
using MediaServer.Services.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace MediaServer.Services.Cache
{
    public class MediaCache
    {
		public const string LatestTalksKey = "lastettalks";

		readonly IMemoryCache memoryCache;      

		readonly MemoryCacheEntryOptions options;

        public MediaCache(IMemoryCache memoryCache)
        {
			this.memoryCache = memoryCache;
			options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromDays(730))
                .SetSlidingExpiration(TimeSpan.FromDays(365));
        }
                
		public async Task<T> GetOrSet<T>(string key, Func<Task<T>> create)
        {
			if (!memoryCache.TryGetValue(key, out T view))
            {
				Console.WriteLine($"Cache miss for {key}");
				view = await create();
				memoryCache.Set(key, view, options);
            }

            return view;
        }

		public void CacheTalk(Talk talk) {
			var key = ClearCache(talk);
			memoryCache.Set(key, talk, options);
		}

		public string ClearCache(Talk talk) {
			var talkKey = GetTalkKey(talk.ConferenceId, talk.TalkName);
            memoryCache.Remove(LatestTalksKey);
            memoryCache.Remove(talk.Speaker);
            memoryCache.Remove(talk.ConferenceId);
			memoryCache.Remove(GetTalkViewKey(talk.ConferenceId, talk.TalkName));
			memoryCache.Remove(GetTalkNamesKey(talk.ConferenceId));
            memoryCache.Remove(talkKey);
			return talkKey;
		}

		public void ClearForThumbnail(Talk talk) {
			memoryCache.Remove(BlobStoragePersistence.GetThumbnailKey(talk.TalkName));
			memoryCache.Remove(BlobStoragePersistence.GetThumnnailHashName(talk.TalkName));
		}

		public string GetTalkKey(string conferenceId, string talkName)
		    => talkName + conferenceId;

		public string GetTalkViewKey(string conferenceId, string talkName)
            => "view" + talkName + conferenceId;

		public string GetTalkNamesKey(string conferenceId)
		    => "names" + conferenceId;
    }
}
