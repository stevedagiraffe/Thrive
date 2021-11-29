﻿// This file contains all the different microbe stage spawner types
// just so that they are in one place.

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Helpers for making different types of spawners
/// </summary>
public static class Spawners
{
    public static MicrobeSpawner MakeMicrobeSpawner(Species species,
        CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        return new MicrobeSpawner(species, cloudSystem, currentGame);
    }

    public static ChunkSpawner MakeChunkSpawner(ChunkConfiguration chunkType, CompoundCloudSystem cloudSystem)
    {
        foreach (var mesh in chunkType.Meshes)
        {
            if (mesh.LoadedScene == null)
                throw new ArgumentException("configured chunk spawner has a mesh that has no scene loaded");
        }

        return new ChunkSpawner(chunkType, cloudSystem);
    }

    public static CompoundCloudSpawner MakeCompoundSpawner(Compound compound,
        CompoundCloudSystem clouds, float amount)
    {
        return new CompoundCloudSpawner(compound, clouds, amount);
    }
}

/// <summary>
///   Helper functions for spawning various things
/// </summary>
public static class SpawnHelpers
{
    public static Microbe SpawnMicrobe(Species species, Vector2 location,
        Node worldRoot, PackedScene microbeScene, bool aiControlled,
        CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        var microbe = (Microbe)microbeScene.Instance();

        // The second parameter is (isPlayer), and we assume that if the
        // cell is not AI controlled it is the player's cell
        microbe.Init(cloudSystem, currentGame, !aiControlled);

        worldRoot.AddChild(microbe);
        microbe.Translation = new Vector3(location.x, 0, location.y);

        microbe.AddToGroup(Constants.AI_TAG_MICROBE);
        microbe.AddToGroup(Constants.PROCESS_GROUP);

        if (aiControlled)
            microbe.AddToGroup(Constants.AI_GROUP);

        microbe.ApplySpecies(species);
        microbe.SetInitialCompounds();
        return microbe;
    }

    // TODO: this is likely a huge cause of lag. Would be nice to be able
    // to spawn these so that only one per tick is spawned.
    public static IEnumerable<Microbe> SpawnBacteriaColony(Species species, Vector2 location,
        Node worldRoot, PackedScene microbeScene, CompoundCloudSystem cloudSystem,
        GameProperties currentGame, Random random)
    {
        var curSpawn = new Vector2(random.Next(1, 8), random.Next(1, 8));

        // Three kinds of colonies are supported, line colonies and clump colonies and Networks
        if (random.Next(0, 5) < 2)
        {
            // Clump
            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_COLONY_SIZE,
                Constants.MAX_BACTERIAL_COLONY_SIZE + 1); i++)
            {
                // Dont spawn them on top of each other because it
                // causes them to bounce around and lag
                yield return SpawnMicrobe(species, location + curSpawn, worldRoot, microbeScene, true,
                    cloudSystem, currentGame);

                curSpawn = curSpawn + new Vector2(random.Next(-7, 8), random.Next(-7, 8));
            }
        }
        else if (random.Next(0, 31) > 2)
        {
            // Line
            // Allow for many types of line
            // (I combined the lineX and lineZ here because they have the same values)
            var line = random.Next(-5, 6) + random.Next(-5, 6);

            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_LINE_SIZE,
                Constants.MAX_BACTERIAL_LINE_SIZE + 1); i++)
            {
                // Dont spawn them on top of each other because it
                // Causes them to bounce around and lag
                yield return SpawnMicrobe(species, location + curSpawn, worldRoot, microbeScene, true,
                    cloudSystem, currentGame);

                curSpawn = curSpawn + new Vector2(line + random.Next(-2, 3), line + random.Next(-2, 3));
            }
        }
        else
        {
            // Network
            // Allows for "jungles of cyanobacteria"
            // Network is extremely rare

            // To prevent bacteria being spawned on top of each other
            var vertical = false;

            var colony = new ColonySpawnInfo
            {
                Horizontal = false,
                Random = random,
                Species = species,
                CloudSystem = cloudSystem,
                CurrentGame = currentGame,
                CurSpawn = curSpawn,
                MicrobeScene = microbeScene,
                WorldRoot = worldRoot,
            };

            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_COLONY_SIZE,
                Constants.MAX_BACTERIAL_COLONY_SIZE + 1); i++)
            {
                if (random.Next(0, 5) < 2 && !colony.Horizontal)
                {
                    colony.Horizontal = true;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !vertical)
                {
                    colony.Horizontal = false;
                    vertical = true;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !colony.Horizontal)
                {
                    colony.Horizontal = true;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !vertical)
                {
                    colony.Horizontal = false;
                    vertical = true;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
                else
                {
                    // Diagonal
                    colony.Horizontal = false;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location))
                        yield return microbe;
                }
            }
        }
    }

    public static PackedScene LoadMicrobeScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    public static FloatingChunk SpawnChunk(ChunkConfiguration chunkType,
        Vector2 location, Node worldNode, PackedScene chunkScene,
        CompoundCloudSystem cloudSystem, Random random)
    {
        var chunk = (FloatingChunk)chunkScene.Instance();

        // Settings need to be applied before adding it to the scene
        var selectedMesh = chunkType.Meshes.Random(random);
        chunk.GraphicsScene = selectedMesh.LoadedScene;
        chunk.ConvexPhysicsMesh = selectedMesh.LoadedConvexShape;

        if (chunk.GraphicsScene == null)
            throw new ArgumentException("couldn't find a graphics scene for a chunk");

        // Pass on the chunk data
        chunk.Init(chunkType, cloudSystem, selectedMesh.SceneModelPath);
        chunk.UsesDespawnTimer = !chunkType.Dissolves;

        worldNode.AddChild(chunk);

        // Chunk is spawned with random rotation
        chunk.Transform = new Transform(new Quat(
                new Vector3(0, 1, 1).Normalized(), 2 * Mathf.Pi * (float)random.NextDouble()),
            new Vector3(location.x, 0, location.y));

        chunk.GetNode<Spatial>("NodeToScale").Scale = new Vector3(chunkType.ChunkScale, chunkType.ChunkScale,
            chunkType.ChunkScale);

        chunk.AddToGroup(Constants.FLUID_EFFECT_GROUP);
        chunk.AddToGroup(Constants.AI_TAG_CHUNK);
        return chunk;
    }

    public static PackedScene LoadChunkScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/FloatingChunk.tscn");
    }

    public static void SpawnCloud(CompoundCloudSystem clouds, Vector2 location, Compound compound, float amount)
    {
        int resolution = Settings.Instance.CloudResolution;

        // This spreads out the cloud spawn a bit
        clouds.AddCloud(compound, amount, location + new Vector2(0 + resolution, 0));
        clouds.AddCloud(compound, amount, location + new Vector2(0 - resolution, 0));
        clouds.AddCloud(compound, amount, location + new Vector2(0, 0 + resolution));
        clouds.AddCloud(compound, amount, location + new Vector2(0, 0 - resolution));
        clouds.AddCloud(compound, amount, location + new Vector2(0, 0));
    }

    /// <summary>
    ///   Spawns an agent projectile
    /// </summary>
    public static AgentProjectile SpawnAgent(AgentProperties properties, float amount,
        float lifetime, Vector2 location, Vector2 direction,
        Node worldRoot, PackedScene agentScene, IEntity emitter)
    {
        var normalizedDirection = direction.Normalized();

        var agent = (AgentProjectile)agentScene.Instance();
        agent.Properties = properties;
        agent.Amount = amount;
        agent.TimeToLiveRemaining = lifetime;
        agent.Emitter = new EntityReference<IEntity>(emitter);

        worldRoot.AddChild(agent);
        agent.Translation = (location + direction * 1.5f).ToVector3();

        agent.ApplyCentralImpulse(normalizedDirection.ToVector3() *
            Constants.AGENT_EMISSION_IMPULSE_STRENGTH);

        agent.AddToGroup(Constants.TIMED_GROUP);
        return agent;
    }

    public static PackedScene LoadAgentScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/AgentProjectile.tscn");
    }

    private static IEnumerable<Microbe> MicrobeColonySpawnHelper(ColonySpawnInfo colony, Vector2 location)
    {
        for (int c = 0; c < colony.Random.Next(Constants.MIN_BACTERIAL_LINE_SIZE,
            Constants.MAX_BACTERIAL_LINE_SIZE + 1); c++)
        {
            // Dont spawn them on top of each other because
            // It causes them to bounce around and lag
            // And add a little organicness to the look

            if (colony.Horizontal)
            {
                colony.CurSpawn.x += colony.Random.Next(5, 8);
                colony.CurSpawn.y += colony.Random.Next(-2, 3);
            }
            else
            {
                colony.CurSpawn.y += colony.Random.Next(5, 8);
                colony.CurSpawn.x += colony.Random.Next(-2, 3);
            }

            yield return SpawnMicrobe(colony.Species, location + colony.CurSpawn, colony.WorldRoot,
                colony.MicrobeScene, true, colony.CloudSystem, colony.CurrentGame);
        }
    }

    private class ColonySpawnInfo
    {
        public Species Species;
        public Node WorldRoot;
        public PackedScene MicrobeScene;
        public Vector2 CurSpawn;
        public bool Horizontal;
        public Random Random;
        public CompoundCloudSystem CloudSystem;
        public GameProperties CurrentGame;
    }
}

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner : Spawner
{
    private readonly PackedScene microbeScene;
    private readonly Species species;
    private readonly CompoundCloudSystem cloudSystem;
    private readonly GameProperties currentGame;
    private readonly Random random;

    public MicrobeSpawner(Species species, CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        this.species = species ?? throw new ArgumentException("species is null");

        microbeScene = SpawnHelpers.LoadMicrobeScene();
        this.cloudSystem = cloudSystem;
        this.currentGame = currentGame;

        random = new Random();
    }

    public override IEnumerable<SpawnedRigidBody> Spawn(Node worldNode, Vector2 location)
    {
        // The true here is that this is AI controlled
        var first = SpawnHelpers.SpawnMicrobe(species, location, worldNode, microbeScene, true, cloudSystem,
            currentGame);

        yield return first;

        if (first.Species.IsBacteria)
        {
            foreach (var colonyMember in SpawnHelpers.SpawnBacteriaColony(species, location, worldNode, microbeScene,
                cloudSystem, currentGame, random))
            {
                yield return colonyMember;
            }
        }
    }
}

/// <summary>
///   Spawns compound clouds of a certain type
/// </summary>
public class CompoundCloudSpawner : Spawner
{
    private readonly Compound compound;
    private readonly CompoundCloudSystem clouds;
    private readonly float amount;

    public CompoundCloudSpawner(Compound compound, CompoundCloudSystem clouds, float amount)
    {
        this.compound = compound ?? throw new ArgumentException("compound is null");
        this.clouds = clouds ?? throw new ArgumentException("clouds is null");
        this.amount = amount;
    }

    public override int BinomialN => (int)(amount / 50000);
    public override float BinomialP => 0.25f;
    public override float MinDistanceSquared => 10;

    public override IEnumerable<SpawnedRigidBody> Spawn(Node worldNode, Vector2 location)
    {
        SpawnHelpers.SpawnCloud(clouds, location, compound, amount);

        // We don't spawn entities
        return null;
    }
}

/// <summary>
///   Spawns chunks of a specific type
/// </summary>
public class ChunkSpawner : Spawner
{
    private readonly PackedScene chunkScene;
    private readonly ChunkConfiguration chunkType;
    private readonly Random random = new Random();
    private readonly CompoundCloudSystem cloudSystem;

    public ChunkSpawner(ChunkConfiguration chunkType, CompoundCloudSystem cloudSystem)
    {
        this.chunkType = chunkType;
        this.cloudSystem = cloudSystem;
        chunkScene = SpawnHelpers.LoadChunkScene();
    }

    public override IEnumerable<SpawnedRigidBody> Spawn(Node worldNode, Vector2 location)
    {
        var chunk = SpawnHelpers.SpawnChunk(chunkType, location, worldNode, chunkScene,
            cloudSystem, random);

        yield return chunk;
    }
}
