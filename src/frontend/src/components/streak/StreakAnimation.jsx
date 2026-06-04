import { useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { FaFireAlt } from "react-icons/fa";
import { useStreakStore } from "../../stores/useStreakStore";

export default function StreakAnimation() {
  const streak = useStreakStore((state) => state.streak);
  const showAnimation = useStreakStore((state) => state.showAnimation);
  const closeAnimation = useStreakStore((state) => state.closeAnimation);

  useEffect(() => {
    if (!showAnimation) return;

    const timer = setTimeout(() => {
      closeAnimation();
    }, 4200);

    return () => clearTimeout(timer);
  }, [showAnimation, closeAnimation]);

  return (
    <AnimatePresence>
      {showAnimation && streak && (
        <motion.div
          onClick={closeAnimation}
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.42 }}
          className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/45 px-4"
        >
          <motion.div
            onClick={(event) => event.stopPropagation()}
            initial={{
              opacity: 0,
              y: -90,
              scale: 0.72,
            }}
            animate={{
              opacity: 1,
              y: 0,
              scale: 1,
            }}
            exit={{
              opacity: 0,
              y: 40,
              scale: 0.92,
            }}
            transition={{
              duration: 0.65,
              ease: [0.16, 1, 0.3, 1],
            }}
            className="w-full max-w-sm rounded-3xl bg-white p-7 text-center shadow-2xl"
          >
            <motion.div
              initial={{
                scale: 0.35,
                rotate: -18,
                y: -20,
              }}
              animate={{
                scale: [0.35, 1.45, 1.12, 1.25, 1],
                rotate: [-18, 10, -6, 4, 0],
                y: [-20, 0, -10, 0, 0],
              }}
              transition={{
                duration: 1.25,
                ease: "easeOut",
              }}
              className="mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-orange-50"
            >
              <FaFireAlt className="text-6xl text-orange-500" />
            </motion.div>

            <motion.h2
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.45, duration: 0.4 }}
              className="mt-6 text-2xl font-bold text-slate-900"
            >
              Streak increased!
            </motion.h2>

            <motion.p
              initial={{ opacity: 0, y: 14 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.62, duration: 0.42 }}
              className="mt-3 text-sm leading-6 text-slate-600"
            >
              Great job. You are now on a{" "}
              <span className="font-semibold text-orange-600">
                {streak.currentStreak}-day streak
              </span>
              .
            </motion.p>

            <motion.button
              type="button"
              onClick={closeAnimation}
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.82, duration: 0.35 }}
              className="mt-6 rounded-xl bg-orange-500 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-orange-600"
            >
              Nice
            </motion.button>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
