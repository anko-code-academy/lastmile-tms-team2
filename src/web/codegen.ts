import type { CodegenConfig } from "@graphql-codegen/cli";

const config: CodegenConfig = {
  schema: "schema.graphql",
  documents: "src/graphql/documents/**/*.graphql",
  generates: {
    "src/graphql/generated.ts": {
      plugins: [
        "typescript",
        "typescript-operations",
        "typed-document-node",
      ],
      config: {
        enumsAsTypes: true,
        scalars: {
          DateTime: "string",
          DateTimeOffset: "string",
          Geometry: "string",
          TimeSpan: "string",
          UUID: "string",
        },
      },
    },
  },
};

export default config;
