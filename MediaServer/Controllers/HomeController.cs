﻿using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MediaServer.Models;
using MediaServer.Services;
using MediaServer.Services.Cache;
using System.Collections.Generic;
using System;
using MediaServer.ViewModels;

namespace MediaServer.Controllers
{
	public class HomeController : NavigateableController
    {
		readonly ConferenceService conferenceService;
		readonly MediaCache cache;
        
		public HomeController(Dictionary<string, Conference> conferences, ConferenceService conferenceService, MediaCache talkCache) 
			: base(conferences) {
			this.conferenceService = conferenceService;
			this.cache = talkCache;
        }
        
		[ResponseCache(NoStore = true)]      
        public async Task<IActionResult> Index() {
			Console.WriteLine("GetLatestTalks ");
			var view = await cache.GetOrSet(MediaCache.LatestTalksKey, GetView);
            return view;
        }

		public IActionResult Error() {
			SetCurrentNavigation("Error");
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
                  
		async Task<IActionResult> GetView() {
			SetCurrentNavigationToHome();         
            ViewData["Talks"] = await conferenceService.GetLatestTalks();         
            // TODO: Use model binding
            return View();
		}
    }
}
