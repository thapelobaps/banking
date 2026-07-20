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
    <main className="flex min-h-dvh w-full max-w-[100vw] overflow-hidden font-inter">
      <Sidebar user={loggedIn} />
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <div className="root-layout">
          <Image src="/icons/logo.svg" width={30} height={30} alt="Kape App" />
          <MobileNav user={loggedIn} />
        </div>
        <div className="min-w-0 flex-1 overflow-hidden">{children}</div>
      </div>
    </main>
  );
}
