﻿using System;

namespace Axvemi.Inventories;

/// <summary>
/// Slot of the inventory. Contains a T, or nothing (null)
/// </summary>
public class InventorySlot<T> where T : IInventoryItem
{
	public event Action<InventorySlot<T>> OnSlotContentUpdated;

	public Inventory<T> Inventory { get; set; }
	public T Item { get; set; }

	private int _amount;

	public InventorySlot()
	{
	}

	public InventorySlot(Inventory<T> inventory)
	{
		Inventory = inventory;
	}

	public int Amount
	{
		get => _amount;
		set
		{
			if (Item == null)
			{
				throw new NullReferenceException("Can not set an amount when the item stored is null");
			}

			if (value < 0)
			{
				throw new ArgumentException("The amount can not be less than 0");
			}

			if (!Item.IsInfiniteStack() && value > Item.GetMaxStackAmount())
			{
				throw new ArgumentException("Trying to store an amount bigger than the stack size");
			}

			_amount = value;

			if (_amount == 0)
			{
				Clear();
				return;
			}

			OnSlotContentUpdated?.Invoke(this);
		}
	}

	/// <summary>
	/// If they are the same object, move all the possible amount from "originSlot" to "targetSlot".
	/// If they are two different slots, swap the contents. Transfer amount will not be used
	/// </summary>
	/// <param name="originSlot">Origin slot</param>
	/// <param name="targetSlot">Target slot</param>
	/// <param name="transferAmount">Amount to transfer</param>
	public static void MoveBetweenSlots(InventorySlot<T> originSlot, InventorySlot<T> targetSlot, int transferAmount = 1)
	{
		if (originSlot == null)
		{
			throw new ArgumentNullException(nameof(originSlot), "Can't transfer from a null slot");
		}

		if (targetSlot == null)
		{
			throw new ArgumentNullException(nameof(targetSlot), "Can't transfer to a null slot");
		}

		if (transferAmount > originSlot._amount)
		{
			throw new ArgumentException("You can't transfer an amount bigger than the one existing in the origin slot");
		}

		//Move all POSSIBLE amount from origin to target
		if (targetSlot.Item == null || originSlot.Item.IsSameItem(targetSlot.Item))
		{
			//While there is remaining content in the origin, and remaining (or infinite) space on target
			while (transferAmount > 0 && (targetSlot._amount < originSlot.Item.GetMaxStackAmount() || originSlot.Item.IsInfiniteStack()))
			{
				targetSlot.StoreItem(originSlot.Item);
				originSlot.RemoveItem();
				transferAmount--;
			}
		}
		//Swap slots
		else
		{
			(originSlot._amount, targetSlot._amount) = (targetSlot._amount, originSlot._amount);
			(originSlot.Item, targetSlot.Item) = (targetSlot.Item, originSlot.Item);
		}

		originSlot.OnSlotContentUpdated?.Invoke(originSlot);
		targetSlot.OnSlotContentUpdated?.Invoke(targetSlot);
	}

	/// <summary>
	/// If they are the same object, move all the possible amount from "this slot" to "targetSlot".
	/// If they are two different slots, swap the contents. Transfer amount will not be used
	/// </summary>
	/// <param name="targetSlot">Target slot</param>
	/// <param name="transferAmount">Amount to transfer</param>
	public void MoveBetweenSlots(InventorySlot<T> targetSlot, int transferAmount = 1)
	{
		MoveBetweenSlots(this, targetSlot, transferAmount);
	}

	/// <summary>
	/// Adds the item to this slot
	/// </summary>
	/// <exception cref="FailedToStoreException">If its not the correct Item to add this exception gets raised</exception>
	public void StoreItem(T item, int amount = 1)
	{
		if (item == null)
		{
			throw new ArgumentException("Can't store a null item! Call Clear() if it's your intention");
		}

		if (amount <= 0)
		{
			throw new ArgumentException("Amount can't be less or equal to 0!");
		}

		if (amount + Amount > item.GetMaxStackAmount() && !item.IsInfiniteStack())
		{
			throw new ArgumentException("Can't store an amount bigger than the stack size!");
		}

		if (Item != null && !Item.IsSameItem(item))
		{
			throw new FailedToStoreException("You are trying to store a different item on an occupied slot!");
		}

		Item = item;
		Amount += amount;
		OnSlotContentUpdated?.Invoke(this);
	}

	/// <summary>
	/// Removes "amount" amount of items in this slot.
	/// </summary>
	/// <param name="amount">Amount to remove. Can't be larger than the current slot amount</param>
	public void RemoveItem(int amount = 1)
	{
		if (amount <= 0 || amount > _amount)
		{
			throw new ArgumentException("Amount must be larger than 0, and less or equal to the current amount stored");
		}

		_amount -= amount;
		if (_amount == 0)
		{
			Clear();
			return;
		}

		OnSlotContentUpdated?.Invoke(this);
	}

	/// <summary>
	/// Clears the content of the slot
	/// </summary>
	public void Clear()
	{
		Item = default;
		_amount = 0;
		OnSlotContentUpdated?.Invoke(this);
	}

	public override string ToString()
	{
		return $"Item: {Item}; Amount: {Amount}";
	}
}