# TODO

1. Simplify Go entity to flash using animation instead of all that code
1. Per frame hurtboxes?
1. Global game speed setting so we can pause gameplay and do slowmotion and stuff
1. Replace EntityManager's type-check-based list registration with interface-based dispatch — register via typed methods or auto-discovery to remove the `is` chain in `AddToTypedLists`/`RemoveFromTypedLists`
1. Make oildrum collision box shorter so the player can walk past below the drum without having to clear the bottom of the drum head
