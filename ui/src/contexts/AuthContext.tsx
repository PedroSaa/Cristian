import { createContext, useContext, useReducer, useCallback, useEffect, type ReactNode } from 'react';
import { mapAuthUser, type AuthState as UserAuthState, type User } from '../types/auth';
import { getProfile } from '../lib/api/auth';

// ---------------------------------------------------------------------------
// State
// ---------------------------------------------------------------------------

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  authState: UserAuthState | null;
  setupToken: string | null;
  canLogout: boolean;
  requiresMfa: boolean;
  mfaToken: string | null;
}

// ---------------------------------------------------------------------------
// Actions
// ---------------------------------------------------------------------------

type AuthAction =
  | { type: 'LOGIN'; payload: { user: User } }
  | { type: 'LOGOUT' }
  | { type: 'SET_USER'; payload: User }
  | { type: 'SESSION_RESTORED'; payload: { user: User } }
  | { type: 'SESSION_FAILED' }
  | { type: 'MFA_REQUIRED'; payload: { mfaToken: string } }
  | { type: 'MFA_CANCEL' };

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function readStoredUser(): User | null {
  const raw = localStorage.getItem('user');
  if (!raw) return null;
  try {
    const user = JSON.parse(raw) as User;
    return mapAuthUser(user);
  } catch {
    return null;
  }
}

function persistLogin(user: User) {
  localStorage.setItem('user', JSON.stringify(mapAuthUser(user)));
}

function clearPersistedAuth() {
  // access_token is HttpOnly (cookie) — not stored in localStorage
  localStorage.removeItem('user');
}

// ---------------------------------------------------------------------------
// Initial state
// ---------------------------------------------------------------------------

function getInitialState(): AuthState {
  const user = readStoredUser();
  const authState = user?.authState ?? null;

  return {
    user,
    isAuthenticated: user !== null,
    isLoading: user !== null && authState !== 'mfa_setup_required', // skip /me for setup-only sessions
    authState,
    setupToken: user?.setupToken ?? null,
    canLogout: user?.canLogout ?? true,
    requiresMfa: false,
    mfaToken: null,
  };
}

// ---------------------------------------------------------------------------
// Reducer
// ---------------------------------------------------------------------------

function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'LOGIN': {
      const user = mapAuthUser(action.payload.user);
      persistLogin(user);
      return {
        user,
        isAuthenticated: true,
        isLoading: false,
        authState: user.authState ?? null,
        setupToken: user.setupToken ?? null,
        canLogout: user.canLogout ?? true,
        requiresMfa: false,
        mfaToken: null,
      };
    }

    case 'LOGOUT':
      clearPersistedAuth();
      return {
        user: null,
        isAuthenticated: false,
        isLoading: false,
        authState: null,
        setupToken: null,
        canLogout: true,
        requiresMfa: false,
        mfaToken: null,
      };

    case 'SET_USER': {
      const user = mapAuthUser(action.payload);
      localStorage.setItem('user', JSON.stringify(user));
      return {
        ...state,
        user,
        authState: user.authState ?? null,
        setupToken: user.setupToken ?? null,
        canLogout: user.canLogout ?? state.canLogout,
      };
    }

    case 'SESSION_RESTORED': {
      const user = mapAuthUser(action.payload.user);
      persistLogin(user);
      return {
        ...state,
        user,
        isAuthenticated: true,
        isLoading: false,
        authState: user.authState ?? null,
        setupToken: user.setupToken ?? null,
        canLogout: user.canLogout ?? state.canLogout,
      };
    }

    case 'SESSION_FAILED':
      clearPersistedAuth();
      return {
        ...state,
        user: null,
        isAuthenticated: false,
        isLoading: false,
        authState: null,
        setupToken: null,
        canLogout: true,
      };

    case 'MFA_REQUIRED':
      return { ...state, requiresMfa: true, mfaToken: action.payload.mfaToken };

    case 'MFA_CANCEL':
      return { ...state, requiresMfa: false, mfaToken: null };

    default:
      return state;
  }
}

// ---------------------------------------------------------------------------
// Context value
// ---------------------------------------------------------------------------

interface AuthContextValue {
  state: AuthState;
  login: (user: User) => void;
  logout: () => void;
  setUser: (user: User) => void;
  requireMfa: (mfaToken: string) => void;
  cancelMfa: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

// ---------------------------------------------------------------------------
// Provider
// ---------------------------------------------------------------------------

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(authReducer, undefined, getInitialState);

  // On mount, verify the session if we have a stored user
  useEffect(() => {
    if (!state.user || state.authState === 'mfa_setup_required') return;

    let cancelled = false;

    getProfile()
      .then((user) => {
        if (!cancelled) {
          dispatch({ type: 'SESSION_RESTORED', payload: { user } });
        }
      })
      .catch(() => {
        if (!cancelled) {
          dispatch({ type: 'SESSION_FAILED' });
        }
      });

    return () => {
      cancelled = true;
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const login = useCallback((user: User) => {
    dispatch({ type: 'LOGIN', payload: { user } });
  }, []);

  const logout = useCallback(() => {
    dispatch({ type: 'LOGOUT' });
  }, []);

  const setUser = useCallback((user: User) => {
    dispatch({ type: 'SET_USER', payload: user });
  }, []);

  const requireMfa = useCallback((mfaToken: string) => {
    dispatch({ type: 'MFA_REQUIRED', payload: { mfaToken } });
  }, []);

  const cancelMfa = useCallback(() => {
    dispatch({ type: 'MFA_CANCEL' });
  }, []);

  return (
    <AuthContext.Provider value={{ state, login, logout, setUser, requireMfa, cancelMfa }}>
      {children}
    </AuthContext.Provider>
  );
}

// ---------------------------------------------------------------------------
// Hook
// ---------------------------------------------------------------------------

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
