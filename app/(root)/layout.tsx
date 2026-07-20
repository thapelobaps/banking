import MobileNav from '@/components/MobileNav';
import Sidebar from '@/components/Sidebar';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import Image from 'next/image';
import { redirect } from 'next/navigation';

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const loggedIn = await getLoggedInUser();

  if (!loggedIn) redirect('/sign-in');

  return (
    <main className="kape-shell font-inter">
      <Sidebar user={loggedIn} />
      <div className="kape-main">
        <div className="kape-mobile-header">
          <Image src="/icons/logo.svg" width={28} height={28} alt="Kape App" />
          <MobileNav user={loggedIn} />
        </div>
        <div className="kape-viewport">{children}</div>
      </div>
    </main>
  );
}
