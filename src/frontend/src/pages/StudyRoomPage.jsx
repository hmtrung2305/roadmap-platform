import { useEffect, useMemo, useState } from "react";
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

export default function StudyRoomPage() {
  const { resourceId } = useParams();
  const location = useLocation();

  const resources = useResourceStore((state) => state.resources);
  const fetchResources = useResourceStore((state) => state.fetchResources);
  const isFetching = useResourceStore((state) => state.isFetching);

  const [markdownContent, setMarkdownContent] = useState("");
  const [isLoadingDocument, setIsLoadingDocument] = useState(false);
  const [documentError, setDocumentError] = useState(null);

  const resourceFromState = location.state?.resource;

  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [isChatOpen, setIsChatOpen] = useState(false);

  const [sidebarWidth, setSidebarWidth] = useState(208);
  const [chatWidth, setChatWidth] = useState(290);
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
        setDocumentError("Không thể tải nội dung tài liệu.");
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
        const nextWidth = Math.min(Math.max(event.clientX, 200), 520);
        setSidebarWidth(nextWidth);
      }

      if (resizingPanel === "chat") {
        const nextWidth = Math.min(
          Math.max(window.innerWidth - event.clientX, 200),
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

  return (
    <div className="relative flex h-screen flex-col bg-slate-50">
      <div className="shrink-0">
        <DocumentHeader resource={currentResource} />
      </div>

      <main className="h-[calc(100vh-4rem)] flex-1  px-6 py-8">
        <div className="mx-auto max-w-4xl">
          {isFetching || isLoadingDocument ? (
            <DocumentLoading />
          ) : documentError ? (
            <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700">
              {documentError}
            </div>
          ) : (
            <DocumentReader markdownContent={markdownContent} />
          )}
        </div>
      </main>

      <LearningResourceSidebar
        resources={resources}
        isOpen={isSidebarOpen}
        width={sidebarWidth}
        onStartResize={(event) => {
          event.preventDefault();
          setResizingPanel("sidebar");
        }}
        onClose={() => setIsSidebarOpen(false)}
      />

      <AiChatPanel
        resource={currentResource}
        isOpen={isChatOpen}
        width={chatWidth}
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
          className="fixed left-4 top-24 z-40 inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-lg hover:bg-slate-50"
        >
          <PanelLeftOpen size={18} />
          Lessons
        </button>
      )}

      {!isChatOpen && (
        <button
          type="button"
          onClick={() => setIsChatOpen(true)}
          className="fixed bottom-6 right-6 z-40 inline-flex items-center gap-2 rounded-full bg-blue-600 px-5 py-3 text-sm font-semibold text-white shadow-xl hover:bg-blue-700"
        >
          <Bot size={18} />
          AI Mentor
        </button>
      )}
    </div>
  );
}
