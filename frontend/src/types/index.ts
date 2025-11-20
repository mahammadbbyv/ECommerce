// User and Authentication Types
export interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

// Product Types
export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  imageUrl?: string;
  categoryId: number;
  categoryName?: string;
  createdAt: string;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  imageUrl?: string;
  categoryId: number;
}

// Category Types
export interface Category {
  id: number;
  name: string;
  description: string;
  productCount?: number;
}

export interface CreateCategoryRequest {
  name: string;
  description: string;
}

// Cart Types
export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  productPrice: number;
  productImageUrl?: string;
  quantity: number;
  subtotal: number;
}

export interface Cart {
  id: number;
  userId: number;
  items: CartItem[];
  totalAmount: number;
  createdAt: string;
  updatedAt: string;
}

export interface AddToCartRequest {
  productId: number;
  quantity: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

// Order Types
export interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  price: number;
  subtotal: number;
}

export interface Order {
  id: number;
  orderNumber: string;
  userId: number;
  totalAmount: number;
  status: string;
  paymentStatus: string;
  shippingAddress: string;
  items: OrderItem[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateOrderRequest {
  shippingAddress: string;
}

// Payment Types
export interface CreatePaymentIntentRequest {
  orderId: number;
}

export interface PaymentIntentResponse {
  clientSecret: string;
}

// Admin Analytics Types
export interface TopProduct {
  productId: number;
  productName: string;
  totalSold: number;
  totalRevenue: number;
}

export interface Analytics {
  totalRevenue: number;
  totalOrders: number;
  totalCustomers: number;
  totalProducts: number;
  ordersByStatus: Record<string, number>;
  topProducts: TopProduct[];
}

// API Response Types
export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
}
