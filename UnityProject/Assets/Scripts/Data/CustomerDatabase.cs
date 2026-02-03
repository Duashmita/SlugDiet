using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UndercoverBarber.Data
{
    [CreateAssetMenu(fileName = "CustomerDatabase", menuName = "Undercover Barber/Customer Database")]
    public class CustomerDatabase : ScriptableObject
    {
        [SerializeField] private CustomerData[] allCustomers;

        public Customer[] GenerateCustomerLineup(int count)
        {
            if (allCustomers == null || allCustomers.Length == 0)
            {
                Debug.LogError("No customers in database!");
                return new Customer[0];
            }

            // Shuffle and pick customers
            List<CustomerData> shuffled = allCustomers.OrderBy(x => Random.value).ToList();
            int actualCount = Mathf.Min(count, shuffled.Count);

            Customer[] lineup = new Customer[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                lineup[i] = new Customer(shuffled[i]);
            }

            return lineup;
        }

        public CustomerData GetCustomerByName(string name)
        {
            foreach (var customer in allCustomers)
            {
                if (customer.customerName == name)
                    return customer;
            }
            return null;
        }

        public int Count => allCustomers?.Length ?? 0;
    }
}
