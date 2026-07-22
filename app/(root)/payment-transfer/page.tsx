import { redirect } from 'next/navigation';
import {
  BadgeCheck,
  Clock3,
  Scale,
  ShieldCheck,
  UserRoundCheck,
  WalletCards,
} from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import WalletTransferPanel from '@/components/wallet/WalletTransferPanel';
import { getWallet } from '@/lib/actions/wallet.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount } from '@/lib/utils';

const Transfer = async () => {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const wallet = await getWallet();

  return (
    <section className="kape-page wallet-send-page">
      <header className="kape-page-header">
        <HeaderBox
          title="Send money"
          subtext="Transfer money securely from your Kape Wallet to another verified Kape user."
        />
        <span className="wallet-send-status"><ShieldCheck size={14} /> Ledger protected</span>
      </header>

      {wallet ? (
        <div className="wallet-send-grid">
          <section className="wallet-send-form-card">
            <WalletTransferPanel
              wallet={wallet}
              senderName={`${loggedIn.firstName} ${loggedIn.lastName}`}
            />
          </section>

          <aside className="wallet-send-help">
            <article className="wallet-send-help__hero">
              <span><WalletCards size={18} /> Available Kape Wallet balance</span>
              <strong>{formatAmount(wallet.availableBalance)}</strong>
              <p>Transfers debit only the Kape Wallet. Connected bank balances remain unchanged unless you first top up the wallet.</p>
            </article>

            <article>
              <div><UserRoundCheck size={18} /></div>
              <div>
                <h3>Verified recipients</h3>
                <p>Enter a Kape user’s email address, South African mobile number, or user ID. The recipient is resolved before review.</p>
              </div>
            </article>

            <article>
              <div><Scale size={18} /></div>
              <div>
                <h3>Balanced posting</h3>
                <p>The sender and recipient wallet entries are created atomically through one balanced double-entry journal.</p>
              </div>
            </article>

            <article>
              <div><BadgeCheck size={18} /></div>
              <div>
                <h3>Safe retries</h3>
                <p>Every confirmed transfer uses an idempotency key, preventing accidental duplicate debits when a request is retried.</p>
              </div>
            </article>

            <article>
              <div><Clock3 size={18} /></div>
              <div>
                <h3>Immediate wallet activity</h3>
                <p>Completed transfers appear instantly as transfer out for the sender and transfer in for the recipient.</p>
              </div>
            </article>

            <div className="wallet-send-disclaimer">
              <ShieldCheck size={16} />
              <p>The current development environment posts to the Kape SQL Server wallet ledger. It does not use a live bank payment rail.</p>
            </div>
          </aside>
        </div>
      ) : (
        <div className="kape-empty-state">
          <strong>Kape Wallet unavailable</strong>
          <span>Confirm that the API is running and sign in again before sending money.</span>
        </div>
      )}
    </section>
  );
};

export default Transfer;
