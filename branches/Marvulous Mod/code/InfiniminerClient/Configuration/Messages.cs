namespace Infiniminer
{
    public enum InfiniminerMessage : byte
    {
        MapInfo,                // server information (currently only mapsize)
        BlockBulkTransfer,      // x-value, y-value, followed by mapsize bytes of blocktype ; 
        BlockSet,               // x, y, z, type
        UseTool,                // position, heading, tool, blocktype 
        SelectClass,            // class
        ResourceUpdate,         // ore, cash, weight, max ore, max weight, team ore, team A cash, team B cash: ReliableInOrder1
        DepositOre,
        DepositCash,
        WithdrawOre,
        TriggerExplosion,       // position

        PlayerUpdate,           // (uint id for server), position, heading, current tool, animate using (bool): UnreliableInOrder1
        PlayerJoined,           // uint id, player name :ReliableInOrder2
        PlayerLeft,             // uint id              :ReliableInOrder2
        PlayerSetTeam,          // (uint id for server), byte team   :ReliableInOrder2
        PlayerDead,             // (uint id for server) :ReliableInOrder2
        PlayerAlive,            // (uint id for server) :ReliableInOrder2
        PlayerPing,             // uint id

        ChatMessage,            // byte type, string message : ReliableInOrder3
        GameOver,               // byte team
        PlaySound,              // byte sound, bool isPositional, ?Vector3 location : ReliableUnordered
        TriggerConstructionGunAnimation,
        SetBeacon,              // vector3 position, string text ("" means remove)
        TeamConfig,             // byte team, string name, string color, string blood
        compatibleClient,       // string url to download page (preferably web page, not direct link)
    }
}