import { useEffect, useState } from "react";
import { getMyPortfolioApi, getPortfolioByUsernameApi } from "../api/portfolioApi";

export function usePortfolio(username, isOwnPortfolio = false) {
  const [portfolio, setPortfolio] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function fetchPortfolio() {
      try {
        setLoading(true);
        setError("");

        const data = isOwnPortfolio
          ? await getMyPortfolioApi()
          : await getPortfolioByUsernameApi(username);

        setPortfolio(data);
      } catch (err) {
        console.error("Failed to fetch portfolio:", err);
        setError(err?.message || "Cannot load portfolio data");
      } finally {
        setLoading(false);
      }
    }

    if (isOwnPortfolio || username) {
      fetchPortfolio();
    } else {
      setLoading(false);
      setError("Username was not found.");
    }
  }, [username, isOwnPortfolio]);

  return {
    portfolio,
    loading,
    error,
  };
}
