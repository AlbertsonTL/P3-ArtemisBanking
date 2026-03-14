namespace ArtemisBanking.Infrastructure.Services;

public static class EmailTemplates
{
    public static string ActivateAccount(string fullName, string activationLink) => $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background:#f4f4f4; margin:0; padding:0; }}
    .container {{ max-width:600px; margin:40px auto; background:#ffffff; border-radius:8px;
                  box-shadow:0 2px 8px rgba(0,0,0,.1); overflow:hidden; }}
    .header {{ background:#1a3c5e; padding:30px; text-align:center; }}
    .header h1 {{ color:#ffffff; margin:0; font-size:24px; }}
    .body {{ padding:30px; color:#333333; }}
    .body p {{ line-height:1.6; }}
    .btn {{ display:inline-block; margin:20px 0; padding:14px 28px;
             background:#1a3c5e; color:#ffffff !important; text-decoration:none;
             border-radius:5px; font-size:16px; }}
    .footer {{ background:#f0f0f0; padding:15px; text-align:center;
                font-size:12px; color:#888888; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'><h1>Artemis Banking</h1></div>
    <div class='body'>
      <p>Hola <strong>{fullName}</strong>,</p>
      <p>Tu cuenta ha sido creada exitosamente. Para activarla y comenzar a usar el sistema,
         haz clic en el botón a continuación:</p>
      <a href='{activationLink}' class='btn'>Activar mi cuenta</a>
      <p>Si no solicitaste esta cuenta, ignora este correo.</p>
      <p>Atentamente,<br><strong>Equipo Artemis Banking</strong></p>
    </div>
    <div class='footer'>© 2025 Artemis Banking. Todos los derechos reservados.</div>
  </div>
</body>
</html>";

    public static string ResetPassword(string fullName, string resetLink) => $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background:#f4f4f4; margin:0; padding:0; }}
    .container {{ max-width:600px; margin:40px auto; background:#ffffff; border-radius:8px;
                  box-shadow:0 2px 8px rgba(0,0,0,.1); overflow:hidden; }}
    .header {{ background:#c0392b; padding:30px; text-align:center; }}
    .header h1 {{ color:#ffffff; margin:0; font-size:24px; }}
    .body {{ padding:30px; color:#333333; }}
    .body p {{ line-height:1.6; }}
    .btn {{ display:inline-block; margin:20px 0; padding:14px 28px;
             background:#c0392b; color:#ffffff !important; text-decoration:none;
             border-radius:5px; font-size:16px; }}
    .footer {{ background:#f0f0f0; padding:15px; text-align:center;
                font-size:12px; color:#888888; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'><h1>Artemis Banking</h1></div>
    <div class='body'>
      <p>Hola <strong>{fullName}</strong>,</p>
      <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta.
         Haz clic en el botón a continuación para continuar:</p>
      <a href='{resetLink}' class='btn'>Restablecer contraseña</a>
      <p><strong>Este enlace expira en 2 horas.</strong></p>
      <p>Si no solicitaste este cambio, tu cuenta ha sido desactivada temporalmente
         por seguridad. Contáctanos para reactivarla.</p>
      <p>Atentamente,<br><strong>Equipo Artemis Banking</strong></p>
    </div>
    <div class='footer'>© 2025 Artemis Banking. Todos los derechos reservados.</div>
  </div>
</body>
</html>";

    public static string ResetPasswordApi(string fullName, string token) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background:#f4f4f4; }}
    .container {{ max-width:600px; margin:40px auto; background:#fff;
                  border-radius:8px; overflow:hidden; }}
    .header {{ background:#c0392b; padding:30px; text-align:center; }}
    .header h1 {{ color:#fff; margin:0; }}
    .body {{ padding:30px; color:#333; line-height:1.6; }}
    .token-box {{ background:#f8f8f8; border:1px solid #ddd; border-radius:5px;
                   padding:15px; font-family:monospace; font-size:14px;
                   word-break:break-all; margin:15px 0; }}
    .footer {{ background:#f0f0f0; padding:15px; text-align:center;
                font-size:12px; color:#888; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'><h1>Artemis Banking API</h1></div>
    <div class='body'>
      <p>Hola <strong>{fullName}</strong>,</p>
      <p>Tu token para restablecer la contraseña es:</p>
      <div class='token-box'>{token}</div>
      <p>Usa este token en el endpoint <code>POST /account/reset-password</code>.</p>
      <p>Expira en 2 horas.</p>
    </div>
    <div class='footer'>© 2025 Artemis Banking.</div>
  </div>
</body>
</html>";

    /// <summary>
    /// Correo enviado al cliente cuando el admin actualiza la tasa de interés de su préstamo.
    /// Cubre Issue #21 - Editar tasa de interés préstamo + recalcular cuotas + email.
    /// </summary>
    public static string LoanRateUpdated(
        string fullName,
        string loanNumber,
        decimal nuevaTasa,
        decimal nuevaCuota,
        DateTime proximaFecha) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'>
  <style>
    body {{ font-family:Arial,sans-serif; background:#f4f4f4; }}
    .container {{ max-width:600px; margin:40px auto; background:#fff;
                  border-radius:8px; overflow:hidden; }}
    .header {{ background:#e67e22; padding:30px; text-align:center; }}
    .header h1 {{ color:#fff; margin:0; font-size:22px; }}
    .body {{ padding:30px; color:#333; line-height:1.6; }}
    table {{ width:100%; border-collapse:collapse; margin:15px 0; }}
    td {{ padding:10px; border-bottom:1px solid #eee; }}
    td:first-child {{ font-weight:bold; color:#555; width:55%; }}
    .notice {{ background:#fff8e1; border-left:4px solid #e67e22;
               padding:12px 16px; margin:16px 0; border-radius:4px; }}
    .footer {{ background:#f0f0f0; padding:15px; text-align:center;
                font-size:12px; color:#888; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'><h1>Actualización de Tasa de Interés 📋</h1></div>
    <div class='body'>
      <p>Hola <strong>{fullName}</strong>,</p>
      <p>Te informamos que la tasa de interés de tu préstamo ha sido actualizada:</p>
      <table>
        <tr><td>Número de préstamo</td><td>{loanNumber}</td></tr>
        <tr><td>Nueva tasa de interés anual</td><td>{nuevaTasa:N2}%</td></tr>
        <tr><td>Nuevo monto de cuota mensual</td><td>RD$ {nuevaCuota:N2}</td></tr>
        <tr><td>Aplica desde</td><td>{proximaFecha:dd/MM/yyyy}</td></tr>
      </table>
      <div class='notice'>
        Las cuotas ya pagadas no se ven afectadas. El nuevo monto aplica
        únicamente a las cuotas futuras pendientes.
      </div>
      <p>Si tienes alguna pregunta, contáctanos.</p>
      <p>Atentamente,<br><strong>Equipo Artemis Banking</strong></p>
    </div>
    <div class='footer'>© 2025 Artemis Banking. Todos los derechos reservados.</div>
  </div>
</body>
</html>";

    public static string LoanApproved(string fullName, decimal amount,
        int termMonths, decimal rate, decimal monthlyPayment) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'>
  <style>
    body {{ font-family:Arial,sans-serif; background:#f4f4f4; }}
    .container {{ max-width:600px; margin:40px auto; background:#fff;
                  border-radius:8px; overflow:hidden; }}
    .header {{ background:#27ae60; padding:30px; text-align:center; }}
    .header h1 {{ color:#fff; margin:0; }}
    .body {{ padding:30px; color:#333; }}
    table {{ width:100%; border-collapse:collapse; margin:15px 0; }}
    td {{ padding:10px; border-bottom:1px solid #eee; }}
    td:first-child {{ font-weight:bold; color:#555; width:50%; }}
    .footer {{ background:#f0f0f0; padding:15px; text-align:center;
                font-size:12px; color:#888; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'><h1>Préstamo Aprobado ✓</h1></div>
    <div class='body'>
      <p>Hola <strong>{fullName}</strong>, tu préstamo ha sido aprobado:</p>
      <table>
        <tr><td>Monto aprobado</td><td>RD$ {amount:N2}</td></tr>
        <tr><td>Plazo</td><td>{termMonths} meses</td></tr>
        <tr><td>Tasa de interés anual</td><td>{rate}%</td></tr>
        <tr><td>Cuota mensual</td><td>RD$ {monthlyPayment:N2}</td></tr>
      </table>
      <p>El monto ha sido acreditado a tu cuenta principal.</p>
    </div>
    <div class='footer'>© 2025 Artemis Banking.</div>
  </div>
</body>
</html>";
}