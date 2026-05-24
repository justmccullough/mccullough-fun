export default function Footer() {
  const year = new Date().getFullYear()
  return (
    <footer className="footer">
      <p className="footer-line">
        Made with <span aria-hidden="true">🧡</span> by the McCulloughs
      </p>
      <p className="footer-sub">
        mccullough.fun · {year} · always room for one more at the table
      </p>
    </footer>
  )
}
