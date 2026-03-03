import { AppShell } from '@/components/app-shell';
import { PagePlaceholder } from '@/components/common/page-placeholder';

type OrderDetailPageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function OrderDetailPage({ params }: OrderDetailPageProps) {
  const { id } = await params;

  return (
    <AppShell
      title={`Order ${id}`}
      subtitle="Order detail route scaffold."
    >
      <PagePlaceholder
        eyebrow="Coming soon"
        title={`Order detail for ${id} is not implemented yet`}
        description="This route will render the order summary, customer details, items, totals, shipping data, and status history once the Orders feature phase begins."
      />
    </AppShell>
  );
}
