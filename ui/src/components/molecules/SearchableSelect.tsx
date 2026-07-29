import { useState, useRef, useEffect, useMemo, useId } from 'react';

interface SearchableSelectProps<T> extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'value' | 'onChange' | 'placeholder'> {
  options: T[];
  value: string;
  onChange: (value: string) => void;
  getOptionLabel: (option: T) => string;
  getOptionValue: (option: T) => string;
  placeholder?: string;
  disabled?: boolean;
  loading?: boolean;
  noOptionsText?: string;
  allLabel?: string;
}

interface AllOption {
  isAllLabel: true;
  label: string;
  value: '';
}

function defaultLabel<T>(option: T): string {
  if (option && typeof option === 'object') {
    const obj = option as Record<string, unknown>;
    return String(obj.label ?? obj.name ?? '');
  }
  return String(option);
}

function defaultValue<T>(option: T): string {
  if (option && typeof option === 'object') {
    const obj = option as Record<string, unknown>;
    return String(obj.value ?? obj.id ?? '');
  }
  return String(option);
}

function isAllOption<T>(option: T | AllOption): option is AllOption {
  return typeof option === 'object' && option !== null && 'isAllLabel' in option;
}

/** Lowercase + strip diacritics, for accent-insensitive matching ("per" ↔ "Pérez"). */
function normalizeText(text: string): string {
  return text.toLowerCase().normalize('NFD').replace(/\p{Diacritic}/gu, '');
}

export default function SearchableSelect<T>({
  options,
  value,
  onChange,
  getOptionLabel = defaultLabel as (o: T) => string,
  getOptionValue = defaultValue as (o: T) => string,
  placeholder = 'Seleccionar...',
  disabled = false,
  loading = false,
  noOptionsText = 'No se encontraron resultados',
  allLabel,
  ...inputProps
}: SearchableSelectProps<T>) {
  const generatedId = useId();
  const [inputValue, setInputValue] = useState('');
  const [open, setOpen] = useState(false);
  const [highlightedIndex, setHighlightedIndex] = useState(-1);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const inputId = inputProps.id ?? generatedId;
  const listboxId = `${inputId}-listbox`;

  const selectedOption = useMemo(
    () => options.find((o) => getOptionValue(o) === value),
    [options, value, getOptionValue],
  );

  // Filter options based on input value (only when dropdown is open).
  // Accent- and case-insensitive so "per" matches "Pérez" (Spanish names with tildes).
  const filtered = useMemo(() => {
    if (!inputValue || !open) return options;
    const search = normalizeText(inputValue);
    return options.filter((o) => normalizeText(getOptionLabel(o)).includes(search));
  }, [options, inputValue, open, getOptionLabel]);

  // Build display list: allLabel + filtered options
  const displayOptions = useMemo<Array<T | AllOption>>(() => {
    if (allLabel) {
      return [{ isAllLabel: true, label: allLabel, value: '' }, ...filtered];
    }
    return filtered;
  }, [allLabel, filtered]);

  // Reset highlight when filtered list changes
  useEffect(() => {
    setHighlightedIndex(-1);
  }, [filtered.length, open]);

  // Click-outside handler
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
        setInputValue('');
      }
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  // Reset inputValue when dropdown closes
  useEffect(() => {
    if (!open) {
      setInputValue('');
    }
  }, [open]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
    setOpen(true);
  };

  const handleFocus = () => {
    if (!disabled) setOpen(true);
  };

  const handleSelect = (opt: T | AllOption) => {
    if (isAllOption(opt)) {
      onChange('');
    } else {
      onChange(getOptionValue(opt));
    }
    setOpen(false);
    setInputValue('');
    inputRef.current?.blur();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!open) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        setOpen(true);
        return;
      }
    }

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setHighlightedIndex((prev) =>
          prev < displayOptions.length - 1 ? prev + 1 : 0,
        );
        break;
      case 'ArrowUp':
        e.preventDefault();
        setHighlightedIndex((prev) =>
          prev > 0 ? prev - 1 : displayOptions.length - 1,
        );
        break;
      case 'Enter':
        e.preventDefault();
        if (highlightedIndex >= 0 && highlightedIndex < displayOptions.length) {
          handleSelect(displayOptions[highlightedIndex]);
        }
        break;
      case 'Escape':
        e.preventDefault();
        setOpen(false);
        setInputValue('');
        break;
      case 'Tab':
        setOpen(false);
        setInputValue('');
        break;
    }
  };

  const activeDescendantId =
    highlightedIndex >= 0 && highlightedIndex < displayOptions.length
      ? `${listboxId}-option-${highlightedIndex}`
      : undefined;

  const optionId = (index: number) => `${listboxId}-option-${index}`;

  return (
    <div ref={containerRef} className="relative">
      <input
        {...inputProps}
        ref={inputRef}
        id={inputId}
        type="text"
        role="combobox"
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-autocomplete="list"
        aria-activedescendant={activeDescendantId}
        aria-controls={open ? listboxId : undefined}
        disabled={disabled}
        placeholder={selectedOption ? getOptionLabel(selectedOption) : placeholder}
        value={open ? inputValue : (selectedOption ? getOptionLabel(selectedOption) : '')}
        onChange={handleInputChange}
        onFocus={handleFocus}
        onKeyDown={handleKeyDown}
        className="block w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-gray-400"
      />

      {open && !disabled && (
        <ul
          id={listboxId}
          role="listbox"
          className="absolute z-[60] mt-1 max-h-48 w-full overflow-auto rounded border border-gray-200 bg-white shadow-lg"
        >
          {loading && (
            <li className="px-3 py-2 text-sm text-gray-500">Cargando...</li>
          )}

          {!loading && displayOptions.length === 0 && (
            <li className="px-3 py-2 text-sm text-gray-500">{noOptionsText}</li>
          )}

          {!loading &&
            displayOptions.map((opt, index) => {
              if (isAllOption(opt)) {
                return (
                  <li
                    key="all"
                    id={optionId(index)}
                    role="option"
                    aria-selected={highlightedIndex === index}
                    onMouseDown={(e) => {
                      e.preventDefault();
                      handleSelect(opt);
                    }}
                    onMouseEnter={() => setHighlightedIndex(index)}
                    className={`cursor-pointer px-3 py-2 text-sm ${
                      highlightedIndex === index
                        ? 'bg-blue-100 text-blue-900'
                        : 'text-gray-700 hover:bg-blue-50'
                    }`}
                  >
                    {opt.label}
                  </li>
                );
              }

              const optValue = getOptionValue(opt);
              const optLabel = getOptionLabel(opt);

              return (
                <li
                  key={optValue}
                  id={optionId(index)}
                  role="option"
                  aria-selected={highlightedIndex === index}
                  onMouseDown={(e) => {
                    e.preventDefault();
                    handleSelect(opt);
                  }}
                  onMouseEnter={() => setHighlightedIndex(index)}
                  className={`cursor-pointer truncate px-3 py-2 text-sm ${
                    highlightedIndex === index
                      ? 'bg-blue-100 text-blue-900'
                      : 'text-gray-700 hover:bg-blue-50'
                  }`}
                  title={optLabel}
                >
                  {optLabel}
                </li>
              );
            })}
        </ul>
      )}
    </div>
  );
}
