export interface Star {
    id: number;
    name: string;
    position: { x: number; y: number; z: number };
    type: string;
    mass: number;
    lifetime: number;
    age: number;
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
