export interface Star {
    id: number;
    name: string;
    displayName: string;
    position: { x: number; y: number; z: number };
    type: string;
    spectr: string;
    temperature: number;
    luminosity: number;
    radius: number;
    dysonRadius: number;
    planetCount: number;
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

export type GeneratorType = 'solar' | 'wind' | 'gamma' | 'geothermal' | 'thermal' | 'fusion' | 'artificial_star';

export interface Planet {
    id: number;
    name: string;
    starId: number;
    starName: string;
    type: string;
    singularity: string;
    theme: number;
    radius: number;
    orbitRadius: number;
    rotationPeriod: number;
    obliquity: number;
    orbitalPeriod: number;
    position: { x: number; y: number; z: number };
}

export interface ShipTransport {
    shipIndex: number;
    originStationGId: number;
    originPlanet: string;
    destStationGId: number;
    destPlanet: string;
    itemCount: number;
    stage: string;
    t: number;
    uSpeed: number;
    distance: number;
    remainingTicks: number;
    remainingSeconds: number;
}

export interface ItemTransportResponse {
    itemId: number;
    itemName: string;
    ships: ShipTransport[];
}

export interface RouteAggregation {
    fromStar: Star;
    toStar: Star;
    shipCount: number;
    totalItems: number;
    forwardShips: number;  // Ships going from fromStar to toStar
    backwardShips: number; // Ships going from toStar to fromStar
}
