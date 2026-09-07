import { HubConnectionBuilder, HttpTransportType } from '@microsoft/signalr'
import { api } from '../lib/apiClient'
import { apiUrl } from '../lib/config'
import { deviceInfo } from '../auth/deviceInfo'
import type { LoginCodeResponse, LoginCodePollResponse } from '../types'

export const devicesApi = {
    generateLoginCode(deviceName: string, platform: string): Promise<LoginCodeResponse> {
        return api.post<LoginCodeResponse>(
            'api/devices/login-code',
            { deviceName, platform, installationId: deviceInfo.installationId },
            { skipAuth: true },
        )
    },

    pollLoginCode(code: string): Promise<LoginCodePollResponse> {
        return api.get<LoginCodePollResponse>(`api/devices/login-code/${encodeURIComponent(code)}`, {
            skipAuth: true,
        })
    },

    /**
     * Waits over SignalR until a login code is approved/expired, then redeems and
     * returns the poll response. Mirrors the web WaitForLoginApproval flow.
     */
    async waitForLoginApproval(code: string, signal?: AbortSignal): Promise<LoginCodePollResponse> {
        const connection = new HubConnectionBuilder()
            .withUrl(apiUrl('/hubs/messages'), {
                transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
            })
            .build()

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

            await Promise.race([
                approvedOnce,
                new Promise<void>((_, reject) => {
                    signal?.addEventListener('abort', () =>
                        reject(new DOMException('Aborted', 'AbortError')),
                    )
                }),
            ])

            result = await this.pollLoginCode(code)
            return result
        } finally {
            await connection.stop()
        }
    },
}
