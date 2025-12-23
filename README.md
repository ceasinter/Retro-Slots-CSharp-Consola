# 🎰 Retro Slots

Retro Slots es un juego de tragamonedas retro hecho en **C#/.NET** para consola.  
Combina estética clásica con lógica moderna: animaciones de rodillos, tabla de pagos configurable, mensajes con parpadeo y hasta soporte para sonidos 🎵.

---

## ✨ Características

- Interfaz retro en consola con colores y ASCII Art.
- Animación de rodillos con símbolos de frutas 🍒🍋🍉🍓🍐.
- Tabla de pagos configurable (tres iguales, dos iguales).
- Créditos iniciales y rango de apuestas personalizable.
- Mensajes con efecto de parpadeo (Blink).
- Soporte para sonidos simples (`Console.Beep`) o archivos `.wav`.
- Código modular y fácil de extender.

---

## 📂 Estructura del proyecto

- `Program.cs` → Punto de entrada.
- `Game.cs` → Orquesta el flujo principal.
- `SlotMachine.cs` → Lógica de rodillos y animación.
- `PayTable.cs` → Reglas de pago.
- `ConsoleUI.cs` → Helpers para interfaz retro.
- `InputHelper.cs` → Manejo de entradas con soporte ESC.
- `SpinResult.cs` → Resultado de cada giro.

---

## ▶️ Cómo ejecutar

1. Clona el repositorio:
   ```bash
   [git clone https://github.com/ceasinter/retro-slots.git]
)
   cd retro-slots
   dotnet run

---

🎮 Controles- Enter → Jugar
- A → Cambiar apuesta
- H → Ver tabla de pagos
- Esc → Regresar al menú
- X → Salir

---

🛠️ Requisitos- .NET 8 SDK (o superior).
- Windows, Linux o macOS con soporte para consola.


---

📸 Capturas de Pantalla

- Ver Carpeta CapturasPantalla
