# Introduction #

GreyMario and myself (SignpostMarv) were discussing the initial implementation of the "Natural Environments" feature. The problem with enabling "Natural Environments" is that it currently requires additional block types to be created for special texturing cases- such as dirt at ground level being exposed to the air having the top face replaced with a grass texture.


# Details #
Adding additional block types to a map completely breaks compatibility with the trunk client.

A solution to this problem is to texture block faces based on context.

All methods involved with creating or accessing the blockTextureMap have access to map data in some way. This knowledge allows for the possibility to have the top face of a trunk-based client's BlockType.Dirt textured with the grass texture used by Marvulous Mod's BlockType.DirtGrass.

It has been suggested that a block's context be stored as a bitfield of values, calculated when the client recieves an InfiniminerMessage.BlockBulkTransfer update.

# Contexts #
Any context that can be determined purely on the co-ordinates of a block need not be held within the scope of the ContextualTextures system, e.g. whether a block is above or below ground level.

  * Exposed to sunlight - for purposes of simplicity, light may be considered to only travel horizontally, vertically, or even just downwards.
  * Exposed to air (BlockType.None) (this is currently what is actually used to decide whether BlockType.Dirt should be changed to BlockType.DirtGrass)
  * Exposed to lava - it has been suggested in IRC that some block types, when exposed to lava, should "melt".
  * Obscured by solid block (the block is hidden by any block that is not see-through)
  * Obscured by transparent block (the block is obscured by a shield- may be useful for animating an interference pattern if block context has "exposed to lava" as well)
  * Is tunnel
    * North-South tunnel
    * East-West tunnel
    * T Junction
    * crossroads