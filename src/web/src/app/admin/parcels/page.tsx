import {
  dashboardContentMaxClass,
  dashboardGutterXClass,
  dashboardPageVerticalClass,
} from "@/lib/navigation/dashboard-layout";
import ParcelsPage from "@/components/parcels/parcels-page";

export default function AdminParcelsPage() {
  return (
    <main
      className={`${dashboardGutterXClass} ${dashboardPageVerticalClass}`}
    >
      <div className={dashboardContentMaxClass}>
        <ParcelsPage />
      </div>
    </main>
  );
}
