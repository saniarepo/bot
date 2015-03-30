using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.Common;

namespace bot
{
    public enum UnitField
    {
        CachedSubName = 0,
        UnitClassificationOffset2 = 32,
        CachedQuestItem1 = 48,
        CachedTypeFlag = 76,
        IsBossOffset2 = 76,
        CachedModelId1 = 92,
        CachedName = 108,
        UNIT_SPEED = 128,
        TaxiStatus = 0xc0,
        TransportGUID = 2096,
        UNIT_FIELD_X = 0x838,
        UNIT_FIELD_Y = 0x83C,
        UNIT_FIELD_Z = 0x840,
        UNIT_FIELD_R = 2120,
        DBCacheRow = 2484,
        IsBossOffset1 = 2484,
        UnitClassificationOffset1 = 2484,
        CanInterrupt = 3172,
        CastingSpellID = 3256,
        ChannelSpellID = 3280,
    }

    public enum UnitFields
    {
        Charm = ObjectFields.End + 0,
        Summon = 10,
        Critter = 12,
        CharmedBy = 14,
        SummonedBy = 16,
        CreatedBy = 18,
        DemonCreator = 20,
        Target = 22,
        BattlePetCompanionGUID = 24,
        ChannelObject = 26,
        ChannelSpell = 28,
        SummonedByHomeRealm = 29,
        Sex = 30,
        DisplayPower = 31,
        OverrideDisplayPowerID = 32,
        Health = 33,
        Power = 34,
        MaxHealth = 39,
        MaxPower = 40,
        PowerRegenFlatModifier = 45,
        PowerRegenInterruptedFlatModifier = 50,
        Level = 55,
        EffectiveLevel = 56,
        FactionTemplate = 57,
        VirtualItemID = 58,
        Flags = 61,
        Flags2 = 62,
        AuraState = 63,
        AttackRoundBaseTime = 64,
        RangedAttackRoundBaseTime = 66,
        BoundingRadius = 67,
        CombatReach = 68,
        DisplayID = 69,
        NativeDisplayID = 70,
        MountDisplayID = 71,
        MinDamage = 72,
        MaxDamage = 73,
        MinOffHandDamage = 74,
        MaxOffHandDamage = 75,
        AnimTier = 76,
        PetNumber = 77,
        PetNameTimestamp = 78,
        PetExperience = 79,
        PetNextLevelExperience = 80,
        ModCastingSpeed = 81,
        ModSpellHaste = 82,
        ModHaste = 83,
        ModRangedHaste = 84,
        ModHasteRegen = 85,
        CreatedBySpell = 86,
        NpcFlag = 87,
        EmoteState = 89,
        Stats = 90,
        StatPosBuff = 95,
        StatNegBuff = 100,
        Resistances = 105,
        ResistanceBuffModsPositive = 112,
        ResistanceBuffModsNegative = 119,
        BaseMana = 126,
        BaseHealth = 127,
        ShapeshiftForm = 128,
        AttackPower = 129,
        AttackPowerModPos = 130,
        AttackPowerModNeg = 131,
        AttackPowerMultiplier = 132,
        RangedAttackPower = 133,
        RangedAttackPowerModPos = 134,
        RangedAttackPowerModNeg = 135,
        RangedAttackPowerMultiplier = 136,
        MinRangedDamage = 137,
        MaxRangedDamage = 138,
        PowerCostModifier = 139,
        PowerCostMultiplier = 146,
        MaxHealthModifier = 153,
        HoverHeight = 154,
        MinItemLevel = 155,
        MaxItemLevel = 156,
        WildBattlePetLevel = 157,
        BattlePetCompanionNameTimestamp = 158,
        InteractSpellID = 159,
        End = 160,
    }

    
    
    public class WowUnit : WowObject
    {
        private IntPtr Pointer;

        public WowUnit(IntPtr address): base(address)
        {
            this.Pointer = address;
        }
        public int Health
        {
            get { return GetValue<int>((int)UnitFields.Health); }
        }

        public int MaxHealth
        {
            get { return GetValue<int>((int)UnitFields.MaxHealth); }
        }

        public bool IsAlive
        {
            get { return !IsDead; }
        }

        public bool IsDead
        {
            get { return this.Health <= 0 || (ObjectDynamicFlags.Dead) != 0; }
        }


        public ulong TransportGuid
        {
            get { return GetValue<ulong>((int)UnitField.TransportGUID); }
        }


        public bool InTransport
        {
            get { return TransportGuid > 0; }
        }

        public T GetValue<T>(int index) where T : struct
        {
            return ProcessMemory.ReadStruct<T>(ObjectData.Descriptors + (int)index * IntPtr.Size);
        }

    
        public Vector3 Position
        {
            get
            {
                if (Pointer == IntPtr.Zero) return Vector3.Zero;
                if (InTransport)
                {
                    var wowObject = ObjectManager.GetObjectByGUID(TransportGuid);
                    if (wowObject != null)
                    {
                        var wowUnit = new WowUnit(Pointer);
                        if (wowUnit.IsValid && wowUnit.IsAlive)
                            return wowUnit.Position;
                    }
                }

                var position = new Vector3(
                    ProcessMemory.Read(Pointer + (int)UnitField.UNIT_FIELD_X),
                    ProcessMemory.Read(Pointer + (int)UnitField.UNIT_FIELD_Y),
                    ProcessMemory.Read(Pointer + (int)UnitField.UNIT_FIELD_Z)
                );

                return position;
            }
        }
    }
}
