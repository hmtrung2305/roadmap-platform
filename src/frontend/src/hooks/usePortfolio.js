import { useEffect, useState } from "react";
import { getPortfolio } from "../api/portfolioApi";

export function usePortfolio(userId){
    const [portfolio, setPortfolio] = useState(null);
    const [loading, setLoading] = useState(true);
    const  [error, setError] = useState("");

    useEffect(()=> {
        if(!userId) return;

        const fetchPortfolio = async () => {
            try {
                setLoading(true);
                const data = await getPortfolio(userId);
                setPortfolio(data);
            } catch (err) {
                console.error("Failed to fetch portfolio:", err);
                setError("Cannot load portfolio data");
            }finally {
                setLoading(false);
            }
        };
        fetchPortfolio();
    }, [userId]);
    return {
        portfolio,
        loading,
        error,
    }
}