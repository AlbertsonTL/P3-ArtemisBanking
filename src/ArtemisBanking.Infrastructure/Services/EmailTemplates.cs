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

  public static string TransactionNotification(string fullName, string concept, decimal amount, string target) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'>
  <style>
    body {{ font-family:Arial,sans-serif; background:#f4f4f4; }}
    .container {{ max-width:600px; margin:40px auto; background:#fff;
                  border-radius:8px; overflow:hidden; }}
    .header {{ background:#1a3c5e; padding:30px; text-align:center; }}
    .header h1 {{ color:#fff; margin:0; font-size:22px; }}
    .body {{ padding:30px; color:#333; line-height:1.6; }}
    .amount-box {{ text-align:center; padding:20px; background:#f8fafc; border-radius:10px; margin:20px 0; }}
    .amount {{ font-size:32px; font-weight:bold; color:#1a3c5e; }}
    .details {{ width:100%; border-collapse:collapse; margin:15px 0; }}
    .details td {{ padding:10px; border-bottom:1px solid #eee; }}
    .details td:first-child {{ font-weight:bold; color:#555; width:40%; }}
    .footer {{ background:#f0f0f0; padding:15px; text-align:center;
                font-size:12px; color:#888; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'><h1>Notificación de Transacción 🔔</h1></div>
    <div class='body'>
      <p>Hola <strong>{fullName}</strong>,</p>
      <p>Se ha procesado una transacción en tu cuenta:</p>
      
      <div class='amount-box'>
        <div class='amount'>RD$ {amount:N2}</div>
        <div class='text-muted small'>{concept}</div>
      </div>

      <table class='details'>
        <tr><td>Concepto</td><td>{concept}</td></tr>
        <tr><td>Destino/Origen</td><td>{target}</td></tr>
        <tr><td>Fecha</td><td>{DateTime.UtcNow:dd/MM/yyyy HH:mm}</td></tr>
      </table>

      <p>Si no reconoces esta actividad, por favor contáctanos de inmediato.</p>
      <p>Atentamente,<br><strong>Equipo Artemis Banking</strong></p>
    </div>
    <div class='footer'>© 2025 Artemis Banking. Todos los derechos reservados.</div>
  </div>
</body>
</html>";

  public static string DepositNotification(string clientName, decimal amount, string accountNumber, DateTime dateTime)
  {
    return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background: #f9f9f9; border-radius: 8px; }}
                .header {{ background: #28a745; color: white; padding: 20px; border-radius: 8px 8px 0 0; text-align: center; }}
                .content {{ background: white; padding: 20px; border-radius: 0 0 8px 8px; }}
                .amount {{ font-size: 24px; font-weight: bold; color: #28a745; margin: 10px 0; }}
                .details {{ background: #f0f0f0; padding: 10px; border-radius: 4px; margin: 10px 0; }}
                .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h2>Depósito Realizado</h2>
                </div>
                <div class='content'>
                    <p>Hola {clientName},</p>
                    <p>Te confirmamos que hemos recibido un depósito en tu cuenta.</p>
                    
                    <div class='details'>
                        <p><strong>Número de Cuenta:</strong> {accountNumber}</p>
                        <p><strong>Monto Depositado:</strong></p>
                        <div class='amount'>RD$ {amount:N2}</div>
                        <p><strong>Fecha y Hora:</strong> {dateTime:dd/MM/yyyy HH:mm:ss}</p>
                    </div>
                    
                    <p>Si tienes preguntas sobre esta transacción, no dudes en contactarnos.</p>
                    <p>Saludos,<br/><strong>Artemis Banking</strong></p>
                </div>
                <div class='footer'>
                    <p>Este es un mensaje automático. No responder a este email.</p>
                </div>
            </div>
        </body>
        </html>";
  }

  public static string WithdrawalNotification(string clientName, decimal amount, string accountNumber, DateTime dateTime)
  {
    return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background: #f9f9f9; border-radius: 8px; }}
                .header {{ background: #dc3545; color: white; padding: 20px; border-radius: 8px 8px 0 0; text-align: center; }}
                .content {{ background: white; padding: 20px; border-radius: 0 0 8px 8px; }}
                .amount {{ font-size: 24px; font-weight: bold; color: #dc3545; margin: 10px 0; }}
                .details {{ background: #f0f0f0; padding: 10px; border-radius: 4px; margin: 10px 0; }}
                .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h2>Retiro Realizado</h2>
                </div>
                <div class='content'>
                    <p>Hola {clientName},</p>
                    <p>Te confirmamos que hemos procesado un retiro de tu cuenta.</p>
                    
                    <div class='details'>
                        <p><strong>Número de Cuenta:</strong> {accountNumber}</p>
                        <p><strong>Monto Retirado:</strong></p>
                        <div class='amount'>RD$ {amount:N2}</div>
                        <p><strong>Fecha y Hora:</strong> {dateTime:dd/MM/yyyy HH:mm:ss}</p>
                    </div>
                    
                    <p>Si tienes preguntas sobre esta transacción, no dudes en contactarnos.</p>
                    <p>Saludos,<br/><strong>Artemis Banking</strong></p>
                </div>
                <div class='footer'>
                    <p>Este es un mensaje automático. No responder a este email.</p>
                </div>
            </div>
        </body>
        </html>";
  }

  public static string CreditCardPaymentNotification(string clientName, decimal amountPaid, string accountNumber, string last4CardDigits, DateTime dateTime)
  {
    return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Pago a Tarjeta de Crédito</title>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 15px; border-radius: 5px; text-align: center; margin-bottom: 20px; }}
        .header h2 {{ margin: 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #ddd; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .label {{ font-weight: bold; color: #666; }}
        .value {{ color: #333; }}
        .amount {{ font-size: 24px; color: #007bff; font-weight: bold; }}
        .footer {{ text-align: center; padding-top: 20px; color: #999; font-size: 12px; }}
        .success-icon {{ color: #28a745; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2><span class='success-icon'>✓</span> Pago a Tarjeta de Crédito Realizado</h2>
        </div>
        <div class='content'>
            <p>Estimado/a <strong>{clientName}</strong>,</p>
            <p>Confirmamos que tu pago a tarjeta de crédito ha sido procesado exitosamente.</p>
            <div class='detail-row'>
                <span class='label'>Tarjeta:</span>
                <span class='value'>**** **** **** {last4CardDigits}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Cuenta Origen:</span>
                <span class='value'>{accountNumber}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Monto Pagado:</span>
                <span class='value amount'>RD$ {amountPaid:N2}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Fecha y Hora:</span>
                <span class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</span>
            </div>
            <p style='margin-top: 20px; color: #666; font-size: 14px;'>Si tienes preguntas sobre esta transacción, por favor contacta con nuestro servicio al cliente.</p>
        </div>
        <div class='footer'>
            <p>Artemis Banking © 2026 - Banco Digital Seguro</p>
            <p>Este es un correo automático, por favor no responder.</p>
        </div>
    </div>
</body>
</html>";
  }

  public static string LoanPaymentNotification(string clientName, decimal amountPaid, string last4AccountDigits, string loanNumber, DateTime dateTime, decimal excessAmount = 0)
  {
    string excessMessage = excessAmount > 0
        ? $"<div class='detail-row'><span class='label'>Excedente Retornado:</span><span class='value'>RD$ {excessAmount:N2}</span></div>"
        : "";

    return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Pago a Préstamo</title>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #17a2b8; color: white; padding: 15px; border-radius: 5px; text-align: center; margin-bottom: 20px; }}
        .header h2 {{ margin: 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #ddd; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .label {{ font-weight: bold; color: #666; }}
        .value {{ color: #333; }}
        .amount {{ font-size: 24px; color: #17a2b8; font-weight: bold; }}
        .footer {{ text-align: center; padding-top: 20px; color: #999; font-size: 12px; }}
        .success-icon {{ color: #28a745; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2><span class='success-icon'>✓</span> Pago a Préstamo Realizado</h2>
        </div>
        <div class='content'>
            <p>Estimado/a <strong>{clientName}</strong>,</p>
            <p>Confirmamos que tu pago al préstamo ha sido procesado exitosamente de forma secuencial.</p>
            <div class='detail-row'>
                <span class='label'>Préstamo #:</span>
                <span class='value'>{loanNumber}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Cuenta Origen:</span>
                <span class='value'>**** **** **** {last4AccountDigits}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Monto Pagado:</span>
                <span class='value amount'>RD$ {amountPaid:N2}</span>
            </div>
            {excessMessage}
            <div class='detail-row'>
                <span class='label'>Fecha y Hora:</span>
                <span class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</span>
            </div>
            <p style='margin-top: 20px; color: #666; font-size: 14px;'>Tu pago ha sido aplicado de acuerdo al cronograma de cuotas de tu préstamo.</p>
        </div>
        <div class='footer'>
            <p>Artemis Banking © 2026 - Banco Digital Seguro</p>
            <p>Este es un correo automático, por favor no responder.</p>
        </div>
    </div>
</body>
</html>";
  }

  public static string ThirdPartyTransferSentNotification(string clientName, decimal amount, string last4DestinationDigits, DateTime dateTime)
  {
    return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Transacción Enviada</title>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 15px; border-radius: 5px; text-align: center; margin-bottom: 20px; }}
        .header h2 {{ margin: 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #ddd; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .label {{ font-weight: bold; color: #666; }}
        .value {{ color: #333; }}
        .amount {{ font-size: 24px; color: #dc3545; font-weight: bold; }}
        .footer {{ text-align: center; padding-top: 20px; color: #999; font-size: 12px; }}
        .success-icon {{ color: #28a745; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2><span class='success-icon'>✓</span> Transacción Enviada</h2>
        </div>
        <div class='content'>
            <p>Estimado/a <strong>{clientName}</strong>,</p>
            <p>Confirmamos que tu transferencia ha sido enviada exitosamente.</p>
            <div class='detail-row'>
                <span class='label'>Cuenta Destino (últimos 4):</span>
                <span class='value'>{last4DestinationDigits}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Monto Transferido:</span>
                <span class='value amount'>RD$ {amount:N2}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Fecha y Hora:</span>
                <span class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</span>
            </div>
            <p style='margin-top: 20px; color: #666; font-size: 14px;'>La transferencia se ha procesado y el dinero estará disponible en la cuenta destino dentro de poco.</p>
        </div>
        <div class='footer'>
            <p>Artemis Banking © 2026 - Banco Digital Seguro</p>
            <p>Este es un correo automático, por favor no responder.</p>
        </div>
    </div>
</body>
</html>";
  }

  public static string ThirdPartyTransferReceivedNotification(string clientName, decimal amount, string last4SourceDigits, DateTime dateTime)
  {
    return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Transacción Recibida</title>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 15px; border-radius: 5px; text-align: center; margin-bottom: 20px; }}
        .header h2 {{ margin: 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 5px; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #ddd; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .label {{ font-weight: bold; color: #666; }}
        .value {{ color: #333; }}
        .amount {{ font-size: 24px; color: #28a745; font-weight: bold; }}
        .footer {{ text-align: center; padding-top: 20px; color: #999; font-size: 12px; }}
        .success-icon {{ color: #28a745; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2><span class='success-icon'>✓</span> Transacción Recibida</h2>
        </div>
        <div class='content'>
            <p>Estimado/a <strong>{clientName}</strong>,</p>
            <p>¡Excelentes noticias! Has recibido una transferencia exitosamente.</p>
            <div class='detail-row'>
                <span class='label'>Cuenta Origen (últimos 4):</span>
                <span class='value'>{last4SourceDigits}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Monto Recibido:</span>
                <span class='value amount'>RD$ {amount:N2}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Fecha y Hora:</span>
                <span class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</span>
            </div>
            <p style='margin-top: 20px; color: #666; font-size: 14px;'>El dinero ya está disponible en tu cuenta y puedes utilizarlo de inmediato.</p>
        </div>
        <div class='footer'>
            <p>Artemis Banking © 2026 - Banco Digital Seguro</p>
            <p>Este es un correo automático, por favor no responder.</p>
        </div>
    </div>
</body>
</html>";
  }
}