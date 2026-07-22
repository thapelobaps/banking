'use client';

import { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { Landmark, Link2, Loader2, RefreshCw, Unlink } from 'lucide-react';

import { connectBank } from '@/lib/actions/bank-connection.actions';
import {
  disconnectBankConnection,
  syncBankConnection,
} from '@/lib/actions/wallet.actions';
import { formatDateTime } from '@/lib/utils';
import type { BankConnection } from '@/types/wallet';

type BankConnectionPanelProps = {
  connections: BankConnection[];
  providerId: 'demo' | 'stitch';
};

const institutions = [
  { id: 'capitec', name: 'Capitec' },
  { id: 'standard-bank', name: 'Standard Bank' },
  { id: 'fnb', name: 'FNB' },
  { id: 'absa', name: 'Absa' },
  { id: 'nedbank', name: 'Nedbank' },
];

export default function BankConnectionPanel({ connections, providerId }: BankConnectionPanelProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [institutionId, setInstitutionId] = useState('capitec');
  const [message, setMessage] = useState<string | null>(null);
  const [isError, setIsError] = useState(false);
  const [activeConnectionId, setActiveConnectionId] = useState<string | null>(null);
  const usesStitch = providerId === 'stitch';

  const connect = () => {
    setActiveConnectionId('new');
    startTransition(async () => {
      const result = await connectBank(institutionId);
      if (!result.ok) {
        setIsError(true);
        setMessage(result.error);
        setActiveConnectionId(null);
        return;
      }

      if (result.data.mode === 'redirect') {
        window.location.assign(result.data.linkUrl);
        return;
      }

      setIsError(false);
      setMessage(`${result.data.connection.institutionName} connected and synchronised.`);
      setActiveConnectionId(null);
      router.refresh();
    });
  };

  const sync = (connectionId: string) => {
    setActiveConnectionId(connectionId);
    startTransition(async () => {
      const result = await syncBankConnection(connectionId);
      if (!result.ok) {
        setIsError(true);
        setMessage(result.error);
        setActiveConnectionId(null);
        return;
      }

      setIsError(false);
      setMessage(
        `Sync complete: ${result.data.linkedAccounts} accounts, ${result.data.importedTransactions} new transactions, ${result.data.importedDebitOrders} new debit orders.`
      );
      setActiveConnectionId(null);
      router.refresh();
    });
  };

  const disconnect = (connectionId: string, institutionName: string) => {
    setActiveConnectionId(connectionId);
    startTransition(async () => {
      const result = await disconnectBankConnection(connectionId);
      if (!result.ok) {
        setIsError(true);
        setMessage(result.error);
        setActiveConnectionId(null);
        return;
      }

      setIsError(false);
      setMessage(`${institutionName} was disconnected.`);
      setActiveConnectionId(null);
      router.refresh();
    });
  };

  return (
    <section className="wallet-panel bank-connection-panel">
      <div className="wallet-panel__heading">
        <span className="wallet-eyebrow">Consent-based access</span>
        <h2>Connect another bank</h2>
        <p>
          {usesStitch
            ? 'Continue through Stitch’s secure consent screen. Kape never asks for or stores your banking password.'
            : 'The demo aggregator is active. Switch configuration to Stitch when sandbox credentials are available.'}
        </p>
      </div>

      <div className="bank-connect-form">
        <label className="wallet-field">
          <span>Institution</span>
          <select value={institutionId} onChange={(event) => setInstitutionId(event.target.value)}>
            {institutions.map((institution) => (
              <option key={institution.id} value={institution.id}>{institution.name}</option>
            ))}
          </select>
        </label>
        <button type="button" className="wallet-button wallet-button--primary" onClick={connect} disabled={isPending}>
          {isPending && activeConnectionId === 'new' ? <Loader2 size={16} className="animate-spin" /> : <Link2 size={16} />}
          {usesStitch ? 'Connect with Stitch' : 'Connect demo bank'}
        </button>
      </div>

      <div className="bank-connection-list">
        {connections.length ? (
          connections.map((connection) => (
            <article key={connection.id} className="bank-connection-row">
              <div className="bank-connection-row__icon">
                <Landmark size={19} />
              </div>
              <div className="bank-connection-row__details">
                <div>
                  <strong>{connection.institutionName}</strong>
                  <span className={`wallet-status wallet-status--${connection.status}`}>{connection.status}</span>
                </div>
                <p>
                  Last synced {connection.lastSyncedAt ? formatDateTime(connection.lastSyncedAt).dateTime : 'not yet'}
                </p>
              </div>
              <div className="bank-connection-row__actions">
                <button
                  type="button"
                  title="Synchronise connection"
                  onClick={() => sync(connection.id)}
                  disabled={isPending}
                >
                  {isPending && activeConnectionId === connection.id ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
                </button>
                <button
                  type="button"
                  title="Disconnect bank"
                  className="is-danger"
                  onClick={() => disconnect(connection.id, connection.institutionName)}
                  disabled={isPending}
                >
                  <Unlink size={16} />
                </button>
              </div>
            </article>
          ))
        ) : (
          <div className="wallet-empty-inline">
            <Landmark size={20} />
            <div>
              <strong>No linked institutions</strong>
              <span>{usesStitch ? 'Connect through Stitch to import consented balances and activity.' : 'Connect a demo bank to import balances, transactions and debit orders.'}</span>
            </div>
          </div>
        )}
      </div>

      {message ? (
        <p className={`wallet-feedback ${isError ? 'is-error' : 'is-success'}`} role="status">
          {message}
        </p>
      ) : null}
    </section>
  );
}
