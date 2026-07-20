import Copy from './Copy';

const recipients = [
  { id: '11111111-1111-4111-8111-111111111111', label: 'FNB Demo •••• 9021' },
  { id: '22222222-2222-4222-8222-222222222222', label: 'Absa Demo •••• 4816' },
  { id: '33333333-3333-4333-8333-333333333333', label: 'Standard Bank Demo •••• 7754' },
];

const DemoRecipientReferences = () => (
  <article>
    <h3>Ready-to-use recipients</h3>
    <p className="mb-3 text-[10px] leading-4 text-[#8a756b]">
      Copy a fictional demo recipient reference and paste it into the transfer form.
    </p>
    <div className="grid gap-2">
      {recipients.map((recipient) => (
        <Copy key={recipient.id} title={recipient.id} label={recipient.label} />
      ))}
    </div>
  </article>
);

export default DemoRecipientReferences;
