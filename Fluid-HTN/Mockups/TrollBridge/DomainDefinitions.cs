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
			var domain = new DomainBuilder<TrollContext>()
				.Select("Be Trunk Thumper")
					
					// Sprint toward the enemy and attack them
					.Sequence("Engage enemy")
						.Condition("Has Enemy", (ctx) => ctx.HasEnemy)
						.Action("Sprint to enemy")
							.Do((ctx) => NavigateTo(ctx, ctx.Enemy.Location, Speed.Fast))
							.Effect("At enemy location", EffectType.PlanOnly, (ctx) => ctx.Location = Location.EnemyLoc)
							.Effect("Is tired", EffectType.PlanAndExecute, (ctx) => ctx.IsTired = true)
						.End()
						.Action("Trunk slam!")
						.End()
					.End()

					// Idly walk to next bridge
					.Action("Walk to next bridge")
						.Do((ctx) => NavigateTo(ctx, ctx.NextBridgeLocation, Speed.Slow))
						.Effect("At bridge location", EffectType.PlanOnly, (ctx) => ctx.Location = Location.BridgeLoc)
						.Effect("Getting bored", EffectType.PlanAndExecute, (ctx) => ctx.Bored++)
					.End()

				.End()
				.Build();

			return domain;
		}

		public static TaskStatus NavigateTo( TrollContext ctx, float3 destination, Speed speed )
		{
			return TaskStatus.Success;
		}
	}
}