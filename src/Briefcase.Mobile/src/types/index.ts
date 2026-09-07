// Domain types mirroring the Briefcase API DTOs.
// NOTE: The API has no global JsonStringEnumConverter, so enum-typed response
// fields (MessageResponse.kind, DeviceResponse.platform) arrive as NUMBERS.

export const MessageKind = {
    Text: 0,
    Url: 1,
    File: 2,
} as const
export type MessageKind = (typeof MessageKind)[keyof typeof MessageKind]

export const NavigationProcessingStatus = {
    None: 0,
    Pending: 1,
    Processing: 2,
    Completed: 3,
    Failed: 4,
} as const
export type NavigationProcessingStatus =
    (typeof NavigationProcessingStatus)[keyof typeof NavigationProcessingStatus]

export interface NavigationTarget {
    applicationId: string
    displayName: string
    uri: string
}

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
    navigationStatus: NavigationProcessingStatus
    navigationTargets: NavigationTarget[]
    createdAt: string
    updatedAt: string
}

export interface PagedResponse<T> {
    items: T[]
    page: number
    pageSize: number
    totalCount: number
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
