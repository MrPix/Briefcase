import { useCallback, useEffect, useState } from 'react'
import {
    ActivityIndicator,
    FlatList,
    KeyboardAvoidingView,
    Platform,
    Pressable,
    StyleSheet,
    Text,
    TextInput,
    View,
} from 'react-native'
import * as Clipboard from 'expo-clipboard'
import { MessageKind, type Message } from '@/types'
import { messagesApi } from '@/services/messages'
import { messageStream } from '@/realtime/messageStream'
import { colors, radius, sharedStyles, spacing } from '@/ui/theme'

function isUrl(text: string): boolean {
    return /^https?:\/\//i.test(text.trim())
}

export default function ClipboardScreen() {
    const [messages, setMessages] = useState<Message[] | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [draft, setDraft] = useState('')
    const [sending, setSending] = useState(false)

    const load = useCallback(async () => {
        try {
            setError(null)
            setMessages(await messagesApi.list())
        } catch (err) {
            setMessages([])
            setError(err instanceof Error ? err.message : String(err))
        }
    }, [])

    useEffect(() => {
        load()
    }, [load])

    // Real-time updates from other devices.
    useEffect(() => {
        const upsert = (incoming: Message) =>
            setMessages((prev) => {
                const list = prev ? [...prev] : []
                const idx = list.findIndex((x) => x.id === incoming.id)
                if (idx >= 0) list[idx] = incoming
                else list.push(incoming)
                return list
            })
        const remove = (id: string) =>
            setMessages((prev) => prev?.filter((m) => m.id !== id) ?? prev)

        const unsub = [
            messageStream.onCreated(upsert),
            messageStream.onUpdated(upsert),
            messageStream.onRemoved(remove),
        ]
        messageStream.start().catch(() => { })
        return () => unsub.forEach((u) => u())
    }, [])

    const send = async () => {
        const text = draft.trim()
        if (!text || sending) return
        setSending(true)
        try {
            await messagesApi.create(isUrl(text) ? MessageKind.Url : MessageKind.Text, text)
            setDraft('')
            await load()
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err))
        } finally {
            setSending(false)
        }
    }

    const copy = async (m: Message) => {
        if (m.content) await Clipboard.setStringAsync(m.content)
    }

    const remove = async (m: Message) => {
        await messagesApi.remove(m.id)
        await load()
    }

    const togglePin = async (m: Message) => {
        await messagesApi.togglePin(m.id)
        await load()
    }

    const sorted = (messages ?? [])
        .slice()
        .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())

    return (
        <KeyboardAvoidingView
            style={sharedStyles.screen}
            behavior={Platform.OS === 'ios' ? 'padding' : undefined}
            keyboardVerticalOffset={90}
        >
            {error && (
                <Text style={[sharedStyles.errorText, styles.errorBanner]}>{error}</Text>
            )}

            {messages === null ? (
                <View style={styles.center}>
                    <ActivityIndicator color={colors.accent} size="large" />
                </View>
            ) : (
                <FlatList
                    data={sorted}
                    keyExtractor={(m) => m.id}
                    contentContainerStyle={styles.list}
                    renderItem={({ item }) => (
                        <MessageRow
                            message={item}
                            onCopy={() => copy(item)}
                            onDelete={() => remove(item)}
                            onTogglePin={() => togglePin(item)}
                        />
                    )}
                    ListEmptyComponent={
                        <View style={styles.center}>
                            <Text style={sharedStyles.subtitle}>Nothing here yet. Send your first note below.</Text>
                        </View>
                    }
                />
            )}

            <View style={styles.composer}>
                <TextInput
                    style={[sharedStyles.input, styles.composerInput]}
                    placeholder="Type text or paste a link…"
                    placeholderTextColor={colors.textMuted}
                    value={draft}
                    onChangeText={setDraft}
                    multiline
                />
                <Pressable
                    onPress={send}
                    disabled={sending || draft.trim().length === 0}
                    style={[styles.sendButton, (sending || draft.trim().length === 0) && styles.dim]}
                >
                    {sending ? (
                        <ActivityIndicator color={colors.onAccent} />
                    ) : (
                        <Text style={styles.sendLabel}>Send</Text>
                    )}
                </Pressable>
            </View>
        </KeyboardAvoidingView>
    )
}

function MessageRow({
    message,
    onCopy,
    onDelete,
    onTogglePin,
}: {
    message: Message
    onCopy: () => void
    onDelete: () => void
    onTogglePin: () => void
}) {
    const encrypted = message.isEncrypted
    const body = encrypted ? '🔒 Encrypted message (unlock on web to view)' : message.content ?? ''

    return (
        <View style={[styles.card, message.isPinned && styles.cardPinned]}>
            <Text style={styles.cardKind}>
                {message.kind === MessageKind.Url ? 'Link' : message.kind === MessageKind.File ? 'File' : 'Text'}
                {message.isPinned ? ' · Pinned' : ''}
            </Text>
            <Text style={styles.cardBody} selectable>
                {body}
            </Text>
            <View style={styles.cardActions}>
                {!encrypted && (
                    <Pressable onPress={onCopy} hitSlop={8}>
                        <Text style={styles.action}>Copy</Text>
                    </Pressable>
                )}
                <Pressable onPress={onTogglePin} hitSlop={8}>
                    <Text style={styles.action}>{message.isPinned ? 'Unpin' : 'Pin'}</Text>
                </Pressable>
                <Pressable onPress={onDelete} hitSlop={8}>
                    <Text style={[styles.action, styles.actionDanger]}>Delete</Text>
                </Pressable>
            </View>
        </View>
    )
}

const styles = StyleSheet.create({
    center: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        padding: spacing.lg,
    },
    list: {
        padding: spacing.md,
        gap: spacing.sm,
        flexGrow: 1,
    },
    errorBanner: {
        padding: spacing.md,
    },
    card: {
        backgroundColor: colors.surface,
        borderRadius: radius.md,
        borderWidth: 1,
        borderColor: colors.border,
        padding: spacing.md,
        gap: spacing.xs,
    },
    cardPinned: {
        borderColor: colors.accent,
    },
    cardKind: {
        color: colors.textMuted,
        fontSize: 12,
        fontWeight: '600',
    },
    cardBody: {
        color: colors.text,
        fontSize: 16,
    },
    cardActions: {
        flexDirection: 'row',
        gap: spacing.md,
        marginTop: spacing.xs,
    },
    action: {
        color: colors.accentSoft,
        fontWeight: '600',
    },
    actionDanger: {
        color: colors.danger,
    },
    composer: {
        flexDirection: 'row',
        alignItems: 'flex-end',
        gap: spacing.sm,
        padding: spacing.md,
        borderTopWidth: 1,
        borderTopColor: colors.border,
        backgroundColor: colors.bg,
    },
    composerInput: {
        flex: 1,
        maxHeight: 120,
    },
    sendButton: {
        backgroundColor: colors.accent,
        borderRadius: radius.md,
        paddingHorizontal: spacing.lg,
        paddingVertical: spacing.sm + 6,
        alignItems: 'center',
        justifyContent: 'center',
    },
    sendLabel: {
        color: colors.onAccent,
        fontWeight: '700',
    },
    dim: {
        opacity: 0.5,
    },
})
