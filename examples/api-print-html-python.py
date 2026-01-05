#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Contoh Print HTML Content menggunakan API /api/print
Pastikan aplikasi DXN POS Printer sedang berjalan
"""

import json
import requests
from datetime import datetime

# Konfigurasi
API_BASE_URL = "http://localhost:8080"
API_URL = f"{API_BASE_URL}/api/print"

def open_cash_drawer_via_api(api_base_url, pin=None):
    """
    Fungsi untuk buka cash drawer via API
    
    Args:
        api_base_url (str): Base URL API (tanpa /api/cashdrawer)
        pin (int, optional): Pin yang digunakan (1, 2, atau None untuk default)
    
    Returns:
        dict: Response dari API
    """
    try:
        data = {}
        if pin is not None:
            data["pin"] = pin
        
        response = requests.post(
            f"{api_base_url}/api/cashdrawer",
            json=data,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        response.raise_for_status()
        return response.json()
    
    except requests.exceptions.RequestException as e:
        return {
            "success": False,
            "message": f"Request error: {str(e)}"
        }

def print_and_open_drawer_via_api(api_base_url, html_content, cut_paper=True, drawer_pin=None, drawer_delay=500):
    """
    Fungsi untuk print HTML content dan buka cash drawer sekaligus via API
    
    Args:
        api_base_url (str): Base URL API (tanpa /api/print-and-drawer)
        html_content (str): HTML content yang ingin dicetak
        cut_paper (bool): Apakah kertas harus dipotong setelah print
        drawer_pin (int, optional): Pin yang digunakan untuk cash drawer (1, 2, atau None untuk default)
        drawer_delay (int): Delay dalam milliseconds sebelum buka drawer (default: 500)
    
    Returns:
        dict: Response dari API dengan detail print dan drawer success
    """
    try:
        data = {
            "content": html_content,
            "cutPaper": cut_paper,
            "drawerDelay": drawer_delay
        }
        
        if drawer_pin is not None:
            data["drawerPin"] = drawer_pin
        
        response = requests.post(
            f"{api_base_url}/api/print-and-drawer",
            json=data,
            headers={"Content-Type": "application/json"},
            timeout=15  # Timeout lebih lama karena ada delay
        )
        
        response.raise_for_status()
        return response.json()
    
    except requests.exceptions.RequestException as e:
        return {
            "success": False,
            "message": f"Request error: {str(e)}",
            "printSuccess": False,
            "drawerSuccess": False
        }

def print_via_api(api_base_url, html_content, cut_paper=True):
    """
    Fungsi untuk print HTML content via API
    
    Args:
        html_content (str): HTML content yang ingin dicetak
        cut_paper (bool): Apakah kertas harus dipotong setelah print
    
    Returns:
        dict: Response dari API
    """
    try:
        data = {
            "content": html_content,
            "cutPaper": cut_paper
        }
        
        response = requests.post(
            API_BASE_URL + "/api/print",
            json=data,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        response.raise_for_status()
        return response.json()
    
    except requests.exceptions.RequestException as e:
        return {
            "success": False,
            "message": f"Request error: {str(e)}"
        }
    except json.JSONDecodeError as e:
        return {
            "success": False,
            "message": f"JSON decode error: {str(e)}"
        }

# Contoh 1: Print HTML sederhana
def test_print_simple():
    """Test print HTML sederhana"""
    print("Test 1: Print HTML sederhana...")
    
    html_content = """
    <div style="text-align: center;">
        <h2 style="font-size: 18px; font-weight: bold;">TOKO CONTOH</h2>
        <p style="font-size: 12px;">Jl. Contoh No. 123</p>
        <p style="font-size: 12px;">Telp: 081234567890</p>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <table style="width: 100%; font-size: 12px;">
            <tr>
                <td>Item 1</td>
                <td style="text-align: right;">Rp 10.000</td>
            </tr>
            <tr>
                <td>Item 2</td>
                <td style="text-align: right;">Rp 15.000</td>
            </tr>
        </table>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <p style="text-align: right; font-weight: bold;">Total: Rp 25.000</p>
        <p style="text-align: center; font-weight: bold;">Terima Kasih</p>
    </div>
    """
    
    result = print_via_api(html_content, cut_paper=True)
    if result.get("success"):
        print(f"✓ {result.get('message')}")
    else:
        print(f"✗ {result.get('message')}")
    print()

# Contoh 2: Print dengan data dinamis
def test_print_dynamic():
    """Test print dengan data dinamis"""
    print("Test 2: Print dengan data dinamis...")
    
    order_data = {
        "order_no": f"ORD-{datetime.now().strftime('%Y%m%d%H%M%S')}",
        "date": datetime.now().strftime("%d/%m/%Y %H:%M:%S"),
        "items": [
            {"name": "Produk A", "qty": 2, "price": 10000},
            {"name": "Produk B", "qty": 1, "price": 15000},
            {"name": "Produk C", "qty": 3, "price": 5000}
        ]
    }
    
    # Calculate total
    total = sum(item["qty"] * item["price"] for item in order_data["items"])
    
    # Build HTML
    items_html = ""
    for item in order_data["items"]:
        subtotal = item["qty"] * item["price"]
        items_html += f"""
            <tr>
                <td>{item['name']} x{item['qty']}</td>
                <td style="text-align: right;">Rp {subtotal:,}</td>
            </tr>
        """
    
    html_content = f"""
    <div style="text-align: center;">
        <h2 style="font-size: 18px; font-weight: bold;">TOKO CONTOH</h2>
        <p style="font-size: 12px;">Jl. Contoh No. 123</p>
        <p style="font-size: 12px;">Telp: 081234567890</p>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <p style="font-size: 12px;"><strong>Order No:</strong> {order_data['order_no']}</p>
        <p style="font-size: 12px;"><strong>Tanggal:</strong> {order_data['date']}</p>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <table style="width: 100%; font-size: 12px;">
            {items_html}
        </table>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <p style="text-align: right; font-weight: bold; font-size: 14px;">Total: Rp {total:,}</p>
        <p style="text-align: center; font-weight: bold;">Terima Kasih</p>
        <p style="text-align: center; font-size: 10px;">Barang yang sudah dibeli tidak dapat ditukar/dikembalikan</p>
    </div>
    """
    
    result = print_via_api(html_content, cut_paper=True)
    if result.get("success"):
        print(f"✓ {result.get('message')}")
    else:
        print(f"✗ {result.get('message')}")
    print()

# Contoh 3: Print dari template
def test_print_template():
    """Test print dari template"""
    print("Test 3: Print dari template...")
    
    # Data dari database atau form
    receipt_data = {
        "company_name": "TOKO CONTOH",
        "address": "Jl. Contoh No. 123",
        "phone": "081234567890",
        "invoice_no": "INV-20240101001",
        "cashier": "Admin",
        "items": [
            {"name": "Produk A", "qty": 2, "price": 10000, "subtotal": 20000},
            {"name": "Produk B", "qty": 1, "price": 15000, "subtotal": 15000},
        ],
        "subtotal": 35000,
        "discount": 5000,
        "total": 30000
    }
    
    # Build items HTML
    items_html = ""
    for item in receipt_data["items"]:
        items_html += f"""
            <tr>
                <td>{item['name']}</td>
                <td style="text-align: center;">{item['qty']}</td>
                <td style="text-align: right;">Rp {item['price']:,}</td>
                <td style="text-align: right;">Rp {item['subtotal']:,}</td>
            </tr>
        """
    
    html_content = f"""
    <div style="text-align: center; width: 80mm; margin: 0 auto;">
        <h2 style="font-size: 16px; font-weight: bold; margin-bottom: 10px;">{receipt_data['company_name']}</h2>
        <p style="font-size: 12px; margin: 5px 0;">{receipt_data['address']}</p>
        <p style="font-size: 12px; margin: 5px 0;">Telp: {receipt_data['phone']}</p>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <p style="font-size: 12px; text-align: left;"><strong>Invoice:</strong> {receipt_data['invoice_no']}</p>
        <p style="font-size: 12px; text-align: left;"><strong>Kasir:</strong> {receipt_data['cashier']}</p>
        <p style="font-size: 12px; text-align: left;"><strong>Tanggal:</strong> {datetime.now().strftime('%d/%m/%Y %H:%M:%S')}</p>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <table style="width: 100%; font-size: 12px; border-collapse: collapse;">
            <thead>
                <tr style="border-bottom: 1px solid #000;">
                    <th style="text-align: left; padding: 5px 0;">Item</th>
                    <th style="text-align: center; padding: 5px 0;">Qty</th>
                    <th style="text-align: right; padding: 5px 0;">Harga</th>
                    <th style="text-align: right; padding: 5px 0;">Total</th>
                </tr>
            </thead>
            <tbody>
                {items_html}
            </tbody>
        </table>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <div style="text-align: right; font-size: 12px;">
            <p>Subtotal: Rp {receipt_data['subtotal']:,}</p>
            <p>Diskon: Rp {receipt_data['discount']:,}</p>
            <p style="font-size: 14px; font-weight: bold; border-top: 1px solid #000; padding-top: 5px;">
                TOTAL: Rp {receipt_data['total']:,}
            </p>
        </div>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <p style="text-align: center; font-weight: bold; margin-top: 10px;">Terima Kasih</p>
        <p style="text-align: center; font-size: 10px;">Barang yang sudah dibeli tidak dapat ditukar/dikembalikan</p>
    </div>
    """
    
    result = print_via_api(html_content, cut_paper=True)
    if result.get("success"):
        print(f"✓ {result.get('message')}")
    else:
        print(f"✗ {result.get('message')}")
    print()

# Contoh 4: Print dan buka cash drawer sekaligus
def test_print_and_drawer():
    """Test print dan buka cash drawer sekaligus"""
    print("Test 4: Print dan buka cash drawer sekaligus...")
    
    html_content = """
    <div style="text-align: center;">
        <h2 style="font-size: 18px; font-weight: bold;">TOKO CONTOH</h2>
        <p style="font-size: 12px;">Jl. Contoh No. 123</p>
        <p style="font-size: 12px;">Telp: 081234567890</p>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <p style="font-size: 12px;"><strong>Tanggal:</strong> {}</p>
        <p style="font-size: 12px;"><strong>Order No:</strong> ORD-{}</p>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <table style="width: 100%; font-size: 12px;">
            <tr>
                <td>Produk A x2</td>
                <td style="text-align: right;">Rp 20.000</td>
            </tr>
            <tr>
                <td>Produk B x1</td>
                <td style="text-align: right;">Rp 15.000</td>
            </tr>
        </table>
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        <p style="text-align: right; font-weight: bold;">Total: Rp 35.000</p>
        <p style="text-align: center; font-weight: bold;">Terima Kasih</p>
    </div>
    """.format(
        datetime.now().strftime("%d/%m/%Y %H:%M:%S"),
        datetime.now().strftime("%Y%m%d%H%M%S")
    )
    
    result = print_and_open_drawer_via_api(API_BASE_URL, html_content, cut_paper=True, drawer_pin=2, drawer_delay=500)
    if result.get("success"):
        print(f"✓ {result.get('message')}")
        print(f"  Print: {'✓ Berhasil' if result.get('printSuccess') else '✗ Gagal'}")
        print(f"  Drawer: {'✓ Berhasil' if result.get('drawerSuccess') else '✗ Gagal'}")
    else:
        print(f"✗ {result.get('message')}")
        print(f"  Print: {'✓ Berhasil' if result.get('printSuccess') else '✗ Gagal'}")
        print(f"  Drawer: {'✓ Berhasil' if result.get('drawerSuccess') else '✗ Gagal'}")
    print()

if __name__ == "__main__":
    print("=" * 60)
    print("Contoh Print HTML Content via API")
    print("=" * 60)
    print()
    
    try:
        test_print_simple()
        test_print_dynamic()
        test_print_template()
        test_print_and_drawer()
        
        print("=" * 60)
        print("Semua test selesai!")
        print("=" * 60)
    
    except KeyboardInterrupt:
        print("\n\nTest dibatalkan oleh user")
    except Exception as e:
        print(f"\n\nError: {str(e)}")

