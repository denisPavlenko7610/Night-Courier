using System;
using RD.ScreenNavigation;

namespace NightCourier.UI
{
	public static class UiShutdown
	{
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
