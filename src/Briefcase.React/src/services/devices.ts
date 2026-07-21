import { HubConnectionBuilder } from '@microsoft/signalr'
import { api } from '../lib/apiClient'
import { apiUrl } from '../lib/config'
import {
    type Device,
    type LoginCodeResponse,
    type LoginCodePollResponse,
    type PairCodeResponse,
} from '../types'

export interface ApproveResult {
    deviceName: string
}

export const devicesApi = {
    list(): Promise<Device[]> {
        return api.get<Device[]>('api/devices')
    },

    remove(id: string): Promise<void> {
        return api.del<void>(`api/devices/${id}`)
    },

    async generatePairCode(): Promise<PairCodeResponse> {
        return api.post<PairCodeResponse>('api/devices/pair-code')
    },

    claim(token: string, deviceName: string, platform: number): Promise<void> {
        return api.post<void>('api/devices/claim', { token, deviceName, platform })
    },

    generateLoginCode(deviceName: string, platform: string): Promise<LoginCodeResponse> {
        return api.post<LoginCodeResponse>('api/devices/login-code', { deviceName, platform })
    },

    pollLoginCode(code: string): Promise<LoginCodePollResponse> {
        return api.get<LoginCodePollResponse>(`api/devices/login-code/${encodeURIComponent(code)}`)
    },

    async approveLoginCode(code: string): Promise<ApproveResult> {
        return api.post<ApproveResult>('api/devices/login-code/approve', { code })
    },

    /**
     * Waits over SignalR until a login code is approved/expired, then redeems and
     * returns the poll response. Mirrors WebDeviceService.WaitForLoginApprovalAsync.
     */
    async waitForLoginApproval(code: string, signal?: AbortSignal): Promise<LoginCodePollResponse> {
        const connection = new HubConnectionBuilder().withUrl(apiUrl('/hubs/messages')).build()

        let notify: () => void = () => { }
        const approvedOnce = new Promise<void>((resolve) => {
            notify = resolve
        })
        connection.on('LoginCodeApproved', () => notify())

        try {
            await connection.start()
            await connection.invoke('JoinLoginCode', code)

            // Redeem immediately in case approval happened before we connected.
            let result = await this.pollLoginCode(code)
            if (result.status !== 'pending') return result

            // Wait for the approval push (or abort), then redeem.
            await Promise.race([
                approvedOnce,
                new Promise<void>((_, reject) => {
                    signal?.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')))
                }),
            ])

            result = await this.pollLoginCode(code)
            return result
        } finally {
            await connection.stop()
        }
    },
}
