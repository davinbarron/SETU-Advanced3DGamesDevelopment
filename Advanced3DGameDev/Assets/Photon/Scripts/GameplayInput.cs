using UnityEngine;
using Fusion;

namespace Example
{
	/// <summary>
	/// Input structure polled by Fusion. This is sent over network and processed by server, keep it optimized and remove unused data.
	/// </summary>
	public struct GameplayInput : INetworkInput
	{
		public Vector2        MoveDirection;
		public Vector2        LookRotationDelta;
		public NetworkButtons Actions;

		public const int JUMP_BUTTON = 0;
		public static readonly int EMOTE_WAVE  = 1;
		public static readonly int EMOTE_CHEER = 2;
		public static readonly int EMOTE_TAUNT = 3;
	}
}
