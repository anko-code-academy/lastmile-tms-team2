import type { Polygon } from "geojson";

export interface GeometryHistoryState {
  past: (Polygon | null)[];
  present: Polygon | null;
  future: (Polygon | null)[];
}

function serializeGeometry(geometry: Polygon | null): string {
  return JSON.stringify(geometry);
}

export function clonePolygonGeometry(geometry: Polygon | null): Polygon | null {
  return geometry ? (JSON.parse(JSON.stringify(geometry)) as Polygon) : null;
}

export function createGeometryHistory(initialGeometry: Polygon | null = null): GeometryHistoryState {
  return {
    past: [],
    present: clonePolygonGeometry(initialGeometry),
    future: [],
  };
}

export function pushGeometrySnapshot(
  state: GeometryHistoryState,
  geometry: Polygon | null,
): GeometryHistoryState {
  const nextGeometry = clonePolygonGeometry(geometry);
  if (serializeGeometry(state.present) === serializeGeometry(nextGeometry)) {
    return state;
  }

  return {
    past: [...state.past, clonePolygonGeometry(state.present)],
    present: nextGeometry,
    future: [],
  };
}

export function undoGeometryHistory(state: GeometryHistoryState): GeometryHistoryState {
  const previous = state.past[state.past.length - 1];
  if (previous === undefined) {
    return state;
  }

  return {
    past: state.past.slice(0, -1),
    present: clonePolygonGeometry(previous),
    future: [clonePolygonGeometry(state.present), ...state.future],
  };
}

export function redoGeometryHistory(state: GeometryHistoryState): GeometryHistoryState {
  const [next, ...future] = state.future;
  if (next === undefined) {
    return state;
  }

  return {
    past: [...state.past, clonePolygonGeometry(state.present)],
    present: clonePolygonGeometry(next),
    future,
  };
}
