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
