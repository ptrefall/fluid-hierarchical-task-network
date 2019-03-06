using System;

namespace FluidHTN.Mockups.Dummy
{
	public enum Speed { Slow, Fast }

	public struct float3
	{
		public float x;
		public float y;
		public float z;
	}

	public class GameEntity
	{
		public float3 Location;
	}

	public class DummyContext : IContext
	{
		public bool HasFlag;
		public bool IsDefender;
		public bool HasSpeedBoostTarget => SpeedBoostTarget != null;
		public GameEntity SpeedBoostTarget;
		public bool SpeedBoosted;
		public bool CanReachSpeedBoostTarget;
		public bool CanGrabSpeedBoostTarget;
	}

	public class DummyTest
	{
		private Domain<DummyContext> _domain;

		public void Initialize()
		{
			var speedBoostDomain = new DomainBuilder<DummyContext>()
				.Sequence( "Grab speed boost")
					.Condition( "Not carrying flag", (ctx) => ctx.HasFlag == false )
					.Condition( "Not a defender", (ctx) => ctx.IsDefender == false )
					.Condition( "Speed Boost spawned", (ctx) => ctx.HasSpeedBoostTarget )
					.Condition( "Not speed boosted", (ctx) => ctx.SpeedBoosted == false )
					.Action( "Sprint to speed boost" )
						.Condition( "Can navigate to target", (ctx) => ctx.CanReachSpeedBoostTarget )
						.Do( (ctx) => NavigateTo( ctx, ctx.SpeedBoostTarget.Location, Speed.Fast ))
						.Effect( "Reached speed boost target", EffectType.PlanOnly, (ctx) => ctx.CanGrabSpeedBoostTarget = true )
					.End()
					.Action( "Grab speed boost" )
						.Condition( "Can grab speed boost", (ctx) => ctx.CanGrabSpeedBoostTarget == true )
						.Do( (ctx) => PickUp( ctx, ctx.SpeedBoostTarget ) )
						.Effect( "Is speed boosted", EffectType.PlanAndExecute, (ctx) => ctx.SpeedBoosted = true )
					.End()
				.Build();
			
			_domain = new DomainBuilder<DummyContext>()
				.Splice( speedBoostDomain )
				.Action( "Idle" )
					.Do( Idle )
				.Build();

			_domain.Save( "myDomain.json" );

			var domainFromFile = new DomainBuilder< DummyContext >()
				.Load( "myDomain.json" );
		}

		public static TaskStatus NavigateTo( DummyContext ctx, float3 destination, Speed speed )
		{
			return TaskStatus.Success;
		}

		public static TaskStatus PickUp( DummyContext ctx, GameEntity target )
		{
			return TaskStatus.Success;
		}

		public static TaskStatus Idle( DummyContext ctx )
		{
			return TaskStatus.Continue;
		}
	}
}