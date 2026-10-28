import type { Transition, Variants } from 'motion/react'

export const swiftEase = [0.22, 1, 0.36, 1] as const

export const quickTransition: Transition = {
  duration: 0.22,
  ease: swiftEase,
}

export const routeVariants: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: { opacity: 1, y: 0, transition: quickTransition },
  exit: { opacity: 0, y: -4, transition: { duration: 0.14, ease: 'easeIn' } },
}

export const panelVariants: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: { opacity: 1, y: 0, transition: quickTransition },
  exit: { opacity: 0, y: -5, transition: { duration: 0.14, ease: 'easeIn' } },
}

export const staggerContainer: Variants = {
  hidden: {},
  visible: {
    transition: {
      delayChildren: 0.04,
      staggerChildren: 0.055,
    },
  },
}

export const fadeUpItem: Variants = {
  hidden: { opacity: 0, y: 10 },
  visible: { opacity: 1, y: 0, transition: quickTransition },
}
