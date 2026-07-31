import { api, type Plant } from '../api';

export const plantStore = $state({ plants: [] as Plant[] });

export async function loadPlants() {
    plantStore.plants = await api.plants.list();
}

export async function addPlant(name: string, species: string | undefined, wateringIntervalDays: number) {
    await api.plants.create({ name, species: species ?? null, watering_interval_days: wateringIntervalDays });
    await loadPlants();
}

export async function waterPlant(plant: Plant) {
    await api.plants.water(plant.id);
    await loadPlants();
}

export async function editPlant(
    plant: Plant,
    name: string,
    species: string | undefined,
    wateringIntervalDays: number
) {
    await api.plants.update(plant.id, {
        name,
        species: species ?? null,
        watering_interval_days: wateringIntervalDays,
    });
    await loadPlants();
}

export async function removePlant(id: number) {
    await api.plants.delete(id);
    plantStore.plants = plantStore.plants.filter((p) => p.id !== id);
}
