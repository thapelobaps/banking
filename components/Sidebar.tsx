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
    <aside className="kape-sidebar">
      <div className="kape-sidebar__inner">
        <Link href="/" className="kape-brand" aria-label="Kape App home">
          <span className="kape-brand__mark">
            <Image src="/icons/logo.svg" alt="" width={22} height={22} className="brightness-0 invert" />
          </span>
          <span className="kape-brand__copy">
            <strong>Kape</strong>
            <small>Money, simplified</small>
          </span>
        </Link>

        <nav className="kape-sidebar__nav" aria-label="Main navigation">
          <ul>
            {sidebarLinks.map((item) => {
              const isActive = pathname === item.route || pathname.startsWith(`${item.route}/`);
              return (
                <li key={item.route}>
                  <Link
                    className={`kape-nav-link ${isActive ? 'is-active' : ''}`}
                    href={item.route}
                    title={item.label}
                  >
                    <Image
                      src={item.imgURL}
                      alt=""
                      width={18}
                      height={18}
                      className={isActive ? 'brightness-0 invert' : 'opacity-70'}
                    />
                    <span>{item.label}</span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>

        <div className="kape-sidebar__footer">
          <Footer user={user} />
        </div>
      </div>
    </aside>
  );
};

export default Sidebar;
