namespace FluidHTN.Mockups.TrollBridge
{
	public static class DomainDefinitions
	{
		public enum Speed { Slow, Fast }
		public enum Location { EnemyLoc, BridgeLoc, }

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

		public class TrollContext : IContext
		{
			public bool HasEnemy { get; set; }
			public GameEntity Enemy { get; set; }
			public Location Location { get; set; }
			public bool IsTired { get; set; }
			public float3 NextBridgeLocation { get; set; }
			public int Bored { get; set; }
		}

		public static Domain<TrollContext> CreateTrollDomain1()
		{
			var domain = new DomainBuilder<TrollContext>( "Be Trunk Thumper" )

				// Sprint toward the enemy and attack them
				.Sequence("Engage enemy")
					.Condition("Has Enemy", (ctx) => ctx.HasEnemy)
					.Action("Sprint to enemy")
						.Do((ctx) => NavigateTo(ctx, ctx.Enemy.Location, Speed.Fast))
						.Effect("At enemy location", EffectType.PlanOnly, (ctx) => ctx.Location = Location.EnemyLoc)
						.Effect("Is tired", EffectType.PlanAndExecute, (ctx) => ctx.IsTired = true)
					.End()
					.Action("Trunk slam!")
						.Condition( "At enemy location", (ctx) => ctx.Location == Location.EnemyLoc )
						.Do((ctx) => AnimatedAttack( ctx, ctx.Enemy.Location, "Attack_TrunkSlam" ) )
					.End()
				.End()

				// Idly walk to next bridge
				.Sequence("Patrol next bridge")
					.Action("Choose bridge to patrol")
						.Do( ChooseBridgeToCheck )
					.End()
					.Action("Walk to next bridge")
						.Do((ctx) => NavigateTo(ctx, ctx.NextBridgeLocation, Speed.Slow))
						.Effect("At bridge location", EffectType.PlanOnly, (ctx) => ctx.Location = Location.BridgeLoc)
						.Effect("Getting bored", EffectType.PlanAndExecute, (ctx) => ctx.Bored++)
					.End()
					.Action( "Check bridge" )
						.Condition( "At bridge location", (ctx) => ctx.Location == Location.BridgeLoc )
						.Do( (ctx) => CheckBridge( ctx, "Scout" ) )
					.End()
				.End()
				.Build();

			return domain;
		}

		public static TaskStatus ChooseBridgeToCheck( TrollContext ctx )
		{
			return TaskStatus.Success;
		}

		public static TaskStatus CheckBridge( TrollContext ctx, string animationName )
		{
			return TaskStatus.Success;
		}

		public static TaskStatus NavigateTo( TrollContext ctx, float3 destination, Speed speed )
		{
			return TaskStatus.Success;
		}

		public static TaskStatus AnimatedAttack( TrollContext ctx, float3 targetPosition, string animationName )
		{
			return TaskStatus.Success;
		}
	}
}