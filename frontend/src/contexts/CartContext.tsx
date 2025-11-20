'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { Cart, AddToCartRequest, UpdateCartItemRequest } from '@/types';
import apiClient from '@/lib/api-client';
import { useAuth } from './AuthContext';
import { AxiosError } from 'axios';

interface CartContextType {
  cart: Cart | null;
  loading: boolean;
  fetchCart: () => Promise<void>;
  addToCart: (productId: number, quantity: number) => Promise<void>;
  updateCartItem: (itemId: number, quantity: number) => Promise<void>;
  removeFromCart: (itemId: number) => Promise<void>;
  clearCart: () => Promise<void>;
  cartItemCount: number;
}

const CartContext = createContext<CartContextType | undefined>(undefined);

export const CartProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [cart, setCart] = useState<Cart | null>(null);
  const [loading, setLoading] = useState(false);
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    if (isAuthenticated) {
      fetchCart();
    }
  }, [isAuthenticated]);

  const fetchCart = async () => {
    if (!isAuthenticated) return;
    
    try {
      setLoading(true);
      const response = await apiClient.get<Cart>('/cart');
      setCart(response.data);
    } catch (error) {
      console.error('Failed to fetch cart:', error);
    } finally {
      setLoading(false);
    }
  };

  const addToCart = async (productId: number, quantity: number) => {
    try {
      const request: AddToCartRequest = { productId, quantity };
      await apiClient.post('/cart/items', request);
      await fetchCart();
    } catch (error) {
      const axiosError = error as AxiosError<{ message: string }>;
      throw new Error(axiosError.response?.data?.message || 'Failed to add item to cart');
    }
  };

  const updateCartItem = async (itemId: number, quantity: number) => {
    try {
      const request: UpdateCartItemRequest = { quantity };
      await apiClient.put(`/cart/items/${itemId}`, request);
      await fetchCart();
    } catch (error) {
      const axiosError = error as AxiosError<{ message: string }>;
      throw new Error(axiosError.response?.data?.message || 'Failed to update cart item');
    }
  };

  const removeFromCart = async (itemId: number) => {
    try {
      await apiClient.delete(`/cart/items/${itemId}`);
      await fetchCart();
    } catch (error) {
      const axiosError = error as AxiosError<{ message: string }>;
      throw new Error(axiosError.response?.data?.message || 'Failed to remove item from cart');
    }
  };

  const clearCart = async () => {
    try {
      await apiClient.delete('/cart');
      await fetchCart();
    } catch (error) {
      console.error('Failed to clear cart:', error);
    }
  };

  const cartItemCount = cart?.items.reduce((total, item) => total + item.quantity, 0) || 0;

  const value: CartContextType = {
    cart,
    loading,
    fetchCart,
    addToCart,
    updateCartItem,
    removeFromCart,
    clearCart,
    cartItemCount,
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
};

export const useCart = (): CartContextType => {
  const context = useContext(CartContext);
  if (context === undefined) {
    throw new Error('useCart must be used within a CartProvider');
  }
  return context;
};
