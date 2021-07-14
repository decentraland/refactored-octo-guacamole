import * as glob from "glob"
import { writeFileSync, readFileSync } from "fs"
import * as path from "path"
import { copyFile, ensureFileExists } from "./utils"

import { OutputOptions, rollup } from "rollup"
import typescript from "rollup-plugin-typescript2"
import resolve from "rollup-plugin-node-resolve"
import commonjs from "rollup-plugin-commonjs"
import rollupJson from "@rollup/plugin-json"
import { generatedFiles } from "../package.json"

const PROD = !!process.env.CI

console.log(`production: ${PROD}`)

const plugins = [
  typescript({
    verbosity: 2,
    clean: true,
    useTsconfigDeclarationDir: true,
  }),
  resolve({
    browser: true,
    preferBuiltins: false,
  }),
  commonjs({
    ignoreGlobal: true,
    include: [/node_modules/],
    namedExports: {},
  }),
  rollupJson({ preferConst: true }),
]

process.env.BUILD_PATH = path.resolve(
  process.env.BUILD_PATH || path.resolve(__dirname, "../../unity-renderer/Builds/unity")
)
const DIST_PATH = path.resolve(__dirname, "../dist")

async function main() {
  await copyBuiltFiles()
  await buildRollup()
  await createTypings()
  await createPackageJson()
}

async function copyBuiltFiles() {
  const basePath = path.resolve(process.env.BUILD_PATH!, "Build")

  for (let file of glob.sync("**/*", { cwd: basePath, absolute: true })) {
    copyFile(file, path.resolve(DIST_PATH, file.replace(basePath + "/", "./")))
  }
}

async function buildRollup() {
  ensureFileExists(DIST_PATH, "unity.loader.js")
  console.log("> reading unity.loader.js")
  const banner = readFileSync(path.resolve(DIST_PATH, "unity.loader.js")).toString()
  console.log("> compiling src folder")

  for (let file of Object.values(generatedFiles)) {
    ensureFileExists(DIST_PATH, file)
  }

  const bundle = await rollup({
    input: "./src/index.ts",
    context: "globalThis",
    plugins,
  })

  console.log(bundle.watchFiles) // an array of file names this bundle depends on

  const outputOptions: OutputOptions = {
    file: "./dist/index.js",
    format: "iife",
    name: "DclRenderer",
    sourcemap: true,
    banner,
  }

  // generate output specific code in-memory
  // you can call this function multiple times on the same bundle object
  const { output } = await bundle.generate(outputOptions)

  for (const chunkOrAsset of output) {
    if (chunkOrAsset.type === "asset") {
      console.log("Asset", chunkOrAsset)
    } else {
      console.log("Chunk", chunkOrAsset.modules)
    }
  }

  // or write the bundle to disk
  await bundle.write(outputOptions)

  // closes the bundle
  await bundle.close()
}

async function createTypings() {
  console.log("> writing typings.d.ts")
  writeFileSync(
    path.resolve(DIST_PATH, "typings.d.ts"),
    `
import * as Renderer from './index'
declare var DclRenderer: typeof Renderer
    `
  )
}

async function createPackageJson() {
  console.log("> writing package.json")
  writeFileSync(
    path.resolve(DIST_PATH, "package.json"),
    JSON.stringify(
      {
        name: "@dcl/unity-renderer",
        main: "index.js",
        typings: "typings.d.ts",
        version: `1.0.${process.env.CIRCLE_BUILD_NUM || "0-development"}`,
        tag: process.env.CIRCLE_TAG,
        commit: process.env.CIRCLE_SHA1,
        branch: process.env.CIRCLE_BRANCH,
        author: "Decentraland Contributors",
        license: "Apache-2.0",
        publishConfig: {
          access: "public",
        },
        repository: {
          type: "git",
          url: "https://github.com/decentraland/unity-renderer.git",
        },
      },
      null,
      2
    )
  )
}

main().catch((err) => {
  console.error(err)
  process.exit(1)
})
