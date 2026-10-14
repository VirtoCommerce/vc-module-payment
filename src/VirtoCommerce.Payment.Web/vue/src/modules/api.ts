// Small same-origin fetch helper for endpoints without a generated client here.
// Cookie auth works because the host is same-origin.
async function http<T>(method: string, url: string, body?: unknown): Promise<T> {
  const res = await fetch(url, {
    method,
    credentials: "same-origin",
    headers: body === undefined ? { Accept: "application/json" } : { Accept: "application/json", "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) {
    throw new Error(`${method} ${url}: ${res.status} ${(await res.text()).slice(0, 300)}`);
  }
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: <T>(url: string) => http<T>("GET", url),
  post: <T>(url: string, body?: unknown) => http<T>("POST", url, body ?? {}),
  put: <T>(url: string, body?: unknown) => http<T>("PUT", url, body ?? {}),
};

// uploads one file to the platform assets (POST api/assets?folderUrl=) and returns its public url
export async function uploadAsset(file: File, folderUrl: string) {
  const body = new FormData();
  body.append("file", file); // the server names an upload after its file name
  const res = await fetch(`/api/assets?${new URLSearchParams({ folderUrl })}`, { method: "POST", credentials: "same-origin", body });
  if (!res.ok) throw new Error(`upload ${file.name}: ${res.status}`);
  const [blob] = (await res.json()) as { url: string }[];
  return blob.url;
}
