export default function Failure({ message, retry }) {
  return <p className="alert error" role="alert">{message} <button type="button" className="service-link" onClick={retry}>Újrapróbálás</button></p>
}
