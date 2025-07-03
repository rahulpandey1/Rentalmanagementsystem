import React, { useState, useEffect } from 'react';
import { dashboardAPI } from '../services/api';
import { Dashboard as DashboardType } from '../types';

const StatCard: React.FC<{ title: string; value: string | number; icon: string; color: string }> = ({ 
  title, value, icon, color 
}) => (
  <div className="bg-white overflow-hidden shadow rounded-lg">
    <div className="p-5">
      <div className="flex items-center">
        <div className="flex-shrink-0">
          <div className={`p-3 rounded-md ${color}`}>
            <span className="text-2xl">{icon}</span>
          </div>
        </div>
        <div className="ml-5 w-0 flex-1">
          <dl>
            <dt className="text-sm font-medium text-gray-500 truncate">{title}</dt>
            <dd className="text-lg font-medium text-gray-900">{value}</dd>
          </dl>
        </div>
      </div>
    </div>
  </div>
);

const Dashboard: React.FC = () => {
  const [dashboard, setDashboard] = useState<DashboardType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        setLoading(true);
        const response = await dashboardAPI.getDashboard();
        setDashboard(response.data);
      } catch (err) {
        setError('Failed to load dashboard data');
        console.error('Dashboard error:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
  }, []);

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-md p-4">
        <div className="flex">
          <div className="flex-shrink-0">
            <span className="text-red-400">⚠️</span>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-red-800">Error</h3>
            <div className="mt-2 text-sm text-red-700">
              <p>{error}</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!dashboard) return null;

  const occupancyRate = dashboard.totalRooms > 0 
    ? Math.round((dashboard.occupiedRooms / dashboard.totalRooms) * 100) 
    : 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Dashboard</h1>
        <p className="mt-1 text-sm text-gray-600">
          Overview of your rental property management system
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Total Rooms"
          value={dashboard.totalRooms}
          icon="🏠"
          color="bg-blue-100"
        />
        <StatCard
          title="Occupied Rooms"
          value={`${dashboard.occupiedRooms} (${occupancyRate}%)`}
          icon="👥"
          color="bg-green-100"
        />
        <StatCard
          title="Available Rooms"
          value={dashboard.availableRooms}
          icon="🆓"
          color="bg-yellow-100"
        />
        <StatCard
          title="Maintenance Rooms"
          value={dashboard.maintenanceRooms}
          icon="🔧"
          color="bg-red-100"
        />
      </div>

      {/* Revenue Stats */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-3">
        <StatCard
          title="Rent Collected (This Month)"
          value={`₹${dashboard.totalRentCollectedThisMonth.toLocaleString()}`}
          icon="💰"
          color="bg-green-100"
        />
        <StatCard
          title="Electricity Collected (This Month)"
          value={`₹${dashboard.totalElectricityCollectedThisMonth.toLocaleString()}`}
          icon="⚡"
          color="bg-yellow-100"
        />
        <StatCard
          title="Pending Payments"
          value={`₹${dashboard.pendingPayments.toLocaleString()}`}
          icon="⏰"
          color="bg-red-100"
        />
      </div>

      {/* Alerts */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
        <StatCard
          title="Pending Maintenance"
          value={dashboard.pendingMaintenanceRequests}
          icon="🔧"
          color="bg-orange-100"
        />
        <StatCard
          title="Upcoming Move-outs"
          value={dashboard.upcomingMoveOutsCount}
          icon="📦"
          color="bg-purple-100"
        />
      </div>

      {/* Recent Activity */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Recent Payments */}
        <div className="bg-white shadow rounded-lg">
          <div className="px-4 py-5 sm:p-6">
            <h3 className="text-lg leading-6 font-medium text-gray-900 mb-4">
              Recent Payments
            </h3>
            <div className="space-y-3">
              {dashboard.recentPayments.length > 0 ? (
                dashboard.recentPayments.slice(0, 5).map((payment, index) => (
                  <div key={index} className="flex items-center justify-between py-2 border-b border-gray-200">
                    <div>
                      <p className="text-sm font-medium text-gray-900">
                        {payment.tenantName} - {payment.roomNumber}
                      </p>
                      <p className="text-sm text-gray-500">{payment.paymentType}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-medium text-green-600">
                        ₹{payment.amount.toLocaleString()}
                      </p>
                      <p className="text-xs text-gray-500">
                        {new Date(payment.paymentDate).toLocaleDateString()}
                      </p>
                    </div>
                  </div>
                ))
              ) : (
                <p className="text-sm text-gray-500">No recent payments</p>
              )}
            </div>
          </div>
        </div>

        {/* Upcoming Move-outs */}
        <div className="bg-white shadow rounded-lg">
          <div className="px-4 py-5 sm:p-6">
            <h3 className="text-lg leading-6 font-medium text-gray-900 mb-4">
              Upcoming Move-outs
            </h3>
            <div className="space-y-3">
              {dashboard.upcomingMoveOuts.length > 0 ? (
                dashboard.upcomingMoveOuts.map((moveOut, index) => (
                  <div key={index} className="flex items-center justify-between py-2 border-b border-gray-200">
                    <div>
                      <p className="text-sm font-medium text-gray-900">
                        {moveOut.tenantName} - {moveOut.roomNumber}
                      </p>
                      <p className="text-sm text-gray-500">
                        {new Date(moveOut.moveOutDate).toLocaleDateString()}
                      </p>
                    </div>
                    <div className="text-right">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        moveOut.daysRemaining <= 7 
                          ? 'bg-red-100 text-red-800' 
                          : 'bg-yellow-100 text-yellow-800'
                      }`}>
                        {moveOut.daysRemaining} days
                      </span>
                    </div>
                  </div>
                ))
              ) : (
                <p className="text-sm text-gray-500">No upcoming move-outs</p>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;