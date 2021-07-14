// This compilated file is appended to the unity.loader.js. You can
// assume everything from the loader will be available here

import future from "fp-future"
import { generatedFiles } from "../package.json"

// the following function is defined by unity, accessible via unity.loader.js
// https://docs.unity3d.com/Manual/webgl-templates.html
// prettier-ignore
declare function createUnityInstance(canvas: HTMLCanvasElement, config: any, onProgress?: (progress: number) => void, onSuccess?: (unityInstance: any) => void, onError?: (message: any) => void): Promise<UnityGame>

/** Expose the original interface from the Unity Instance. */
export type UnityGame = {
  Module: {
    /** this handler can be overwritten, return true to stop error propagation */
    errorHandler?: (message: string, filename: string, lineno: number) => boolean
  }
  SendMessage(object: string, method: string, args: number | string): void
  SetFullscreen(): void
  Quit(): Promise<void>
}

export type RendererOptions = {
  canvas: HTMLCanvasElement

  onProgress?: (progress: number) => void
  onSuccess?: (unityInstance: any) => void
  onError?: (error: any) => void
  /** Legacy messaging system */
  onMessageLegacy: (type: string, payload: string) => void
  /** used to append a ?v={} to the URL. Useful to debug cache issues */
  versionQueryParam?: string
  /** baseUrl where all the assets are deployed */
  baseUrl: string

  enableBrotli?: boolean
}

export type DecentralandRendererInstance = {
  /**
   * Signal sent by unity after it started correctly
   * it is a promise, that makes it awaitable.
   * The content of the resolved promise is an empty object to
   * enable future extensions.
   */
  engineStartedFuture: Promise<{}>

  // soon there will be more protocol functions here https://github.com/decentraland/renderer-protocol
  // and originalUnity will be deprecated to decouple the kernel from unity's impl internals
  originalUnity: UnityGame
}

export async function initializeWebRenderer(options: RendererOptions): Promise<DecentralandRendererInstance> {
  const rendererVersion = options.versionQueryParam || performance.now()
  const { canvas, baseUrl, onProgress, onSuccess, onError, onMessageLegacy } = options
  const resolveWithBaseUrl = (file: string) => new URL(file + "?v=" + rendererVersion, baseUrl).toString()

  const enableBrotli =
    typeof options.enableBrotli != "undefined" ? !!options.enableBrotli : document.location.protocol == "https:"

  const postfix = enableBrotli ? ".br" : ""

  const config = {
    dataUrl: resolveWithBaseUrl(generatedFiles.dataUrl + postfix),
    frameworkUrl: resolveWithBaseUrl(generatedFiles.frameworkUrl + postfix),
    codeUrl: resolveWithBaseUrl(generatedFiles.codeUrl + postfix),
    streamingAssetsUrl: resolveWithBaseUrl("StreamingAssets"),
    companyName: "Decentraland",
    productName: "Decentraland World Client",
    productVersion: "0.1",
  }

  const engineStartedFuture = future<{}>()

  // The namespace DCL is exposed to global because the unity template uses it to send the messages
  // @see https://github.com/decentraland/unity-renderer/blob/bc2bf1ee0d685132c85606055e592bac038b3471/unity-renderer/Assets/Plugins/JSFunctions.jslib#L6-L29
  ;(globalThis as any)["DCL"] = {
    // This function get's called by the engine
    EngineStarted() {
      engineStartedFuture.resolve({})
    },

    // This function is called from the unity renderer to send messages back to the scenes
    MessageFromEngine(type: string, jsonEncodedMessage: string) {
      onMessageLegacy(type, jsonEncodedMessage)
    },
  }

  const originalUnity = await createUnityInstance(canvas, config, onProgress, onSuccess, onError)

  // TODO: replace originalUnity.errorHandler with a version that redirects errors to -> onError

  return {
    engineStartedFuture,
    originalUnity,
  }
}
