'use client';
import Image from 'next/image';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import Footer from './Footer';
import { sidebarLinks } from '@/constants';
import { createMockBank } from '@/lib/actions/user.actions';
import { useState } from 'react';

import type { User,  } from '@/types';

type SidebarProps = {
  user: User;
};

const Sidebar = ({ user }: SidebarProps) => {
  const pathname = usePathname();
  const [isLoading, setIsLoading] = useState(false);

  const handleAddBank = async () => {
    setIsLoading(true);
    try {
      await createMockBank({ userId: user.userId, email: user.email });
      // Optionally, refresh the page or update state to reflect the new bank
      window.location.reload(); // Simple refresh; consider a state update for better UX
    } catch (error) {
      console.error('Error creating mock bank:', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <aside className="sidebar">
      <div className="flex flex-col size-full gap-4">
        <Link href="/" className="sidebar-logo">
          <Image src="/icons/logo.svg" alt="logo" width={180} height={28} />
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
                      className={`${isActive && 'brightness-200'}`}
                    />
                    {item.label}
                  </Link>
                </li>
              );
            })}
            <li className="sidebar-nav_element">
              <button
                onClick={handleAddBank}
                disabled={isLoading}
                className="flex gap-2 items-center p-4 text-gray-700 hover:bg-blue-100"
              >
                <Image src="/icons/plus.svg" alt="plus" width={24} height={24} />
                <span>{isLoading ? 'Adding Bank...' : 'Add Bank'}</span>
              </button>
            </li>
          </ul>
        </nav>

        <Footer user={user} />
      </div>
    </aside>
  );
};

export default Sidebar;