export function getParcelDetailPath(trackingNumber: string): string {
  return `/parcels/${trackingNumber}`;
}

export function getParcelEditPath(trackingNumber: string): string {
  return `/parcels/${trackingNumber}/edit`;
}

export function getParcelInboundPath(): string {
  return "/parcels/inbound";
}

export function getParcelSortPath(): string {
  return "/parcels/sort";
}

export function getParcelStagingPath(): string {
  return "/parcels/staging";
}
