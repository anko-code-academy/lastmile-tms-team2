import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useDebounce } from "@/hooks/use-debounce";

describe("useDebounce", () => {
  it("delays value updates until the debounce window elapses", () => {
    vi.useFakeTimers();

    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      {
        initialProps: { value: "a" },
      }
    );

    expect(result.current).toBe("a");

    rerender({ value: "alex" });
    expect(result.current).toBe("a");

    act(() => {
      vi.advanceTimersByTime(299);
    });

    expect(result.current).toBe("a");

    act(() => {
      vi.advanceTimersByTime(1);
    });

    expect(result.current).toBe("alex");

    vi.useRealTimers();
  });
});
