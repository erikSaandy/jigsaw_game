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
		Entries = new List<ActionEntry>();
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
