import { AnimalSpecies } from '../models';

export interface InaphAnimal {
  earTag: string;
  species: AnimalSpecies;
  breed?: string;
  ageMonths?: number;
  sex: string;
  ownerName?: string;
  ownerMobile?: string;
  village?: string;
  district?: string;
  lastVaccinationDate?: string;
  lastVaccineCode?: string;
  registeredOnBharatPashudhan: boolean;
  isStub: boolean;
}

export interface OutbreakAlert {
  diseaseProxy: string;
  species: AnimalSpecies;
  district?: string;
  eventCount: number;
  distinctAnimals: number;
  firstSeenAt: string;
  lastSeenAt: string;
  severity: 'Watch' | 'Warning' | 'Critical';
}
