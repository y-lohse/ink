﻿using System;
using System.Collections.Generic;

namespace inklecate2Sharp.Parsed
{
	public class Story : ContainerBase
	{
		public Story (List<Parsed.Object> toplevelObjects) : base(null, toplevelObjects)
		{
		}

		public Runtime.Story ExportRuntime()
		{
			// Get default implementation of runtimeObject, which calls ContainerBase's generation method
			var rootContainer = runtimeObject as Runtime.Container;

			// Replace runtimeObject with Story object instead of the Runtime.Container generated by Parsed.ContainerBase
			var runtimeStory = new Runtime.Story (rootContainer);
			runtimeObject = runtimeStory;

			return runtimeStory;
		}
	}
}
