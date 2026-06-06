import { Loader2, Sparkles } from "lucide-react";
import AuthLogo from "../auth/AuthLogo";

export default function AppLoading({
  title = "Loading workspace",
  message = "Please wait while TechMap prepares your page.",
  fullScreen = false,
}) {
  return (
    <div
      className={`flex items-center justify-center bg-[#F7F1E8] px-4 ${
        fullScreen ? "min-h-screen" : "min-h-[calc(100vh-4rem)]"
      }`}
    >
      <div className="w-full max-w-sm rounded-[2rem] border border-[#B9D8CC] bg-white p-7 text-center shadow-[0_20px_60px_rgba(31,111,95,0.12)]">
        <div className="flex justify-center">
          <AuthLogo compact showTagline={false} />
        </div>

        <div className="relative mx-auto mt-7 flex h-16 w-16 items-center justify-center">
          <div className="absolute inset-0 rounded-full border-4 border-[#DCEBE5]" />
          <Loader2 className="relative z-10 animate-spin text-[#2FA084]" size={34} />
          <Sparkles className="absolute -right-1 -top-1 text-[#6FCF97]" size={18} />
        </div>

        <h2 className="mt-6 text-lg font-extrabold text-[#18332D]">
          {title}
        </h2>

        <p className="mt-2 text-sm leading-6 text-slate-500">
          {message}
        </p>

        <div className="mt-6 flex justify-center gap-1.5">
          <span className="h-2 w-2 animate-bounce rounded-full bg-[#1F6F5F] [animation-delay:-0.2s]" />
          <span className="h-2 w-2 animate-bounce rounded-full bg-[#2FA084] [animation-delay:-0.1s]" />
          <span className="h-2 w-2 animate-bounce rounded-full bg-[#6FCF97]" />
        </div>
      </div>
    </div>
  );
}
