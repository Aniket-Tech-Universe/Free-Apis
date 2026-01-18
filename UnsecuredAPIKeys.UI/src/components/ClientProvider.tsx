'use client'

import { HeroUIProvider } from "@heroui/system"
import { ReactNode } from "react"

interface ClientProviderProps {
  children: ReactNode
}

export default function ClientProvider({ children }: ClientProviderProps) {
  return (
    <HeroUIProvider>
      {children}
    </HeroUIProvider>
  )
}
