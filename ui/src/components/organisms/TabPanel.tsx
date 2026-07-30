import { useState, useCallback, type ReactNode } from 'react';

export interface Tab {
  id: string;
  label: string;
  icon?: ReactNode;
  count?: number;
  content: ReactNode;
  visible?: boolean;
}

interface TabPanelProps {
  tabs: Tab[];
  activeTab?: string;
  onTabChange?: (tabId: string) => void;
  className?: string;
}

export default function TabPanel({ tabs, activeTab: controlled, onTabChange, className = '' }: TabPanelProps) {
  const visibleTabs = tabs.filter((t) => t.visible !== false);
  const [internalTab, setInternalTab] = useState(visibleTabs[0]?.id ?? '');
  const active = controlled ?? internalTab;

  const handleTabChange = useCallback(
    (tabId: string) => {
      if (!controlled) setInternalTab(tabId);
      onTabChange?.(tabId);
    },
    [controlled, onTabChange],
  );

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent, index: number) => {
      let nextIndex = index;
      if (e.key === 'ArrowRight') {
        e.preventDefault();
        nextIndex = (index + 1) % visibleTabs.length;
      } else if (e.key === 'ArrowLeft') {
        e.preventDefault();
        nextIndex = (index - 1 + visibleTabs.length) % visibleTabs.length;
      } else {
        return;
      }
      const targetId = visibleTabs[nextIndex]?.id;
      if (targetId) {
        handleTabChange(targetId);
        // Focus the newly active tab button
        const buttons = document.querySelectorAll<HTMLButtonElement>('[data-tab-button]');
        buttons[nextIndex]?.focus();
      }
    },
    [visibleTabs, handleTabChange],
  );

  const activeTab = visibleTabs.find((t) => t.id === active);
  const activeTabId = `tab-${active}`;
  const activePanelId = `tabpanel-${active}`;

  return (
    <div className={`bg-surface-secondary ${className}`}>
      {/* Barra de tabs */}
      <div className="flex overflow-x-auto border-b border-border-base bg-surface" role="tablist" aria-label="Pestañas de contenido">
        {visibleTabs.map((tab, index) => (
          <button
            key={tab.id}
            id={`tab-${tab.id}`}
            role="tab"
            aria-selected={tab.id === active}
            aria-controls={`tabpanel-${tab.id}`}
            data-tab-button
            tabIndex={tab.id === active ? 0 : -1}
            onClick={() => handleTabChange(tab.id)}
            onKeyDown={(e) => handleKeyDown(e, index)}
            className={[
              'flex-shrink-0 border-b-2 px-4 py-3 text-sm font-semibold whitespace-nowrap transition-colors focus:outline-none focus:ring-2 focus:ring-inset focus:ring-primary-400',
              active === tab.id
                ? 'border-primary-600 text-primary-700'
                : 'border-transparent text-text-base/55 hover:text-text-base',
            ].join(' ')}
          >
            <span className="flex items-center gap-2.5">
              <span className={[
                'inline-flex h-5 w-5 items-center justify-center rounded-full',
                active === tab.id ? 'text-primary-700' : 'text-text-base/55',
              ].join(' ')}>
                {tab.icon}
              </span>
              <span>{tab.label}</span>
              {tab.count !== undefined && (
                <span
                  className={[
                    'rounded-full px-1.5 py-0.5 text-[10px] font-semibold leading-none',
                    tab.count > 0
                      ? 'bg-primary-100 text-primary-700'
                      : 'bg-surface-secondary text-text-base/45',
                  ].join(' ')}
                >
                  {tab.count}
                </span>
              )}
            </span>
          </button>
        ))}
      </div>

      {/* Contenido */}
      <div
        id={activePanelId}
        role="tabpanel"
        aria-labelledby={activeTabId}
        tabIndex={0}
        className="p-4 focus:outline-none"
      >
        {activeTab?.content}
      </div>
    </div>
  );
}
