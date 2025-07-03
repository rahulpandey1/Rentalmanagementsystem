import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useLocation } from 'react-router-dom';
import Dashboard from './components/Dashboard';
import Tenants from './components/Tenants';
import Rooms from './components/Rooms';
import Payments from './components/Payments';
import Electricity from './components/Electricity';
import Maintenance from './components/Maintenance';
import './App.css';

const NavigationBar: React.FC = () => {
  const location = useLocation();

  const navItems = [
    { path: '/', label: 'Dashboard', icon: '📊' },
    { path: '/tenants', label: 'Tenants', icon: '👥' },
    { path: '/rooms', label: 'Rooms', icon: '🏠' },
    { path: '/payments', label: 'Payments', icon: '💰' },
    { path: '/electricity', label: 'Electricity', icon: '⚡' },
    { path: '/maintenance', label: 'Maintenance', icon: '🔧' },
  ];

  return (
    <nav className="bg-white shadow-lg">
      <div className="max-w-7xl mx-auto px-4">
        <div className="flex justify-between h-16">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <h1 className="text-xl font-bold text-gray-800">Rental Property Management</h1>
            </div>
          </div>
          <div className="flex space-x-8">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className={`inline-flex items-center px-1 pt-1 text-sm font-medium ${
                  location.pathname === item.path
                    ? 'border-b-2 border-blue-500 text-gray-900'
                    : 'text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                <span className="mr-2">{item.icon}</span>
                {item.label}
              </Link>
            ))}
          </div>
        </div>
      </div>
    </nav>
  );
};

const App: React.FC = () => {
  return (
    <Router>
      <div className="min-h-screen bg-gray-50">
        <NavigationBar />
        <main className="max-w-7xl mx-auto py-6 px-4 sm:px-6 lg:px-8">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/tenants" element={<Tenants />} />
            <Route path="/rooms" element={<Rooms />} />
            <Route path="/payments" element={<Payments />} />
            <Route path="/electricity" element={<Electricity />} />
            <Route path="/maintenance" element={<Maintenance />} />
          </Routes>
        </main>
      </div>
    </Router>
  );
};

export default App;
