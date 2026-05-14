using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

namespace Example
{
	/// <summary>
	/// Player implementation, processes input and controls KCC.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	public sealed class Player : NetworkBehaviour
	{
		public SimpleKCC   KCC;
		public PlayerInput Input;
		public Transform   CameraPivot;
		public Transform   CameraHandle;
		public PlayerNameTag  NameTag;
		public Animator    Animator;

		[Header("Movement")]
		public float MoveSpeed          = 10.0f;
		public float JumpImpulse        = 10.0f;
		public float UpGravity          = -25.0f;
		public float DownGravity        = -40.0f;
		public float GroundAcceleration = 55.0f;
		public float GroundDeceleration = 25.0f;
		public float AirAcceleration    = 25.0f;
		public float AirDeceleration    = 1.3f;

		[Networked] private Vector3 _moveVelocity { get; set; }
		[Networked] public int Score { get; set; }
		[Networked] private float _networkedMotionSpeed { get; set; }
		[Networked] private NetworkBool _networkedJumpStarted { get; set; }

		private int _animSpeedHash;
		private int _animJumpHash;
		private int _animGroundedHash;
		private int _animFreeFallHash;
		private int _animMotionSpeedHash;

		private void Awake()
		{
			if (Animator != null)
			{
				_animSpeedHash = Animator.StringToHash("Speed");
				_animJumpHash = Animator.StringToHash("Jump");
				_animGroundedHash = Animator.StringToHash("Grounded");
				_animFreeFallHash = Animator.StringToHash("FreeFall");
				_animMotionSpeedHash = Animator.StringToHash("MotionSpeed");
			}
		}

		// ---- Scoring ----

		public void AddScore(int amount)
		{
			// Only the client with StateAuthority writes the score
			if (!HasStateAuthority) return;
			Score = Mathf.Max(0, Score + amount);
			Debug.Log($"Player {Object.InputAuthority} scored! Total: {Score}");
		}

		public void ResetScore()
		{
			if (HasStateAuthority)
			{
				Score = 0;
				Debug.Log($"Player {Object.InputAuthority} score reset.");
				return;
			}

			Rpc_ScoreReset();
		}

		// ---- NetworkBehaviour overrides ----

		public override void FixedUpdateNetwork()
		{
			// Apply look rotation delta. This propagates to Transform component immediately.
			KCC.AddLookRotation(Input.CurrentInput.LookRotationDelta);

			// Set default world space input direction and jump impulse.
			Vector3 inputDirection = KCC.TransformRotation * new Vector3(Input.CurrentInput.MoveDirection.x, 0.0f, Input.CurrentInput.MoveDirection.y);
			float   jumpImpulse    = default;

			// Comparing current input to previous input - this prevents glitches when input is lost.
			if (Input.CurrentInput.Actions.WasPressed(Input.PreviousInput.Actions, GameplayInput.JUMP_BUTTON) == true)
			{
				if (KCC.IsGrounded == true)
				{
					// Set world space jump vector.
					jumpImpulse = JumpImpulse;
				}
			}

			// Emote detection — only InputAuthority detects and fires the RPC
			// WasPressed compares current vs previous to catch a single press, not held
			if (HasInputAuthority)
			{
				if (Input.CurrentInput.Actions.WasPressed(Input.PreviousInput.Actions, GameplayInput.EMOTE_WAVE))
					Rpc_PlayEmote(EmoteType.Wave);
				else if (Input.CurrentInput.Actions.WasPressed(Input.PreviousInput.Actions, GameplayInput.EMOTE_CHEER))
					Rpc_PlayEmote(EmoteType.Cheer);
				else if (Input.CurrentInput.Actions.WasPressed(Input.PreviousInput.Actions, GameplayInput.EMOTE_TAUNT))
					Rpc_PlayEmote(EmoteType.Taunt);
			}

			// It feels better when the player falls quicker.
			KCC.SetGravity(KCC.RealVelocity.y >= 0.0f ? UpGravity : DownGravity);

			Vector3 desiredMoveVelocity = inputDirection * MoveSpeed;

			if (KCC.ProjectOnGround(desiredMoveVelocity, out Vector3 projectedDesiredMoveVelocity) == true)
			{
				desiredMoveVelocity = Vector3.Normalize(projectedDesiredMoveVelocity) * MoveSpeed;
			}

			float acceleration;
			if (desiredMoveVelocity == Vector3.zero)
			{
				// No desired move velocity - we are stopping.
				acceleration = KCC.IsGrounded == true ? GroundDeceleration : AirDeceleration;
			}
			else
			{
				acceleration = KCC.IsGrounded == true ? GroundAcceleration : AirAcceleration;
			}

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);

			_networkedMotionSpeed = Input.CurrentInput.MoveDirection.magnitude;
			_networkedJumpStarted = jumpImpulse > 0.0f;

			KCC.Move(_moveVelocity, jumpImpulse);
		}

		// ---- RPCs ----

		// Source = InputAuthority: only the owning player triggers this
		// Targets = All: executes on every peer's copy of this object
		[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
		private void Rpc_PlayEmote(EmoteType emote)
		{
			if (NameTag == null)
			{
				Debug.LogWarning("Rpc_PlayEmote: NameTag is not assigned on Player prefab.");
				return;
			}
			NameTag.ShowEmote(emote);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void Rpc_ScoreReset()
		{
			Score = 0;
			Debug.Log($"Player {Object.InputAuthority} score reset.");
		}

		// ---- LateUpdate for camera ----

		private void LateUpdate()
		{
			// Only InputAuthority needs to update camera.
			if (HasInputAuthority == false)
				return;

			// Update camera pivot and transfer properties from camera handle to Main Camera.
			// Render() is executed before KCC because of [OrderBefore(typeof(KCC))].
			// So we have to do it from LateUpdate() - which is called after Render().

			Vector2 pitchRotation = KCC.GetLookRotation(true, false);
			CameraPivot.localRotation = Quaternion.Euler(pitchRotation);

			Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
		}

		public override void Render()
		{
			if (Animator != null)
			{
				float speed = _moveVelocity.magnitude;
				bool isGrounded = KCC.IsGrounded;
				bool isFreeFall = !isGrounded && KCC.RealVelocity.y < 0.0f;
				float motionSpeed = _networkedMotionSpeed;
				bool jumpStarted = _networkedJumpStarted;

				Animator.SetFloat(_animSpeedHash, speed);
				Animator.SetBool(_animGroundedHash, isGrounded);
				Animator.SetBool(_animFreeFallHash, isFreeFall);
				Animator.SetFloat(_animMotionSpeedHash, motionSpeed);
				Animator.SetBool(_animJumpHash, jumpStarted);
			}
			else
			{
				Debug.LogWarning("[Player] Player animator is null");
			}
		}

		// --- Animation Event Receivers ---

		private void OnFootstep(AnimationEvent animationEvent) { }

		private void OnLand(AnimationEvent animationEvent) { }
	}
}
