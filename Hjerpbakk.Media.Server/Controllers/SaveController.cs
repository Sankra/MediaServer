﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Hjerpbakk.Media.Server.Clients;
using Hjerpbakk.Media.Server.Configuration;
using Hjerpbakk.Media.Server.Model;
using Hjerpbakk.Media.Server.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hjerpbakk.Media.Server.Controllers
{
    [Route("/[controller]")]
    public class SaveController : Controller
    {
        readonly SlackIntegration slackIntegration;
        readonly CloudStorageClient cloudStorageClient;
        readonly HttpClient httpClient;
        readonly IConferenceConfiguration conferenceConfiguration;

        public SaveController(SlackIntegration slackIntegration, CloudStorageClient cloudStorageClient, HttpClient httpClient, IConferenceConfiguration conferenceConfiguration)
        {
            this.slackIntegration = slackIntegration;
            this.cloudStorageClient = cloudStorageClient;
            this.httpClient = httpClient;
            this.conferenceConfiguration = conferenceConfiguration;
        }

        [HttpGet("/[controller]/{conferenceName}")]
        public async Task<IActionResult> Index(string conferenceName)
        {
            if (string.IsNullOrEmpty(conferenceName)) {
                throw new ArgumentNullException(nameof(conferenceName));
            }

            var conference = conferenceConfiguration.GetConference(conferenceName);
            var availableVideos = await cloudStorageClient.GetAvailableNewVideos(conference);
            ViewBag.VideoList = new SelectList(availableVideos, "Id", "Name");
            return View();
        }

        [HttpPost("/[controller]/{conferenceName}")]
        public async Task<IActionResult> Index(string conferenceName, [Bind("Title,Description,Author,Id,SpeakerDeckURL")] Talk hourOfInterest)
        {
            if (string.IsNullOrEmpty(hourOfInterest.Title) || string.IsNullOrEmpty(hourOfInterest.Description) || string.IsNullOrEmpty(hourOfInterest.Author) || string.IsNullOrEmpty(hourOfInterest.Id)) {
                // TODO: Clientside verification together with enabling of button
                // TODO: Error page...
                throw new Exception("Damned user...");
            }



            // TODO: Select video first
            // TODO: Populate title and speaker deck from files found on disk


            hourOfInterest.URL = hourOfInterest.GetURL(HttpContext.Request);
            var filePart = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + "/videos/" + hourOfInterest.Id;
            hourOfInterest.VideoURL = filePart + Video.SupportedFileType;
            hourOfInterest.ConferenceName = conferenceName;

            if (string.IsNullOrEmpty(hourOfInterest.SpeakerDeckURL)) {
                var potentialSpeakerDeckURL = filePart + Talk.SpeakerDeckFileType;
                var response = await httpClient.GetAsync(potentialSpeakerDeckURL);
                if (response.IsSuccessStatusCode) {
                    hourOfInterest.SpeakerDeckURL = potentialSpeakerDeckURL;    
                }
            }

            hourOfInterest.TimeStamp = DateTime.UtcNow;

            await cloudStorageClient.Save(hourOfInterest);

            await slackIntegration.PostInterestingHourToChannel(hourOfInterest);

            return new RedirectResult(hourOfInterest.URL, false, false);
        }

    }
}
