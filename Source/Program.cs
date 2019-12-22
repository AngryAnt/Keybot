using System;
using System.Threading;
using Keybase;


namespace Keybot
{
	internal class Program : Channel.IListener
	{
		private static void Main (string[] args)
		{
			new Program ().Run ();
		}


		private Connection m_Connection = null;
		private Channel m_Channel = null;


		private void Run ()
		{
			const int sleepSeconds = 30;

			Console.WriteLine ("Main loop start");

			using (m_Connection = new Connection ())
			{
				(m_Channel = m_Connection.Self).AddListener (this);

				while (true)
				{
					bool keyAvailable;
					for (int count = 0; false == (keyAvailable = Console.KeyAvailable) && count < sleepSeconds; ++count)
					{
						Thread.Sleep (1000);
					}

					if (keyAvailable)
					{
						break;
					}
				}
			}

			Console.WriteLine ("Program terminating");
		}


		private async void TryRespond ([NotNull] string text)
		{
			if (!await m_Connection.MessageAsync (m_Channel, text))
			{
				Log.Error ("Failed to send response");
			}
		}


		void Channel.IListener.OnMessage (Channel.Message message)
		{
			const string responsePrefix = "\n> ";

			if (string.IsNullOrWhiteSpace (message.Text))
			{
				Log.Message ("Ignoring empty message (probably just unhandled non-text)");
				return;
			}

			if (message.Text.StartsWith (responsePrefix))
			{
				return;
			}

			TryRespond (responsePrefix + message.Text);
		}
	}
}
