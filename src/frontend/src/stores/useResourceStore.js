import { create } from "zustand";
import { resourceApi } from "../api/resourceApi";

export const useResourceStore = create((set, get) => ({
  resources: [],

  isFetching: false,
  isUploading: false,
  isDeleting: false,

  error: null,

  fetchResources: async () => {
    try {
      set({
        isFetching: true,
        error: null,
      });
      const data = await resourceApi.getAll();

      set({
        resources: data,
      });
    } catch (error) {
      console.log("Failed to load the resources:", error);

      set({
        error: "Failed to load the resource",
      });
    } finally {
      set({
        isFetching: false,
      });
    }
  },
  uploadResource: async ({ title, skillName, file }) => {
    try {
      set({
        isUploading: true,
        error: null,
      });

      await resourceApi.upload({
        title,
        skillName,
        file,
      });

      await get().fetchResources();
      return true;
    } catch (error) {
      console.error("Upload resource failed:", error);

      const message = "Upload unsuccessfully resource. Please check again backend site or file .md";

      set({
        error: message,
      });
      throw error;
    } finally {
      set({
        isUploading: false,
      });
    }
  },
  deleteResource: async (resourceId) => {
    try {
        set({
            isDeleting: true,
            error: null
        })
        
        await resourceApi.delete(resourceId);
        set((state) =>({
            resources: state.resources.filter(
                (resources) => resources.resourceId !== resourceId
            )
        }))
        return true
    } catch (error) {
        console.log("Failed to delete resource:", error);
        
        const message = "Failed to delete resource"
        set({
            error: message
        })
    }finally{
        set({
            isDeleting: false
        })
    }
  },
  clearError: () => {
    set({
        error: null
    });
  }
}));
