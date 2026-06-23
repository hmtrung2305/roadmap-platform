import { useEffect, useRef, useState } from "react";
import {
  CHAT_MAX_WIDTH,
  CHAT_MIN_WIDTH,
  HEADER_HEIGHT,
  PAGE_GAP,
  SIDEBAR_MAX_WIDTH,
  SIDEBAR_MIN_WIDTH,
} from "../utils/studyRoomUtils";

export function useStudyRoomLayout() {
  const lastScrollTopRef = useRef(0);
  const [showQuiz, setShowQuiz] = useState(false);
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [isChatOpen, setIsChatOpen] = useState(true);
  const [isHeaderVisible, setIsHeaderVisible] = useState(true);
  const [sidebarWidth, setSidebarWidth] = useState(280);
  const [chatWidth, setChatWidth] = useState(380);
  const [resizingPanel, setResizingPanel] = useState(null);

  useEffect(() => {
    if (!resizingPanel) return;

    document.body.style.userSelect = "none";
    document.body.style.cursor = "col-resize";

    const handleMouseMove = (event) => {
      event.preventDefault();

      if (resizingPanel === "sidebar") {
        const nextWidth = Math.min(
          Math.max(event.clientX - PAGE_GAP, SIDEBAR_MIN_WIDTH),
          SIDEBAR_MAX_WIDTH,
        );

        setSidebarWidth(nextWidth);
        return;
      }

      if (resizingPanel === "chat") {
        const nextWidth = Math.min(
          Math.max(window.innerWidth - event.clientX - PAGE_GAP, CHAT_MIN_WIDTH),
          CHAT_MAX_WIDTH,
        );

        setChatWidth(nextWidth);
      }
    };

    const handleMouseUp = () => {
      setResizingPanel(null);
      document.body.style.userSelect = "";
      document.body.style.cursor = "";
    };

    document.addEventListener("mousemove", handleMouseMove);
    document.addEventListener("mouseup", handleMouseUp);

    return () => {
      document.body.style.userSelect = "";
      document.body.style.cursor = "";
      document.removeEventListener("mousemove", handleMouseMove);
      document.removeEventListener("mouseup", handleMouseUp);
    };
  }, [resizingPanel]);

  const handleDocumentScroll = (event) => {
    const currentScrollTop = event.currentTarget.scrollTop;
    const previousScrollTop = lastScrollTopRef.current;
    const scrollDelta = currentScrollTop - previousScrollTop;

    if (currentScrollTop <= 12) {
      setIsHeaderVisible(true);
      lastScrollTopRef.current = currentScrollTop;
      return;
    }

    if (scrollDelta > 8) {
      setIsHeaderVisible(false);
    }

    if (scrollDelta < -8) {
      setIsHeaderVisible(true);
    }

    lastScrollTopRef.current = currentScrollTop;
  };

  const sidePanelTopOffset = isHeaderVisible ? HEADER_HEIGHT : 0;
  const mainLeftPadding = isSidebarOpen ? sidebarWidth + PAGE_GAP : PAGE_GAP;
  const mainRightPadding = isChatOpen ? chatWidth + PAGE_GAP : PAGE_GAP;

  return {
    showQuiz,
    setShowQuiz,
    isSidebarOpen,
    setIsSidebarOpen,
    isChatOpen,
    setIsChatOpen,
    isHeaderVisible,
    sidebarWidth,
    chatWidth,
    sidePanelTopOffset,
    mainLeftPadding,
    mainRightPadding,
    handleDocumentScroll,
    startSidebarResize: (event) => {
      event.preventDefault();
      setResizingPanel("sidebar");
    },
    startChatResize: (event) => {
      event.preventDefault();
      setResizingPanel("chat");
    },
  };
}
