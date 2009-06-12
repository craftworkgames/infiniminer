namespace Infiniminer
{
    public class BlockInfo
    {
        public static BlockInfo typeNone(Point3D pos)
        {
            return new BlockInfo(pos, BlockType.None, PlayerTeam.None);
        }
        public static BlockInfo typeDirt(Point3D pos)
        {
            return new BlockInfo(pos, BlockType.Dirt, PlayerTeam.None);
        }
        public BlockInfo(Point3D pos, BlockType type, PlayerTeam team)
        {
            _pos = pos;
            _type = type;
            _team = team;
        }
        private Point3D _pos;
        public Point3D pos
        {
            get { return _pos; }
        }

        private BlockType _type;
        public BlockType type
        {
            get { return _type; }
        }

        private PlayerTeam _team;
        public PlayerTeam team
        {
            get { return _team; }
        }
        public static bool isBeacon(BlockInfo block)
        {
            return (block.type == BlockType.BeaconA || block.type == BlockType.BeaconB);
        }
        public static void changeType(ref BlockInfo block, BlockType type)
        {
            if (block.type == type)
            {
                return;
            }
            else
            {
                block = new BlockInfo(block.pos, type, unownedType(type) ? PlayerTeam.None : block.team);
            }
        }
        public static bool unownedType(BlockType type)
        {
            switch (type)
            {
                case BlockType.Diamond:
                case BlockType.Dirt:
                case BlockType.DirtSign:
                case BlockType.Gold:
                case BlockType.Lava:
                case BlockType.Rock:
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }
    }
}