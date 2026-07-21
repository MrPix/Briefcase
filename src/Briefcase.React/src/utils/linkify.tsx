import type { ReactNode } from 'react'

const URL_REGEX = /https?:\/\/[^\s<]+/gi

/** Renders text with http(s) URLs turned into safe anchor links (React-escaped). */
export function linkify(text: string | null | undefined): ReactNode {
    if (!text) return null
    const nodes: ReactNode[] = []
    let lastIndex = 0
    let key = 0

    for (const match of text.matchAll(URL_REGEX)) {
        const index = match.index ?? 0
        if (index > lastIndex) nodes.push(text.slice(lastIndex, index))

        const raw = match[0]
        const trimmed = raw.replace(/[.,;:!?)\]}]+$/, '')
        const trailing = raw.slice(trimmed.length)

        let isValid = false
        try {
            const u = new URL(trimmed)
            isValid = u.protocol === 'http:' || u.protocol === 'https:'
        } catch {
            isValid = false
        }

        if (isValid) {
            nodes.push(
                <a key={key++} className="msg-link" href={trimmed} target="_blank" rel="noopener noreferrer">
                    {trimmed}
                </a>,
            )
        } else {
            nodes.push(trimmed)
        }
        if (trailing) nodes.push(trailing)
        lastIndex = index + raw.length
    }

    if (lastIndex < text.length) nodes.push(text.slice(lastIndex))
    return nodes
}
