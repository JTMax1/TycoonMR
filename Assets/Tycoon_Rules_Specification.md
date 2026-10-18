# Tycoon Metropolis VR — Authoritative Game Rules Specification

**Document Version:** 1.0.0 (Hardened Baseline)  
**Authority:** Single-Source-of-Truth for Domain Rules, Economy Engines, AI Decisions, and Tests.

---

## 1. Board Layout and Tile Taxonomy (40 Tiles)

The board consists of 40 canonical tiles arranged clockwise:
- **Properties (22 tiles):** Divided into 8 distinct color groups.
- **Railroads / Transit Hubs (4 tiles):** Tiles 5, 15, 25, 35.
- **Utilities (2 tiles):** Electric Grid (Tile 12), Water Works (Tile 28).
- **Tax Tiles (2 tiles):** Luxury Tax (Tile 4) and Income Tax (Tile 38). Proceeds feed into the Free Parking Jackpot.
- **Corner & Action Tiles (6 tiles):**
  - Tile 0: **GO** (Collects $200 salary on passing or landing).
  - Tile 10: **Just Visiting / In Jail**.
  - Tile 20: **Free Parking** (Collects accumulated tax/penalty jackpot, minimum $100).
  - Tile 30: **Go To Jail** (Teleports player token to Tile 10, sets jail flag, turn ends).
  - Tiles 7, 22, 36: **Chance Decks**.
  - Tiles 2, 17, 33: **Community Chest Decks**.

---

## 2. Color Groups and Group Completion (Monopoly)

A player owns a full color group ("Monopoly") only when possessing 100% of the deeds in that color group:
1. **Group 0 (Brown):** 2 properties (Tiles 1, 3). Build cost: $50.
2. **Group 1 (Light Blue):** 3 properties (Tiles 6, 8, 9). Build cost: $50.
3. **Group 2 (Pink):** 3 properties (Tiles 11, 13, 14). Build cost: $100.
4. **Group 3 (Orange):** 3 properties (Tiles 16, 18, 19). Build cost: $100.
5. **Group 4 (Red):** 3 properties (Tiles 21, 23, 24). Build cost: $150.
6. **Group 5 (Yellow):** 3 properties (Tiles 26, 27, 29). Build cost: $150.
7. **Group 6 (Green):** 3 properties (Tiles 31, 32, 34). Build cost: $200.
8. **Group 7 (Dark Blue):** 2 properties (Tiles 37, 39). Build cost: $200.

---

## 3. Property Acquisition and Unimproved Status

1. **Initial Acquisition:** When an unowned property is purchased (or won at auction), deed ownership transfers to the buyer.
2. **Initial Development Level:** Newly acquired properties start at **Level 0 (Unimproved)**. Purchase **never** automatically erects houses or skyscrapers.
3. **Deed Purchase Affordability:** Player cash must be strictly greater than or equal to `tile.price`. Deduct cash and commit ownership atomically.

---

## 4. Authoritative Rent Calculations

### 4.1 Standard Properties
- **Mortgaged:** If `isMortgaged == true`, rent is **$0**.
- **Level 0 (Unimproved, Incomplete Group):** Rent is the base rent tier `tile.rentByLevel[0]`.
- **Level 0 (Unimproved, Complete Monopoly Group):** If the owner possesses all properties in that color group and **none** are mortgaged, base rent is doubled: `tile.rentByLevel[0] * 2`.
- **Levels 1 to 4 (Houses):** Rent is strictly `tile.rentByLevel[buildingLevel]`. The 2x monopoly multiplier does **not** stack on top of improved building tiers.
- **Level 5 (Skyscraper Hotel):** Rent is strictly `tile.rentByLevel[5]`.

### 4.2 Railroads
- Rent scales with the number of railroads owned by the deed holder:
  - 1 Railroad: $25
  - 2 Railroads: $50
  - 3 Railroads: $100
  - 4 Railroads: $200
  - Formula: `25 * 2^(railroadsOwned - 1)`

### 4.3 Utilities
- If 1 Utility owned: Rent = `diceRoll * 4`.
- If both Utilities owned (Tiles 12 and 28): Rent = `diceRoll * 10`.

### 4.4 Market Cycle Multipliers
Market cycle multipliers apply to calculated rent before hand-off:
- **Standard:** 1.0x.
- **Bull Boom:** 1.5x on all property rents.
- **Bear Recession:** 0.70x on property rents; building construction cost reduced to 70%.
- **Tech IPO Surge:** 2.5x multiplier on Railroad and Utility rents.

---

## 5. Building Construction & Even-Building Rule

A player may only build an improvement (Levels 1–5) on a property when all preconditions pass (`CanBuildResult.Success`):
1. **Ownership Precondition:** The player must own the target property.
2. **Group Completion:** The player must own **all** properties in that color group (`OwnsFullColorGroup`).
3. **No Mortgages in Group:** No property in that color group may be mortgaged.
4. **Even-Building Rule:** Improvements must be built evenly across all properties in the color group. A property cannot be upgraded to level `L + 1` until **all** other properties in that same color group have reached level `L`. (Maximum allowed level delta across properties in the group is 0 before an upgrade).
5. **Maximum Level Ceiling:** Maximum development level is 5 (Skyscraper Hotel). Properties at Level 5 cannot be built further.
6. **Affordability:** Player must have available cash >= `actualBuildCost`.

---

## 6. Auctions and Passing

1. When an active player lands on an unowned property and declines purchase (or clicks Pass), the property immediately enters **Public Auction**.
2. **Eligible Bidders:** All non-bankrupt players (including the player who passed) may participate.
3. **Starting Bid:** Minimum bid starts at $10 (or 10% of tile price).
4. **Bidding Increments:** Minimum bid increment is $10.
5. **Affordability Enforcement:** A player or AI cannot place a bid higher than their current available cash.
6. **Pass Rule:** A bidder who passes is withdrawn from the current auction.
7. **Resolution:** When only one bidder remains (or all other bidders pass), the property is awarded to the highest bidder at the committed bid amount. If all players pass without bidding, the property remains unowned.

---

## 7. Bankruptcy Settlement and Elimination

1. **Trigger:** A player is bankrupt when an unavoidable debt (rent, tax, penalty) exceeds their total net worth (cash + liquidation value of all unmortgaged assets).
2. **Order of Liquidation:**
   - 1. Buildings sold back to the bank at 50% of build cost (in reverse-even order).
   - 2. Properties mortgaged to the bank for 50% of deed price.
   - 3. If cash remains insufficient, all remaining deeds transfer to the creditor (or to the bank if debt was owed to taxes/bank).
3. **Player Elimination:** The bankrupt player is eliminated from the game.
4. **Match Continuity:** The match **continues** with the remaining active players. Match over is declared **only** when exactly 1 non-bankrupt player remains. Match reset must **never** be triggered by the bankruptcy of a single non-final player.
