import { spawn, spawnSync } from "node:child_process";
import fs from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const webRoot = path.resolve(__dirname, "..");
const repoRoot = path.resolve(__dirname, "../../..");
const standaloneRoot = path.resolve(webRoot, ".next/standalone");
const backendProject = path.resolve(
  repoRoot,
  "src/backend/src/LastMile.TMS.Api/LastMile.TMS.Api.csproj"
);

const backendUrl = "http://127.0.0.1:5100";
const webUrl = "http://127.0.0.1:3100";
const supportKey = process.env.TEST_SUPPORT_KEY ?? "e2e-test-support-key";
const mapboxToken = process.env.NEXT_PUBLIC_MAPBOX_ACCESS_TOKEN ?? "test-mapbox-token";

const npmExecutable = process.platform === "win32" ? "npm.cmd" : "npm";
const commandShell = process.platform === "win32"
  ? process.env.ComSpec ?? "cmd.exe"
  : null;
const taskKillExecutable = process.platform === "win32" ? "taskkill" : null;

const children = [];
let shuttingDown = false;

function spawnProcess(command, args, options) {
  const child = spawn(command, args, {
    stdio: "inherit",
    ...options,
  });

  child.on("exit", (code, signal) => {
    if (!shuttingDown) {
      const reason = signal ? `signal ${signal}` : `code ${code ?? 0}`;
      console.error(`Process ${command} exited unexpectedly with ${reason}.`);
      shutdown(1);
    }
  });

  children.push(child);
  return child;
}

function runCommand(command, args, options) {
  const result = spawnSync(command, args, {
    stdio: "inherit",
    ...options,
  });

  if (result.status !== 0) {
    throw new Error(`Command ${command} exited with code ${result.status ?? 1}.`);
  }
}

function findStandaloneServerEntry(rootDirectory) {
  const directServerEntry = path.resolve(rootDirectory, "server.js");
  if (fs.existsSync(directServerEntry)) {
    return directServerEntry;
  }

  const directories = [rootDirectory];

  while (directories.length > 0) {
    const currentDirectory = directories.pop();
    const entries = fs.readdirSync(currentDirectory, { withFileTypes: true });

    for (const entry of entries) {
      const entryPath = path.resolve(currentDirectory, entry.name);

      if (entry.isDirectory()) {
        if (entry.name === "node_modules") {
          continue;
        }

        directories.push(entryPath);
        continue;
      }

      if (
        entry.name === "server.js" &&
        fs.existsSync(path.resolve(currentDirectory, ".next/required-server-files.json"))
      ) {
        return entryPath;
      }
    }
  }

  throw new Error(`Unable to find the standalone server entry under ${rootDirectory}.`);
}

function resolveStandaloneServerEntry() {
  const requiredServerFilesPath = path.resolve(
    webRoot,
    ".next/required-server-files.json"
  );

  if (fs.existsSync(requiredServerFilesPath)) {
    const requiredServerFiles = JSON.parse(
      fs.readFileSync(requiredServerFilesPath, "utf8")
    );
    const relativeAppDir = requiredServerFiles.relativeAppDir;

    if (typeof relativeAppDir === "string") {
      const candidateEntry = path.resolve(standaloneRoot, relativeAppDir, "server.js");
      if (fs.existsSync(candidateEntry)) {
        return candidateEntry;
      }
    }
  }

  return findStandaloneServerEntry(standaloneRoot);
}

function copyStandaloneAssets(standaloneServerRoot) {
  const staticSource = path.resolve(webRoot, ".next/static");
  const staticTarget = path.resolve(standaloneServerRoot, ".next/static");
  const publicSource = path.resolve(webRoot, "public");
  const publicTarget = path.resolve(standaloneServerRoot, "public");

  fs.mkdirSync(path.dirname(staticTarget), { recursive: true });
  fs.cpSync(staticSource, staticTarget, { recursive: true, force: true });

  if (fs.existsSync(publicSource)) {
    fs.cpSync(publicSource, publicTarget, { recursive: true, force: true });
  }
}

function createEnv(overrides) {
  const baseEnv = Object.fromEntries(
    Object.entries(process.env).filter(
      ([key, value]) => !key.startsWith("=") && value !== undefined
    )
  );

  return {
    ...baseEnv,
    ...overrides,
  };
}

async function waitForUrl(url, { method = "GET", timeoutMs = 120_000 } = {}) {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    try {
      const response = await fetch(url, { method, redirect: "manual" });
      if (response.status < 500) {
        return;
      }
    } catch {
      // Keep polling until the process is ready.
    }

    await new Promise((resolve) => setTimeout(resolve, 1_000));
  }

  throw new Error(`Timed out waiting for ${url}`);
}

async function shutdown(exitCode = 0) {
  if (shuttingDown) {
    return;
  }

  shuttingDown = true;

  await Promise.all(
    children.map(async (child) => {
      if (child.exitCode !== null || child.signalCode !== null) {
        return;
      }

      if (process.platform === "win32" && taskKillExecutable) {
        await new Promise((resolve) => {
          const killer = spawn(taskKillExecutable, [
            "/pid",
            String(child.pid),
            "/t",
            "/f",
          ]);
          killer.on("exit", () => resolve());
        });
        return;
      }

      child.kill("SIGTERM");
    })
  );

  process.exit(exitCode);
}

process.on("SIGINT", () => {
  void shutdown(0);
});

process.on("SIGTERM", () => {
  void shutdown(0);
});

async function main() {
  spawnProcess(
    "dotnet",
    [
      "run",
      "--project",
      backendProject,
      "--urls",
      backendUrl,
    ],
    {
      cwd: repoRoot,
      env: createEnv({
        ASPNETCORE_ENVIRONMENT: "Development",
        ConnectionStrings__DefaultConnection: "InMemory",
        Testing__EnableTestSupport: "true",
        Testing__DisableExternalInfrastructure: "true",
        Testing__SupportKey: supportKey,
        Email__DisableDelivery: "true",
        Frontend__BaseUrl: webUrl,
        Frontend__AllowedOrigins__0: webUrl,
        AdminCredentials__Email: "admin@lastmile.com",
        AdminCredentials__Password: "Admin@12345",
      }),
    }
  );

  runCommand(
    process.platform === "win32" ? commandShell : npmExecutable,
    process.platform === "win32"
      ? ["/d", "/s", "/c", "npm.cmd run build"]
      : ["run", "build"],
    {
      cwd: webRoot,
      env: createEnv({
        NEXT_PUBLIC_API_URL: backendUrl,
        API_URL: backendUrl,
        NEXT_PUBLIC_MAPBOX_ACCESS_TOKEN: mapboxToken,
        AUTH_SECRET: "lastmile-tms-e2e-secret",
        AUTH_TRUST_HOST: "true",
        NEXTAUTH_URL: webUrl,
      }),
    }
  );

  const standaloneServerEntry = resolveStandaloneServerEntry();
  const standaloneServerRoot = path.dirname(standaloneServerEntry);

  copyStandaloneAssets(standaloneServerRoot);

  spawnProcess(
    process.execPath,
    [standaloneServerEntry],
    {
      cwd: standaloneServerRoot,
      env: createEnv({
        NEXT_PUBLIC_API_URL: backendUrl,
        API_URL: backendUrl,
        NEXT_PUBLIC_MAPBOX_ACCESS_TOKEN: mapboxToken,
        AUTH_SECRET: "lastmile-tms-e2e-secret",
        AUTH_TRUST_HOST: "true",
        NEXTAUTH_URL: webUrl,
        HOSTNAME: "127.0.0.1",
        PORT: "3100",
      }),
    }
  );

  await waitForUrl(`${backendUrl}/swagger/index.html`);
  await waitForUrl(`${webUrl}/login`);

  console.log("E2E stack is ready.");
}

void main().catch(async (error) => {
  console.error(error);
  await shutdown(1);
});
