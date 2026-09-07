import { useCallback, useEffect, useState } from 'react'
import {
    ActivityIndicator,
    Alert,
    FlatList,
    Image,
    Pressable,
    StyleSheet,
    Text,
    View,
} from 'react-native'
import * as ImagePicker from 'expo-image-picker'
import * as DocumentPicker from 'expo-document-picker'
import { MessageKind, type Message } from '@/types'
import { messagesApi, type PickedFile } from '@/services/messages'
import { messageStream } from '@/realtime/messageStream'
import { previewImageUrl, downloadAndShare } from '@/lib/media'
import { colors, radius, sharedStyles, spacing } from '@/ui/theme'

export default function FilesScreen() {
    const [messages, setMessages] = useState<Message[] | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [uploading, setUploading] = useState(false)

    const load = useCallback(async () => {
        try {
            setError(null)
            setMessages(await messagesApi.list({ kind: MessageKind.File }))
        } catch (err) {
            setMessages([])
            setError(err instanceof Error ? err.message : String(err))
        }
    }, [])

    useEffect(() => {
        load()
    }, [load])

    useEffect(() => {
        const onChange = (incoming: Message) => {
            if (incoming.kind !== MessageKind.File) return
            setMessages((prev) => {
                const list = prev ? [...prev] : []
                const idx = list.findIndex((x) => x.id === incoming.id)
                if (idx >= 0) list[idx] = incoming
                else list.push(incoming)
                return list
            })
        }
        const onRemove = (id: string) =>
            setMessages((prev) => prev?.filter((m) => m.id !== id) ?? prev)

        const unsub = [
            messageStream.onCreated(onChange),
            messageStream.onUpdated(onChange),
            messageStream.onRemoved(onRemove),
        ]
        messageStream.start().catch(() => { })
        return () => unsub.forEach((u) => u())
    }, [])

    const upload = async (file: PickedFile) => {
        setUploading(true)
        setError(null)
        try {
            await messagesApi.uploadFile(file)
            await load()
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err))
        } finally {
            setUploading(false)
        }
    }

    const pickFromGallery = async () => {
        const perm = await ImagePicker.requestMediaLibraryPermissionsAsync()
        if (!perm.granted) return
        const result = await ImagePicker.launchImageLibraryAsync({ quality: 0.9 })
        if (result.canceled) return
        const asset = result.assets[0]
        await upload({
            uri: asset.uri,
            name: asset.fileName ?? `image-${Date.now()}.jpg`,
            mimeType: asset.mimeType ?? 'image/jpeg',
        })
    }

    const takePhoto = async () => {
        const perm = await ImagePicker.requestCameraPermissionsAsync()
        if (!perm.granted) return
        const result = await ImagePicker.launchCameraAsync({ quality: 0.9 })
        if (result.canceled) return
        const asset = result.assets[0]
        await upload({
            uri: asset.uri,
            name: asset.fileName ?? `photo-${Date.now()}.jpg`,
            mimeType: asset.mimeType ?? 'image/jpeg',
        })
    }

    const pickDocument = async () => {
        const result = await DocumentPicker.getDocumentAsync({ copyToCacheDirectory: true })
        if (result.canceled) return
        const asset = result.assets[0]
        await upload({
            uri: asset.uri,
            name: asset.name,
            mimeType: asset.mimeType ?? 'application/octet-stream',
        })
    }

    const chooseSource = () => {
        Alert.alert('Add a file', 'Choose a source', [
            { text: 'Take photo', onPress: takePhoto },
            { text: 'Photo library', onPress: pickFromGallery },
            { text: 'Document', onPress: pickDocument },
            { text: 'Cancel', style: 'cancel' },
        ])
    }

    const download = async (m: Message) => {
        if (!m.fileId) return
        try {
            await downloadAndShare(m.fileId, m.fileName ?? m.content ?? 'download')
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err))
        }
    }

    const remove = async (m: Message) => {
        await messagesApi.remove(m.id)
        await load()
    }

    const sorted = (messages ?? [])
        .slice()
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())

    return (
        <View style={sharedStyles.screen}>
            {error && <Text style={[sharedStyles.errorText, styles.errorBanner]}>{error}</Text>}

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
                        <FileRow message={item} onDownload={() => download(item)} onDelete={() => remove(item)} />
                    )}
                    ListEmptyComponent={
                        <View style={styles.center}>
                            <Text style={sharedStyles.subtitle}>No files yet. Tap “Add file” to upload one.</Text>
                        </View>
                    }
                />
            )}

            <Pressable
                onPress={chooseSource}
                disabled={uploading}
                style={[styles.fab, uploading && styles.dim]}
            >
                {uploading ? (
                    <ActivityIndicator color={colors.onAccent} />
                ) : (
                    <Text style={styles.fabLabel}>+ Add file</Text>
                )}
            </Pressable>
        </View>
    )
}

function FileRow({
    message,
    onDownload,
    onDelete,
}: {
    message: Message
    onDownload: () => void
    onDelete: () => void
}) {
    const preview = previewImageUrl(message.filePreviewUrl)
    return (
        <View style={styles.card}>
            {preview ? (
                <Image source={{ uri: preview }} style={styles.preview} resizeMode="cover" />
            ) : (
                <View style={[styles.preview, styles.previewPlaceholder]}>
                    <Text style={styles.previewPlaceholderText}>FILE</Text>
                </View>
            )}
            <Text style={styles.name} numberOfLines={1}>
                {message.fileName ?? message.content ?? 'Attachment'}
            </Text>
            <View style={styles.actions}>
                <Pressable onPress={onDownload} hitSlop={8}>
                    <Text style={styles.action}>Download</Text>
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
    errorBanner: {
        padding: spacing.md,
    },
    list: {
        padding: spacing.md,
        gap: spacing.sm,
        flexGrow: 1,
        paddingBottom: 96,
    },
    card: {
        backgroundColor: colors.surface,
        borderRadius: radius.md,
        borderWidth: 1,
        borderColor: colors.border,
        padding: spacing.md,
        gap: spacing.sm,
    },
    preview: {
        width: '100%',
        height: 180,
        borderRadius: radius.sm,
        backgroundColor: colors.surfaceAlt,
    },
    previewPlaceholder: {
        alignItems: 'center',
        justifyContent: 'center',
    },
    previewPlaceholderText: {
        color: colors.textMuted,
        fontWeight: '700',
        letterSpacing: 2,
    },
    name: {
        color: colors.text,
        fontSize: 15,
        fontWeight: '600',
    },
    actions: {
        flexDirection: 'row',
        gap: spacing.md,
    },
    action: {
        color: colors.accentSoft,
        fontWeight: '600',
    },
    actionDanger: {
        color: colors.danger,
    },
    fab: {
        position: 'absolute',
        right: spacing.lg,
        bottom: spacing.lg,
        backgroundColor: colors.accent,
        borderRadius: radius.pill,
        paddingHorizontal: spacing.lg,
        paddingVertical: spacing.md,
        shadowColor: '#000',
        shadowOpacity: 0.3,
        shadowRadius: 8,
        shadowOffset: { width: 0, height: 4 },
        elevation: 6,
    },
    fabLabel: {
        color: colors.onAccent,
        fontWeight: '700',
        fontSize: 15,
    },
    dim: {
        opacity: 0.6,
    },
})
