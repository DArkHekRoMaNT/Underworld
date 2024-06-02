using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[assembly: ModInfo("Underworld", "underworld",
    WorldConfig = @"
    {
	    playstyles: [
		    {
			    code: ""underworld"",
				playListCode: ""underworld"",
                langcode: ""preset-underworld"",
                listOrder: 1,
			    mods: [""game"", ""survival"", ""underworld""],
			    worldType: ""underworld"",
			    worldConfig:  {
				    worldClimate: ""realistic"",
				    gameMode: ""survival"",
                    temporalStability: ""true"",
                    temporalStorms: ""sometimes"",
                    temporalRifts: ""off"",
                    graceTimer: ""0"",
                    microblockChiseling: ""stonewood"",
                    polarEquatorDistance: ""10000"",
                    lungCapacity: ""40000"",
                    harshWinters: ""true"",
                    daysPerMonth: ""9"",
                    toolDurability: ""2"",
                    saplingGrowthRate: ""0.1"",
                    propickNodeSearchRadius: ""8"",
                    allowUndergroundFarming: ""true"",
                    temporalGearRespawnUses: ""-1"",
                    temporalStormSleeping: ""1"",
                    clutterObtainable: ""yes"",
					snowAccum: ""false"",
					colorAccurateWorldmap: ""true""
			    }
		    }
	    ],
	    worldConfigAttributes: [
			
	    ]
    }
")]

namespace Underworld
{
    public static class Settings
    {
        public const string WorldType = "underworld";
    }

    public class Core : ModSystem
    {
        private ICoreServerAPI _api = null!;
        private IWorldGenBlockAccessor _wgBlockAccessor = null!;

        private int _chunkSize;
        private int _worldHeight;

        public override void StartServerSide(ICoreServerAPI api)
        {
            _api = api;
            api.Event.InitWorldGenerator(InitWorldGen, Settings.WorldType);
            api.Event.ChunkColumnGeneration(ChunkColumnGeneration, EnumWorldGenPass.Terrain, Settings.WorldType);
            api.Event.MapRegionGeneration(MapRegionGeneration, Settings.WorldType);
            api.Event.GetWorldgenBlockAccessor((chunkProvider) =>
            {
                _wgBlockAccessor = chunkProvider.GetBlockAccessor(true);
            });
            api.Event.PlayerCreate += OnFirstJoin;
        }

        private void OnFirstJoin(IServerPlayer byPlayer)
        {
            byPlayer.Entity.TeleportTo(new EntityPos(_api.WorldManager.MapSizeX / 2, 50, _api.WorldManager.MapSizeZ / 2, 0, 0, 0), () =>
            {
                const int size = 7;
                var tmpPos = new BlockPos(0);
                IterateCube(size, size, size, (x, y, z) =>
                {
                    tmpPos.X = (int)byPlayer.Entity.Pos.X + x - size / 2;
                    tmpPos.Y = (int)byPlayer.Entity.Pos.Y + y - size / 2;
                    tmpPos.Z = (int)byPlayer.Entity.Pos.Z + z - size / 2;

                    var blockId = 0; // Air
                    if (x == 0 || y == 0 || z == 0 || x == size - 1 || y == size - 1 || z == size - 1)
                    {
                        blockId = GetBlockId("game:cobblestone-granite");
                    }
                    if (y == size - 1 && x == size / 2 && z == size / 2)
                    {
                        blockId = GetBlockId("game:paperlantern-on");
                    }

                    _api.World.BlockAccessor.SetBlock(blockId, tmpPos);
                });
            });
        }

        private void InitWorldGen()
        {
            _chunkSize = _api.WorldManager.ChunkSize;
            _worldHeight = _api.WorldManager.MapSizeY;
        }

        private void ChunkColumnGeneration(IChunkColumnGenerateRequest request)
        {
            var tmpPos = new BlockPos(0);
            IterateCube(_chunkSize, _worldHeight, _chunkSize, (x, y, z) =>
            {
                tmpPos.X = request.ChunkX * _chunkSize + x;
                tmpPos.Y = y;
                tmpPos.Z = request.ChunkZ * _chunkSize + z;

                var blockId = GetBlockId("game:rock-granite");
                if (y == 0 || y == _worldHeight - 1)
                {
                    blockId = 1;
                }

                _wgBlockAccessor.SetBlock(blockId, tmpPos);
            });
        }

        private void MapRegionGeneration(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams)
        {
            mapRegion.ClimateMap = null; // Fix crash
        }

        private static void IterateCube(int sizeX, int sizeY, int sizeZ, Action<int, int, int> action)
        {
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    for (int k = 0; k < sizeZ; k++)
                    {
                        action.Invoke(i, j, k);
                    }
                }
            }
        }

        private int GetBlockId(string code)
        {
            return _api.World.GetBlock(new AssetLocation(code))?.BlockId ?? 0;
        }
    }
}
