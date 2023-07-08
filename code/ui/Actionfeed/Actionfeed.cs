using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Jigsaw;

public partial class Actionfeed : Panel
{

	public static Actionfeed Current { get; set; }

	public Actionfeed()
	{
		Current = this;
		Entries = new List<string>();
	}

	[ConCmd.Server]
	public static void AddEntry( string message )
	{
		AddEntryClient( To.Everyone, message );
	}

	[ConCmd.Client( "add_entry_client", CanBeCalledFromServer = true )]
	public static void AddEntryClient( string message )
	{
		Current?.AddActionEntry( message );
	}

}

//public partial class ActionEntry : Panel
//{
//	public Label Message { get; internal set; }
//	public Image Icon { get; internal set; }

//	public RealTimeSince TimeSinceBorn = 0;

//	public ActionEntry()
//	{
//		Icon = Add.Image();
//		Message = Add.Label( "Message", "message" );
//	}

//	public override void Tick()
//	{
//		base.Tick();

//		if ( TimeSinceBorn > 15 )
//		{
//			Delete();
//		}
//	}
//}
