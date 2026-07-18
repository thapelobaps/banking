'use client';

import Image from 'next/image';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import Footer from './Footer';
import { sidebarLinks } from '@/constants';
import type { User } from '@/types';

type SidebarProps = {
  user: User;
};

const Sidebar = ({ user }: SidebarProps) => {
  const pathname = usePathname();

  return (
    <aside className="sidebar">
      <div className="flex size-full flex-col gap-4">
        <Link href="/" className="sidebar-logo" aria-label="Kape App home">
          <Image src="/icons/logo.svg" alt="Kape App" width={180} height={28} />
        </Link>

        <nav className="sidebar-nav">
          <ul className="sidebar-nav_elements">
            {sidebarLinks.map((item) => {
              const isActive = pathname === item.route || pathname.startsWith(`${item.route}/`);
              return (
                <li
                  key={item.route}
                  className={`sidebar-nav_element group ${isActive ? 'bg-blue-500 text-white' : 'text-gray-700'}`}
                >
                  <Link className="sidebar-link" href={item.route}>
                    <Image
                      src={item.imgURL}
                      alt={item.label}
                      width={24}
                      height={24}
                      className={isActive ? 'brightness-200' : undefined}
                    />
                    {item.label}
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>

        <div className="mx-4 rounded-lg border border-gray-200 bg-gray-50 p-3 text-xs text-gray-600">
          Demo accounts are managed by the Kape API and stored in SQL Server.
        </div>

        <Footer user={user} />
      </div>
    </aside>
  );
};

export default Sidebar;
