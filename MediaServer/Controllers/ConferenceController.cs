﻿using System;
using System.Linq;
using System.Diagnostics;
using MediaServer.Configuration;
using MediaServer.Models;
using MediaServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using System.Collections.Generic;

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
			var conference = GetConferenceFromId(conferenceId);
			if (conference == null)
			{
				return NotFound();
			}

            // TODO: Create a conference viewmodel and use model binding
			ViewData["Title"] = conference.Name;
			ViewData["ConferenceId"] = conferenceId;
            ViewData["VideoPath"] = conference.VideoPath;

            var talks = (await talkService.GetTalksFromConference(conference)).
                            OrderByDescending(t => t.DateOfTalk).
                            Select(t => new TalkSummaryViewModel(t, talk => GetTalkUrl(conference, talk)));
			ViewData["Talks"] = talks;

			return View("Index");
		}

		[HttpGet("/[controller]/{conferenceId}/{talkName}")]
		public async Task<IActionResult> GetTalkView(string conferenceId, string talkName)
		{
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

            ViewData["Title"] = talk.TalkName;
			ViewData["ConferenceId"] = conferenceId;
			var talkVM = new TalkViewModel(talk);
			ViewData["Talk"] = talkVM;

			return View("Talk");
		}

        [HttpGet("/[controller]/{conferenceId}/{talkName}/Edit")]
		public async Task<IActionResult> GetEditView(string conferenceId, string talkName)
		{
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
			return View("Save", new Talk());
		}

		[HttpPost("/[controller]/{conferenceId}/Save")]
        public async Task<IActionResult> SaveTalk(string conferenceId, [FromQuery] string oldName, [Bind("VideoName, Description, Speaker, SpeakerDeck, ThumbnailImageFile, TalkName, DateOfTalkString")] Talk talk)
		{
			var conference = conferenceConfig.Conferences[conferenceId];

			if (oldName != null)
			{
                var oldTalk = new Talk { TalkName = oldName };
				await talkService.DeleteTalkFromConference(conference, oldTalk);
            }

            talk.TalkName = talk.TalkName.Remove('?');
			await talkService.SaveTalkFromConference(conference, talk);
            
			var talkUrl = GetTalkUrl(conference, talk);

            if (oldName == null)
            {
                await slackService.PostTalkToChannel(conference, talk, talkUrl);
            }

            return new RedirectResult(talk.TalkName, false, false);
		}

		Conference GetConferenceFromId(string conferenceId)
			=> conferenceConfig.Conferences.ContainsKey(conferenceId) ?
			   conferenceConfig.Conferences[conferenceId] :
			   null;

		string GetTalkUrl(Conference conference, Talk talk)
            => GetConferenceUrl(conference) + talk.TalkName;

		string GetConferenceUrl(Conference conference) 
			=> $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/{ControllerContext.RouteData.Values["controller"]}/{conference.Id}/";
    }
}
