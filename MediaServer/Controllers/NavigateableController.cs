﻿using System.Collections.Generic;
using System.Linq;
using MediaServer.Configuration;
using MediaServer.Models;
using MediaServer.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaServer.Controllers {
	public abstract class NavigateableController : Controller {
		public const string Conference = "Conference";

		protected readonly Dictionary<string, Conference> conferences;      
		readonly int lastNavigation;      
        
		// TODO: Inject or something #singleton         
		protected NavigateableController(Dictionary<string, Conference> conferences) {
			this.conferences = conferences;
			var navigations = new List<Navigation>(conferences.Count + 2) {
				new Navigation("Index", "Latest Talks", "Home")            
			};
			navigations.AddRange(conferences.Values.Select(c => new Navigation(c, "Conference")));
			navigations.Add(new Navigation("List", "Speakers", "Speaker"));
			Navigations = navigations;
			lastNavigation = navigations.Count - 1;
		}

		public List<Navigation> Navigations { get; }
               
		protected void SetCurrentNavigationToHome() {
			var navigation = Navigations[0];
			SetCurrentNavigation(navigation);
		}

		protected void SetCurrentNavigationToSpeakerList() {
			var navigation = Navigations[lastNavigation];
            SetCurrentNavigation(navigation);
		}

		protected void SetCurrentNavigation(Conference conference, string title) {
			SetTitle(title);
			SetSlug(conference);
			SetAvailableMenuItems();
		}

		protected void SetCurrentNavigation(string title) {
			SetTitle(title);
			ClearCurrentMenuItem();
			SetAvailableMenuItems();
		}      

		protected bool ConferenceExists(string conferenceId) => conferences.ContainsKey(conferenceId);
		protected Conference GetConferenceFromId(string conferenceId) => conferences[conferenceId];

		protected IActionResult PageNotFound() {
			SetCurrentNavigation("Page Not Found");
            Response.StatusCode = 404;
            return View("NotFoundView");
		}

		void SetSlug(Conference conference) => ViewData["Slug"] = conference.Id;
		void ClearCurrentMenuItem() => ViewData["Slug"] = string.Empty;
		void SetAvailableMenuItems() => ViewData["Navigations"] = Navigations;
		void SetTitle(string title) => ViewData["Title"] = title;

		// TODO: Improve entire setup here, ref use of conference and dictionary
		void SetCurrentNavigation(Navigation navigation) {
			ViewData["Title"] = navigation.Title;
			ViewData["Slug"] = navigation.Slug;
			ViewData["Navigations"] = Navigations;
		}

		// TODO: Code special words like Conference only once
        // TODO: Use this everywhere
        // TODO: Make less brittle
        // TODO: Remember that we have Navigation
        public static string GetConferenceUrl(string conferenceId)
            => "/" + Conference + "/" + conferenceId + "/";

        public static string GetThumbnailUrl(Talk talk)
            => GetConferenceUrl(talk.ConferenceId) + "Thumbnails/" + talk.TalkName;

        public static string GetTalkUrl(Talk talk)
            => GetConferenceUrl(talk.ConferenceId) + talk.TalkName;

        public static string GetUrlToTalk(HttpContext httpContext, string urlPart)
            => httpContext.Request.Scheme + "://" + httpContext.Request.Host + urlPart;

	}
}
