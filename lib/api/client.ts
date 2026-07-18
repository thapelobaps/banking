const API_URL = process.env.KAPE_API_URL ?? 'http://localhost:5000';

type ApiProblem = {
  title?: string;
  detail?: string;
  message?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly errors?: Record<string, string[]>
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  accessToken?: string
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers,
    cache: 'no-store',
  });

  if (!response.ok) {
    let problem: ApiProblem | undefined;

    try {
      problem = (await response.json()) as ApiProblem;
    } catch {
      problem = undefined;
    }

    const firstFieldError = problem?.errors
      ? Object.values(problem.errors).flat()[0]
      : undefined;

    throw new ApiError(
      firstFieldError ?? problem?.detail ?? problem?.message ?? problem?.title ?? 'Request failed.',
      response.status,
      problem?.errors
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
