using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public partial class TycoonBoardManager
{
    // =========================================================================
    // TYCOON BOARD STATE: MAP INITIALIZATION, 3D GEOMETRY, TILES & MONOPOLIES
    // =========================================================================

    public enum TileType { Go, Property, Railroad, Utility, Tax, Chance, Chest, Jail, FreeParking, GoToJail }

    public class BoardTile
    {
        public int index;
        public string name;
        public TileType type;
        public int colorGroup;
        public int price;
        public int buildCost;
        public int[] rentByLevel;
        public int owner = -1;
        public int buildingLevel = 0;
        public bool isMortgaged = false;
        public Transform tileTransform;
        public Renderer tileRenderer;
        public GameObject ownershipBadge;
        public GameObject spawnedBuilding;
    }

    public BoardTile GetTile(int index) => (board != null && index >= 0 && index < 40) ? board[index] : null;

    public void InitializeOfficialMonopolyMap()
    {
        SetTile(0, "GO (COLLECT $200)", TileType.Go, -1, 0, 0, null);
        SetTile(1, "MEDITERRANEAN AVE", TileType.Property, 0, 60, 50, new int[] { 2, 10, 30, 90, 160, 250 });
        SetTile(2, "COMMUNITY CHEST", TileType.Chest, -1, 0, 0, null);
        SetTile(3, "BALTIC AVE", TileType.Property, 0, 60, 50, new int[] { 4, 20, 60, 180, 320, 450 });
        SetTile(4, "INCOME TAX ($200)", TileType.Tax, -1, 200, 0, null);
        SetTile(5, "READING RAILROAD", TileType.Railroad, -1, 200, 0, null);
        SetTile(6, "ORIENTAL AVE", TileType.Property, 1, 100, 50, new int[] { 6, 30, 90, 270, 400, 550 });
        SetTile(7, "CHANCE", TileType.Chance, -1, 0, 0, null);
        SetTile(8, "VERMONT AVE", TileType.Property, 1, 100, 50, new int[] { 6, 30, 90, 270, 400, 550 });
        SetTile(9, "CONNECTICUT AVE", TileType.Property, 1, 120, 50, new int[] { 8, 40, 100, 300, 450, 600 });

        SetTile(10, "JAIL / JUST VISITING", TileType.Jail, -1, 0, 0, null);
        SetTile(11, "ST. CHARLES PLACE", TileType.Property, 2, 140, 100, new int[] { 10, 50, 150, 450, 625, 750 });
        SetTile(12, "ELECTRIC COMPANY", TileType.Utility, -1, 150, 0, null);
        SetTile(13, "STATES AVE", TileType.Property, 2, 140, 100, new int[] { 10, 50, 150, 450, 625, 750 });
        SetTile(14, "VIRGINIA AVE", TileType.Property, 2, 160, 100, new int[] { 12, 60, 180, 500, 700, 900 });
        SetTile(15, "PENNSYLVANIA RR", TileType.Railroad, -1, 200, 0, null);
        SetTile(16, "ST. JAMES PLACE", TileType.Property, 3, 180, 100, new int[] { 14, 70, 200, 550, 750, 950 });
        SetTile(17, "COMMUNITY CHEST", TileType.Chest, -1, 0, 0, null);
        SetTile(18, "TENNESSEE AVE", TileType.Property, 3, 180, 100, new int[] { 14, 70, 200, 550, 750, 950 });
        SetTile(19, "NEW YORK AVE", TileType.Property, 3, 200, 100, new int[] { 16, 80, 220, 600, 800, 1000 });

        SetTile(20, "FREE PARKING", TileType.FreeParking, -1, 0, 0, null);
        SetTile(21, "KENTUCKY AVE", TileType.Property, 4, 220, 150, new int[] { 18, 90, 250, 700, 875, 1050 });
        SetTile(22, "CHANCE", TileType.Chance, -1, 0, 0, null);
        SetTile(23, "INDIANA AVE", TileType.Property, 4, 220, 150, new int[] { 18, 90, 250, 700, 875, 1050 });
        SetTile(24, "ILLINOIS AVE", TileType.Property, 4, 240, 150, new int[] { 20, 100, 300, 750, 925, 1100 });
        SetTile(25, "B. & O. RAILROAD", TileType.Railroad, -1, 200, 0, null);
        SetTile(26, "ATLANTIC AVE", TileType.Property, 5, 260, 150, new int[] { 22, 110, 330, 800, 975, 1150 });
        SetTile(27, "VENTNOR AVE", TileType.Property, 5, 260, 150, new int[] { 22, 110, 330, 800, 975, 1150 });
        SetTile(28, "WATER WORKS", TileType.Utility, -1, 150, 0, null);
        SetTile(29, "MARVIN GARDENS", TileType.Property, 5, 280, 150, new int[] { 24, 120, 360, 850, 1025, 1200 });

        SetTile(30, "GO TO JAIL", TileType.GoToJail, -1, 0, 0, null);
        SetTile(31, "PACIFIC AVE", TileType.Property, 6, 300, 200, new int[] { 26, 130, 390, 900, 1100, 1275 });
        SetTile(32, "NORTH CAROLINA AVE", TileType.Property, 6, 300, 200, new int[] { 26, 130, 390, 900, 1100, 1275 });
        SetTile(33, "COMMUNITY CHEST", TileType.Chest, -1, 0, 0, null);
        SetTile(34, "PENNSYLVANIA AVE", TileType.Property, 6, 320, 200, new int[] { 28, 150, 450, 1000, 1200, 1400 });
        SetTile(35, "SHORT LINE RR", TileType.Railroad, -1, 200, 0, null);
        SetTile(36, "CHANCE", TileType.Chance, -1, 0, 0, null);
        SetTile(37, "PARK PLACE", TileType.Property, 7, 350, 200, new int[] { 35, 175, 500, 1100, 1300, 1500 });
        SetTile(38, "LUXURY TAX ($100)", TileType.Tax, -1, 100, 0, null);
        SetTile(39, "BOARDWALK", TileType.Property, 7, 400, 200, new int[] { 50, 200, 600, 1400, 1700, 2000 });
    }

    void SetTile(int index, string name, TileType type, int colorGroup, int price, int buildCost, int[] rents)
    {
        board[index] = new BoardTile
        {
            index = index,
            name = name,
            type = type,
            colorGroup = colorGroup,
            price = price,
            buildCost = buildCost,
            rentByLevel = rents
        };
    }

    void Build40TileClockwiseTable()
    {
        if (darkMarbleMaterial == null)
            darkMarbleMaterial = CreateColoredMaterial(new Color(0.06f, 0.07f, 0.09f), 0.9f);

        if (brushedGoldMaterial == null)
            brushedGoldMaterial = CreateColoredMaterial(new Color(0.82f, 0.65f, 0.26f), 0.78f, 0.88f);

        GameObject tableBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tableBase.name = "Tycoon_Marble_Base";
        tableBase.transform.SetParent(transform, false);
        tableBase.transform.localScale = new Vector3(boardSize, 0.04f, boardSize);
        tableBase.GetComponent<Renderer>().material = darkMarbleMaterial;

        GameObject goldRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goldRim.name = "Brushed_Gold_Bezel";
        goldRim.transform.SetParent(transform, false);
        goldRim.transform.localPosition = new Vector3(0, -0.005f, 0);
        goldRim.transform.localScale = new Vector3(boardSize + 0.04f, 0.03f, boardSize + 0.04f);
        Destroy(goldRim.GetComponent<Collider>());
        goldRim.GetComponent<Renderer>().material = brushedGoldMaterial;

        int gridCount = 11;
        float span = boardSize * 0.88f;
        float step = span / (gridCount - 1);
        float half = span / 2f;

        List<Vector3> tilePositions = new List<Vector3>();
        for (int i = 0; i < gridCount; i++) tilePositions.Add(new Vector3(half - i * step, 0.025f, -half));
        for (int i = 1; i < gridCount; i++) tilePositions.Add(new Vector3(-half, 0.025f, -half + i * step));
        for (int i = 1; i < gridCount; i++) tilePositions.Add(new Vector3(-half + i * step, 0.025f, half));
        for (int i = 1; i < gridCount - 1; i++) tilePositions.Add(new Vector3(half, 0.025f, half - i * step));

        for (int i = 0; i < 40; i++)
        {
            bool isCorner = (i % 10 == 0);
            GameObject plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plot.name = $"Tile_{i}_{board[i].name}";
            plot.transform.SetParent(transform, false);
            plot.transform.localPosition = tilePositions[i];

            int side = i / 10;
            plot.transform.localRotation = Quaternion.Euler(0f, side * 90f, 0f);

            float width = isCorner ? step * 0.92f : step * 0.80f;
            float depth = isCorner ? step * 0.92f : step * 0.96f;
            plot.transform.localScale = new Vector3(width, 0.014f, depth);

            BoxCollider tileCol = plot.GetComponent<BoxCollider>();
            if (tileCol != null) tileCol.isTrigger = true;

            Renderer plotRenderer = plot.GetComponent<Renderer>();
            plotRenderer.material = CreateColoredMaterial(new Color(0.82f, 0.65f, 0.26f), 0.78f, 0.5f);
            board[i].tileRenderer = plotRenderer;

            if (!isCorner && (board[i].colorGroup >= 0 || board[i].type == TileType.Railroad || board[i].type == TileType.Utility))
            {
                GameObject colorStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                colorStrip.name = "District_Color_Band";
                colorStrip.transform.SetParent(plot.transform, false);
                colorStrip.transform.localPosition = new Vector3(0f, 0.75f, 0.32f);
                colorStrip.transform.localScale = new Vector3(0.94f, 0.45f, 0.28f);
                Destroy(colorStrip.GetComponent<Collider>());
                colorStrip.GetComponent<Renderer>().material = CreateColoredMaterial(GetOfficialColor(board[i]), 0.9f);
            }

            board[i].tileTransform = plot.transform;
        }

        SpawnCornerLandmark(0, goCornerLandmarkPrefab, "Landmark_Go_Market");
        SpawnCornerLandmark(10, jailCornerLandmarkPrefab, "Landmark_Jail_PoliceStation");
        SpawnCornerLandmark(20, freeParkingLandmarkPrefab, "Landmark_FreeParking_Hospital");
        SpawnCornerLandmark(30, goToJailLandmarkPrefab, "Landmark_GoToJail_School");

        StaticBatchingUtility.Combine(new GameObject[] { tableBase, goldRim }, gameObject);
    }

    void SpawnCornerLandmark(int cornerTileIdx, GameObject prefab, string landmarkName)
    {
        if (!showCornerLandmarks || prefab == null) return;
        GameObject holder = new GameObject(landmarkName);
        holder.transform.SetParent(transform, false);

        Vector3 localPos;
        Quaternion localRot;

        switch (cornerTileIdx)
        {
            case 0: // GO - South-East (Market)
                localPos = new Vector3(0.18f, 0.028f, -0.18f);
                localRot = Quaternion.Euler(0f, -90f, 0f);
                break;
            case 10: // Jail - South-West (Police_Station)
                localPos = new Vector3(-0.18f, 0.028f, -0.18f);
                localRot = Quaternion.Euler(0f, 0f, 0f);
                break;
            case 20: // Free Parking - North-West (Hospital)
                localPos = new Vector3(-0.18f, 0.028f, 0.18f);
                localRot = Quaternion.Euler(0f, 90f, 0f);
                break;
            case 30: // Go To Jail - North-East (School)
                localPos = new Vector3(0.18f, 0.028f, 0.18f);
                localRot = Quaternion.Euler(0f, 180f, 0f);
                break;
            default:
                localPos = Vector3.zero;
                localRot = Quaternion.identity;
                break;
        }

        holder.transform.localPosition = localPos;
        holder.transform.localRotation = localRot;

        GameObject instance = Instantiate(prefab, holder.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        StripRoguePhysicsFromImportedPrefab(instance);
        NormalizePrefabSize(instance, 0.065f);
    }

    Color GetOfficialColor(BoardTile tile)
    {
        if (tile.type == TileType.Railroad) return new Color(0.15f, 0.17f, 0.22f);
        if (tile.type == TileType.Utility) return new Color(0.88f, 0.92f, 0.98f);
        switch (tile.colorGroup)
        {
            case 0: return new Color(0.55f, 0.28f, 0.14f);
            case 1: return new Color(0.35f, 0.78f, 0.98f);
            case 2: return new Color(0.92f, 0.20f, 0.68f);
            case 3: return new Color(0.98f, 0.55f, 0.08f);
            case 4: return new Color(0.92f, 0.14f, 0.18f);
            case 5: return new Color(0.98f, 0.88f, 0.12f);
            case 6: return new Color(0.12f, 0.76f, 0.32f);
            case 7: return new Color(0.14f, 0.30f, 0.92f);
            default: return Color.gray;
        }
    }

    void BuildLivingCenterCourtyard()
    {
        GameObject courtyardRoot = new GameObject("Inner_Metropolis_Courtyard");
        courtyardRoot.transform.SetParent(transform, false);

        Material asphaltMat = CreateColoredMaterial(new Color(0.14f, 0.16f, 0.19f), 0.45f);
        Material laneLineMat = CreateColoredMaterial(new Color(0.95f, 0.80f, 0.25f), 0.7f);
        Material parkLawnMat = CreateColoredMaterial(new Color(0.14f, 0.48f, 0.24f), 0.35f);
        Material helipadMat = CreateColoredMaterial(new Color(0.20f, 0.23f, 0.28f), 0.75f);

        GameObject roadPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadPlate.name = "Inner_Ring_Road_Asphalt";
        roadPlate.transform.SetParent(courtyardRoot.transform, false);
        roadPlate.transform.localPosition = new Vector3(0f, 0.021f, 0f);
        roadPlate.transform.localScale = new Vector3(0.76f, 0.004f, 0.76f);
        roadPlate.GetComponent<Renderer>().material = asphaltMat;

        GameObject parkIsland = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parkIsland.name = "Central_Park_Plaza";
        parkIsland.transform.SetParent(courtyardRoot.transform, false);
        parkIsland.transform.localPosition = new Vector3(0f, 0.024f, 0f);
        parkIsland.transform.localScale = new Vector3(0.56f, 0.008f, 0.56f);
        parkIsland.GetComponent<Renderer>().material = parkLawnMat;

        GameObject parkCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parkCurb.name = "Central_Park_Gold_Curb";
        parkCurb.transform.SetParent(courtyardRoot.transform, false);
        parkCurb.transform.localPosition = new Vector3(0f, 0.023f, 0f);
        parkCurb.transform.localScale = new Vector3(0.58f, 0.006f, 0.58f);
        Destroy(parkCurb.GetComponent<Collider>());
        parkCurb.GetComponent<Renderer>().material = laneLineMat;

        GameObject helipad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        helipad.name = "VIP_Dice_Helipad";
        helipad.transform.SetParent(courtyardRoot.transform, false);
        helipad.transform.localPosition = new Vector3(0f, 0.029f, -0.05f);
        helipad.transform.localScale = new Vector3(0.24f, 0.003f, 0.24f);
        Destroy(helipad.GetComponent<Collider>());
        helipad.GetComponent<Renderer>().material = helipadMat;

        GameObject chestRoot = new GameObject("Free_Parking_Jackpot_Chest");
        chestRoot.transform.SetParent(courtyardRoot.transform, false);
        chestRoot.transform.localPosition = new Vector3(-0.13f, 0.028f, 0.08f);
        chestRoot.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

        if (jackpotChestPrefab != null)
        {
            GameObject chestModel = Instantiate(jackpotChestPrefab, chestRoot.transform, false);
            StripRoguePhysicsFromImportedPrefab(chestModel);
            NormalizePrefabSize(chestModel, 0.055f, true);
            chestModel.transform.localPosition = Vector3.zero;
        }
        else
        {
            Material woodMat = CreateColoredMaterial(new Color(0.42f, 0.24f, 0.12f), 0.6f, 0.05f);
            Material goldTrimMat = CreateColoredMaterial(new Color(1f, 0.85f, 0.22f), 0.95f, 0.85f);
            Material metalLockMat = CreateColoredMaterial(new Color(0.85f, 0.88f, 0.92f), 0.9f, 0.8f);

            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.transform.SetParent(chestRoot.transform, false);
            baseBox.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            baseBox.transform.localScale = new Vector3(0.055f, 0.024f, 0.038f);
            Destroy(baseBox.GetComponent<Collider>());
            baseBox.GetComponent<Renderer>().material = woodMat;

            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lid.transform.SetParent(chestRoot.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0.024f, 0f);
            lid.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            lid.transform.localScale = new Vector3(0.038f, 0.028f, 0.038f);
            Destroy(lid.GetComponent<Collider>());
            lid.GetComponent<Renderer>().material = woodMat;

            GameObject band1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band1.transform.SetParent(chestRoot.transform, false);
            band1.transform.localPosition = new Vector3(-0.018f, 0.016f, 0f);
            band1.transform.localScale = new Vector3(0.006f, 0.028f, 0.040f);
            Destroy(band1.GetComponent<Collider>());
            band1.GetComponent<Renderer>().material = goldTrimMat;

            GameObject band2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band2.transform.SetParent(chestRoot.transform, false);
            band2.transform.localPosition = new Vector3(0.018f, 0.016f, 0f);
            band2.transform.localScale = new Vector3(0.006f, 0.028f, 0.040f);
            Destroy(band2.GetComponent<Collider>());
            band2.GetComponent<Renderer>().material = goldTrimMat;

            GameObject lockLatch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lockLatch.transform.SetParent(chestRoot.transform, false);
            lockLatch.transform.localPosition = new Vector3(0f, 0.018f, -0.020f);
            lockLatch.transform.localScale = new Vector3(0.008f, 0.010f, 0.004f);
            Destroy(lockLatch.GetComponent<Collider>());
            lockLatch.GetComponent<Renderer>().material = metalLockMat;
        }

        jackpotGoldPile = chestRoot.transform;

        SpawnVIPHelicopter(courtyardRoot.transform);

        if (parkCenterpiecePrefab != null && parkCenterpiecePrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject cp = Instantiate(parkCenterpiecePrefab, courtyardRoot.transform);
            StripRoguePhysicsFromImportedPrefab(cp);
            cp.transform.localPosition = new Vector3(0f, 0.03f, 0.14f);
            NormalizePrefabSize(cp, 0.085f);
        }

        GameObject auraRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        auraRing.name = "MarketAuraRing";
        auraRing.transform.SetParent(courtyardRoot.transform, false);
        auraRing.transform.localPosition = new Vector3(0f, 0.040f, 0f);
        auraRing.transform.localScale = new Vector3(0.55f, 0.002f, 0.55f);
        Destroy(auraRing.GetComponent<Collider>());
        auraRing.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(1f, 0.82f, 0.15f), 0.95f, 0.9f);
        marketAuraRing = auraRing;
        marketAuraRing.SetActive(currentMarketCycle == MarketCycle.BullBoom);

        Vector3[] treeSpots = new Vector3[]
        {
            new Vector3(-0.22f, 0.028f, -0.22f),
            new Vector3( 0.22f, 0.028f, -0.22f),
            new Vector3( 0.22f, 0.028f,  0.22f),
            new Vector3(-0.22f, 0.028f,  0.00f),
            new Vector3( 0.22f, 0.028f,  0.00f),
            new Vector3( 0.00f, 0.028f, -0.22f),
            new Vector3( 0.00f, 0.028f,  0.22f)
        };

        for (int i = 0; i < treeSpots.Length; i++)
        {
            SpawnParkTree(courtyardRoot.transform, treeSpots[i], i);
        }

        Vector3[] lampSpots = new Vector3[]
        {
            new Vector3(-0.27f, 0.028f, -0.27f),
            new Vector3( 0.27f, 0.028f, -0.27f),
            new Vector3(-0.27f, 0.028f,  0.27f),
            new Vector3( 0.27f, 0.028f,  0.27f)
        };

        for (int i = 0; i < lampSpots.Length; i++)
        {
            SpawnStreetlight(courtyardRoot.transform, lampSpots[i], i);
        }

        for (int i = 0; i < 4; i++)
        {
            SpawnAmbientTrafficCar(courtyardRoot.transform, i, i * 0.25f);
        }
    }

    void SpawnVIPHelicopter(Transform parent)
    {
        GameObject chopperHolder = new GameObject("VIP_Executive_Chopper");
        chopperHolder.transform.SetParent(parent, false);
        chopperHolder.transform.localPosition = new Vector3(0f, 0.20f, 0f);
        vipChopperTransform = chopperHolder.transform;

        AudioSource chopperAudio = chopperHolder.AddComponent<AudioSource>();
        chopperAudio.clip = procChopperHumClip;
        chopperAudio.loop = true;
        chopperAudio.spatialBlend = 1.0f;
        chopperAudio.volume = 0.08f;
        chopperAudio.minDistance = 0.15f;
        chopperAudio.maxDistance = 0.85f;
        chopperAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        chopperAudio.playOnAwake = true;
        if (procChopperHumClip != null) chopperAudio.Play();

        if (vipHelicopterPrefab != null && vipHelicopterPrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject model = Instantiate(vipHelicopterPrefab, chopperHolder.transform);
            StripRoguePhysicsFromImportedPrefab(model);
            model.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(model, 0.055f);
            return;
        }

        GameObject fuselage = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fuselage.transform.SetParent(chopperHolder.transform, false);
        fuselage.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        fuselage.transform.localScale = new Vector3(0.018f, 0.026f, 0.018f);
        Destroy(fuselage.GetComponent<Collider>());
        fuselage.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.10f, 0.12f, 0.16f), 0.92f, 0.6f);

        GameObject tailBoom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tailBoom.transform.SetParent(chopperHolder.transform, false);
        tailBoom.transform.localPosition = new Vector3(0f, 0.002f, -0.025f);
        tailBoom.transform.localScale = new Vector3(0.004f, 0.005f, 0.028f);
        Destroy(tailBoom.GetComponent<Collider>());
        tailBoom.GetComponent<Renderer>().material = brushedGoldMaterial;

        GameObject rotor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rotor.name = "Main_Rotor";
        rotor.transform.SetParent(chopperHolder.transform, false);
        rotor.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        rotor.transform.localScale = new Vector3(0.055f, 0.0015f, 0.005f);
        Destroy(rotor.GetComponent<Collider>());
        rotor.GetComponent<Renderer>().material = brushedGoldMaterial;
        chopperRotorTransform = rotor.transform;
    }

    void SpawnParkTree(Transform parent, Vector3 localPos, int index)
    {
        GameObject holder = new GameObject($"Park_Tree_{index}");
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPos;

        if (parkTreePrefab != null && parkTreePrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject tree = Instantiate(parkTreePrefab, holder.transform);
            StripRoguePhysicsFromImportedPrefab(tree);
            tree.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(tree, 0.065f);
            return;
        }

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(holder.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        trunk.transform.localScale = new Vector3(0.008f, 0.015f, 0.008f);
        Destroy(trunk.GetComponent<Collider>());
        trunk.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.42f, 0.26f, 0.14f), 0.4f);

        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.transform.SetParent(holder.transform, false);
        foliage.transform.localPosition = new Vector3(0f, 0.042f, 0f);
        foliage.transform.localScale = new Vector3(0.032f, 0.048f, 0.032f);
        Destroy(foliage.GetComponent<Collider>());
        foliage.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.18f, 0.68f, 0.32f), 0.5f);
    }

    void SpawnStreetlight(Transform parent, Vector3 localPos, int index)
    {
        GameObject holder = new GameObject($"Streetlight_{index}");
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPos;

        if (streetlightPrefab != null && streetlightPrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject lamp = Instantiate(streetlightPrefab, holder.transform);
            StripRoguePhysicsFromImportedPrefab(lamp);
            lamp.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(lamp, 0.06f);
        }
        else
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(holder.transform, false);
            pole.transform.localPosition = new Vector3(0f, 0.028f, 0f);
            pole.transform.localScale = new Vector3(0.004f, 0.028f, 0.004f);
            Destroy(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().material = brushedGoldMaterial;

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(holder.transform, false);
            bulb.transform.localPosition = new Vector3(0f, 0.058f, 0f);
            bulb.transform.localScale = Vector3.one * 0.012f;
            Destroy(bulb.GetComponent<Collider>());
            bulb.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(1f, 0.92f, 0.65f), 0.95f);
        }

        GameObject lightGO = new GameObject("Warm_City_Light");
        lightGO.transform.SetParent(holder.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.065f, 0f);
        Light ptLight = lightGO.AddComponent<Light>();
        ptLight.type = LightType.Point;
        ptLight.color = new Color(1f, 0.85f, 0.55f);
        ptLight.range = 0.45f;
        ptLight.intensity = 0.03f;
        cityNightLights.Add(ptLight);
    }

    void SpawnAmbientTrafficCar(Transform parent, int index, float startProgress)
    {
        GameObject holder = new GameObject($"Ambient_City_Car_{index}");
        holder.transform.SetParent(parent, false);

        GameObject prefabToUse = (index % 2 == 0) ? ambientTrafficCar1Prefab : ambientTrafficCar2Prefab;
        if (prefabToUse == null) prefabToUse = ambientTrafficCar1Prefab != null ? ambientTrafficCar1Prefab : playerTokenPrefab;

        if (prefabToUse != null && prefabToUse.GetComponent<OVRCameraRig>() == null)
        {
            GameObject model = Instantiate(prefabToUse, holder.transform);
            StripRoguePhysicsFromImportedPrefab(model);
            model.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(model, 0.032f);
        }
        else
        {
            GameObject carBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carBody.transform.SetParent(holder.transform, false);
            carBody.transform.localScale = new Vector3(0.014f, 0.008f, 0.026f);
            Destroy(carBody.GetComponent<Collider>());
            carBody.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.9f, 0.75f, 0.2f), 0.9f);
        }

        ambientCars.Add(holder.transform);
        ambientCarProgress.Add(startProgress);
    }

    void UpdateAmbientCourtyardTrafficAndChopper()
    {
        if (vipChopperTransform != null)
        {
            float angle = Time.time * 0.65f;
            float radius = 0.20f;
            Vector3 nextPos = new Vector3(Mathf.Cos(angle) * radius, 0.20f + Mathf.Sin(Time.time * 2f) * 0.012f, Mathf.Sin(angle) * radius);
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)).normalized;
            vipChopperTransform.localPosition = nextPos;
            vipChopperTransform.localRotation = Quaternion.LookRotation(tangent);
        }
        if (chopperRotorTransform != null)
        {
            chopperRotorTransform.Rotate(Vector3.up, 1080f * Time.deltaTime, Space.Self);
        }

        float roadHalf = 0.33f;
        for (int i = 0; i < ambientCars.Count; i++)
        {
            if (ambientCars[i] == null) continue;
            ambientCarProgress[i] = (ambientCarProgress[i] + Time.deltaTime * 0.055f) % 1f;
            float p = ambientCarProgress[i] * 4f;
            int side = Mathf.FloorToInt(p);
            float t = p - side;

            Vector3 pos;
            Quaternion rot;
            switch (side)
            {
                case 0:
                    pos = new Vector3(Mathf.Lerp(roadHalf, -roadHalf, t), 0.024f, -roadHalf);
                    rot = Quaternion.Euler(0f, -90f, 0f);
                    break;
                case 1:
                    pos = new Vector3(-roadHalf, 0.024f, Mathf.Lerp(-roadHalf, roadHalf, t));
                    rot = Quaternion.Euler(0f, 0f, 0f);
                    break;
                case 2:
                    pos = new Vector3(Mathf.Lerp(-roadHalf, roadHalf, t), 0.024f, roadHalf);
                    rot = Quaternion.Euler(0f, 90f, 0f);
                    break;
                default:
                    pos = new Vector3(roadHalf, 0.024f, Mathf.Lerp(roadHalf, -roadHalf, t));
                    rot = Quaternion.Euler(0f, 180f, 0f);
                    break;
            }

            ambientCars[i].localPosition = pos;
            ambientCars[i].localRotation = rot;
        }
    }

    public int CalculateAuthenticRent(BoardTile tile, int diceRoll)
    {
        if (tile == null) return 0;
        if (tile.isMortgaged) return 0;

        if (tile.type == TileType.Railroad)
        {
            int rrCount = 0;
            int[] rrIndices = { 5, 15, 25, 35 };
            foreach (int idx in rrIndices) if (board != null && board[idx] != null && board[idx].owner == tile.owner) rrCount++;
            int baseRent = 25 * (int)Mathf.Pow(2, Mathf.Max(0, rrCount - 1));
            if (currentMarketCycle == MarketCycle.TechIPOSurge)
            {
                baseRent = (int)(baseRent * 2.5f);
            }
            return baseRent;
        }

        if (tile.type == TileType.Utility)
        {
            bool ownsBoth = board != null && board[12] != null && board[28] != null &&
                            (board[12].owner == tile.owner && board[28].owner == tile.owner);
            int baseRent = diceRoll * (ownsBoth ? 10 : 4);
            if (currentMarketCycle == MarketCycle.TechIPOSurge)
            {
                baseRent = (int)(baseRent * 2.5f);
            }
            return baseRent;
        }

        if (tile.rentByLevel == null || tile.rentByLevel.Length == 0) return 0;
        int propRent = tile.rentByLevel[Mathf.Clamp(tile.buildingLevel, 0, 5)];

        // Monopoly rule: 2x rent applies ONLY to unimproved properties (Level 0), not to houses/skyscrapers
        if (tile.buildingLevel == 0 && OwnsFullColorGroup(tile.owner, tile.colorGroup))
        {
            propRent = propRent * 2;
        }

        if (currentMarketCycle == MarketCycle.BullBoom)
        {
            propRent = (int)(propRent * 1.5f);
        }
        else if (currentMarketCycle == MarketCycle.BearRecession)
        {
            propRent = (int)(propRent * 0.7f);
        }

        return propRent;
    }

    public enum BuildValidationResult
    {
        Success,
        NotAProperty,
        NotOwner,
        IncompleteGroup,
        GroupHasMortgage,
        MaxLevelReached,
        UnevenDevelopment,
        InsufficientFunds
    }

    public BuildValidationResult CanBuild(int playerIdx, int tileIndex)
    {
        if (board == null || tileIndex < 0 || tileIndex >= 40 || board[tileIndex] == null)
            return BuildValidationResult.NotAProperty;

        BoardTile tile = board[tileIndex];
        if (tile.type != TileType.Property)
            return BuildValidationResult.NotAProperty;

        if (tile.owner != playerIdx)
            return BuildValidationResult.NotOwner;

        if (!OwnsFullColorGroup(playerIdx, tile.colorGroup))
            return BuildValidationResult.IncompleteGroup;

        // Check if any property in this color group is mortgaged
        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null && board[i].colorGroup == tile.colorGroup && board[i].isMortgaged)
                return BuildValidationResult.GroupHasMortgage;
        }

        if (tile.buildingLevel >= 5)
            return BuildValidationResult.MaxLevelReached;

        // Enforce even building: target property cannot have higher level than any other property in the group
        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null && board[i].colorGroup == tile.colorGroup && i != tileIndex)
            {
                if (tile.buildingLevel > board[i].buildingLevel)
                    return BuildValidationResult.UnevenDevelopment;
            }
        }

        int actualBuildCost = (currentMarketCycle == MarketCycle.BearRecession) ? (int)(tile.buildCost * 0.7f) : tile.buildCost;
        if (playerCash != null && playerIdx >= 0 && playerIdx < playerCash.Length && playerCash[playerIdx] < actualBuildCost)
            return BuildValidationResult.InsufficientFunds;

        return BuildValidationResult.Success;
    }

    public bool OwnsFullColorGroup(int playerIdx, int colorGroup)
    {
        if (colorGroup < 0 || board == null) return false;
        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null && board[i].colorGroup == colorGroup && board[i].owner != playerIdx)
                return false;
        }
        return true;
    }

    bool PlayerOwnsAnyInGroup(int playerIdx, int colorGroup)
    {
        if (colorGroup < 0) return false;
        for (int i = 0; i < 40; i++)
        {
            if (board[i].colorGroup == colorGroup && board[i].owner == playerIdx) return true;
        }
        return false;
    }

    GameObject GetPrefabForChronologicalLevel(int level)
    {
        switch (level)
        {
            case 1: return level1HousePrefab;
            case 2: return level2HousePrefab;
            case 3: return level3HousePrefab;
            case 4: return level4HousePrefab;
            case 5: return level5SkyscraperPrefab;
            default: return level1HousePrefab;
        }
    }

    void ConstructChronological3DBuilding(BoardTile tile, int chosenStyle = 0)
    {
        if (tile.spawnedBuilding != null) Destroy(tile.spawnedBuilding);

        GameObject prefabToUse = GetPrefabForChronologicalLevel(tile.buildingLevel);

        GameObject holder = new GameObject($"Building_Tile{tile.index}_Lvl{tile.buildingLevel}");
        holder.transform.SetParent(transform, false);
        holder.transform.localPosition = tile.tileTransform.localPosition + new Vector3(0f, 0.006f, 0f);
        holder.transform.localRotation = tile.tileTransform.localRotation;

        GameObject model;
        if (prefabToUse != null && prefabToUse.GetComponent<OVRCameraRig>() == null)
        {
            model = Instantiate(prefabToUse, holder.transform);
            StripRoguePhysicsFromImportedPrefab(model);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.transform.SetParent(holder.transform, false);
            Destroy(model.GetComponent<Collider>());
            Color baseColor = (chosenStyle == 1) ? new Color(0.95f, 0.82f, 0.5f) : GetOfficialColor(tile);
            model.GetComponent<Renderer>().material = CreateColoredMaterial(baseColor, 0.85f);
        }

        // Phase 21: Architectural lighting accent
        GameObject accentLight = new GameObject("Architectural_AccentLight");
        accentLight.transform.SetParent(holder.transform, false);
        accentLight.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        Light l = accentLight.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 0.18f;
        l.intensity = 1.2f;
        l.color = (chosenStyle == 1) ? new Color(1f, 0.85f, 0.3f) : new Color(0.2f, 0.85f, 1f);

        tile.spawnedBuilding = holder;
        StartCoroutine(FitAndRiseBuildingChronological(holder.transform, model, tile.buildingLevel));
    }

    IEnumerator FitAndRiseBuildingChronological(Transform holder, GameObject model, int level)
    {
        if (holder == null || model == null) yield break;
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        float progressiveHeight = 0.55f + (level * 0.22f);

        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float maxFootprint = Mathf.Max(b.size.x, b.size.z);
            if (maxFootprint > 0.0001f)
            {
                float targetFootprint = 0.058f * transform.lossyScale.x;
                float s = targetFootprint / maxFootprint;
                model.transform.localScale = new Vector3(
                    model.transform.localScale.x * s,
                    model.transform.localScale.y * s * progressiveHeight,
                    model.transform.localScale.z * s
                );

                b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                float bottomOffsetWorld = holder.position.y - b.min.y;
                model.transform.position += new Vector3(0f, bottomOffsetWorld, 0f);
            }
        }

        Vector3 targetScale = holder.localScale;
        holder.localScale = new Vector3(targetScale.x, 0.01f, targetScale.z);

        float duration = 0.45f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (holder == null || model == null) yield break;
            elapsed += Time.deltaTime;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            holder.localScale = new Vector3(targetScale.x, Mathf.Lerp(0.01f, targetScale.y, ease), targetScale.z);
            yield return null;
        }
        if (holder != null) holder.localScale = targetScale;
    }

    void ExecuteMortgageOrUnmortgage(BoardTile tile)
    {
        if (tile == null || tile.owner < 0 || tile.owner >= activePlayerCount) return;
        int owner = tile.owner;

        if (!tile.isMortgaged)
        {
            if (tile.buildingLevel > 0)
            {
                int refund = tile.buildCost / 2;
                playerCash[owner] += refund;
                tile.buildingLevel--;
                if (tile.spawnedBuilding != null) Destroy(tile.spawnedBuilding);
                if (tile.buildingLevel > 0) ConstructChronological3DBuilding(tile);
                PlaySFX(cashRegisterSound, procBuildClip, 1.1f);
                RefreshDashboard($"SOLD BUILDING ON {tile.name} (+${refund})", $"Building level reduced to {tile.buildingLevel}/5. Cash: ${playerCash[owner]}");
            }
            else
            {
                int mortgageValue = tile.price / 2;
                playerCash[owner] += mortgageValue;
                tile.isMortgaged = true;
                if (tile.ownershipBadge != null)
                {
                    Renderer r = tile.ownershipBadge.GetComponent<Renderer>();
                    if (r != null) r.material = CreateColoredMaterial(new Color(0.48f, 0.12f, 0.16f), 0.95f);
                }
                PlaySFX(cashRegisterSound, procDiceClip, 0.95f);
                RefreshDashboard($"MORTGAGED {tile.name} (+${mortgageValue})", $"Property mortgaged! Rent is now $0. Cash: ${playerCash[owner]}");
            }
        }
        else
        {
            int unmortgageCost = (int)(tile.price * 0.55f);
            if (playerCash[owner] < unmortgageCost)
            {
                RefreshDashboard($"NEED ${unmortgageCost} TO UNMORTGAGE", $"Insufficient cash to unmortgage {tile.name}.");
                return;
            }

            playerCash[owner] -= unmortgageCost;
            tile.isMortgaged = false;
            if (tile.ownershipBadge != null)
            {
                Renderer r = tile.ownershipBadge.GetComponent<Renderer>();
                if (r != null) r.material = CreateColoredMaterial(new Color(0.18f, 0.85f, 0.45f), 0.95f);
            }
            PlaySFX(cashRegisterSound, procStepClip, 1.15f);
            RefreshDashboard($"UNMORTGAGED {tile.name} (-${unmortgageCost})", $"Property active! Full rent restored. Cash: ${playerCash[owner]}");
        }

        ShowDeedCardForTile(tile);
        UpdateLiveHUDStats();
        SaveGameState();
    }

    void ApplyTileOwnershipVisuals(BoardTile tile, int playerIdx)
    {
        if (tile == null || tile.tileTransform == null) return;
        Color tileTint = GetPlayerColor(playerIdx);
        if (tile.isMortgaged)
        {
            tileTint = new Color(0.48f, 0.12f, 0.16f);
        }

        if (tile.tileRenderer != null)
        {
            tile.tileRenderer.material = CreateColoredMaterial(tileTint, 0.85f);
        }

        if (tile.ownershipBadge != null) Destroy(tile.ownershipBadge);
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = $"Owned_Beacon_P{playerIdx}";
        beacon.transform.SetParent(tile.tileTransform, false);
        beacon.transform.localPosition = new Vector3(0f, 1.8f, -0.35f);
        beacon.transform.localScale = new Vector3(0.24f, 1.4f, 0.24f);
        Destroy(beacon.GetComponent<Collider>());
        beacon.GetComponent<Renderer>().material = CreateColoredMaterial(tileTint, 0.95f);
        tile.ownershipBadge = beacon;

        if (tile.buildingLevel > 0 && tile.type == TileType.Property && !tile.isMortgaged)
        {
            ConstructChronological3DBuilding(tile);
        }
    }

    void RebuildMonopolySynergyBeams()
    {
        if (monopolyBeamsContainer == null) return;
        foreach (Transform child in monopolyBeamsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        for (int group = 0; group < 8; group++)
        {
            int groupOwner = -1;
            for (int p = 0; p < activePlayerCount; p++)
            {
                if (OwnsFullColorGroup(p, group))
                {
                    groupOwner = p;
                    break;
                }
            }

            if (groupOwner != -1)
            {
                List<Transform> groupTiles = new List<Transform>();
                for (int i = 0; i < 40; i++)
                {
                    if (board[i].colorGroup == group) groupTiles.Add(board[i].tileTransform);
                }

                Color beamColor = GetPlayerColor(groupOwner);
                Material beamMat = CreateColoredMaterial(beamColor, 0.95f);

                for (int j = 0; j < groupTiles.Count - 1; j++)
                {
                    Vector3 p1 = groupTiles[j].localPosition + new Vector3(0f, 0.028f, 0f);
                    Vector3 p2 = groupTiles[j + 1].localPosition + new Vector3(0f, 0.028f, 0f);
                    Vector3 mid = (p1 + p2) * 0.5f;
                    float dist = Vector3.Distance(p1, p2);

                    GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    beam.name = $"Monopoly_Synergy_Beam_G{group}";
                    beam.transform.SetParent(monopolyBeamsContainer.transform, false);
                    beam.transform.localPosition = mid;
                    beam.transform.localRotation = Quaternion.LookRotation(p2 - p1);
                    beam.transform.localScale = new Vector3(0.012f, 0.008f, dist);
                    Destroy(beam.GetComponent<Collider>());
                    beam.GetComponent<Renderer>().material = beamMat;
                }
            }
        }
    }

    public void NormalizePrefabSize(GameObject obj, float targetMaxSize, bool snapBottomToOrigin = true)
    {
        if (obj == null) return;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (maxDim > 0.0001f)
        {
            float scaleRatio = (targetMaxSize * transform.lossyScale.x) / maxDim;
            obj.transform.localScale *= scaleRatio;
        }

        if (snapBottomToOrigin)
        {
            b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            float bottomOffset = obj.transform.position.y - b.min.y;
            obj.transform.position += Vector3.up * bottomOffset;
        }
    }
}
