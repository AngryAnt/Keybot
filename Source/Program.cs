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


		void Channel.IListener.OnMessage (Message message)
		{
			const string responsePrefix = "\n> ";

			if (!message.TryRead (out Message.Data data))
			{
				Log.Message ("Ignoring message which could not be read");
				return;
			}

			if (string.IsNullOrWhiteSpace (data.Contents))
			{
				Log.Message ("Ignoring empty message (probably just unhandled non-text)");
				return;
			}

			if (data.Contents.StartsWith (responsePrefix))
			{
				return;
			}

			TryRespond (responsePrefix + data.Contents);
		}
	}
}
