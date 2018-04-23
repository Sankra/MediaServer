﻿using System;
using System.IO;

namespace MediaServer.Models
{
	public struct Video : IEquatable<Video>
    {
        public const string SupportedVideoFileType = ".mp4";

		readonly static char[] supportedVideoFileType;

		static Video()
        {
            supportedVideoFileType = SupportedVideoFileType.ToCharArray();
        }
        
        public Video(string name)
        {
            Name = Path.GetFileNameWithoutExtension(name) + SupportedVideoFileType;
        }

        public string Name { get; }

		public bool Equals(Video other) => Name == other.Name;

		public override bool Equals(object obj)
		{
			if (!(obj is Video)) {
				return false;
			}

			return Equals((Video)obj);
		}

		public override int GetHashCode() => Name.GetHashCode();      
	}
}
