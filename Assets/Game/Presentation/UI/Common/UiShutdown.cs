using System;
using RD.Foundation;
using RD.ScreenNavigation;
using UnityEngine;

namespace NightCourier.UI
{
	public static class UiShutdown
	{
		public static async void Run(Func<Awaitable> operation, UnityEngine.Object context)
		{
			try
			{
				await operation();
			}
			catch (Exception exception) when (IsExpected(exception))
			{
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, context);
			}
		}

		public static bool IsExpected(Exception exception)
		{
			return exception switch
			{
				OperationCanceledException => true,
				ObjectDisposedException => true,
				ScreenTransitionException transition => IsExpected(transition.InnerException),
				_ => false
			};
		}
	}
}
