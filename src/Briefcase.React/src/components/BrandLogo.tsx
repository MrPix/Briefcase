import { useId } from 'react'

type BrandLogoProps = {
    size?: number
}

export function BrandLogo({ size = 28 }: BrandLogoProps) {
    const gradientId = `brand-gradient-${useId().replace(/:/g, '')}`

    return (
        <svg width={size} height={size} viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg" aria-label="Briefcase logo">
            <defs>
                <linearGradient id={gradientId} x1="2" y1="2" x2="26" y2="26" gradientUnits="userSpaceOnUse">
                    <stop offset="0" stopColor="#22C55E" />
                    <stop offset="1" stopColor="#0EA5E9" />
                </linearGradient>
            </defs>
            <rect width="28" height="28" rx="8" fill={`url(#${gradientId})`} />
            <path
                d="M9 6.5V20.9M9 6.5H13.9C16.9 6.5 18.8 8.3 18.8 10.8C18.8 13.2 16.9 15 13.9 15H9M9 15H14.5C17.7 15 19.6 16.9 19.6 19.3C19.6 21.8 17.7 23.5 14.5 23.5H9"
                stroke="white"
                strokeWidth="2.4"
                strokeLinecap="round"
                strokeLinejoin="round"
            />
            <rect x="15.6" y="12.3" width="3.1" height="3.1" rx="0.95" fill="#F59E0B" transform="rotate(12 17.15 13.85)" />
        </svg>
    )
}
