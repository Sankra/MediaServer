﻿using MediaServer.Models;
using Microsoft.AspNetCore.Http;

namespace MediaServer.Extensions {
    public static class HttpContextExtensions {
		// TODO: Create proper pathing abstraction...
        public static string GetConferenceUrl(Conference conference) 
            => $"/Conference/{conference.Id}/";

        public static string GetThumbnailUrl(Conference conference, Talk talk)
            => GetConferenceUrl(conference) + "Thumbnails/" + talk.TalkName;

        public static string GetTalkUrl(Conference conference, Talk talk)
            => GetConferenceUrl(conference) + talk.TalkName;
    }
}
