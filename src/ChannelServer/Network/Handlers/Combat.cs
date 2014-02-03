﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Shared.Network;
using Aura.Channel.Network.Sending;
using Aura.Shared.Util;
using Aura.Shared.Mabi.Const;
using Aura.Channel.World.Entities;

namespace Aura.Channel.Network.Handlers
{
	public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
	{
		/// <summary>
		/// Sent when switching between idle and attack mode.
		/// </summary>
		/// <example>
		/// 001 [..............01] Byte   : 1
		/// </example>
		[PacketHandler(Op.ChangeStanceRequest)]
		public void ChangeStance(ChannelClient client, Packet packet)
		{
			var stance = (BattleStance)packet.GetByte();

			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			if (stance > BattleStance.Ready)
			{
				Log.Warning("HandleChangeStance: Unknown battle stance '{0}'.", stance);
				return;
			}

			// Change stance
			creature.BattleStance = stance;
			Send.ChangeStance(creature);

			// Response (unlocks the char)
			Send.ChangeStanceRequestR(creature);
		}

		/// <summary>
		/// Sent for targetting an enemy.
		/// </summary>
		/// <remarks>
		/// The mode seems to specify in what way the creature is targetting.
		/// With NPCs mode 1 is notice, mode 2 is aggro ("!" and "!!").
		/// I have only one log of a player sending one != 0, coming from
		/// a mount that's able to attack.
		/// 
		/// The purpose of CombatTargetUpdate is unknown, it seems to be only
		/// active for a few ms before it's reset again, maybe it's not even
		/// related to this.
		/// </remarks>
		/// <example>
		/// 0001 [0010F00000005B3B] Long   : 4767482418060091
		/// 0002 [..............00] Byte   : 0
		/// 0003 [................] String : 
		/// </example>
		[PacketHandler(Op.SetCombatTarget)]
		public void SetCombatTarget(ChannelClient client, Packet packet)
		{
			var targetEntityId = packet.GetLong();
			var mode = (TargetMode)packet.GetByte();
			var unkString = packet.GetString();

			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			// Id == 0 means untargetting, go with the null
			Creature target = null;
			if (targetEntityId != 0 && (target = creature.Region.GetCreature(targetEntityId)) == null)
			{
				Log.Warning("Creature '{0}' targetted invalid entity '{1}'.", creature.Name, targetEntityId.ToString("X16"));
				Send.Notice(creature, "Invalid target");
				Send.SetCombatTarget(creature, 0, 0);
				return;
			}

			creature.Target = target;

			Send.SetCombatTarget(creature, targetEntityId, mode);
		}

		/// <summary>
		/// Sent for Combat Mastery and many other skills.
		/// </summary>
		/// <example>
		/// 0001 [0010F00000005B3B] Long   : 4767482418060091
		/// 0002 [................] String : 
		/// </example>
		[PacketHandler(Op.CombatAttack)]
		public void CombatAttack(ChannelClient client, Packet packet)
		{
			var targetEntityId = packet.GetLong();
			var unkString = packet.GetString();

			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			// ...
			//Log.Info("*attack*");

			Send.CombatAttackR(creature);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// The client sends this when initially attacking a creature.
		/// It's answered with op+1, probably the full stun at the time.
		/// Afterwards you get op+2, with relative values, most likely
		/// and update.
		/// Sent again with id 0 when the target dies.
		/// </remarks>
		/// <example>
		/// 0001 [0010F00000046344] Long   : 4767482418324292
		/// </example>
		[PacketHandler(Op.SubsribeStunMeter)]
		public void SubsribeStunMeter(ChannelClient client, Packet packet)
		{
			// ...

			// Answer:
			// Op: 0000AA1E, Id: 00100000000XXXXX
			// 0001 [0010F00000046344] Long   : 4767482418324292
			// 0002 [..............01] Byte   : 1
			// 0003 [........40590000] Float  : 100.0
		}
	}
}
