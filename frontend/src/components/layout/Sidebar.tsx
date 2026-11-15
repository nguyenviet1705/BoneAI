import React from 'react';
import { NavLink } from 'react-router-dom';
import { 
  LayoutDashboard, 
  Scan, 
  Clock, 
  Scale, 
  Boxes, 
  HeartHandshake
} from 'lucide-react';
import { HandXRayIcon } from '../common/HandXRayIcon';

export const Sidebar: React.FC = () => {
  const navItems = [
    {
      label: 'Tổng quan',
      path: '/dashboard',
      icon: LayoutDashboard,
    },
    {
      label: 'Phân tích ảnh',
      path: '/analyze',
      icon: Scan,
    },
    {
      label: 'Lịch sử',
      path: '/history',
      icon: Clock,
    },
    {
      label: 'Đánh giá mô hình',
      path: '/evaluation',
      icon: Scale,
    },
    {
      label: 'Quản lý mô hình',
      path: '/models',
      icon: Boxes,
    },
  ];

  return (
    <aside className="w-64 bg-brand-sidebar text-white flex flex-col justify-between shrink-0 h-screen sticky top-0 border-r border-slate-800/40 select-none">
      {/* Top: Logo & Navigation */}
      <div>
        {/* Brand Header */}
        <div className="p-6 pb-8 flex items-center gap-3">
          <HandXRayIcon size={42} />
          <div>
            <h1 className="font-bold text-xl tracking-tight text-white flex items-center gap-1.5">
              BoneAI
            </h1>
            <p className="text-[11px] text-blue-200/60 font-medium">
              AI for Better Care
            </p>
          </div>
        </div>

        {/* Menu list */}
        <nav className="px-3 space-y-1.5">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3.5 py-3 rounded-xl text-sm font-medium transition-all duration-200 ${
                    isActive
                      ? 'bg-blue-600 text-white shadow-md shadow-blue-600/30 font-semibold'
                      : 'text-slate-300 hover:text-white hover:bg-white/5'
                  }`
                }
              >
                {({ isActive }) => (
                  <>
                    <Icon className={`w-5 h-5 transition-transform duration-200 ${isActive ? 'scale-105 text-white' : 'text-slate-400'}`} />
                    <span>{item.label}</span>
                  </>
                )}
              </NavLink>
            );
          })}
        </nav>
      </div>

      {/* Bottom Disclaimer Footer */}
      <div className="p-4 mx-3 mb-4 rounded-xl bg-white/[0.04] border border-white/[0.06] text-center">
        <div className="flex justify-center mb-1.5 text-rose-400">
          <HeartHandshake className="w-5 h-5" />
        </div>
        <p className="text-[12px] font-semibold text-slate-200">
          Hỗ trợ chẩn đoán
        </p>
        <p className="text-[11px] text-slate-400 mt-0.5">
          Không thay thế bác sĩ
        </p>
      </div>
    </aside>
  );
};
