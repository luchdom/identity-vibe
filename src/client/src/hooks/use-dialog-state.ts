import { useState } from 'react'

export default function useDialogState(defaultOpen = false) {
  const [isOpen, setIsOpen] = useState(defaultOpen)

  const open = () => setIsOpen(true)
  const close = () => setIsOpen(false)
  const toggle = () => setIsOpen((prev) => !prev)

  return [isOpen, setIsOpen, { open, close, toggle }] as const
}