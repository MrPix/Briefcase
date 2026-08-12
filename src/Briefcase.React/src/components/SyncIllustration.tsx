// Small hero illustration: content moving from a phone to a laptop.
export function SyncIllustration() {
    return (
        <svg
            className="sync-illustration"
            width="220"
            height="96"
            viewBox="0 0 220 96"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            role="img"
            aria-label="A note moving from a phone to a laptop"
        >
            {/* Phone */}
            <rect x="14" y="14" width="36" height="68" rx="7" stroke="var(--accent)" strokeWidth="2.5" />
            <line x1="25" y1="72" x2="39" y2="72" stroke="var(--accent)" strokeWidth="2.5" strokeLinecap="round" />

            {/* Laptop */}
            <rect x="146" y="20" width="60" height="40" rx="4" stroke="var(--green)" strokeWidth="2.5" />
            <path d="M132 68h88" stroke="var(--green)" strokeWidth="2.5" strokeLinecap="round" />

            {/* Dashed path with a note travelling from the phone to the laptop */}
            <path
                d="M54 38c30-20 78-20 108 4"
                stroke="var(--accent)"
                strokeWidth="2"
                strokeLinecap="round"
                strokeDasharray="1 7"
            />
            <rect x="96" y="16" width="20" height="16" rx="3" fill="var(--accent)" opacity="0.9" />
            <line x1="99" y1="21" x2="113" y2="21" stroke="white" strokeWidth="1.5" strokeLinecap="round" />
            <line x1="99" y1="26" x2="109" y2="26" stroke="white" strokeWidth="1.5" strokeLinecap="round" />
        </svg>
    )
}
