﻿using MediaServer.Controllers;

namespace MediaServer.Models {
	public class TalkSummary : TalkModel {      
        public TalkSummary(Talk talk, string url, string thumbnail, string conferenceName)
			: base(talk) {
			Url = url;
			Thumbnail = thumbnail;
			ConferenceName = conferenceName;
			ConferenceUrl = NavigateableController.GetConferenceUrl(talk.ConferenceId);
        }

        public string Url { get; }
        public string Thumbnail { get; }        
		public string ConferenceName { get; }
		public string ConferenceUrl { get; }
    }
}
