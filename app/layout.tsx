export const dynamic = 'force-dynamic';

import type { Metadata, Viewport } from 'next';
import { Inter, IBM_Plex_Serif } from 'next/font/google';
import './globals.css';
import './kape-theme.css';
import './kape-responsive-fix.css';

const inter = Inter({ subsets: ['latin'], variable: '--font-inter' });
const ibmPlexSerif = IBM_Plex_Serif({
  subsets: ['latin'],
  weight: ['400', '700'],
  variable: '--font-ibm-plex-serif',
});

export const metadata: Metadata = {
  title: 'Kape App',
  description: 'A South African personal finance experience powered by ASP.NET Core and SQL Server.',
  icons: {
    icon: '/icons/logo.svg',
  },
};

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en-ZA">
      <body className={`${inter.variable} ${ibmPlexSerif.variable}`}>{children}</body>
    </html>
  );
}
