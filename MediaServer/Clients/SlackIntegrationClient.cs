﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MediaServer.Models;
using Newtonsoft.Json;

namespace MediaServer.Clients {
	public class SlackIntegrationClient {
        readonly HttpClient httpClient;

		public SlackIntegrationClient(HttpClient httpClient) {
            this.httpClient = httpClient;
        }

		public void PublishToSlack(Talk talk, string talkUrl) {
            try {
				var talkMetadata = new TalkMetadata(talk.ConferenceId, 
				                                    talk.TalkName, 
				                                    talkUrl, 
				                                    talk.Description, 
				                                    talk.Speaker);
                var metadataAsJson = JsonConvert.SerializeObject(talkMetadata);
                var content = new StringContent(metadataAsJson, Encoding.UTF8, "application/json");
				httpClient.PostAsync("http://slack-integration:1338/Slack", content);
            }
            catch (Exception ex) {
				Console.WriteLine($"Slack Integration failed for {talk} {ex}");
            }
        }

		public async Task<User[]> GetUsers() {
			try
            {
                var response = await httpClient.GetStringAsync("http://slack-integration:1338/Slack");
				var users = JsonConvert.DeserializeObject<User[]>(response);
				return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not get users from Slack {ex}");
				return new User[0];
            }
		}
    }
}
