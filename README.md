# Amity Edits

A non destructive unity avatar creation toolbox, using ndmf

> [!NOTE]
> this project is still in early stages of development.

## Features

- Selore: a shader (WIP: and shader patcher) similar to VRCFury's SPS (compatible with DPS, SPS, TPS) - includes both sides
- Move Object: allows you to organize your avatar however you like, and move objects to their place on build.
- Outfit and Clothing item: purpose built clothing system for my neededs. 
  Allows mix and matching any items of any outfit, and defining fine grained incompaibilities. 
  Can generate clothing item presets (outfits)
- [WIP] Reorder Menus: picks up on menus in the avatar and allows you to reorder them. 
  Requires at least one avatar build to read existing menus for reordering.
- [WIP] Menu Item: similar to VRCF's "Toggle" Component, and MA's in tree Menu Components. 
  Allows you to specify a VRC Menu Control in the avatar hierarchy, 
  while also specifying actions the new menu cotnrol should trigger.

### some future plans:
- the menu item may get scrapped in favor of using Hai's paid Vixen, 
  though its paid nature still motivates me to continue working on this implementation.
- a standalone parameter optimizer similar to VRCFury's.
  There is a standalone version out there, but it isn't as battle tested and doesn't have all the same features.
  My plan here may end up being PRs to the other existing one, depending on how far i get.
- Modularity: Amity features will be split up into their own packages, 
  which you can selectively install. There will be a core package including the header of all components, 
  including the localization framework, as well as the component field search. 
  Utility functions will most likely live here too, unless they end up in a Amity.Utils package.
- Reorder Menus: this component will look for existing components from MA and VRCFury and try to 
  deduplicate "more" or "next" buttons when menus are overflowing.
  It will also use VRCF's "Menu Options" component to generate Amity's own "Next Page" buttons.

## Installation

1. when released Amity Edits will be available on OpenUPM.

2. There may also be a VPM Repository, if there is demand.

3. The Package can always be installed by downloading the code directly, importing it as git package, or manually cloning it.

more detailed instructions will be added as this project approaches a proper release.