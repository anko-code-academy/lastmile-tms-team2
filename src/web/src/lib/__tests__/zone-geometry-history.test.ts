import { describe, expect, it } from "vitest";
import {
  createGeometryHistory,
  pushGeometrySnapshot,
  redoGeometryHistory,
  undoGeometryHistory,
} from "@/lib/zones/geometry-history";
import type { Polygon } from "geojson";

const squareA: Polygon = {
  type: "Polygon",
  coordinates: [[[145, -37.8], [145.1, -37.8], [145.1, -37.7], [145, -37.7], [145, -37.8]]],
};

const squareB: Polygon = {
  type: "Polygon",
  coordinates: [[[145, -37.8], [145.15, -37.8], [145.15, -37.7], [145, -37.7], [145, -37.8]]],
};

describe("geometry history", () => {
  it("tracks snapshots and clears redo on new edits", () => {
    const initial = createGeometryHistory(squareA);
    const updated = pushGeometrySnapshot(initial, squareB);
    const undone = undoGeometryHistory(updated);
    const rewritten = pushGeometrySnapshot(undone, null);

    expect(updated.past).toHaveLength(1);
    expect(undone.present).toEqual(squareA);
    expect(rewritten.present).toBeNull();
    expect(rewritten.future).toHaveLength(0);
  });

  it("ignores duplicate snapshots and supports redo", () => {
    const initial = createGeometryHistory(squareA);
    const unchanged = pushGeometrySnapshot(initial, squareA);
    const updated = pushGeometrySnapshot(initial, squareB);
    const redone = redoGeometryHistory(undoGeometryHistory(updated));

    expect(unchanged).toBe(initial);
    expect(redone.present).toEqual(squareB);
  });
});
