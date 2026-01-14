export interface Star {
    id: number;
    name: string;
    position: { x: number; y: number; z: number };
    type: string; // e.g., "G", "O", "Neutron Star"
    mass: number;
    lifetime: number;
    age: number;
    temperature: number;
    luminosity: number;
    radius: number;
    dysonRadius: number;
    planetCount: number;
}

export interface Planet {
    id: number;
    name: string;
    type: string;
    orbitRadius: number;
    orbitPeriod: number;
    rotationPeriod: number;
}

export interface ILSStorage {
    itemId: number;
    itemName: string;
    count: number;
    inc: number;
    max: number;
    localLogic: string;
    remoteLogic: string;
}

export interface ILSStation {
    id: number;
    gid: number;
    entityId: number;
    isStellar: boolean;
    deliveryDrones: number;
    deliveryShips: number;
    idleDroneCount: number;
    workDroneCount: number;
    idleShipCount: number;
    workShipCount: number;
    warperCount: number;
    warperMaxCount: number;
    energy: number;
    energyMax: number;
    energyPerTick: number;
    storage: ILSStorage[];
}

export interface PlanetILSGroup {
    planetId: number;
    planetName: string;
    starName: string;
    ilsStations: ILSStation[];
}

export interface ProductionStat {
    itemId: number;
    itemName: string;
    productionRate: number;
    consumptionRate: number;
    theoreticalMaxProduction: number;
    theoreticalMaxConsumption: number;
}

export interface ProductionStatsResponse {
    planetId: number;
    timeLevel: number;
    stats: ProductionStat[];
}

export interface AssemblerDetail {
    id: number;
    entityId: number;
    recipeId: number;
    recipeName: string;
    replicating: boolean;
    speed: number;
    extraSpeed: number;
    productive: boolean;
    isExtraProductiveMode: boolean;
    powerRatio: number;
    itemsPerMinute: number;
}

export interface PowerGridStat {
    planetId: number;
    planetName: string;
    starName: string;
    networkCount: number;
    generationCapacityW: number;
    consumptionDemandW: number;
    actualConsumptionW: number;
    energyStored: number;
    satisfactionPercent: number;
}

export interface ResearchProgress {
    currentTech: {
        id: number;
        name: string;
        hashNeeded: number;
        hashUploaded: number;
        progressPercent: number;
    } | null;
    totalHashPerSecond: number;
    totalLabCount: number;
}

export interface TechQueueItem {
    id: number;
    name: string;
    position: number;
    hashNeeded: number;
    hashUploaded: number;
}

export interface UpgradesResponse {
    research: {
        techSpeed: number;
        universeObserveLevel: number;
    };
    mecha: {
        coreEnergyCap: number;
        coreEnergy: number;
        reactorPowerGen: number;
        walkSpeed: number;
        maxSailSpeed: number;
        maxWarpSpeed: number;
    };
    logisticsCapacity: {
        stationDroneCount: number;
        stationShipCount: number;
    };
}

export interface ShippingRoute {
    originPlanetId: number;
    originPlanetName: string;
    originStarName: string;
    destPlanetName: string;
    itemId: number;
    itemName: string;
    itemCount: number;
    stage: string;
    t: number;
}

export interface LogisticsRoutesResponse {
    routes: ShippingRoute[];
}

export interface PowerGridResponse {
    planets: PowerGridStat[];
}
