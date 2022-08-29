datablock PlayerData(DefaultBillboardMount)
{
    shapeFile = "base/data/shapes/empty.dts";
	boundingBox = vectorScale("20 20 20", 4);

	splash = PlayerSplash;
    splashEmitter[0] = PlayerFoamDropletsEmitter;
    splashEmitter[1] = PlayerFoamEmitter;
    splashEmitter[2] = PlayerBubbleEmitter;

    mediumSplashSoundVelocity = 10;
    hardSplashSoundVelocity = 20;
    exitSplashSoundVelocity = 5;

    impactWaterEasy = Splash1Sound;
    impactWaterMedium = Splash1Sound;
    impactWaterHard = Splash1Sound;
    exitingWater = exitWaterSound;

    jetEmitter = playerJetEmitter;
    jetGroundEmitter = playerJetGroundEmitter;
    jetGroundDistance = 4;
    footPuffNumParts = 10;
    footPuffRadius = 0.25;

	className = "BillboardMount";
};

datablock PlayerData(OverheadBillboardMount : DefaultBillboardMount)
{
    shapeFile = "./billboardMount.dts";
};

function BillboardMount::OnAdd(%db,%bbm)
{
	%bbm.applyDamage(1000);
	%bbm.isBillboardMount = true;

	%bbm.billBoardGroup = new scriptGroup();
}

function BillboardMount::OnRemove(%db,%bbm)
{
	BillboardMount_CLearBillboards(%bbm);
	%bbm.billBoardGroup.delete();
}

function BillboardMount::onUnmount(%db,%bbm,%mount,%node) 
{
	%bbm.delete();
}

function BillboardMount::Make(%db)
{
	%obj = new AiPlayer()
	{
		dataBlock = %db;
	};

	return %obj;
}

function BillboardMount_AddBillboard(%bbm,%lightData,%dontGhost)
{
	if(!%bbm.isBillboardMount || %lightData.className !$= "Billboard")
	{
		return;
	}

	%obj = new fxLight()
	{
		dataBlock = %lightData;
	};

	%obj.setNetFlag(6,true);
	if(!%dontGhost)
	{
		%obj.setScopeAlways();
	}
	else
	{
		%obj.setNetFlag(8,false);
	}

	%obj.attachToObject(%bbm);
	%bbm.billBoardGroup.add(%obj);

	return %obj;
}

function BillboardMount_CLearBillboards(%bbm)
{
	%group = %bbm.billBoardGroup;
	%count = %group.getCount();
	for(%i = %count - 1; %i >= 0; %i++)
	{
		%obj = %group.getObject(%i);
		%obj.delete();
	}

	return %bbm;
}

function BillboardMount_AddAVBillboard(%bbm,%avbbg,%lightData,%tag)
{
	if(!%avbbg.loaded)
	{
		return "";
	}
	
	if(!%bbm.isBillboardMount || %avbbg.class !$= "AVBillboardGroup" || %lightData.className !$= "AVBillboard")
	{
		return "";
	}

	%group = %avbbg;
	%count = %group.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%obj = %group.getObject(%i);
		if(!%obj.isActive)
		{
			break;
		}
	}
	if(%i >= %count)
	{
		return "";
	}
	
	%bb = %avbbg.getObject(%i);
	%bb.tag = %tag;
	%bb.isActive = true;
	%avbbg.active++;

	%bb.setNetFlag(8,true);
	%bb.setDatablock(%lightData);
	%bb.setEnable(true);
	%bb.attachToObject(%bbm);
	%bb.setNetFlag(8,false);
	
	return %bbm;
}

datablock fxLightData(DefaultBillboard)
{
	LightOn = false;

	flareOn = true;
	flarebitmap = "base/data/shapes/blank.png";
	ConstantSize = 1;
    ConstantSizeOn = true;
    FadeTime = 0.000001;

	LinkFlare = false;
	blendMode = 1;
	flareColor = "1 0 0 1";

	AnimOffsets = true;
	startOffset = "0 0 0";
	endOffset = "0 0 0";

	className = "Billboard";
};

function Billboard::OnAdd(%db,%bb)
{
	%bb.isBillboard = true; 
}

function Billboard_Ghost(%bb,%client)
{
	%bb.ScopeToClient(%client);
}

function Billboard_ClearGhost(%bb,%client)
{
	%bb.ClearScopeToClient(%client);
}

$AVBillboard::loadMount = $AVBillboard::loadMount || DefaultBillboardMount.Make();
$AVBillboard::loadTransform = "0 0 1000 0 0 0 1";
$AVBillboard::loadMountTransform = vectorAdd(getWords($AVBillboard::loadTransform,0,2),matrixMulVector($AVBillboard::loadTransform,"0 4 0")) SPC getWords($AVBillboard::loadTransform,3);
$AVBillboard::loadMount.setTransform($AVBillboard::loadMountTransform);
$AVBillboard::loadMount.setNetFlag(8,false);

datablock fxLightData(DefaultAVBillboard)
{
	LightOn = false;

	flareOn = true;
	flarebitmap = "base/data/shapes/blank.png";
	ConstantSize = 1;
    ConstantSizeOn = true;
    FadeTime = inf;

	LinkFlare = false;
	blendMode = 1;
	flareColor = "1 0 0 1";

	AnimOffsets = true;
	startOffset = "0 0 0";
	endOffset = "0 0 0";

	classname = "AVBillboard";
};

function AVBillboard::OnAdd(%db,%bb)
{
	%bb.isAVBillboard = true; 
}

function AVBillboardGroup_Make()
{
	%obj = new ScriptGroup()
	{
		class = "AVBillboardGroup";
	};

	return %obj;
}

datablock CameraData(BillboardLoadingCamera)
{
	mode = "Observer";
};

function AVBillboardGroup::Load(%avbbg,%client,%num)
{
	if(%avbbg.loadedClient !$= "")
	{
		return;
	}

	%camera = %client.AVBillboardGroup_LoadCamera = %client.AVBillboardGroup_LoadCamera ||  new Camera(){dataBlock = BillboardLoadingCamera;};
	%dummyCamera = %client.AVBillboardGroup_LoadDummyCamera = %client.AVBillboardGroup_LoadDummyCamera || new Camera(){dataBlock = BillboardLoadingCamera;};
	$AVBillboard::loadMount.scopeToClient(%client);
	%camera.setTransform($AVBillboard::loadTransform);
	%camera.setcontrolObject(%dummyCamera);
	%client.setControlObject(%camera);
	
	for(%i = 0; %i < %num; %i++)
	{
		%bb = BillboardMount_AddBillboard($AVBillboard::loadMount,DefaultBillboard,true);
		Billboard_Ghost(%bb,%client);
		%avbbg.add(%bb);
	}

	%avbbg.loadedClient = %client;

	return %avbbg;
}

function AVBillboardGroup::FinishLoad(%avbbg)
{
	if(%avbbg.loadedClient $= "" || %avbbg.loaded)
	{
		return;
	}

	for(%i = 0; %i < %avbbg.getCount(); %i++)
	{
		%bb = %avbbg.getObject(%i);

		%bb.setNetFlag(8,true);
		%bb.setDatablock(DefaultAVBillboard);
		%bb.setEnable(false);
		%bb.setNetFlag(8,false);
	}

	%client = %avbbg.loadedClient;
	if(isObject(%client.player))
	{
		%client.setControlObject(%client.player);
	}
	else
	{
		%client.setControlObject(%client.camera);
	}

	%avbbg.loaded = true;
	return %avbbg;
}

function AVBillboardGroup::Clear(%avbbg,%tag)
{
	if(!%avbbg.loaded)
	{
		return;
	}

	%group = %avbbg;
	%count = %group.getCount();
	for(%i = %count - 1; %i >= 0; %i--)
	{
		%bb = %group.getObject(%i);
		if((%tag !$= "" && %tag !$= %bb.tag) || !%bb.isActive)
		{
			continue;
		}
		%bb.setNetFlag(8,true);
		%bb.setEnable(false);
		%bb.detachFromObject();
		%bb.setNetFlag(8,false);
		%bb.tag = "";
		%bb.isActive = false;

		%group.active--;
	}

	%group.active = getMax(%group.active,0);
	return %group;
}

function AVBillboardGroup::OnRemove(%avbbg)
{
	%avbbg.deleteall();
}