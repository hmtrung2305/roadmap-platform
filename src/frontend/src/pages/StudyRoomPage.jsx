import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useParams } from "react-router-dom";
import axiosClient from "../api/axiosClient";
import { useResourceStore } from "../stores/useResourceStore";
import DocumentHeader from "../components/document/DocumentHeader";
import DocumentReader from "../components/document/DocumentReader";
import DocumentLoading from "../components/document/DocumentLoading";
import LearningResourceSidebar from "../components/learning/LearningResourceSidebar";
import { Bot, PanelLeftOpen } from "lucide-react";
import AiChatPanel from "../components/chat/AiChatPanel";
import { BACKEND_BASE_URL } from "../api/apiConfig";

const HEADER_HEIGHT = 72;

export default function StudyRoomPage() {
  const { resourceId } = useParams();
  const location = useLocation();
  const lastScrollTopRef = useRef(0);

  const resources = useResourceStore((state) => state.resources);
  const fetchResources = useResourceStore((state) => state.fetchResources);
  const isFetching = useResourceStore((state) => state.isFetching);

  const [markdownContent, setMarkdownContent] = useState("");
  const [isLoadingDocument, setIsLoadingDocument] = useState(false);
  const [documentError, setDocumentError] = useState(null);

  const resourceFromState = location.state?.resource;

  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [isChatOpen, setIsChatOpen] = useState(false);
  const [isHeaderVisible, setIsHeaderVisible] = useState(true);

  const [sidebarWidth, setSidebarWidth] = useState(248);
  const [chatWidth, setChatWidth] = useState(360);
  const [resizingPanel, setResizingPanel] = useState(null);

  const currentResource = useMemo(() => {
    return (
      resources.find((resource) => resource.resourceId === resourceId) ||
      resourceFromState ||
      null
    );
  }, [resources, resourceId, resourceFromState]);

  useEffect(() => {
    if (resources.length === 0) {
      fetchResources();
    }
  }, [resources.length, fetchResources]);

  useEffect(() => {
    const loadMarkdown = async () => {
      if (!currentResource?.url) return;

      try {
        setIsLoadingDocument(true);
        setDocumentError(null);

        const response = await axiosClient.get(currentResource.url, {
          baseURL: BACKEND_BASE_URL,
          responseType: "text",
        });

        setMarkdownContent(response.data);
      } catch (error) {
        console.error("Load markdown failed:", error);
        setDocumentError("Unable to load the document content.");
      } finally {
        setIsLoadingDocument(false);
      }
    };

    loadMarkdown();
  }, [currentResource?.url]);

  useEffect(() => {
    if (!resizingPanel) return;

    document.body.style.userSelect = "none";
    document.body.style.cursor = "col-resize";

    const handleMouseMove = (event) => {
      event.preventDefault();

      if (resizingPanel === "sidebar") {
        const nextWidth = Math.min(Math.max(event.clientX, 220), 520);
        setSidebarWidth(nextWidth);
      }

      if (resizingPanel === "chat") {
        const nextWidth = Math.min(
          Math.max(window.innerWidth - event.clientX, 260),
          620,
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

  return (
    <div className="relative flex h-screen overflow-hidden bg-[#F7F1E8] text-[#18332D]">
      <div
        className={`fixed left-0 right-0 top-0 z-50 transition-transform duration-300 ease-out ${
          isHeaderVisible ? "translate-y-0" : "-translate-y-full"
        }`}
      >
        <DocumentHeader resource={currentResource} />
      </div>

      <LearningResourceSidebar
        resources={resources}
        isOpen={isSidebarOpen}
        width={sidebarWidth}
        topOffset={sidePanelTopOffset}
        onStartResize={(event) => {
          event.preventDefault();
          setResizingPanel("sidebar");
        }}
        onClose={() => setIsSidebarOpen(false)}
      />

      <div
        className="flex min-w-0 flex-1 flex-col transition-[margin] duration-300"
        style={{
          marginLeft: isSidebarOpen ? sidebarWidth : 0,
          marginRight: isChatOpen ? chatWidth : 0,
        }}
      >
        <main
          onScroll={handleDocumentScroll}
          className="min-h-0 flex-1 overflow-y-auto px-4 pb-6 transition-[padding] duration-300 sm:px-6 lg:px-8"
          style={{
            paddingTop: isHeaderVisible ? HEADER_HEIGHT + 24 : 24,
          }}
        >
          <div className="mx-auto max-w-4xl">
            {isFetching || isLoadingDocument ? (
              <DocumentLoading />
            ) : documentError ? (
              <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm font-semibold text-red-700 shadow-sm">
                {documentError}
              </div>
            ) : (
              <DocumentReader markdownContent={markdownContent} />
            )}
          </div>
        </main>
      </div>

      <AiChatPanel
        resource={currentResource}
        isOpen={isChatOpen}
        width={chatWidth}
        topOffset={sidePanelTopOffset}
        onStartResize={(event) => {
          event.preventDefault();
          setResizingPanel("chat");
        }}
        onClose={() => setIsChatOpen(false)}
      />

      {!isSidebarOpen && (
        <button
          type="button"
          onClick={() => setIsSidebarOpen(true)}
          className="fixed left-5 z-50 inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-white px-4 py-2 text-sm font-bold text-[#1F6F5F] shadow-lg shadow-emerald-900/10 transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/20"
          style={{ top: isHeaderVisible ? HEADER_HEIGHT + 16 : 16 }}
        >
          <PanelLeftOpen size={17} />
          Resources
        </button>
      )}

      {!isChatOpen && (
        <button
          type="button"
          onClick={() => setIsChatOpen(true)}
          className="fixed bottom-6 right-6 z-50 inline-flex items-center gap-2 rounded-full bg-[#2FA084] px-6 py-3 text-sm font-bold text-white shadow-xl shadow-emerald-900/20 transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
        >
          <Bot size={18} />
          AI Mentor
        </button>
      )}
    </div>
  );
}
