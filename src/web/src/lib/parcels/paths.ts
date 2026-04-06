export function getParcelDetailPath(trackingNumber: string): string {
  return `/parcels/${trackingNumber}`;
}

export function getParcelEditPath(trackingNumber: string): string {
  return `/parcels/${trackingNumber}/edit`;
}
