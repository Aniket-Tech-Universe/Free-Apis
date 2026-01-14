import { motion, AnimatePresence, useSpring, useTransform } from "framer-motion";
import { useEffect, useMemo } from "react";

interface AnimatedNumberProps {
  value: string | number;
  className?: string;
  digitClassName?: string;
  stagger?: number;
}

const AnimatedDigit = ({ 
  digit, 
  delay = 0 
}: { 
  digit: string; 
  delay?: number;
}) => {
  // For non-digit characters (like commas), just display them without animation
  if (!/\d/.test(digit)) {
    return <span>{digit}</span>;
  }

  return (
    <AnimatePresence mode="wait">
      <motion.span
        key={digit}
        initial={{ 
          y: 20, 
          opacity: 0,
          scale: 0.8
        }}
        animate={{ 
          y: 0, 
          opacity: 1,
          scale: 1
        }}
        exit={{ 
          y: -20, 
          opacity: 0,
          scale: 0.8
        }}
        transition={{ 
          duration: 0.4,
          delay,
          type: "spring",
          stiffness: 300,
          damping: 25
        }}
        style={{ display: 'inline-block' }}
      >
        {digit}
      </motion.span>
    </AnimatePresence>
  );
};

const AnimatedNumber = ({ 
  value, 
  className, 
  digitClassName,
  stagger = 0.03 
}: AnimatedNumberProps) => {
  const valueStr = String(value);
  const characters = useMemo(() => valueStr.split(''), [valueStr]);
  
  return (
    <span className={className}>
      {characters.map((char, index) => (
        <AnimatedDigit 
          key={`${index}-${char}`} 
          digit={char} 
          delay={index * stagger}
        />
      ))}
    </span>
  );
};

// Export a simpler version for whole number animation
export const AnimatedWholeNumber = ({ value, className }: AnimatedNumberProps) => {
  const spring = useSpring(0, { 
    stiffness: 50, 
    damping: 20 
  });
  
  useEffect(() => {
    const numValue = typeof value === 'string' ? parseFloat(value.replace(/,/g, '')) : value;
    if (!isNaN(numValue)) {
      spring.set(numValue);
    }
  }, [value, spring]);
  
  const display = useTransform(spring, (current) => {
    return Math.floor(current).toLocaleString('en-US');
  });
  
  return (
    <motion.span className={className}>
      {display}
    </motion.span>
  );
};

export default AnimatedNumber;
