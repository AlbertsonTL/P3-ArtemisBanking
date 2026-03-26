namespace ArtemisBanking.Infrastructure.Services;

public static class EmailTemplates
{
    private static string BuildTemplate(string title, string icon, string colorHex, string mainContent)
    {
        return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f1f5f9; margin: 0; padding: 0; -webkit-font-smoothing: antialiased; }}
    .email-wrapper {{ width: 100%; background-color: #f1f5f9; padding: 40px 0; }}
    .email-container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1); }}
    .header {{ background: linear-gradient(135deg, {colorHex} 0%, #0f172a 100%); padding: 40px; text-align: center; border-bottom: 4px solid rgba(255,255,255,0.1); }}
    .header-icon {{ display: inline-block; padding: 15px; background: rgba(255,255,255,0.15); border-radius: 50%; font-size: 32px; line-height: 1; margin-bottom: 15px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); backdrop-filter: blur(4px); }}
    .header h1 {{ color: #ffffff; margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px; text-transform: uppercase; }}
    .content {{ padding: 40px; color: #334155; line-height: 1.7; font-size: 16px; }}
    .content p {{ margin: 0 0 20px 0; }}
    .greeting {{ font-size: 20px; font-weight: 600; color: #0f172a; margin-top: 0; margin-bottom: 25px; }}
    .btn-action {{ display: block; width: max-content; margin: 30px auto; background: linear-gradient(to right, {colorHex}, #3b82f6); color: #ffffff !important; text-decoration: none; padding: 15px 35px; border-radius: 50px; font-weight: 600; font-size: 16px; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.1); transition: transform 0.2s; text-align: center; }}
    .footer {{ background-color: #f8fafc; padding: 30px 40px; text-align: center; border-top: 1px solid #e2e8f0; }}
    .footer p {{ margin: 0; color: #64748b; font-size: 13px; line-height: 1.5; }}
    .highlight-card {{ background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%); border: 1px solid #e2e8f0; border-radius: 16px; padding: 30px; margin: 30px 0; text-align: center; box-shadow: inset 0 2px 4px rgba(0,0,0,0.02); }}
    .highlight-amount {{ font-size: 42px; font-weight: 800; color: {colorHex}; letter-spacing: -1px; margin: 10px 0; display: block; }}
    .highlight-label {{ font-size: 12px; text-transform: uppercase; letter-spacing: 1.5px; color: #64748b; font-weight: 700; }}
    .data-table {{ width: 100%; border-collapse: collapse; margin: 25px 0; }}
    .data-table tr:not(:last-child) td {{ border-bottom: 1px dotted #cbd5e1; }}
    .data-table td {{ padding: 16px 5px; vertical-align: middle; }}
    .data-table .label {{ color: #64748b; font-weight: 600; font-size: 14px; width: 45%; }}
    .data-table .value {{ color: #0f172a; font-weight: 600; font-size: 15px; text-align: right; }}
    .token-box {{ background: #e0e7ff; border: 1px dashed #6366f1; color: #4338ca; border-radius: 8px; padding: 20px; font-family: 'Courier New', Courier, monospace; font-size: 18px; font-weight: bold; text-align: center; word-break: break-all; margin: 25px 0; letter-spacing: 2px; }}
    .notice {{ background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 16px 20px; border-radius: 0 8px 8px 0; font-size: 14px; color: #92400e; margin: 25px 0; }}
  </style>
</head>
<body>
  <div class='email-wrapper'>
    <div class='email-container'>
      <div class='header'>
        <div class='header-icon'>{icon}</div>
        <h1>{title}</h1>
      </div>
      <div class='content'>
        {mainContent}
      </div>
      <div class='footer'>
        <p><strong>Artemis Banking Group</strong></p>
        <p>Seguridad digital e innovación financiera.</p>
        <p style='margin-top: 15px; font-size: 11px; opacity: 0.8;'>Este es un mensaje generado automáticamente. Por favor no responder al remitente.</p>
        <p style='font-size: 11px; opacity: 0.8;'>© {DateTime.Now.Year} Artemis Banking. Todos los derechos reservados.</p>
      </div>
    </div>
  </div>
</body>
</html>";
    }

    public static string ActivateAccount(string fullName, string activationLink) => BuildTemplate(
        "Bienvenido a Artemis", "👋", "#3b82f6", $@"
        <h3 class='greeting'>Hola {fullName},</h3>
        <p>Es un honor tenerte en nuestra plataforma digital. Tu perfil financiero ha sido generado con éxito en nuestros sistemas centrales.</p>
        <p>Para activar tu cuenta y desbloquear todos los servicios bancarios interactivos, simplemente haz clic en el botón inferior:</p>
        <a href='{activationLink}' class='btn-action'>Activar mi Cuenta Segura</a>
        <p>Si no fuiste tú quien solicitó esta apertura digital, por favor ignora este protocolo.</p>"
    );

    public static string ResetPassword(string fullName, string resetLink) => BuildTemplate(
        "Restablecer Acceso", "🔐", "#ef4444", $@"
        <h3 class='greeting'>Hola {fullName},</h3>
        <p>El sistema ha detectado una solicitud segura para restablecer la contraseña administrativa de tu perfil.</p>
        <p>Si aprobaste esta acción, haz clic en el siguiente enlace cifrado para continuar con la actualización de tu credencial:</p>
        <a href='{resetLink}' class='btn-action'>Crear Nueva Contraseña</a>
        <p style='color: #ef4444; font-weight: 600; font-size: 14px; text-align: center;'>⚠️ Este acceso temporal expirará automáticamente en 2 horas.</p>
        <div class='notice'>Si no solicitaste este cambio, te recomendamos contactar a soporte de inmediato para proteger tu liquidez.</div>"
    );

    public static string ResetPasswordApi(string fullName, string token) => BuildTemplate(
        "Token de Seguridad API", "🔑", "#6366f1", $@"
        <h3 class='greeting'>Hola {fullName},</h3>
        <p>Se ha generado tu token único de validación para el flujo de restablecimiento de contraseña remoto:</p>
        <div class='token-box'>{token}</div>
        <p>Transmite este valor de autorización hacia el endpoint <code style='background:#f1f5f9; padding:2px 6px; border-radius:4px;'>POST /account/reset-password</code>.</p>
        <p style='font-size: 14px;'>El ciclo de vida de este token caduca en exactamente 120 minutos por protocolos de seguridad bancaria.</p>"
    );

    public static string LoanRateUpdated(string fullName, string loanNumber, decimal nuevaTasa, decimal nuevaCuota, DateTime proximaFecha) => BuildTemplate(
        "Actualización de Tasa 📊", "📈", "#f59e0b", $@"
        <h3 class='greeting'>Estimado/a {fullName},</h3>
        <p>Queremos informarte que se ha emitido una reestructuración de la tasa de interés en tu producto de financiamiento activo.</p>
        <table class='data-table'>
            <tr><td class='label'>Expediente de Préstamo</td><td class='value'>#{loanNumber}</td></tr>
            <tr><td class='label'>Nueva Tasa Anualizada</td><td class='value' style='color:#f59e0b;'>{nuevaTasa:N2}%</td></tr>
            <tr><td class='label'>Nueva Cuota Estipulada</td><td class='value'>RD$ {nuevaCuota:N2}</td></tr>
            <tr><td class='label'>Vigencia Comercial</td><td class='value'>{proximaFecha:dd/MM/yyyy}</td></tr>
        </table>
        <div class='notice'>Nota Importante: Las cuotas de ciclos cerrados no son afectadas. El balance reflectará esta nueva proyección a partir del siguiente corte.</div>"
    );

    public static string LoanApproved(string fullName, decimal amount, int termMonths, decimal rate, decimal monthlyPayment) => BuildTemplate(
        "Préstamo Desembolsado ✓", "💰", "#10b981", $@"
        <h3 class='greeting'>¡Felicidades {fullName}!</h3>
        <p>Nuestro equipo de crédito ha fallado a tu favor. Los fondos líquidos ya están disponibles y acreditados en tu cuenta principal.</p>
        <div class='highlight-card'>
            <span class='highlight-label'>Fondos Aprobados</span>
            <span class='highlight-amount'>RD$ {amount:N2}</span>
        </div>
        <table class='data-table'>
            <tr><td class='label'>Plazo Amortizable</td><td class='value'>{termMonths} Meses</td></tr>
            <tr><td class='label'>Tasa de Interés Nominal</td><td class='value'>{rate}% Anual</td></tr>
            <tr><td class='label'>Cuota Secuencial Asignada</td><td class='value' style='color:#10b981;'>RD$ {monthlyPayment:N2} /mes</td></tr>
        </table>"
    );

    public static string TransactionNotification(string fullName, string concept, decimal amount, string target) => BuildTemplate(
        "Aviso de Transacción 🔔", "⚡", "#8b5cf6", $@"
        <h3 class='greeting'>Hola {fullName},</h3>
        <p>Hemos procesado recientemente un movimiento financiero que involucra tus fondos dentro de Artemis Banking.</p>
        
        <div class='highlight-card'>
            <span class='highlight-label'>Impacto del Movimiento</span>
            <span class='highlight-amount'>RD$ {amount:N2}</span>
            <span style='color: #64748b; font-size: 14px; font-weight: 500; margin-top: 5px; display: block;'>{concept}</span>
        </div>

        <table class='data-table'>
            <tr><td class='label'>Contraparte</td><td class='value'>{target}</td></tr>
            <tr><td class='label'>Timestamp (UTC)</td><td class='value'>{DateTime.UtcNow:dd/MM/yyyy HH:mm}</td></tr>
        </table>
        <p style='font-size: 14px;'>Si desconoces esta entidad transaccional, comunícate de emergencia con tu asesor financiero llamando al *ART.</p>"
    );

    public static string DepositNotification(string clientName, decimal amount, string accountNumber, DateTime dateTime) => BuildTemplate(
        "Abono de Fondos", "📥", "#10b981", $@"
        <h3 class='greeting'>Buen día {clientName},</h3>
        <p>Tus reservas aumentan. Se ha registrado un depósito directo de liquidez en el sistema Core Artemis.</p>
        <div class='highlight-card mb-4'>
            <span class='highlight-label'>Suma Acreditada</span>
            <span class='highlight-amount'>RD$ {amount:N2}</span>
        </div>
        <table class='data-table mt-1'>
            <tr><td class='label'>Aplica en Cuenta</td><td class='value font-monospace'>{accountNumber}</td></tr>
            <tr><td class='label'>Impacto del Balance</td><td class='value text-success'>Positivo (+)</td></tr>
            <tr><td class='label'>Liquidación en Línea</td><td class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</td></tr>
        </table>"
    );

    public static string WithdrawalNotification(string clientName, decimal amount, string accountNumber, DateTime dateTime) => BuildTemplate(
        "Retiro de Fondos", "📤", "#dc2626", $@"
        <h3 class='greeting'>Hola {clientName},</h3>
        <p>Te notificamos que se ejecutó una reducción de balance por concepto de retiro desde tu cuenta personal.</p>
        <div class='highlight-card'>
            <span class='highlight-label'>Volumen Extraído</span>
            <span class='highlight-amount'>RD$ {amount:N2}</span>
        </div>
        <table class='data-table'>
            <tr><td class='label'>Canal de Origen</td><td class='value font-monospace'>{accountNumber}</td></tr>
            <tr><td class='label'>Corte de Cajero</td><td class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</td></tr>
        </table>"
    );

    public static string CreditCardPaymentNotification(string clientName, decimal amountPaid, string accountNumber, string last4CardDigits, DateTime dateTime) => BuildTemplate(
        "Honorarios de Tarjeta ✓", "💳", "#2563eb", $@"
        <h3 class='greeting'>Estimado/a {clientName},</h3>
        <p>Hemos liberado margen operativo en tu Tarjeta de Crédito tras recibir de manera satisfactoria tu registro de pago.</p>
        <div class='highlight-card'>
            <span class='highlight-label'>Saldo Cubierto</span>
            <span class='highlight-amount'>RD$ {amountPaid:N2}</span>
        </div>
        <table class='data-table'>
            <tr><td class='label'>Tarjeta Impactada</td><td class='value'>**** **** **** {last4CardDigits}</td></tr>
            <tr><td class='label'>Bóveda de Débito</td><td class='value font-monospace'>{accountNumber}</td></tr>
            <tr><td class='label'>Validación de Nodo</td><td class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</td></tr>
        </table>"
    );

    public static string LoanPaymentNotification(string clientName, decimal amountPaid, string last4AccountDigits, string loanNumber, DateTime dateTime, decimal excessAmount = 0)
    {
        string excessRow = excessAmount > 0 
            ? $"<tr><td class='label'>Excedente Retornado</td><td class='value' style='color:#10b981;'>+ RD$ {excessAmount:N2}</td></tr>" 
            : "";

        return BuildTemplate("Abono a Préstamo", "📑", "#0ea5e9", $@"
        <h3 class='greeting'>Hola {clientName},</h3>
        <p>El cargo a tu cuota secuencial preestablecida se ha enrutado favorablemente. El ciclo de amortización sigue al día.</p>
        <div class='highlight-card'>
            <span class='highlight-label'>Capital Abonado</span>
            <span class='highlight-amount'>RD$ {amountPaid:N2}</span>
        </div>
        <table class='data-table'>
            <tr><td class='label'>Sub-Sistema Origen</td><td class='value'>Cuenta **{last4AccountDigits}</td></tr>
            <tr><td class='label'>Expediente Pasivo</td><td class='value'>#{loanNumber}</td></tr>
            {excessRow}
            <tr><td class='label'>Traza Activa</td><td class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</td></tr>
        </table>");
    }

    public static string ThirdPartyTransferSentNotification(string clientName, decimal amount, string last4DestinationDigits, DateTime dateTime) => BuildTemplate(
        "Transferencia ACH ✓", "🚀", "#db2777", $@"
        <h3 class='greeting'>Hola {clientName},</h3>
        <p>Tus fondos han viajado digitalmente de forma exitosa hacia el beneficiario registrado.</p>
        <div class='highlight-card'>
            <span class='highlight-label'>Valor Cursado</span>
            <span class='highlight-amount'>RD$ {amount:N2}</span>
        </div>
        <table class='data-table'>
            <tr><td class='label'>Receptor (Últimos dígitos)</td><td class='value font-monospace'>{last4DestinationDigits}</td></tr>
            <tr><td class='label'>Sello de Transmisión</td><td class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</td></tr>
        </table>"
    );

    public static string ThirdPartyTransferReceivedNotification(string clientName, decimal amount, string last4SourceDigits, DateTime dateTime) => BuildTemplate(
        "Fondos Entrantes", "🎉", "#10b981", $@"
        <h3 class='greeting'>¡Buenas noticias, {clientName}!</h3>
        <p>Una nueva recarga de fondos mediante transferencia bancaria ha tocado base en tu perfil personal.</p>
        <div class='highlight-card'>
            <span class='highlight-label'>Captación Positiva</span>
            <span class='highlight-amount'>RD$ {amount:N2}</span>
        </div>
        <table class='data-table'>
            <tr><td class='label'>Emisor (Últimos dígitos)</td><td class='value font-monospace'>{last4SourceDigits}</td></tr>
            <tr><td class='label'>Sincronización Local</td><td class='value'>{dateTime:dd/MM/yyyy HH:mm:ss}</td></tr>
        </table>
        <p style='font-size: 14px;'>Tus activos están asegurados y el monto integro ya se encuentra disponible para consumo inmediato.</p>"
    );
}
