import { useAuth, useMutex } from "#imports";

export default function() {
  const mutex = useMutex();
  const auth = useAuth();

  async function current(
    shouldRefresh = true
  ) {
    const tokenString = localStorage.getItem("token");
    if(!tokenString) { return null; }

    const result = Token(tokenString);

    // this control is for backward compatibility
    // previous tokens may not contain 'access' value
    if(!result.access) { return null; }

    if(result.refreshIsExpired()) { return null; }
    if(result.accessIsExpired() && shouldRefresh) {
      await refresh();

      return current(false);
    }

    return result;
  }

  async function refresh() {
    await mutex.run(async() => {
      const token = await current(false);
      if(!token?.accessIsExpired()) { return; }

      const result = await auth.refresh(token?.refresh);

      setCurrent(result, false);
    });
  }

  function setCurrent(value,
    dispatch = true
  ) {
    if(!value) {
      localStorage.clear("token");
    } else {
      localStorage.setItem("token", JSON.stringify(value));
    }

    if(dispatch) {
      window.dispatchEvent(new CustomEvent("token-changed"));
    }
  }

  function onChanged(callback) {
    window.addEventListener("token-changed", callback);
  }

  return {
    current,
    setCurrent,
    onChanged
  };
};

function Token(tokenString) {
  const { access, refresh, displayName } = JSON.parse(tokenString);

  function accessIsExpired() {
    return isExpired(access);
  }

  function refreshIsExpired() {
    return isExpired(refresh);
  }

  function isExpired(token) {
    try {
      const claims = JSON.parse(atob(token.split(".")[1]));

      return parseInt(claims.exp) * 1000 < Date.now();
    } catch {
      throw createError({ statusCode: 401 });
    }
  }

  return {
    access,
    refresh,
    displayName,
    accessIsExpired,
    refreshIsExpired
  };
}
