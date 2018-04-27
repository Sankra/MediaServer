﻿using System;
using System.Threading.Tasks;
using MediaServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace MediaServer.Services.Cache
{
    public class TalkCache
    {
		const string LatestTalksKey = "lastettalks";

		readonly IMemoryCache memoryCache;      
        readonly MemoryCacheEntryOptions options;

        public TalkCache(IMemoryCache memoryCache)
        {
			this.memoryCache = memoryCache;
			options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromDays(730))
                .SetSlidingExpiration(TimeSpan.FromDays(365));
        }
        
		// TODO: remove if not needed
		public ViewResult GetOrSetView(string key, Func<ViewResult> create) {
			if (!memoryCache.TryGetValue(key, out ViewResult view))
			{
				view = create();
				memoryCache.Set(key, view, options);
			}

			return view;
		}

		public async Task<IActionResult> GetOrSetView(Func<Task<IActionResult>> create)
            => await GetOrSetView(LatestTalksKey, create);
        
		public async Task<IActionResult> GetOrSetView(string key, Func<Task<IActionResult>> create)
        {
			if (!memoryCache.TryGetValue(key, out IActionResult view))
            {
				view = await create();
				memoryCache.Set(key, view, options);
            }

            return view;
        }

		public async Task<Talk> GetOrSetTalk(string key, Func<Task<Talk>> create) {
			if (!memoryCache.TryGetValue(key, out Talk talk))
            {
				talk = await create();
				memoryCache.Set(key, talk, options);
            }

			return talk;
		}

		public void ClearCachesForTalk(Talk talk) {
			memoryCache.Remove(LatestTalksKey);
			memoryCache.Remove(talk.Speaker);
			memoryCache.Remove(talk.ConferenceId);
			memoryCache.Remove(GetTalkKey(talk.ConferenceId, talk.TalkName));         
		}

		public static string GetTalkKey(string conferenceId, string talkName)
		    => conferenceId = talkName;
    }
}
