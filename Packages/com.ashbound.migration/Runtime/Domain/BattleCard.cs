using System;

namespace Ashbound.Domain
{
    public sealed class BattleCard
    {
        public int InstanceId { get; }
        public int DefinitionId { get; }
        public bool HasSporeAttachment { get; private set; }

        public BattleCard(int instanceId, int definitionId)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
        }

        public void AttachSpore() => HasSporeAttachment = true;

        public int GetFixedCost(int baseCost)
        {
            if (baseCost < 0)
                throw new ArgumentOutOfRangeException(nameof(baseCost));
            return checked(baseCost + (HasSporeAttachment ? 1 : 0));
        }

        public void OnPlayedSuccessfully() => HasSporeAttachment = false;
    }
}
