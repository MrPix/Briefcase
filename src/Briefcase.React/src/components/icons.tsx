import type { SVGProps } from 'react'

type IconProps = SVGProps<SVGSVGElement> & { size?: number }

function base({ size = 20, children, ...props }: IconProps & { children: React.ReactNode }) {
    return (
        <svg
            width={size}
            height={size}
            fill="none"
            stroke="currentColor"
            strokeWidth={1.5}
            viewBox="0 0 24 24"
            strokeLinecap="round"
            strokeLinejoin="round"
            {...props}
        >
            {children}
        </svg>
    )
}

export const ClipboardIcon = (p: IconProps) =>
    base({ ...p, children: <path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z" /> })

export const StarIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118L2.05 10.1c-.783-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
        ),
    })

export const FileIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
        ),
    })

export const LinkIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101M10.172 13.828a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
        ),
    })

export const TextIcon = (p: IconProps) => base({ ...p, children: <path d="M4 6h16M4 12h10M4 18h14" /> })

export const DevicesIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
        ),
    })

export const TransferIcon = (p: IconProps) =>
    base({ ...p, children: <path d="M3 10h10a8 8 0 018 8v2M3 10l6 6M3 10l6-6" /> })

export const TrashIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
        ),
    })

export const SettingsIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <>
                <path d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                <path d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </>
        ),
    })

export const InfoIcon = (p: IconProps) =>
    base({ ...p, children: <path d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /> })

export const DownloadIcon = (p: IconProps) =>
    base({ ...p, children: <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3" /> })

export const CopyIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <>
                <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
                <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1" />
            </>
        ),
    })

export const PinIcon = (p: IconProps) =>
    base({ ...p, children: <path d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" /> })

export const EditIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
        ),
    })

export const SendIcon = (p: IconProps) =>
    base({ ...p, children: <path d="M22 2L11 13M22 2l-7 20-4-9-9-4 20-7z" /> })

export const RestoreIcon = (p: IconProps) =>
    base({ ...p, children: <path d="M3 10h10a8 8 0 018 8v2M3 10l6 6M3 10l6-6" /> })

export const CheckIcon = (p: IconProps) => base({ ...p, children: <path d="M20 6L9 17l-5-5" /> })

export const BackIcon = (p: IconProps) => base({ ...p, children: <path d="M19 12H5M12 5l-7 7 7 7" /> })

export const PlusIcon = (p: IconProps) => base({ ...p, children: <path d="M12 5v14M5 12h14" /> })

export const CloseIcon = (p: IconProps) => base({ ...p, children: <path d="M18 6L6 18M6 6l12 12" /> })

export const PaperclipIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M21.44 11.05l-9.19 9.19a6 6 0 01-8.49-8.49l9.19-9.19a4 4 0 015.66 5.66l-9.2 9.19a2 2 0 01-2.83-2.83l8.49-8.48" />
        ),
    })

export const PasteIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        ),
    })

export const LockIcon = (p: IconProps) =>
    base({ ...p, children: <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /> })

export const WarningIcon = (p: IconProps) =>
    base({
        ...p,
        children: (
            <path d="M12 9v4m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
        ),
    })
