"""
Aplikasi Sederhana untuk Membuka Cash Drawer
Menggunakan ESC/POS commands untuk membuka laci kasir
Mendukung koneksi Serial/USB, Bluetooth, dan WiFi
"""

import tkinter as tk
from tkinter import ttk, messagebox
import serial
import serial.tools.list_ports
import socket
import time
from datetime import datetime


class CashDrawerApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Cash Drawer Controller")
        self.root.geometry("500x600")
        self.root.resizable(False, False)
        
        # Variabel
        self.connection_type_var = tk.StringVar(value="Serial")
        self.port_var = tk.StringVar()
        self.baudrate_var = tk.StringVar(value="9600")
        self.ip_address_var = tk.StringVar(value="192.168.1.100")
        self.port_number_var = tk.StringVar(value="9100")
        self.serial_connection = None
        self.port_info = {}  # Menyimpan info port untuk deteksi bluetooth
        
        self.create_widgets()
        self.refresh_ports()
        
    def create_widgets(self):
        # Frame utama
        main_frame = ttk.Frame(self.root, padding="20")
        main_frame.grid(row=0, column=0, sticky=(tk.W, tk.E, tk.N, tk.S))
        
        # Judul
        title_label = ttk.Label(
            main_frame, 
            text="Cash Drawer Controller", 
            font=("Arial", 16, "bold")
        )
        title_label.grid(row=0, column=0, columnspan=2, pady=(0, 10))
        
        # Info Koneksi
        connection_info = ttk.Label(
            main_frame,
            text="✓ Mendukung Serial/USB, Bluetooth, dan WiFi",
            font=("Arial", 8),
            foreground="blue"
        )
        connection_info.grid(row=1, column=0, columnspan=2, pady=(0, 15))
        
        # Pilihan Tipe Koneksi
        ttk.Label(main_frame, text="Tipe Koneksi:").grid(row=2, column=0, sticky=tk.W, pady=5)
        connection_type_combo = ttk.Combobox(
            main_frame,
            textvariable=self.connection_type_var,
            values=["Serial", "WiFi"],
            width=15,
            state="readonly"
        )
        connection_type_combo.grid(row=2, column=1, sticky=tk.W, pady=5)
        connection_type_combo.bind("<<ComboboxSelected>>", self.on_connection_type_changed)
        
        # Frame untuk Serial/Bluetooth settings
        self.serial_frame = ttk.LabelFrame(main_frame, text="Pengaturan Serial/Bluetooth", padding="10")
        self.serial_frame.grid(row=3, column=0, columnspan=2, sticky=(tk.W, tk.E), pady=10)
        
        # Pilihan Port
        ttk.Label(self.serial_frame, text="Port COM:").grid(row=0, column=0, sticky=tk.W, pady=5)
        port_frame = ttk.Frame(self.serial_frame)
        port_frame.grid(row=0, column=1, sticky=(tk.W, tk.E), pady=5)
        
        self.port_combo = ttk.Combobox(port_frame, textvariable=self.port_var, width=20, state="readonly")
        self.port_combo.grid(row=0, column=0, padx=(0, 5))
        self.port_combo.bind("<<ComboboxSelected>>", self.on_port_selected)
        
        refresh_btn = ttk.Button(port_frame, text="Refresh", command=self.refresh_ports)
        refresh_btn.grid(row=0, column=1)
        
        # Info Port (Bluetooth/USB)
        self.port_type_label = ttk.Label(
            self.serial_frame,
            text="",
            font=("Arial", 8),
            foreground="gray"
        )
        self.port_type_label.grid(row=1, column=0, columnspan=2, pady=(0, 5))
        
        # Baudrate
        ttk.Label(self.serial_frame, text="Baudrate:").grid(row=2, column=0, sticky=tk.W, pady=5)
        baudrate_combo = ttk.Combobox(
            self.serial_frame, 
            textvariable=self.baudrate_var, 
            values=["9600", "19200", "38400", "57600", "115200"],
            width=15,
            state="readonly"
        )
        baudrate_combo.grid(row=2, column=1, sticky=tk.W, pady=5)
        
        self.serial_frame.columnconfigure(1, weight=1)
        
        # Frame untuk WiFi settings
        self.wifi_frame = ttk.LabelFrame(main_frame, text="Pengaturan WiFi", padding="10")
        self.wifi_frame.grid(row=4, column=0, columnspan=2, sticky=(tk.W, tk.E), pady=10)
        
        # IP Address
        ttk.Label(self.wifi_frame, text="IP Address:").grid(row=0, column=0, sticky=tk.W, pady=5)
        ip_entry = ttk.Entry(self.wifi_frame, textvariable=self.ip_address_var, width=20)
        ip_entry.grid(row=0, column=1, sticky=tk.W, pady=5)
        
        # Port Number
        ttk.Label(self.wifi_frame, text="Port:").grid(row=1, column=0, sticky=tk.W, pady=5)
        port_entry = ttk.Entry(self.wifi_frame, textvariable=self.port_number_var, width=20)
        port_entry.grid(row=1, column=1, sticky=tk.W, pady=5)
        
        # Info WiFi
        wifi_info = ttk.Label(
            self.wifi_frame,
            text="Default port: 9100 (Raw TCP/IP)",
            font=("Arial", 8),
            foreground="gray"
        )
        wifi_info.grid(row=2, column=0, columnspan=2, pady=(5, 0))
        
        self.wifi_frame.columnconfigure(1, weight=1)
        
        # Sembunyikan WiFi frame secara default
        self.wifi_frame.grid_remove()
        
        # Frame untuk tombol
        button_frame = ttk.Frame(main_frame)
        button_frame.grid(row=5, column=0, columnspan=2, pady=20)
        
        # Tombol Test Print
        test_print_btn = ttk.Button(
            button_frame,
            text="Test Print",
            command=self.test_print
        )
        test_print_btn.grid(row=0, column=0, padx=5, sticky=(tk.W, tk.E))
        
        # Tombol Buka Cash Drawer
        open_btn = ttk.Button(
            button_frame,
            text="Buka Cash Drawer",
            command=self.open_cash_drawer,
            style="Accent.TButton"
        )
        open_btn.grid(row=0, column=1, padx=5, sticky=(tk.W, tk.E))
        
        button_frame.columnconfigure(0, weight=1)
        button_frame.columnconfigure(1, weight=1)
        
        # Status
        self.status_label = ttk.Label(
            main_frame, 
            text="Status: Siap", 
            foreground="green"
        )
        self.status_label.grid(row=6, column=0, columnspan=2, pady=10)
        
        # Info
        info_text = (
            "Pilih tipe koneksi dan konfigurasi printer Anda.\n"
            "Gunakan 'Test Print' untuk menguji printer,\n"
            "atau 'Buka Cash Drawer' untuk membuka laci kasir."
        )
        info_label = ttk.Label(main_frame, text=info_text, justify=tk.CENTER, font=("Arial", 8))
        info_label.grid(row=6, column=0, columnspan=2, pady=10)
        
        # Configure grid weights
        main_frame.columnconfigure(1, weight=1)
        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(0, weight=1)
        
        # Inisialisasi tampilan berdasarkan tipe koneksi
        self.on_connection_type_changed()
    
    def on_connection_type_changed(self, event=None):
        """Update UI ketika tipe koneksi berubah"""
        connection_type = self.connection_type_var.get()
        if connection_type == "WiFi":
            self.serial_frame.grid_remove()
            self.wifi_frame.grid()
        else:  # Serial
            self.wifi_frame.grid_remove()
            self.serial_frame.grid()
        
    def refresh_ports(self):
        """Refresh daftar port COM yang tersedia"""
        ports = []
        self.port_info = {}
        
        for port in serial.tools.list_ports.comports():
            port_name = port.device
            ports.append(port_name)
            
            # Deteksi apakah port adalah bluetooth berdasarkan description atau hwid
            description = port.description.lower()
            hwid = port.hwid.lower() if port.hwid else ""
            is_bluetooth = (
                "bluetooth" in description or 
                "bluetooth" in hwid or
                "bt" in description or
                "rfcomm" in hwid
            )
            
            self.port_info[port_name] = {
                'description': port.description,
                'is_bluetooth': is_bluetooth,
                'hwid': port.hwid
            }
        
        if self.port_combo:
            self.port_combo['values'] = ports
            if ports and not self.port_var.get():
                self.port_var.set(ports[0])
                self.on_port_selected()
        
    def on_port_selected(self, event=None):
        """Update info port ketika port dipilih"""
        port = self.port_var.get()
        if port and port in self.port_info:
            info = self.port_info[port]
            if info['is_bluetooth']:
                self.port_type_label.config(
                    text=f"Tipe: Bluetooth - {info['description']}",
                    foreground="blue"
                )
            else:
                self.port_type_label.config(
                    text=f"Tipe: Serial/USB - {info['description']}",
                    foreground="green"
                )
        else:
            self.port_type_label.config(text="")
    
    def send_command(self, command):
        """Mengirim perintah ke printer berdasarkan tipe koneksi"""
        connection_type = self.connection_type_var.get()
        
        if connection_type == "WiFi":
            # Koneksi WiFi menggunakan socket
            ip_address = self.ip_address_var.get().strip()
            port_number = self.port_number_var.get().strip()
            
            if not ip_address:
                raise ValueError("Silakan masukkan IP address printer!")
            if not port_number:
                raise ValueError("Silakan masukkan port number!")
            
            try:
                port_num = int(port_number)
            except ValueError:
                raise ValueError("Port number harus berupa angka!")
            
            # Buat socket connection
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(3)  # Timeout 3 detik
            
            try:
                sock.connect((ip_address, port_num))
                sock.sendall(command)
                time.sleep(0.1)  # Tunggu sebentar
                sock.close()
                return f"WiFi - {ip_address}:{port_num}"
            except socket.timeout:
                raise ConnectionError(f"Timeout: Tidak dapat terhubung ke {ip_address}:{port_num}")
            except socket.gaierror:
                raise ConnectionError(f"IP address tidak valid: {ip_address}")
            except ConnectionRefusedError:
                raise ConnectionError(f"Koneksi ditolak. Pastikan printer WiFi aktif dan port {port_num} terbuka.")
            except Exception as e:
                raise ConnectionError(f"Error koneksi WiFi: {str(e)}")
        else:
            # Koneksi Serial/Bluetooth
            port = self.port_var.get()
            if not port:
                raise ValueError("Silakan pilih port COM terlebih dahulu!")
            
            baudrate = int(self.baudrate_var.get())
            
            # Buka koneksi serial
            ser = serial.Serial(
                port=port,
                baudrate=baudrate,
                bytesize=serial.EIGHTBITS,
                parity=serial.PARITY_NONE,
                stopbits=serial.STOPBITS_ONE,
                timeout=1
            )
            
            ser.write(command)
            time.sleep(0.1)  # Tunggu sebentar
            ser.close()
            
            port_type = "Bluetooth" if (port in self.port_info and self.port_info[port]['is_bluetooth']) else "Serial/USB"
            return f"{port_type} - {port}"
    
    def open_cash_drawer(self):
        """Mengirim perintah untuk membuka cash drawer"""
        try:
            # ESC/POS command untuk membuka cash drawer
            # ESC p m t1 t2
            # m = 0 atau 1 (pin yang digunakan)
            # t1 = waktu ON dalam milliseconds (LSB)
            # t2 = waktu ON dalam milliseconds (MSB)
            cash_drawer_command = bytes([0x10, 0x14, 0x01, 0x00, 0x01])  # ESC p 1 0 1
            
            # Alternatif: ESC p 0 25 250 (pin 2, 25ms + 250*256ms)
            # cash_drawer_command = bytes([0x10, 0x14, 0x00, 0x19, 0xFA])
            
            connection_info = self.send_command(cash_drawer_command)
            
            self.status_label.config(text="Status: Cash drawer dibuka!", foreground="green")
            messagebox.showinfo(
                "Sukses", 
                f"Perintah untuk membuka cash drawer telah dikirim!\n\n"
                f"Koneksi: {connection_info}"
            )
            
        except ValueError as e:
            self.status_label.config(text="Status: Error!", foreground="red")
            messagebox.showerror("Error", str(e))
        except (serial.SerialException, ConnectionError) as e:
            self.status_label.config(text="Status: Error!", foreground="red")
            messagebox.showerror("Error", str(e))
        except Exception as e:
            self.status_label.config(text="Status: Error!", foreground="red")
            messagebox.showerror("Error", f"Terjadi kesalahan:\n{str(e)}")
    
    def test_print(self):
        """Mengirim perintah test print ke printer"""
        try:
            connection_type = self.connection_type_var.get()
            
            # ESC/POS commands untuk test print
            commands = []
            
            # Initialize printer
            commands.append(bytes([0x1B, 0x40]))  # ESC @
            
            # Center alignment
            commands.append(bytes([0x1B, 0x61, 0x01]))  # ESC a 1 (center)
            
            # Double width and height
            commands.append(bytes([0x1D, 0x21, 0x30]))  # GS ! 0x30 (double width & height)
            
            # Print header
            commands.append(b"TEST PRINT\n")
            
            # Reset character size
            commands.append(bytes([0x1D, 0x21, 0x00]))  # GS ! 0x00 (normal size)
            
            # Left alignment
            commands.append(bytes([0x1B, 0x61, 0x00]))  # ESC a 0 (left)
            
            # Line feed
            commands.append(b"\n")
            
            # Print separator line
            commands.append(b"--------------------------------\n")
            commands.append(b"\n")
            
            # Print information
            current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            
            if connection_type == "WiFi":
                ip_address = self.ip_address_var.get().strip()
                port_number = self.port_number_var.get().strip()
                connection_info = f"WiFi - {ip_address}:{port_number}"
                info_lines = [
                    f"Tipe: WiFi",
                    f"IP: {ip_address}",
                    f"Port: {port_number}",
                    f"Tanggal: {current_time}",
                ]
            else:
                port = self.port_var.get()
                baudrate = int(self.baudrate_var.get())
                port_type = "Bluetooth" if (port in self.port_info and self.port_info[port]['is_bluetooth']) else "Serial/USB"
                connection_info = f"{port_type} - {port}"
                info_lines = [
                    f"Port: {port}",
                    f"Baudrate: {baudrate}",
                    f"Tipe: {port_type}",
                    f"Tanggal: {current_time}",
                ]
            
            info_lines.extend([
                "",
                "Ini adalah test print untuk",
                "memverifikasi printer bekerja",
                "dengan baik.",
                "",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "abcdefghijklmnopqrstuvwxyz",
                "0123456789",
                "!@#$%^&*()_+-=[]{}|;:,.<>?",
                "",
            ])
            
            for line in info_lines:
                commands.append(line.encode('utf-8') + b"\n")
            
            # Print separator line
            commands.append(b"--------------------------------\n")
            commands.append(b"\n")
            
            # Center alignment untuk footer
            commands.append(bytes([0x1B, 0x61, 0x01]))  # ESC a 1 (center)
            commands.append(b"Test Print Selesai\n")
            commands.append(b"\n")
            commands.append(b"\n")
            
            # Feed paper (3 lines)
            commands.append(bytes([0x1B, 0x64, 0x03]))  # ESC d 3
            
            # Cut paper (jika printer mendukung)
            # commands.append(bytes([0x1D, 0x56, 0x41, 0x03]))  # GS V 41 3 (partial cut)
            
            # Gabungkan semua perintah menjadi satu
            full_command = b"".join(commands)
            
            # Kirim perintah
            self.send_command(full_command)
            
            self.status_label.config(text="Status: Test print berhasil!", foreground="green")
            messagebox.showinfo(
                "Sukses", 
                f"Test print telah dikirim ke printer!\n\n"
                f"Koneksi: {connection_info}\n\n"
                f"Periksa hasil print di printer Anda."
            )
            
        except ValueError as e:
            self.status_label.config(text="Status: Error!", foreground="red")
            messagebox.showerror("Error", str(e))
        except (serial.SerialException, ConnectionError) as e:
            self.status_label.config(text="Status: Error!", foreground="red")
            messagebox.showerror("Error", str(e))
        except Exception as e:
            self.status_label.config(text="Status: Error!", foreground="red")
            messagebox.showerror("Error", f"Terjadi kesalahan:\n{str(e)}")


def main():
    root = tk.Tk()
    app = CashDrawerApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()

