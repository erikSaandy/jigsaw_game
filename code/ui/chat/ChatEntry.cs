﻿using Sandbox.UI.Construct;
using Sandbox.UI;
using Sandbox;


namespace Jigsaw
{
	public partial class ChatEntry : Panel
	{
		public Label NameLabel { get; internal set; }
		public Label Message { get; internal set; }
		public Image Avatar { get; internal set; }
		public Image LeaderImage { get; internal set; }

		public RealTimeSince TimeSinceBorn = 0;

		public ChatEntry()
		{
			Avatar = Add.Image();
			LeaderImage = Add.Image( "LeaderImage", "leaderimage" );
			NameLabel = Add.Label( "Name", "name" );
			Message = Add.Label( "Message", "message" );
		}

		public override void Tick()
		{
			base.Tick();

			if ( TimeSinceBorn > 10 )
			{
				Delete();
			}
		}
	}
}
