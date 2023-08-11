using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.UI;
using Sandbox;
using Sandbox.UI.Construct;

namespace Jigsaw
{
	public partial class ScoreboardEntry : Panel
	{
		public IClient Client;

		public Label PlayerName;
		public Label Connections;
		public Label Deaths;
		public Label Ping;

		public ScoreboardEntry()
		{
			AddClass( "entry" );

			PlayerName = Add.Label( "PlayerName", "name" );
			Connections = Add.Label( "", "connections" );
			Ping = Add.Label( "", "ping" );
		}

		RealTimeSince TimeSinceUpdate = 0;

		public override void Tick()
		{
			base.Tick();

			if ( !IsVisible )
				return;

			if ( !Client.IsValid() )
				return;

			if ( TimeSinceUpdate < 0.1f )
				return;

			TimeSinceUpdate = 0;
			UpdateData();
		}

		public virtual void UpdateData()
		{
			PlayerName.Text = Client.Name;
			Connections.Text = Client.GetInt( "connections" ).ToString();
			Ping.Text = Client.Ping.ToString();
			SetClass( "me", Client == Game.LocalClient );
		}

		public virtual void UpdateFrom( IClient client )
		{
			Client = client;
			UpdateData();
		}
	}
}
