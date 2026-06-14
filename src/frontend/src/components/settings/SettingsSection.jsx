export default function SettingsSection({ title, description, children }) {
  return (
    <section className="tm-surface">
      <div className="border-b border-slate-100 px-6 py-5">
        <h2 className="text-base font-bold text-slate-900">{title}</h2>

        {description && (
          <p className="mt-1 text-sm leading-6 text-slate-500">
            {description}
          </p>
        )}
      </div>

      <div className="divide-y divide-slate-100">{children}</div>
    </section>
  );
}