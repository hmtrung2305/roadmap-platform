import axios from "axios";
import axiosClient from "./axiosClient";


export async function getMyPortfolioApi() {
    const res = await axiosClient.get("/me/portfolio");
    return res.data;
}
export async function  getPublicPortfolioApi (username) {
    const res = await axiosClient.get(`/me/portfolio/${username}`);
    return res.data;
}
export async function updatePortfolioRepositoriesApi(repositoryIds){
    const res = await axiosClient.patch("me/portfolio/repositories",
        {repositoryIds}
    );
    return res.data;
}