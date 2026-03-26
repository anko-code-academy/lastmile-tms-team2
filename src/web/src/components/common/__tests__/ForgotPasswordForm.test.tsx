import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { ForgotPasswordForm } from "@/components/common/ForgotPasswordForm";

vi.mock("sonner", () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

vi.mock("@/services/user-management.service", () => ({
  requestPasswordReset: vi.fn(),
}));

import { requestPasswordReset } from "@/services/user-management.service";

const mockedRequestPasswordReset = vi.mocked(requestPasswordReset);

describe("ForgotPasswordForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedRequestPasswordReset.mockResolvedValue({
      success: true,
      message: "If the email exists, a reset link has been sent.",
    });
  });

  it("submits the email and shows the generic success state", async () => {
    render(<ForgotPasswordForm />);

    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/^email$/i), "user@example.com");
    await user.click(screen.getByRole("button", { name: /send reset link/i }));

    await waitFor(() => {
      expect(mockedRequestPasswordReset).toHaveBeenCalledWith({
        email: "user@example.com",
      });
    });

    expect(
      await screen.findByText(/if an account exists for/i)
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /back to login/i })).toBeInTheDocument();
  });
});
