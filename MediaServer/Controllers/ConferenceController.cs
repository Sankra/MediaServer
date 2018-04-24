﻿using System;
using System.Linq;
using MediaServer.Configuration;
using MediaServer.Extensions;
using MediaServer.Models;
using MediaServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MediaServer.Controllers
{
	public class ConferenceController : Controller
	{
		readonly IConferenceConfig conferenceConfig;

		readonly ITalkService talkService;
		readonly IContentService contentService;
		readonly ISlackService slackService;
        
		public ConferenceController(IConferenceConfig conferenceConfig, ITalkService talkService, IContentService contentService, ISlackService slackService)
		{
			// TODO: Too many services, move around?
			this.conferenceConfig = conferenceConfig;
			this.talkService = talkService;
			this.contentService = contentService;
			this.slackService = slackService;        
		}

		[HttpGet("/[controller]/{conferenceId}")]
		public async Task<IActionResult> GetConferenceView(string conferenceId)
		{
            // TODO: Move O: link to bottom
            // TODO: Make O: link clickable
            // TODO: Button for add is blue on click
            // TODO: Button for add is ugly
            // TODO: Phone home so we can see most popular conference
			var conference = GetConferenceFromId(conferenceId);
			if (conference == null)
			{
				return NotFound();
			}

            // TODO: Create a conference viewmodel and use model binding
			ViewData["Title"] = conference.Name;
			ViewData["ConferenceId"] = conferenceId;
            ViewData["VideoPath"] = conference.VideoPath;

            var talks = (await talkService.GetTalksFromConference(conference))
                .OrderByDescending(talk => talk.DateOfTalk)
                .Select(talk => new TalkSummaryViewModel(conference, talk, HttpContext));
			ViewData["Talks"] = talks;

			return View("Index");
		}

		[HttpGet("/[controller]/{conferenceId}/{talkName}")]
		public async Task<IActionResult> GetTalkView(string conferenceId, string talkName)
		{
            // TODO: Phone home so we can see most popular talk
            // TODO: Show image of speaker from Slack
			var conference = GetConferenceFromId(conferenceId);
            if (conference == null)
            {
                return NotFound();
            }

			var talk = await talkService.GetTalkByName(conference, talkName);
			if (talk == null) {
				// TODO: Lag en fin 404
                return NotFound();
            }

            /// TODO: Support single click pause / resume
            /// 
            /// TODO: hotkeys:
           // -space: play / pause
           //- f: fullscreen
           //- opp / ned: volumkontroll
             //- venstre / høyre: skip back/ frem 5 sec elns
            /// 
            ViewData["Title"] = talk.TalkName;
			ViewData["ConferenceId"] = conferenceId;
			var talkVM = new TalkViewModel(talk);
			ViewData["Talk"] = talkVM;

			return View("Talk");
		}

        [HttpGet("/[controller]/{conferenceId}/Thumbnails/{talkName}")]
        public async Task<IActionResult> GetTalkThumbnail(string conferenceId, string talkName)
        {
            var conference = GetConferenceFromId(conferenceId);
            var thumbnail = await talkService.GetTalkThumbnail(conference, talkName);
            return File(thumbnail.ImageData, thumbnail.ContentType);
        }

        [HttpGet("/[controller]/{conferenceId}/{talkName}/Edit")]
		public async Task<IActionResult> GetEditView(string conferenceId, string talkName)
		{
            // TODO: SpeakerDeck dissapears if not from PDF
			var conference = GetConferenceFromId(conferenceId);
            if (conference == null)
            {
                return NotFound();
            }

            var talk = await talkService.GetTalkByName(conference, talkName);
            if (talk == null)
            {
                return NotFound();
            }

            talk.Thumbnail = HttpContext.GetThumbnailUrl(conference, talk);
            var controllerName = ControllerContext.RouteData.Values["controller"].ToString();
            var availableVideos = new List<Video>() { new Video(talk.VideoName) };
			var videosFromConference = await contentService.GetVideosFromConference(controllerName, conference);
			availableVideos.AddRange(videosFromConference);
            ViewBag.VideoList = new SelectList(availableVideos, "Name", "Name", talk.VideoName);
                     
            ViewData["Title"] = $"Edit {talk.TalkName}"; 
			ViewData["ConferenceId"] = conferenceId;
			ViewData["IsSave"] = false;
            ViewData["OldName"] = talk.TalkName;
			return View("Save", talk);
		}

		[HttpGet("/[controller]/{conferenceId}/Save")]
		public async Task<IActionResult> GetSaveView(string conferenceId)
		{
            // TODO: Support choosing speaker name from Slack...
			// TODO: Support uploading slides
			// TODO: Support uploading video
            var conference = GetConferenceFromId(conferenceId);
			if (conference == null)
			{
				return NotFound();
			}

            var controllerName = ControllerContext.RouteData.Values["controller"].ToString();
			var availableVideos = await contentService.GetVideosFromConference(controllerName, conference);
			ViewBag.VideoList = new SelectList(availableVideos, "Name", "Name");

			ViewData["Title"] = $"Create new talk from {conference.Name}";
			ViewData["ConferenceId"] = conferenceId;
			ViewData["IsSave"] = true;         
			ViewData["OldName"] = null;
            return View("Save", new Talk { Thumbnail = "/Placeholder.png" });
		}

		[HttpPost("/[controller]/{conferenceId}/Save")]
        public async Task<IActionResult> SaveTalk(string conferenceId, [FromQuery] string oldName, [Bind("VideoName, Description, Speaker, SpeakerDeck, ThumbnailImageFile, TalkName, DateOfTalkString")] Talk talk)
		{
            // TODO: Get thumbnail from speaker notes or video if not set
			var conference = conferenceConfig.Conferences[conferenceId];

			if (oldName != null)
			{
                var oldTalk = new Talk { TalkName = oldName };
				await talkService.DeleteTalkFromConference(conference, oldTalk);
            }

            // TODO: Client verification also and proper replace here
            talk.TalkName = talk.TalkName.Replace("?", "").Replace(":", "");
			await talkService.SaveTalkFromConference(conference, talk);
            
			var talkUrl = HttpContext.GetTalkUrl(conference, talk);

            if (oldName == null)
            {
                await slackService.PostTalkToChannel(conference, talk, talkUrl);
            }

            var escapedTalkName = Uri.EscapeUriString(talk.TalkName);
            return new RedirectResult(escapedTalkName, false, false);
		}

		Conference GetConferenceFromId(string conferenceId)
			=> conferenceConfig.Conferences.ContainsKey(conferenceId) ?
			   conferenceConfig.Conferences[conferenceId] :
			   null;
    }
}
