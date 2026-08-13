// Domain types mirroring the Briefcase API DTOs.
// NOTE: The API has no global JsonStringEnumConverter, so enum-typed response
// fields (MessageResponse.kind, DeviceResponse.platform) arrive as NUMBERS.

export const MessageKind = {
    Text: 0,
    Url: 1,
    File: 2,
} as const
export type MessageKind = (typeof MessageKind)[keyof typeof MessageKind]

export const Platform = {
    Windows: 0,
    Android: 1,
    iOS: 2,
    macOS: 3,
    Web: 4,
} as const
export type Platform = (typeof Platform)[keyof typeof Platform]

export interface Message {
    id: string
    kind: MessageKind
    content: string | null
    fileId: string | null
    fileName: string | null
    filePreviewUrl: string | null
    isPinned: boolean
    pinnedAt: string | null
    isEncrypted: boolean
    encryptionIV: string | null
    createdAt: string
    updatedAt: string
}

export interface PagedResponse<T> {
    items: T[]
    page: number
    pageSize: number
    totalCount: number
}

export interface Device {
    id: string
    name: string
    platform: Platform
    lastSeenAt: string
    createdAt: string
    isCurrent: boolean
}

export interface AuthResponse {
    accessToken: string
    refreshToken: string
    accessTokenExpiresAt: string
}

export interface ExternalAuthProvider {
    key: string
    displayName: string
}

export interface E2eeSettings {
    isEnabled: boolean
    kdfAlgorithm: string | null
    kdfSalt: string | null
    kdfParams: string | null
    keyVerifier: string | null
}

export interface ShareLinkResult {
    slug: string
    url: string
    expiresAt: string | null
    oneTime: boolean
}

/** Resolved shared message (tokens already baked into preview/download URLs). */
export interface SharedMessage {
    messageId: string
    kind: MessageKind
    content: string | null
    fileId: string | null
    fileName: string | null
    previewUrl: string | null
    downloadUrl: string | null
    createdAt: string
}

export interface PairCodeResponse {
    token: string
    expiresAt: string
}

export interface LoginCodeResponse {
    code: string
    expiresAt: string
}

export type LoginCodeStatus = 'pending' | 'approved' | 'expired' | 'notfound'

export interface LoginCodePollResponse {
    status: LoginCodeStatus
    accessToken: string | null
    refreshToken: string | null
    accessTokenExpiresAt: string | null
}

export function platformLabel(p: Platform): string {
    switch (p) {
        case Platform.Windows:
            return 'Windows'
        case Platform.Android:
            return 'Android'
        case Platform.iOS:
            return 'iOS'
        case Platform.macOS:
            return 'macOS'
        default:
            return 'Web'
    }
}
