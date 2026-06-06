import { useEffect, useRef } from "react";
import { CAPTCHA_ENABLED, CAPTCHA_SITE_KEY } from "../../api/apiConfig";

const TURNSTILE_SCRIPT_SRC =
  "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";

let turnstileScriptPromise;

function loadTurnstileScript() {
  if (window.turnstile) {
    return Promise.resolve(window.turnstile);
  }

  if (!turnstileScriptPromise) {
    turnstileScriptPromise = new Promise((resolve, reject) => {
      const existingScript = document.querySelector(
        `script[src="${TURNSTILE_SCRIPT_SRC}"]`
      );

      if (existingScript) {
        existingScript.addEventListener("load", () => resolve(window.turnstile));
        existingScript.addEventListener("error", reject);
        return;
      }

      const script = document.createElement("script");
      script.src = TURNSTILE_SCRIPT_SRC;
      script.async = true;
      script.defer = true;
      script.onload = () => resolve(window.turnstile);
      script.onerror = reject;
      document.head.appendChild(script);
    });
  }

  return turnstileScriptPromise;
}

export default function TurnstileCaptcha({
  action,
  onVerify,
  resetKey = 0,
  className = "",
}) {
  const containerRef = useRef(null);
  const widgetIdRef = useRef(null);

  useEffect(() => {
    if (!CAPTCHA_ENABLED) {
      return undefined;
    }

    let disposed = false;
    onVerify("");

    loadTurnstileScript()
      .then((turnstile) => {
        if (disposed || !containerRef.current) {
          return;
        }

        widgetIdRef.current = turnstile.render(containerRef.current, {
          sitekey: CAPTCHA_SITE_KEY,
          action,
          theme: "light",
          callback: (token) => onVerify(token),
          "expired-callback": () => onVerify(""),
          "error-callback": () => onVerify(""),
        });
      })
      .catch(() => {
        onVerify("");
      });

    return () => {
      disposed = true;

      if (window.turnstile && widgetIdRef.current !== null) {
        window.turnstile.remove(widgetIdRef.current);
      }

      widgetIdRef.current = null;
    };
  }, [action, onVerify, resetKey]);

  if (!CAPTCHA_ENABLED) {
    return null;
  }

  return <div ref={containerRef} className={className} />;
}