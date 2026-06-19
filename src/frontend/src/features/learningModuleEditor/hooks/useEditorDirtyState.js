import { useCallback, useMemo, useState } from "react";

export default function useEditorDirtyState() {
  const [dirtyScopes, setDirtyScopes] = useState({});

  const setDirtyState = useCallback((scope, isDirty) => {
    setDirtyScopes((current) => {
      const nextValue = Boolean(isDirty);

      if (Boolean(current[scope]) === nextValue) {
        return current;
      }

      if (!nextValue) {
        const next = { ...current };
        delete next[scope];
        return next;
      }

      return { ...current, [scope]: true };
    });
  }, []);

  const clearDirtyState = useCallback((scope) => {
    setDirtyScopes((current) => {
      if (!current[scope]) return current;
      const next = { ...current };
      delete next[scope];
      return next;
    });
  }, []);

  const clearAllDirtyStates = useCallback(() => {
    setDirtyScopes({});
  }, []);

  const hasUnsavedChanges = useMemo(
    () => Object.values(dirtyScopes).some(Boolean),
    [dirtyScopes],
  );

  return {
    dirtyScopes,
    hasUnsavedChanges,
    setDirtyState,
    clearDirtyState,
    clearAllDirtyStates,
  };
}
