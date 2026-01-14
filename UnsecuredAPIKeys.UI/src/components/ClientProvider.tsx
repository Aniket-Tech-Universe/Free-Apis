'use client'

import { HeroUIProvider } from "@heroui/system"
import { ReactNode, useEffect, useState } from "react"

interface ClientProviderProps {
  children: ReactNode
}

export default function ClientProvider({ children }: ClientProviderProps) {
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  if (!mounted) {
    return <div>{children}</div>
  }

  return (
    <HeroUIProvider>
      {children}
    </HeroUIProvider>
  )
}
