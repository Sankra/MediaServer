﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaServer.Models;
using MediaServer.Services;
using MediaServer.Services.Cache;
using Microsoft.AspNetCore.Mvc;

namespace MediaServer.Controllers
{
	public class SpeakerController : NavigateableController {
		readonly MediaCache talkCache;      
		readonly SpeakerService speakerService;

		public SpeakerController(Dictionary<string, Conference> conferences, MediaCache talkCache, SpeakerService speakerService)
			: base(conferences) {                  
			this.talkCache = talkCache;                 
			this.speakerService = speakerService;
		}
              
		[ResponseCache(NoStore = true)]
		[HttpGet("/[controller]/{speakerName}")]
		public async Task<IActionResult> Index(string speakerName) {
			Console.WriteLine("GetTalksForSpeaker " + speakerName);
			return await talkCache.GetOrSet(speakerName, () => GetViewForSpeaker());
                        
			async Task<IActionResult> GetViewForSpeaker() {
                SetCurrentNavigation(speakerName);
				var talksByUser = await speakerService.GetTalksBySpeaker(speakerName);
				return View(talksByUser);
            }
		}
        
		[ResponseCache(NoStore = true)]
		[HttpGet("/[controller]/List")]
		public async Task<IActionResult> List() {
			Console.WriteLine("GetSpeakers");
			return await talkCache.GetOrSet(MediaCache.SpeakersKey, () => List());
            
			async Task<IActionResult> List() {            
				SetCurrentNavigationToSpeakerList();            
				var speakers = await speakerService.GetSpeakers();            
				return View("List", speakers);
			}
		}
	}
}
