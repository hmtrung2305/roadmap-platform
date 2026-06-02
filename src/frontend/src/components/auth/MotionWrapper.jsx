import { motion } from "framer-motion";

export default function MotionWrapper({ children, className = "" }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 14, scale: 0.98 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, y: -10, scale: 0.98 }}
      transition={{
        duration: 0.22,
        ease: "easeOut",
      }}
      className={className}
    >
      {children}
    </motion.div>
  );
}
