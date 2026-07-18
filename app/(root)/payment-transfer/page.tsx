import HeaderBox from '@/components/HeaderBox'
import PaymentTransferForm from '@/components/PaymentTransferForm'
import { getAccounts } from '@/lib/actions/bank.actions'
import { getLoggedInUser } from '@/lib/actions/user.actions'

const Transfer = async () => {
  const loggedIn = await getLoggedInUser()

  if (!loggedIn) {
    return null
  }

  const accounts = await getAccounts({
    userId: loggedIn.userId,
  })

  const accountsData = accounts?.data ?? []

  return (
    <section className="payment-transfer">
      <HeaderBox
        title="Demo transfer"
        subtext="Simulate a transfer between Kape App demo accounts. No real money is moved."
      />

      <section className="size-full pt-5">
        {accountsData.length > 0 ? (
          <PaymentTransferForm accounts={accountsData} />
        ) : (
          <div className="rounded-lg border border-gray-200 bg-white p-6 text-sm text-gray-700">
            Add a demo account before simulating a transfer.
          </div>
        )}
      </section>
    </section>
  )
}

export default Transfer
