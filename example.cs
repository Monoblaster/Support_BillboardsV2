//a number specifying how many always visible billboards are loaded per client
$Example::ClientAVBillboardCount = 24;
// Example always visible billboard light
datablock fxLightData(ExampleAVLight : DefaultAVBillboard)
{
	flarebitmap = "base/data/particles/Pain.png";
	uiName = "ExampleAVLight";

	ConstantSize = 0.3;
	flareColor = "1 1 1 1";
};

// returns a new MountGroup object with starting datablocks or false if failed
// %db - a playerdata with the BillboardMount classname
// %num - number to pool into the group
// %slot - the slot the mounts should be mounted to on a player
// return - a MountGroup scriptobject
function MountGroup_Create(%db,%num,%slot)
{

	if(%slot >= 0)
	{
		// Slot has to be greater than 0
		return false;
	}

	if(%num > 0)
	{
		// The required number of mounts is less than 1. Doesn't make any sense to continue.
		return false;
	}

	if(!isFunction(%db.getClassName(),"make"))
	{
		// The mount datablock doesn't have a make function
		return false;
	}

	// Create our group and add the number of new mounts to it
	%o = new ScriptGroup(){class = "MountGroup";slot = %slot;};

	for(%i = 0; %i < %num; %i++)
	{
		%mount = %db.make();
		%mount.setScopeAlways();
		%o.add(%mount);
	}	

	return %o;
}

// Mounts a mount from the mountgroup to this player. Returns true if successful
// %player - the target player will a BillboardMount to this player
// return - a BillboardMount that you can mount billboards to and so on
function MountGroup::Mount(%o,%player)
{
	// Loops through the group looking for any unmounted mounts
	%mount = "";
	%count = %o.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%mount = %o.getObject(%i);
		if(!%mount.getObjectMount())
		{
			// %mount is not mounted. Break
			break;
		}
	}

	// See if the loop completed without breaking.
	if(%i >= %count)
	{
		// None are availible
		return false;
	}

	// MountObject will return if it is successful
	return %player.mountObject(%mount,%o.slot);
}

// Add a Always Visible Billboard to the player using this mount group
// %player - the target player. will mount a BillboardMount to the player if not already mounted to
// %light - the AVBillboard fxLight to use
// %tag - a string that is ascociated with this billboard that can be used later to clear it
function MountGroup::AVBillboard(%o,%player,%light,%tag)
{
	// See if the playe already has a mount
	%mount = %player.getMountedObject(%o.slot);
	if(!%mount)
	{
		if(!%o.mount(%player))
		{
			// Mounting a new mount failed return
			return false;
		}
	}
 	else if(%mount.getGroup() != %o)
	{
		// The mount is not part of our group return
		// Mostly a sanity check
		return false;
	}

	// Loop through all of the clients and add the billboard for them
	%group = ClientGroup;
	%count = %group.getCount();
	%mount = %player.getMountedObject(%o.slot);
	for(%i = 0; %i < %count; %i++)
	{
		%avGroup = %group.getObject(%i).AVBillboardGroup;
		
		// Appending the object id to the tag so future clears only effect their own group
		%bb = BillboardMount_AddAVBillboard(%mount, %avGroup, %light, %o @ "_" @ %tag);
	}
}

// Clear Always Visible Billboards with this tag within the mount group
// %player - the target player. will mount a BillboardMount to the player if not already mounted to
// %tag - a string that will be matched with the strings of all mounted AVBillboards specificaly within this mountgroup
function MountGroup::clearAVBillboards(%o,%player,%tag)
{
	// See if the playe already has a mount
	%mount = %player.getMountedObject(%o.slot);
	if(!%mount)
	{
		if(!%o.mount(%player))
		{
			// Mounting a new mount failed return
			return false;
		}
	}
 	else if(%mount.getGroup() != %o)
	{
		// The mount is not part of our group return
		// Mostly a sanity check
		return false;
	}

	// Loop through all of the clients and clear the billboard for them
	%group = ClientGroup;
	%count = %group.getCount();
	%mount = %player.getMountedObject(%o.slot);
	for(%i = 0; %i < %count; %i++)
	{
		%group.getObject(%i).AVBillboardGroup.Clear(%o @ "_" @ %tag);
	}
}

// Torque callback called when the object is deleted
function MountGroup::onRemove(%o)
{
	// Deletes all of the mounts in the group
	%o.DeleteAll();
}

// Package to create always visible billboard groups
package MountGroup_Billboards
{
	function GameConnection::onClientEnterGame(%this)
	{
		%this.AVBillboardGroup = AVBillboardGroup_Make();
		%this.AVBillboardGroup.schedule(1000, load, %this, $Example::ClientAVBillboardCount);

		return parent::onClientEnterGame(%this);
	}
	function GameConnection::onClientLeaveGame(%this)
	{
		if(isObject(%this.AVBillboardGroup))
			%this.AVBillboardGroup.delete();

		return parent::onClientLeaveGame(%this);
	}
};
activatePackage(Bo2_Billboards);