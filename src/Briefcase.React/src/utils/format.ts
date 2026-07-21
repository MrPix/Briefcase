/** Groups messages by day, matching the Blazor GetDateLabel logic. */
export function getDateLabel(createdAt: Date, today: Date): string {
    const date = new Date(createdAt.getFullYear(), createdAt.getMonth(), createdAt.getDate())
    const t = new Date(today.getFullYear(), today.getMonth(), today.getDate())
    const dayMs = 24 * 60 * 60 * 1000
    const diffDays = Math.round((t.getTime() - date.getTime()) / dayMs)

    if (diffDays === 0) return 'Today'
    if (diffDays === 1) return 'Yesterday'
    if (diffDays >= 2 && diffDays <= 6) {
        return date.toLocaleDateString(undefined, { weekday: 'long' })
    }
    if (date.getFullYear() === t.getFullYear()) {
        return date.toLocaleDateString(undefined, { month: 'long', day: 'numeric' })
    }
    return date.toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' })
}

export function formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

/** yyyy-MM-dd HH:mm in local time. */
export function formatTimestamp(iso: string): string {
    const d = new Date(iso)
    const pad = (n: number) => String(n).padStart(2, '0')
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}
