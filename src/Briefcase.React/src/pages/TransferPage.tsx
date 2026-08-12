import { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { transferApi } from '../services/transfer'
import { messagesApi } from '../services/messages'
import type { ShareLinkResult } from '../types'
import { SendIcon, LinkIcon, DevicesIcon, CheckIcon, CopyIcon } from '../components/icons'

type ShareMethod = 'device' | 'link'

export function TransferPage() {
    const { t } = useTranslation()
    const [searchParams] = useSearchParams()
    const navigate = useNavigate()
    const messageId = searchParams.get('messageId')
    const isSendMode = !!messageId

    // ── Receive mode ──
    const [code, setCode] = useState<string | null>(null)
    const [received, setReceived] = useState(false)

    // ── Send mode ──
    const [shareMethod, setShareMethod] = useState<ShareMethod>('device')
    const [enteredCode, setEnteredCode] = useState('')
    const [sending, setSending] = useState(false)
    const [sent, setSent] = useState(false)
    const [error, setError] = useState<string | null>(null)

    // Share-by-link
    const [expiryChoice, setExpiryChoice] = useState('1440')
    const [oneTime, setOneTime] = useState(false)
    const [generating, setGenerating] = useState(false)
    const [generatedUrl, setGeneratedUrl] = useState<string | null>(null)
    const [linkResult, setLinkResult] = useState<ShareLinkResult | null>(null)
    const [copied, setCopied] = useState(false)

    const abortRef = useRef<AbortController | null>(null)

    useEffect(() => {
        if (isSendMode) return
        let active = true
        const controller = new AbortController()
        abortRef.current = controller
            ; (async () => {
                try {
                    const sessionCode = await transferApi.createSession()
                    if (!active) return
                    setCode(sessionCode)
                    await transferApi.listenForTransfer(
                        sessionCode,
                        (url) => {
                            setReceived(true)
                            navigate(url)
                        },
                        controller.signal,
                    )
                } catch {
                    /* cancelled or failed */
                }
            })()
        return () => {
            active = false
            controller.abort()
        }
    }, [isSendMode, navigate])

    const handleSend = async () => {
        setError(null)
        setSending(true)
        try {
            await transferApi.sendTo(enteredCode.trim().toUpperCase(), messageId!)
            setSent(true)
        } catch (err) {
            const status = (err as { status?: number })?.status
            setError(status === 404 ? t('transfer.codeNotFound') : t('transfer.genericError'))
        } finally {
            setSending(false)
        }
    }

    const handleGenerateLink = async () => {
        setError(null)
        setGenerating(true)
        try {
            const minutes = Number(expiryChoice)
            const expiresInMinutes = minutes > 0 ? minutes : null
            const result = await messagesApi.createShareLink(messageId!, oneTime, expiresInMinutes)
            setLinkResult(result)
            setGeneratedUrl(`${window.location.origin}${result.url}`)
        } catch {
            setError(t('transfer.linkErrorGeneric'))
        } finally {
            setGenerating(false)
        }
    }

    const copyLink = async () => {
        if (!generatedUrl) return
        try {
            await navigator.clipboard.writeText(generatedUrl)
            setCopied(true)
        } catch {
            /* ignore */
        }
    }

    const resetLink = () => {
        setGeneratedUrl(null)
        setLinkResult(null)
        setCopied(false)
        setError(null)
    }

    const linkHint = linkResult?.oneTime
        ? t('transfer.linkHintOneTime')
        : linkResult?.expiresAt
            ? t('transfer.linkHintExpires', { date: new Date(linkResult.expiresAt).toLocaleString() })
            : t('transfer.linkHintNever')

    if (isSendMode) {
        return (
            <div className="transfer-container">
                <div className="transfer-card">
                    <div className="transfer-method-tabs">
                        <button
                            className={`transfer-tab${shareMethod === 'device' ? ' active' : ''}`}
                            onClick={() => {
                                setShareMethod('device')
                                setError(null)
                            }}
                        >
                            <DevicesIcon size={16} /> {t('transfer.toDevice')}
                        </button>
                        <button
                            className={`transfer-tab${shareMethod === 'link' ? ' active' : ''}`}
                            onClick={() => {
                                setShareMethod('link')
                                setError(null)
                            }}
                        >
                            <LinkIcon size={16} /> {t('transfer.byLink')}
                        </button>
                    </div>

                    {shareMethod === 'device' ? (
                        <>
                            <div className="transfer-icon send">
                                <SendIcon size={40} />
                            </div>
                            <h2 className="transfer-title">{t('transfer.sendTitle')}</h2>
                            <p className="transfer-subtitle">{t('transfer.sendSubtitle')}</p>

                            {!sent ? (
                                <>
                                    <div className="code-input-group">
                                        <input
                                            className="code-input"
                                            type="text"
                                            maxLength={8}
                                            placeholder="ABCD1234"
                                            value={enteredCode}
                                            onChange={(e) => setEnteredCode(e.target.value)}
                                            autoComplete="off"
                                            spellCheck={false}
                                        />
                                    </div>
                                    {error && <div className="transfer-error">{error}</div>}
                                    <button className="btn btn-primary transfer-btn" onClick={handleSend} disabled={sending || enteredCode.trim().length !== 8}>
                                        {sending ? <span className="spinner" /> : <SendIcon size={16} />}
                                        <span>{sending ? t('transfer.sending') : t('transfer.send')}</span>
                                    </button>
                                </>
                            ) : (
                                <>
                                    <div className="transfer-success">
                                        <CheckIcon size={32} style={{ stroke: '#22c55e' }} />
                                        <p>{t('transfer.sentSuccess')}</p>
                                    </div>
                                    <button className="btn btn-outline transfer-btn" onClick={() => navigate('/clipboard')}>
                                        {t('transfer.backToClipboard')}
                                    </button>
                                </>
                            )}
                        </>
                    ) : (
                        <>
                            <div className="transfer-icon send">
                                <LinkIcon size={40} />
                            </div>
                            <h2 className="transfer-title">{t('transfer.shareByLinkTitle')}</h2>
                            <p className="transfer-subtitle">{t('transfer.shareByLinkSubtitle')}</p>

                            {!generatedUrl ? (
                                <>
                                    <div className="link-options">
                                        <label className="link-option-label" htmlFor="expiry-select">
                                            {t('transfer.linkExpires')}
                                        </label>
                                        <select
                                            id="expiry-select"
                                            className="link-select form-control"
                                            value={expiryChoice}
                                            onChange={(e) => setExpiryChoice(e.target.value)}
                                        >
                                            <option value="60">{t('transfer.expiry1h')}</option>
                                            <option value="1440">{t('transfer.expiry24h')}</option>
                                            <option value="10080">{t('transfer.expiry7d')}</option>
                                            <option value="0">{t('transfer.expiryNever')}</option>
                                        </select>

                                        <label className="link-toggle">
                                            <input type="checkbox" checked={oneTime} onChange={(e) => setOneTime(e.target.checked)} />
                                            <span>{t('transfer.selfDestruct')}</span>
                                        </label>
                                    </div>
                                    {error && <div className="transfer-error">{error}</div>}
                                    <button className="btn btn-primary transfer-btn" onClick={handleGenerateLink} disabled={generating}>
                                        {generating ? <span className="spinner" /> : <LinkIcon size={16} />}
                                        <span>{generating ? t('transfer.generating') : t('transfer.generateLink')}</span>
                                    </button>
                                </>
                            ) : (
                                <>
                                    <div className="link-result">
                                        <div className="link-url-box">
                                            <input className="link-url-input form-control" type="text" readOnly value={generatedUrl} />
                                            <button className="link-copy-btn" onClick={copyLink} title={t('transfer.copyLink')}>
                                                {copied ? <CheckIcon size={16} style={{ stroke: '#22c55e' }} /> : <CopyIcon size={16} />}
                                            </button>
                                        </div>
                                        <p className="link-hint">{linkHint}</p>
                                    </div>
                                    <button className="btn btn-outline transfer-btn" onClick={resetLink}>
                                        {t('transfer.createAnotherLink')}
                                    </button>
                                </>
                            )}
                        </>
                    )}
                </div>
            </div>
        )
    }

    // ── Receive mode ──
    return (
        <div className="transfer-container">
            <div className="transfer-card">
                <div className="transfer-icon receive">
                    <DevicesIcon size={40} />
                </div>
                <h2 className="transfer-title">{t('transfer.receiveTitle')}</h2>
                <p className="transfer-subtitle">{t('transfer.receiveSubtitle')}</p>

                {code === null ? (
                    <div className="transfer-loading">
                        <span className="spinner" />
                        <span>{t('transfer.generatingCode')}</span>
                    </div>
                ) : received ? (
                    <div className="transfer-success">
                        <CheckIcon size={32} style={{ stroke: '#22c55e' }} />
                        <p>{t('transfer.received')}</p>
                    </div>
                ) : (
                    <>
                        <div className="code-display-group">
                            <span className="code-half">{code.slice(0, 4)}</span>
                            <span className="code-separator">–</span>
                            <span className="code-half">{code.slice(4)}</span>
                        </div>
                        <div className="transfer-waiting">
                            <span className="pulse-dot" />
                            <span>{t('transfer.waitingForSender')}</span>
                        </div>
                    </>
                )}
                <button className="btn btn-outline transfer-btn" onClick={() => navigate('/')}>
                    {t('transfer.back')}
                </button>
            </div>
        </div>
    )
}
