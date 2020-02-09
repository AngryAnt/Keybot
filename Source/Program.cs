#define ECHO


using System;
using System.Threading;
using Keybase;


namespace Keybot
{
	internal class Program : API.Chat.IListener
	{
#if ECHO
		public const string kResponsePrefix = "\n> ";
#else
		private Question m_Question = default;
#endif


		private static void Main (string[] args)
		{
			new Program ().Run ();
		}


		private void Run ()
		{
			const int sleepSeconds = 30;

			Console.WriteLine ("Main loop start");

			API.Chat.Listen (this);

			while (true)
			{
				bool keyAvailable;
				for (int count = 0; false == (keyAvailable = Console.KeyAvailable) && count < sleepSeconds; ++count)
				{
					Thread.Sleep (1000);
				}

				if (keyAvailable)
				{
					Console.ReadKey (true);
					break;
				}
			}

			Console.WriteLine ("Program terminating");
		}


		private async void TryRespond (Channel channel, [NotNull] string text)
		{
			if (!await API.Chat.MessageAsync (channel, text))
			{
				Log.Error ("Failed to send response");
			}
		}


		private async void TryReact (Channel channel, Message.ID target, [NotNull] string reaction)
		{
			if (!await API.Chat.ReactAsync (channel, target, reaction))
			{
				Log.Error ("Failed to send reaction");
			}
		}


		private async void TryDelete (Channel channel, Message.ID target)
		{
			if (!await API.Chat.DeleteAsync (channel, target))
			{
				Log.Error ("Failed to delete message");
			}
		}


#if !ECHO
		private async void TryAsk (User user)
		{
			m_Question = new Question (
				user.Channel,
				user,
				"New interface. Thoughts?",
				"Ok",
				"Decent",
				"Meh",
				"Whatever"
			);

			Log.Message ("Asking question");

			if (!await m_Question.AskAsync ())
			{
				Log.Error ("Failed to ask question");
				m_Question = null;
				return;
			}

			Log.Message ("Asked question, waiting for response");

			Question.Response response = await m_Question.GetResponseAsync ();

			if (!response.Valid)
			{
				Log.Error ("Received invalid response to question");
				m_Question = null;
				return;
			}

			Log.Message ("@" + response.From + " chose " + response.Text);
			m_Question = null;
		}
#endif


		void API.Chat.IListener.OnIncoming (Message message)
		{
#if !ECHO
			if (null != m_Question && m_Question.ConsiderIncoming (message))
			{
				return;
			}
#endif

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

#if ECHO
			if (data.Contents.StartsWith (kResponsePrefix))
			{
				return;
			}

			TryRespond (data.Channel, kResponsePrefix + data.Contents);
#else
			if (data.Contents.Equals ("ask", StringComparison.InvariantCultureIgnoreCase))
			{
				TryAsk (data.Author);
			}
#endif
		}


		void API.Chat.IListener.OnReaction (Reaction reaction)
		{
#if !ECHO
			if (null != m_Question && m_Question.ConsiderIncoming (reaction))
			{
				return;
			}
#endif

			if (!reaction.TryRead (out Reaction.Data reactionData))
			{
				Log.Message ("Ignoring reaction which could not be read");
				return;
			}

			if (string.IsNullOrWhiteSpace (reactionData.Contents))
			{
				Log.Message ("Ignoring empty reaction");
				return;
			}

#if ECHO
			if (reactionData.Contents.StartsWith (kResponsePrefix))
			{
				return;
			}

			if (!API.Chat.TryReadFromLog (reactionData.Target, out Message.Data messageData))
			{
				TryReact (reactionData.Channel, reactionData.Target, kResponsePrefix + "I saw");
			}
			else
			{
				TryRespond (messageData.Channel, kResponsePrefix + "I saw `" + reactionData.Contents + "` for `" + messageData.Contents + "`");
			}
#else
			if (reactionData.Contents.Equals (":fire:"))
			{
				TryDelete (reactionData.Channel, reactionData.Target);
			}
#endif
		}


		void API.Chat.IListener.OnDelete (Message.ID target)
		{}


		void API.Chat.IListener.OnError ()
		{}


		API.Chat.DeletePolicy API.Chat.IListener.DeletePolicy => API.Chat.DeletePolicy.CallbackAndRemove;
	}
}
