﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace N2.Web.Parsing.Wiki
{
	public class InternalLinkAnalyzer : StartStopAnalyzerBase
	{
		public InternalLinkAnalyzer()
			: base("[[", "]]")
		{
		}
	}
}
