import axiosClient from "./axiosClient";

export const aiCreditApi = {
  getStatus: async () => {
    const response = await axiosClient.get("/ai-credits/status");
    return response.data;
  },
};
