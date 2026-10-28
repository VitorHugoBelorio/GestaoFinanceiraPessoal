const API_URL = import.meta.env.VITE_API_URL ?? '/api'
let csrfTokenPromise: Promise<string> | null = null

export class ApiError extends Error {
  status: number

  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

function publicErrorMessage(path: string, status: number): string {
  if (status === 400) return 'Confira os dados informados e tente novamente.'
  if (status === 401) {
    return path === '/auth/login'
      ? 'E-mail ou senha incorretos.'
      : 'Sua sessão expirou. Entre novamente para continuar.'
  }
  if (status === 403) return 'Você não tem permissão para realizar esta ação.'
  if (status === 404) return 'Não encontramos o que você procurava. Atualize a página e tente novamente.'
  if (status === 409) {
    return path === '/auth/register-and-onboard'
      ? 'Este e-mail já está cadastrado. Tente entrar na sua conta.'
      : 'Não foi possível salvar porque os dados mudaram. Atualize a página e tente novamente.'
  }
  if (status === 429) return 'Muitas tentativas em pouco tempo. Aguarde um momento e tente novamente.'
  return 'Não foi possível concluir esta ação agora. Tente novamente em instantes.'
}

export async function apiRequest<T>(path: string, init: RequestInit = {}, retryCsrf = true): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')

  const method = (init.method ?? 'GET').toUpperCase()
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) {
    csrfTokenPromise ??= fetch(`${API_URL}/auth/csrf`, { credentials: 'include', cache: 'no-store' })
      .then(async (response) => {
        if (!response.ok) throw new ApiError('Não foi possível confirmar a segurança da sessão. Atualize a página e tente novamente.', response.status)
        const data = await response.json() as { token: string }
        return data.token
      })
      .catch((error: unknown) => {
        csrfTokenPromise = null
        throw error instanceof ApiError ? error : new ApiError('Não foi possível confirmar a segurança da sessão. Confira sua conexão e tente novamente.', 0)
      })
    headers.set('X-CSRF-TOKEN', await csrfTokenPromise)
  }

  let response: Response
  try {
    response = await fetch(`${API_URL}${path}`, { ...init, headers, credentials: 'include' })
  } catch {
    throw new ApiError('Não foi possível conectar. Confira sua conexão e tente novamente.', 0)
  }

  if (!response.ok) {
    if (retryCsrf && response.headers.get('X-CSRF-Invalid') === '1') {
      csrfTokenPromise = null
      return apiRequest<T>(path, init, false)
    }
    throw new ApiError(publicErrorMessage(path, response.status), response.status)
  }
  if (path === '/auth/login' || path === '/auth/register-and-onboard' || path === '/auth/logout' || path === '/auth/account') {
    csrfTokenPromise = null
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
