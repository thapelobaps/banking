"use client";

import { useState } from "react";
import { Button } from "./ui/button";

type CopyProps = {
  title: string;
  label?: string;
};

const Copy = ({ title, label = "Reference" }: CopyProps) => {
  const [copyState, setCopyState] = useState<"idle" | "copied" | "failed">("idle");

  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(title);
      setCopyState("copied");
    } catch {
      setCopyState("failed");
    }

    window.setTimeout(() => {
      setCopyState("idle");
    }, 2000);
  };

  return (
    <Button
      type="button"
      className="mt-3 flex max-w-[320px] gap-4"
      variant="secondary"
      onClick={copyToClipboard}
      aria-label={`Copy ${label.toLowerCase()}`}
    >
      <span className="min-w-0 flex-1 text-left">
        <span className="block text-[11px] font-medium uppercase tracking-wide text-gray-500">
          {label}
        </span>
        <span className="block truncate text-xs font-medium text-black-2">
          {copyState === "copied"
            ? "Copied"
            : copyState === "failed"
              ? "Copy failed"
              : title}
        </span>
      </span>

      {copyState !== "copied" ? (
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="mr-2 size-4"
          aria-hidden="true"
        >
          <rect width="14" height="14" x="8" y="8" rx="2" ry="2" />
          <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2" />
        </svg>
      ) : (
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="mr-2 size-4"
          aria-hidden="true"
        >
          <polyline points="20 6 9 17 4 12" />
        </svg>
      )}
    </Button>
  );
};

export default Copy;
